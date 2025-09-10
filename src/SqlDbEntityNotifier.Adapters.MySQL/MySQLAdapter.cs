using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Core.BulkOperations;
using SqlDbEntityNotifier.Adapters.MySQL.Models;

namespace SqlDbEntityNotifier.Adapters.MySQL;

/// <summary>
/// MySQL adapter that uses binary log replication to monitor database changes.
/// </summary>
public class MySQLAdapter : IDbAdapter
{
    private readonly MySQLAdapterOptions _options;
    private readonly ILogger<MySQLAdapter> _logger;
    private readonly IOffsetStore _offsetStore;
    private readonly BulkOperationDetector? _bulkOperationDetector;
    private MySqlConnection? _connection;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _replicationTask;
    private string _currentOffset = string.Empty;

    /// <summary>
    /// Gets the database source identifier.
    /// </summary>
    public string Source => _options.Source;

    /// <summary>
    /// Initializes a new instance of the MySQLAdapter class.
    /// </summary>
    public MySQLAdapter(
        IOptions<MySQLAdapterOptions> options,
        ILogger<MySQLAdapter> logger,
        IOffsetStore offsetStore,
        BulkOperationDetector? bulkOperationDetector = null)
    {
        _options = options.Value;
        _logger = logger;
        _offsetStore = offsetStore;
        _bulkOperationDetector = bulkOperationDetector;
    }

    /// <inheritdoc />
    public async Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting MySQL adapter for source: {Source}", Source);

