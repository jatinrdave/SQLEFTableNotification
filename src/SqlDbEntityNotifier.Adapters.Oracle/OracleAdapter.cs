using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Core.BulkOperations;
using SqlDbEntityNotifier.Adapters.Oracle.Models;

namespace SqlDbEntityNotifier.Adapters.Oracle;

/// <summary>
/// Oracle adapter that uses LogMiner to monitor database changes.
/// </summary>
public class OracleAdapter : IDbAdapter
{
    private readonly OracleAdapterOptions _options;
    private readonly ILogger<OracleAdapter> _logger;
    private readonly IOffsetStore _offsetStore;
    private readonly BulkOperationDetector? _bulkOperationDetector;
    private OracleConnection? _connection;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _logMinerTask;
    private string _currentOffset = string.Empty;

    /// <summary>
    /// Gets the database source identifier.
    /// </summary>
    public string Source => _options.Source;

    /// <summary>
    /// Initializes a new instance of the OracleAdapter class.
    /// </summary>
    public OracleAdapter(
        IOptions<OracleAdapterOptions> options,
        ILogger<OracleAdapter> logger,
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
        _logger.LogInformation("Starting Oracle adapter for source: {Source}", Source);

        try
        {
            // Create connection
            _connection = new OracleConnection(_options.ConnectionString);
            await _connection.OpenAsync(cancellationToken);

            // Verify LogMiner is available
            await VerifyLogMinerAsync(cancellationToken);

            // Get current SCN
            await LoadCurrentOffsetAsync(cancellationToken);

            // Start LogMiner stream
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _logMinerTask = StartLogMinerStreamAsync(onChangeEvent, _cancellationTokenSource.Token);

            _logger.LogInformation("Oracle adapter started successfully for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Oracle adapter for source: {Source}", Source);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Oracle adapter for source: {Source}", Source);

        try
        {
            _cancellationTokenSource?.Cancel();
            
            if (_logMinerTask != null)
            {
                await _logMinerTask;
            }

            // End LogMiner session
            await EndLogMinerSessionAsync(cancellationToken);

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            _logger.LogInformation("Oracle adapter stopped for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Oracle adapter for source: {Source}", Source);
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
            // Parse offset (SCN format)
            var scn = ParseOffset(fromOffset);

            // Create connection for replay
            using var connection = new OracleConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Start LogMiner from the specified SCN
            await StartLogMinerFromScnAsync(connection, scn, onChangeEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying events from offset: {Offset}", fromOffset);
            throw;
        }
    }

    private async Task VerifyLogMinerAsync(CancellationToken cancellationToken)
    {
        try
        {
            var command = new OracleCommand("SELECT COUNT(*) FROM V$LOGMNR_CONTENTS", _connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result == null)
            {
                throw new InvalidOperationException("LogMiner is not available or accessible");
            }

            _logger.LogInformation("LogMiner verification successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify LogMiner availability");
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
                // Get current SCN
                var command = new OracleCommand("SELECT CURRENT_SCN FROM V$DATABASE", _connection);
                var result = await command.ExecuteScalarAsync(cancellationToken);
                
                if (result != null)
                {
                    _currentOffset = result.ToString()!;
                    _logger.LogInformation("Using current SCN as offset: {Offset}", _currentOffset);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current offset");
            throw;
        }
    }

    private async Task StartLogMinerStreamAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var scn = ParseOffset(_currentOffset);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await StartLogMinerFromScnAsync(_connection!, scn, onChangeEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in LogMiner stream for source: {Source}", Source);
                    
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
            _logger.LogInformation("LogMiner stream cancelled for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LogMiner stream for source: {Source}", Source);
            throw;
        }
    }

    private async Task StartLogMinerFromScnAsync(
        OracleConnection connection,
        ulong startScn,
        Func<ChangeEvent, CancellationToken, Task> onChangeEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            // Start LogMiner session
            await StartLogMinerSessionAsync(connection, startScn, cancellationToken);

            // Query LogMiner contents
            await QueryLogMinerContentsAsync(connection, onChangeEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting LogMiner from SCN: {Scn}", startScn);
            throw;
        }
    }

    private async Task StartLogMinerSessionAsync(OracleConnection connection, ulong startScn, CancellationToken cancellationToken)
    {
        try
        {
            // Build LogMiner start command
            var startCommand = BuildLogMinerStartCommand(startScn);
            
            var command = new OracleCommand(startCommand, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Started LogMiner session from SCN: {Scn}", startScn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting LogMiner session");
            throw;
        }
    }

    private string BuildLogMinerStartCommand(ulong startScn)
    {
        var command = "BEGIN DBMS_LOGMNR.START_LOGMNR(";
        
        // Add start SCN
        command += $"STARTSCN => {startScn}";
        
        // Add dictionary type
        var dictType = _options.LogMiner.DictionaryType switch
        {
            LogMinerDictionaryType.OnlineCatalog => "DBMS_LOGMNR.DICT_FROM_ONLINE_CATALOG",
            LogMinerDictionaryType.RedoLogFiles => "DBMS_LOGMNR.DICT_FROM_REDO_LOGS",
            LogMinerDictionaryType.DictionaryFile => "DBMS_LOGMNR.DICT_FROM_DICT_FILE",
            _ => "DBMS_LOGMNR.DICT_FROM_ONLINE_CATALOG"
        };
        command += $", OPTIONS => {dictType}";
        
        // Add options
        if (_options.LogMiner.Options.HasFlag(LogMinerOptionsFlags.CommittedDataOnly))
        {
            command += " + DBMS_LOGMNR.COMMITTED_DATA_ONLY";
        }
        
        command += "); END;";
        
        return command;
    }

    private async Task QueryLogMinerContentsAsync(
        OracleConnection connection,
        Func<ChangeEvent, CancellationToken, Task> onChangeEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = @"
                SELECT 
                    SCN,
                    TIMESTAMP,
                    SEG_OWNER,
                    SEG_NAME,
                    OPERATION,
                    SQL_REDO,
                    SQL_UNDO,
                    ROW_ID,
                    SESSION_INFO
                FROM V$LOGMNR_CONTENTS 
                WHERE SEG_OWNER IS NOT NULL 
                AND SEG_NAME IS NOT NULL
                AND OPERATION IN ('INSERT', 'UPDATE', 'DELETE')
                ORDER BY SCN";

            var command = new OracleCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var changeEvent = CreateChangeEventFromReader(reader);
                if (changeEvent != null)
                {
                    // Process bulk operation detection
                    if (_bulkOperationDetector != null)
                    {
                        await _bulkOperationDetector.ProcessChangeEventAsync(changeEvent, cancellationToken);
                    }

                    await onChangeEvent(changeEvent, cancellationToken);
                    await SetOffsetAsync(changeEvent.Offset, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying LogMiner contents");
            throw;
        }
    }

    private ChangeEvent? CreateChangeEventFromReader(OracleDataReader reader)
    {
        try
        {
            var scn = reader.GetDecimal("SCN");
            var timestamp = reader.GetDateTime("TIMESTAMP");
            var schemaOwner = reader.GetString("SEG_OWNER");
            var tableName = reader.GetString("SEG_NAME");
            var operation = reader.GetString("OPERATION");
            var sqlRedo = reader.IsDBNull("SQL_REDO") ? null : reader.GetString("SQL_REDO");
            var sqlUndo = reader.IsDBNull("SQL_UNDO") ? null : reader.GetString("SQL_UNDO");
            var rowId = reader.IsDBNull("ROW_ID") ? null : reader.GetString("ROW_ID");
            var sessionInfo = reader.IsDBNull("SESSION_INFO") ? null : reader.GetString("SESSION_INFO");

            JsonElement? before = null;
            JsonElement? after = null;

            // Parse SQL_UNDO for before data
            if (_options.IncludeBefore && !string.IsNullOrEmpty(sqlUndo))
            {
                before = JsonSerializer.SerializeToElement(new { sql = sqlUndo, row_id = rowId });
            }

            // Parse SQL_REDO for after data
            if (_options.IncludeAfter && !string.IsNullOrEmpty(sqlRedo))
            {
                after = JsonSerializer.SerializeToElement(new { sql = sqlRedo, row_id = rowId });
            }

            var metadata = new Dictionary<string, string>
            {
                ["scn"] = scn.ToString(),
                ["timestamp"] = timestamp.ToString("O"),
                ["row_id"] = rowId ?? "",
                ["session_info"] = sessionInfo ?? "",
                ["affected_rows"] = "1" // LogMiner processes one row at a time
            };

            return ChangeEvent.Create(
                Source,
                schemaOwner,
                tableName,
                operation.ToUpperInvariant(),
                scn.ToString(),
                before,
                after,
                metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating change event from LogMiner reader");
            return null;
        }
    }

    private async Task EndLogMinerSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var command = new OracleCommand("BEGIN DBMS_LOGMNR.END_LOGMNR(); END;", _connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            
            _logger.LogInformation("Ended LogMiner session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending LogMiner session");
        }
    }

    private ulong ParseOffset(string offset)
    {
        if (string.IsNullOrEmpty(offset))
        {
            return 0;
        }

        if (ulong.TryParse(offset, out var scn))
        {
            return scn;
        }

        return 0;
    }

    /// <summary>
    /// Disposes the Oracle adapter.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _connection?.Dispose();
    }
}