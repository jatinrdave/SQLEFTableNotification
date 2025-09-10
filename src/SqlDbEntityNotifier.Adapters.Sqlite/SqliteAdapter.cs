using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Core.BulkOperations;
using SqlDbEntityNotifier.Adapters.Sqlite.Models;

namespace SqlDbEntityNotifier.Adapters.Sqlite;

/// <summary>
/// SQLite adapter that uses a change log table and triggers to monitor database changes.
/// </summary>
public class SqliteAdapter : IDbAdapter
{
    private readonly SqliteAdapterOptions _options;
    private readonly ILogger<SqliteAdapter> _logger;
    private readonly IOffsetStore _offsetStore;
    private readonly BulkOperationDetector? _bulkOperationDetector;
    private SqliteConnection? _connection;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask;
    private long _lastProcessedId = 0;

    /// <summary>
    /// Gets the database source identifier.
    /// </summary>
    public string Source => _options.Source;

    /// <summary>
    /// Initializes a new instance of the SqliteAdapter class.
    /// </summary>
    public SqliteAdapter(
        IOptions<SqliteAdapterOptions> options,
        ILogger<SqliteAdapter> logger,
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
        _logger.LogInformation("Starting SQLite adapter for source: {Source}", Source);

        try
        {
            // Create connection
            var connectionString = !string.IsNullOrEmpty(_options.ConnectionString) 
                ? _options.ConnectionString 
                : $"Data Source={_options.FilePath}";
            
            _connection = new SqliteConnection(connectionString);
            await _connection.OpenAsync(cancellationToken);

            // Initialize the change log table and triggers if needed
            if (_options.AutoCreateChangeLog)
            {
                await InitializeChangeLogAsync(cancellationToken);
            }

            // Load the last processed offset
            var lastOffset = await _offsetStore.GetOffsetAsync(Source, cancellationToken);
            if (!string.IsNullOrEmpty(lastOffset) && long.TryParse(lastOffset, out var lastId))
            {
                _lastProcessedId = lastId;
            }

            // Start polling for changes
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pollingTask = StartPollingAsync(onChangeEvent, _cancellationTokenSource.Token);

            _logger.LogInformation("SQLite adapter started successfully for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SQLite adapter for source: {Source}", Source);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping SQLite adapter for source: {Source}", Source);

        try
        {
            // Cancel the polling task
            _cancellationTokenSource?.Cancel();

            // Wait for the polling task to complete
            if (_pollingTask != null)
            {
                await _pollingTask;
            }

            // Close the connection
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            _logger.LogInformation("SQLite adapter stopped for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping SQLite adapter for source: {Source}", Source);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetCurrentOffsetAsync(CancellationToken cancellationToken = default)
    {
        return await _offsetStore.GetOffsetAsync(Source, cancellationToken) ?? _lastProcessedId.ToString();
    }

    /// <inheritdoc />
    public async Task SetOffsetAsync(string offset, CancellationToken cancellationToken = default)
    {
        if (long.TryParse(offset, out var id))
        {
            _lastProcessedId = id;
        }
        await _offsetStore.SetOffsetAsync(Source, offset, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReplayFromOffsetAsync(string fromOffset, Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Replaying events from offset: {Offset} for source: {Source}", fromOffset, Source);

        if (!long.TryParse(fromOffset, out var fromId))
        {
            throw new ArgumentException("Invalid offset format for SQLite adapter", nameof(fromOffset));
        }

        var query = $@"
            SELECT id, table_name, operation, old_data, new_data, timestamp, schema_name
            FROM {_options.ChangeTable}
            WHERE id > @fromId
            ORDER BY id";

        using var command = new SqliteCommand(query, _connection);
        command.Parameters.AddWithValue("@fromId", fromId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var changeEvent = CreateChangeEventFromReader(reader);
            if (changeEvent != null)
            {
                await onChangeEvent(changeEvent, cancellationToken);
            }
        }
    }

    private async Task InitializeChangeLogAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing change log table and triggers for source: {Source}", Source);

        // Create the change log table
        var createTableSql = $@"
            CREATE TABLE IF NOT EXISTS {_options.ChangeTable} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                table_name TEXT NOT NULL,
                schema_name TEXT NOT NULL DEFAULT 'main',
                operation TEXT NOT NULL,
                old_data TEXT,
                new_data TEXT,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        using var createTableCommand = new SqliteCommand(createTableSql, _connection);
        await createTableCommand.ExecuteNonQueryAsync(cancellationToken);

        // Create indexes for better performance
        var createIndexSql = $@"
            CREATE INDEX IF NOT EXISTS idx_{_options.ChangeTable}_id ON {_options.ChangeTable}(id);
            CREATE INDEX IF NOT EXISTS idx_{_options.ChangeTable}_table ON {_options.ChangeTable}(table_name);
            CREATE INDEX IF NOT EXISTS idx_{_options.ChangeTable}_timestamp ON {_options.ChangeTable}(timestamp)";

        using var createIndexCommand = new SqliteCommand(createIndexSql, _connection);
        await createIndexCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Change log table and indexes created successfully");
    }

    private async Task StartPollingAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PollForChangesAsync(onChangeEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling for changes in source: {Source}", Source);
                }

                await Task.Delay(_options.PollIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Polling cancelled for source: {Source}", Source);
        }
    }

    private async Task PollForChangesAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken)
    {
        var query = $@"
            SELECT id, table_name, operation, old_data, new_data, timestamp, schema_name
            FROM {_options.ChangeTable}
            WHERE id > @lastId
            ORDER BY id
            LIMIT @batchSize";

        using var command = new SqliteCommand(query, _connection);
        command.Parameters.AddWithValue("@lastId", _lastProcessedId);
        command.Parameters.AddWithValue("@batchSize", _options.MaxBatchSize);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var processedCount = 0;

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
                _lastProcessedId = reader.GetInt64("id");
                await SetOffsetAsync(_lastProcessedId.ToString(), cancellationToken);
                processedCount++;
            }
        }

        if (processedCount > 0)
        {
            _logger.LogDebug("Processed {Count} change events for source: {Source}", processedCount, Source);
        }
    }

