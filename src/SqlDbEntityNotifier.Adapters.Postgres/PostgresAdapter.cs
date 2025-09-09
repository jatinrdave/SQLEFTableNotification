using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.Replication;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Adapters.Postgres.Models;

namespace SqlDbEntityNotifier.Adapters.Postgres;

/// <summary>
/// PostgreSQL adapter that uses logical replication to monitor database changes.
/// </summary>
public class PostgresAdapter : IDbAdapter
{
    private readonly PostgresAdapterOptions _options;
    private readonly ILogger<PostgresAdapter> _logger;
    private readonly IOffsetStore _offsetStore;
    private LogicalReplicationConnection? _connection;
    private PgOutputReplicationSlot? _slot;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _replicationTask;
    private string _currentOffset = string.Empty;

    /// <summary>
    /// Gets the database source identifier.
    /// </summary>
    public string Source => _options.Source;

    /// <summary>
    /// Initializes a new instance of the PostgresAdapter class.
    /// </summary>
    public PostgresAdapter(
        IOptions<PostgresAdapterOptions> options,
        ILogger<PostgresAdapter> logger,
        IOffsetStore offsetStore)
    {
        _options = options.Value;
        _logger = logger;
        _offsetStore = offsetStore;
    }

    /// <inheritdoc />
    public async Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting PostgreSQL adapter for source: {Source}", Source);

        try
        {
            // Load the last known offset
            _currentOffset = await _offsetStore.GetOffsetAsync(Source, cancellationToken) ?? string.Empty;

            // Create replication connection
            _connection = new LogicalReplicationConnection(_options.ConnectionString);
            await _connection.Open(cancellationToken);

            // Create or get the replication slot
            _slot = await GetOrCreateReplicationSlotAsync(cancellationToken);

            // Start the replication stream
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _replicationTask = StartReplicationStreamAsync(onChangeEvent, _cancellationTokenSource.Token);

            _logger.LogInformation("PostgreSQL adapter started successfully for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start PostgreSQL adapter for source: {Source}", Source);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping PostgreSQL adapter for source: {Source}", Source);

        try
        {
            // Cancel the replication task
            _cancellationTokenSource?.Cancel();

            // Wait for the replication task to complete
            if (_replicationTask != null)
            {
                await _replicationTask;
            }

            // Close the replication connection
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            _logger.LogInformation("PostgreSQL adapter stopped for source: {Source}", Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping PostgreSQL adapter for source: {Source}", Source);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetCurrentOffsetAsync(CancellationToken cancellationToken = default)
    {
        return await _offsetStore.GetOffsetAsync(Source, cancellationToken) ?? string.Empty;
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

        // This is a simplified implementation - in a real scenario, you would need to
        // implement proper replay logic based on the PostgreSQL WAL
        throw new NotImplementedException("Replay functionality requires additional implementation");
    }

    private async Task<PgOutputReplicationSlot> GetOrCreateReplicationSlotAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to get existing slot
            var existingSlots = await _connection!.GetReplicationSlots(cancellationToken);
            var existingSlot = existingSlots.FirstOrDefault(s => s.SlotName == _options.SlotName);

            if (existingSlot != null)
            {
                _logger.LogInformation("Using existing replication slot: {SlotName}", _options.SlotName);
                return new PgOutputReplicationSlot(_options.SlotName);
            }

            // Create new slot
            _logger.LogInformation("Creating new replication slot: {SlotName}", _options.SlotName);
            return await _connection!.CreatePgOutputReplicationSlot(_options.SlotName, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create replication slot: {SlotName}", _options.SlotName);
            throw;
        }
    }

    private async Task StartReplicationStreamAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var stream = _connection!.StartReplication(_slot!, cancellationToken: cancellationToken);

            await foreach (var message in stream.WithCancellation(cancellationToken))
            {
                try
                {
                    var changeEvent = await ProcessReplicationMessageAsync(message, cancellationToken);
                    if (changeEvent != null)
                    {
                        await onChangeEvent(changeEvent, cancellationToken);
                        await SetOffsetAsync(changeEvent.Offset, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing replication message for source: {Source}", Source);
                    // Continue processing other messages
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

    private async Task<ChangeEvent?> ProcessReplicationMessageAsync(PgOutputReplicationMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Parse the WAL message based on the plugin type
            if (_options.Plugin == "wal2json")
            {
                return await ProcessWal2JsonMessageAsync(message, cancellationToken);
            }
            else if (_options.Plugin == "pgoutput")
            {
                return await ProcessPgOutputMessageAsync(message, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Unsupported replication plugin: {Plugin}", _options.Plugin);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing replication message");
            return null;
        }
    }

    private async Task<ChangeEvent?> ProcessWal2JsonMessageAsync(PgOutputReplicationMessage message, CancellationToken cancellationToken)
    {
        // wal2json plugin provides JSON-formatted messages
        var jsonData = message.Data;
        var jsonDoc = JsonDocument.Parse(jsonData);

        if (!jsonDoc.RootElement.TryGetProperty("change", out var changeArray) || changeArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var change in changeArray.EnumerateArray())
        {
            if (!change.TryGetProperty("kind", out var kindElement))
                continue;

            var kind = kindElement.GetString();
            if (kind != "insert" && kind != "update" && kind != "delete")
                continue;

            var operation = kind.ToUpperInvariant();
            var schema = change.GetProperty("schema").GetString() ?? "public";
            var table = change.GetProperty("table").GetString() ?? "";
            var timestamp = change.TryGetProperty("timestamp", out var ts) ? ts.GetString() : DateTime.UtcNow.ToString("O");

            JsonElement? before = null;
            JsonElement? after = null;

            if (change.TryGetProperty("oldkeys", out var oldKeys) && _options.IncludeBefore)
            {
                before = oldKeys;
            }

            if (change.TryGetProperty("columnnames", out var columnNames) && change.TryGetProperty("columnvalues", out var columnValues) && _options.IncludeAfter)
            {
                var afterDict = new Dictionary<string, object>();
                var names = columnNames.EnumerateArray().ToArray();
                var values = columnValues.EnumerateArray().ToArray();

                for (int i = 0; i < Math.Min(names.Length, values.Length); i++)
                {
                    afterDict[names[i].GetString() ?? ""] = values[i];
                }

                after = JsonSerializer.SerializeToElement(afterDict);
            }

            var offset = $"{message.WalStart:X8}/{message.WalEnd:X8}";

            return ChangeEvent.Create(
                Source,
                schema,
                table,
                operation,
                offset,
                before,
                after,
                new Dictionary<string, string>
                {
                    ["wal_start"] = message.WalStart.ToString(),
                    ["wal_end"] = message.WalEnd.ToString(),
                    ["timestamp"] = timestamp
                });
        }

        return null;
    }

    private async Task<ChangeEvent?> ProcessPgOutputMessageAsync(PgOutputReplicationMessage message, CancellationToken cancellationToken)
    {
        // pgoutput plugin provides binary messages that need to be parsed
        // This is a simplified implementation - real implementation would need proper binary parsing
        _logger.LogWarning("pgoutput plugin processing not fully implemented");
        return null;
    }
}