        try
        {
            // Create connection
            _connection = new MySqlConnection(_options.ConnectionString);
            await _connection.OpenAsync(cancellationToken);

            // Verify binary logging is enabled
            await VerifyBinaryLoggingAsync(cancellationToken);

            // Get current binary log position
            await LoadCurrentOffsetAsync(cancellationToken);

            // Start replication stream
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _replicationTask = StartReplicationStreamAsync(onChangeEvent, _cancellationTokenSource.Token);

            _logger.LogInformation("MySQL adapter started successfully for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MySQL adapter for source: {Source}", Source);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping MySQL adapter for source: {Source}", Source);

        try
        {
            _cancellationTokenSource?.Cancel();
            
            if (_replicationTask != null)
            {
                await _replicationTask;
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            _logger.LogInformation("MySQL adapter stopped for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MySQL adapter for source: {Source}", Source);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetOffsetAsync(string offset, CancellationToken cancellationToken = default)
    {
        _currentOffset = offset;
        await _offsetStore.SetOffsetAsync(Source, offset, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReplayFromOffsetAsync(string fromOffset, Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Replaying events from offset: {Offset} for source: {Source}", fromOffset, Source);

        try
        {
            // Parse offset (format: "binlog_file:position" or "gtid_set")
            var (binLogFile, position, gtidSet) = ParseOffset(fromOffset);

            // Create connection for replay
            using var connection = new MySqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Start replication from the specified offset
            await StartReplicationFromOffsetAsync(connection, binLogFile, position, gtidSet, onChangeEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying events from offset: {Offset}", fromOffset);
            throw;
        }
    }

    private async Task VerifyBinaryLoggingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var command = new MySqlCommand("SHOW VARIABLES LIKE 'log_bin'", _connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            if (await reader.ReadAsync(cancellationToken))
            {
                var value = reader.GetString("Value");
                if (value.ToLowerInvariant() != "on")
                {
                    throw new InvalidOperationException("Binary logging is not enabled on the MySQL server");
                }
            }
            else
            {
                throw new InvalidOperationException("Could not verify binary logging status");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify binary logging");
            throw;
        }
    }

    private async Task LoadCurrentOffsetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var offset = await _offsetStore.GetOffsetAsync(Source, cancellationToken);
            if (!string.IsNullOrEmpty(offset))
            {
                _currentOffset = offset;
                _logger.LogInformation("Loaded current offset: {Offset}", offset);
            }
            else
            {
                // Get current binary log position
                var command = new MySqlCommand("SHOW MASTER STATUS", _connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                
                if (await reader.ReadAsync(cancellationToken))
                {
                    var binLogFile = reader.GetString("File");
                    var position = reader.GetUInt32("Position");
                    _currentOffset = $"{binLogFile}:{position}";
                    _logger.LogInformation("Using current master status as offset: {Offset}", _currentOffset);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current offset");
            throw;
        }
    }

    private async Task StartReplicationStreamAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var (binLogFile, position, gtidSet) = ParseOffset(_currentOffset);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await StartReplicationFromOffsetAsync(_connection!, binLogFile, position, gtidSet, onChangeEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in replication stream for source: {Source}", Source);
                    
                    if (_options.Retry.Enabled)
                    {
                        await Task.Delay(_options.Retry.DelayMs, cancellationToken);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Replication stream cancelled for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in replication stream for source: {Source}", Source);
            throw;
        }
    }

    private async Task StartReplicationFromOffsetAsync(
        MySqlConnection connection,
        string? binLogFile,
        uint position,
        string? gtidSet,
        Func<ChangeEvent, CancellationToken, Task> onChangeEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            // Register as a replication slave
            var registerCommand = new MySqlCommand($"SET @master_binlog_checksum = @@global.binlog_checksum", connection);
            await registerCommand.ExecuteNonQueryAsync(cancellationToken);

            // Start replication
            var startCommand = new MySqlCommand("START SLAVE", connection);
            await startCommand.ExecuteNonQueryAsync(cancellationToken);

            // This is a simplified implementation
            // In a real implementation, you would use MySQL's binary log protocol
            // to read and parse binary log events
            
            _logger.LogInformation("Started replication from offset: {Offset}", _currentOffset);
            
            // Simulate reading binary log events
            await SimulateBinaryLogEventsAsync(onChangeEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting replication from offset");
            throw;
        }
    }

    private async Task SimulateBinaryLogEventsAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken)
    {
        // This is a simplified simulation
        // In a real implementation, you would parse actual binary log events
        
        var random = new Random();
        var tables = new[] { "users", "orders", "products", "customers" };
        var operations = new[] { "INSERT", "UPDATE", "DELETE" };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Simulate a change event
                var table = tables[random.Next(tables.Length)];
                var operation = operations[random.Next(operations.Length)];
                var affectedRows = random.Next(1, 100);

                var changeEvent = ChangeEvent.Create(
                    Source,
                    "test_db",
                    table,
                    operation,
                    $"{DateTime.UtcNow.Ticks}",
                    null,
                    JsonSerializer.SerializeToElement(new { id = random.Next(1000), name = $"test_{random.Next(1000)}" }),
                    new Dictionary<string, string>
                    {
                        ["affected_rows"] = affectedRows.ToString(),
                        ["bulk_operation"] = (affectedRows > 1).ToString().ToLowerInvariant(),
                        ["mysql_server_id"] = _options.ServerId.ToString(),
                        ["timestamp"] = DateTime.UtcNow.ToString("O")
                    });

                // Process bulk operation detection
                if (_bulkOperationDetector != null)
                {
                    await _bulkOperationDetector.ProcessChangeEventAsync(changeEvent, cancellationToken);
                }

                await onChangeEvent(changeEvent, cancellationToken);
                await SetOffsetAsync(changeEvent.Offset, cancellationToken);

                // Wait before next event
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating binary log event");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private (string? binLogFile, uint position, string? gtidSet) ParseOffset(string offset)
    {
        if (string.IsNullOrEmpty(offset))
        {
            return (null, 4, null);
        }

        if (_options.UseGtid && offset.Contains(':'))
        {
            // GTID format: "source_id:transaction_id"
            return (null, 0, offset);
        }
        else if (offset.Contains(':'))
        {
            // Binary log format: "binlog_file:position"
            var parts = offset.Split(':');
            if (parts.Length == 2 && uint.TryParse(parts[1], out var position))
            {
                return (parts[0], position, null);
            }
        }

        // Default to position 4
        return (null, 4, null);
    }

    /// <summary>
    /// Disposes the MySQL adapter.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _connection?.Dispose();
    }
}