    private ChangeEvent? CreateChangeEventFromReader(SqliteDataReader reader)
    {
        try
        {
            var id = reader.GetInt64("id");
            var tableName = reader.GetString("table_name");
            var schemaName = reader.GetString("schema_name");
            var operation = reader.GetString("operation");
            var timestamp = reader.GetDateTime("timestamp");

            JsonElement? before = null;
            JsonElement? after = null;

            if (_options.IncludeBefore && !reader.IsDBNull("old_data"))
            {
                var oldDataJson = reader.GetString("old_data");
                if (!string.IsNullOrEmpty(oldDataJson))
                {
                    before = JsonDocument.Parse(oldDataJson).RootElement;
                }
            }

            if (_options.IncludeAfter && !reader.IsDBNull("new_data"))
            {
                var newDataJson = reader.GetString("new_data");
                if (!string.IsNullOrEmpty(newDataJson))
                {
                    after = JsonDocument.Parse(newDataJson).RootElement;
                }
            }

            // For SQLite, we'll detect bulk operations based on timing patterns
            // This is a simplified approach - in production, you might want to track
            // transaction boundaries or use more sophisticated detection
            var metadata = new Dictionary<string, string>
            {
                ["timestamp"] = timestamp.ToString("O"),
                ["change_id"] = id.ToString(),
                ["affected_rows"] = "1" // SQLite triggers fire per row
            };

            return ChangeEvent.Create(
                Source,
                schemaName,
                tableName,
                operation.ToUpperInvariant(),
                id.ToString(),
                before,
                after,
                metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating change event from reader");
            return null;
        }
    }
}