using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.Schema;

/// <summary>
/// Detects and monitors database schema changes.
/// </summary>
public class SchemaChangeDetector
{
    private readonly ILogger<SchemaChangeDetector> _logger;
    private readonly SchemaChangeDetectorOptions _options;
    private readonly IDbAdapter _dbAdapter;
    private readonly IChangePublisher _changePublisher;
    private readonly Dictionary<string, TableSchema> _knownSchemas;
    private readonly Timer _detectionTimer;

    /// <summary>
    /// Initializes a new instance of the SchemaChangeDetector class.
    /// </summary>
    public SchemaChangeDetector(
        ILogger<SchemaChangeDetector> logger,
        IOptions<SchemaChangeDetectorOptions> options,
        IDbAdapter dbAdapter,
        IChangePublisher changePublisher)
    {
        _logger = logger;
        _options = options.Value;
        _dbAdapter = dbAdapter;
        _changePublisher = changePublisher;
        _knownSchemas = new Dictionary<string, TableSchema>();

        // Start periodic schema detection
        _detectionTimer = new Timer(DetectSchemaChangesAsync, null, 
            TimeSpan.Zero, TimeSpan.FromSeconds(_options.DetectionIntervalSeconds));
    }

    /// <summary>
    /// Starts monitoring for schema changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the monitoring operation.</returns>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting schema change monitoring");

        try
        {
            // Load initial schemas
            await LoadInitialSchemasAsync(cancellationToken);
            
            _logger.LogInformation("Schema change monitoring started. Monitoring {TableCount} tables", _knownSchemas.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting schema change monitoring");
            throw;
        }
    }

    /// <summary>
    /// Stops monitoring for schema changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the stop operation.</returns>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping schema change monitoring");

        try
        {
            _detectionTimer?.Dispose();
            _logger.LogInformation("Schema change monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping schema change monitoring");
            throw;
        }
    }

    /// <summary>
    /// Manually triggers schema change detection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the detection operation.</returns>
    public async Task DetectChangesAsync(CancellationToken cancellationToken = default)
    {
        await DetectSchemaChangesAsync(cancellationToken);
    }

    private async void DetectSchemaChangesAsync(object? state)
    {
        try
        {
            await DetectSchemaChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in schema change detection timer");
        }
    }

    private async Task DetectSchemaChangesAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            var currentSchemas = await GetCurrentSchemasAsync(cancellationToken);
            var changes = CompareSchemas(_knownSchemas, currentSchemas);

            if (changes.Any())
            {
                _logger.LogInformation("Detected {ChangeCount} schema changes", changes.Count);

                foreach (var change in changes)
                {
                    await PublishSchemaChangeEventAsync(change, cancellationToken);
                }

                // Update known schemas
                _knownSchemas.Clear();
                foreach (var schema in currentSchemas)
                {
                    _knownSchemas[schema.Key] = schema.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting schema changes");
        }
    }

    private async Task LoadInitialSchemasAsync(CancellationToken cancellationToken)
    {
        var currentSchemas = await GetCurrentSchemasAsync(cancellationToken);
        
        foreach (var schema in currentSchemas)
        {
            _knownSchemas[schema.Key] = schema.Value;
        }

        _logger.LogDebug("Loaded initial schemas for {TableCount} tables", _knownSchemas.Count);
    }

    private async Task<Dictionary<string, TableSchema>> GetCurrentSchemasAsync(CancellationToken cancellationToken)
    {
        // This is a simplified implementation - in a real scenario, you would need to
        // implement schema reading based on your database adapter
        var schemas = new Dictionary<string, TableSchema>();

        try
        {
            // For now, return empty schemas - this would be implemented based on the database type
            // SQLite: PRAGMA table_info(table_name)
            // PostgreSQL: information_schema.columns
            // SQL Server: sys.columns, sys.tables
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading current schemas");
        }

        return schemas;
    }

    private IList<SchemaChange> CompareSchemas(
        Dictionary<string, TableSchema> knownSchemas,
        Dictionary<string, TableSchema> currentSchemas)
    {
        var changes = new List<SchemaChange>();

        // Check for new tables
        foreach (var currentSchema in currentSchemas)
        {
            if (!knownSchemas.ContainsKey(currentSchema.Key))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.TableAdded,
                    TableName = currentSchema.Value.Name,
                    SchemaName = currentSchema.Value.Schema,
                    Description = $"Table {currentSchema.Value.Name} was added",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Check for removed tables
        foreach (var knownSchema in knownSchemas)
        {
            if (!currentSchemas.ContainsKey(knownSchema.Key))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.TableRemoved,
                    TableName = knownSchema.Value.Name,
                    SchemaName = knownSchema.Value.Schema,
                    Description = $"Table {knownSchema.Value.Name} was removed",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Check for modified tables
        foreach (var currentSchema in currentSchemas)
        {
            if (knownSchemas.TryGetValue(currentSchema.Key, out var knownSchema))
            {
                var tableChanges = CompareTableSchemas(knownSchema, currentSchema.Value);
                changes.AddRange(tableChanges);
            }
        }

        return changes;
    }

    private IList<SchemaChange> CompareTableSchemas(TableSchema knownSchema, TableSchema currentSchema)
    {
        var changes = new List<SchemaChange>();

        // Check for column changes
        var knownColumns = knownSchema.Columns.ToDictionary(c => c.Name, c => c);
        var currentColumns = currentSchema.Columns.ToDictionary(c => c.Name, c => c);

        // New columns
        foreach (var currentColumn in currentColumns)
        {
            if (!knownColumns.ContainsKey(currentColumn.Key))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.ColumnAdded,
                    TableName = currentSchema.Name,
                    SchemaName = currentSchema.Schema,
                    ColumnName = currentColumn.Value.Name,
                    Description = $"Column {currentColumn.Value.Name} was added to table {currentSchema.Name}",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        ColumnType = currentColumn.Value.DataType,
                        IsNullable = currentColumn.Value.IsNullable,
                        DefaultValue = currentColumn.Value.DefaultValue
                    })
                });
            }
        }

        // Removed columns
        foreach (var knownColumn in knownColumns)
        {
            if (!currentColumns.ContainsKey(knownColumn.Key))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.ColumnRemoved,
                    TableName = currentSchema.Name,
                    SchemaName = currentSchema.Schema,
                    ColumnName = knownColumn.Value.Name,
                    Description = $"Column {knownColumn.Value.Name} was removed from table {currentSchema.Name}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Modified columns
        foreach (var currentColumn in currentColumns)
        {
            if (knownColumns.TryGetValue(currentColumn.Key, out var knownColumn))
            {
                var columnChanges = CompareColumnSchemas(knownColumn, currentColumn.Value, currentSchema);
                changes.AddRange(columnChanges);
            }
        }

        return changes;
    }

    private IList<SchemaChange> CompareColumnSchemas(ColumnSchema knownColumn, ColumnSchema currentColumn, TableSchema tableSchema)
    {
        var changes = new List<SchemaChange>();

        // Check data type changes
        if (knownColumn.DataType != currentColumn.DataType)
        {
            changes.Add(new SchemaChange
            {
                Type = SchemaChangeType.ColumnTypeChanged,
                TableName = tableSchema.Name,
                SchemaName = tableSchema.Schema,
                ColumnName = currentColumn.Name,
                Description = $"Column {currentColumn.Name} type changed from {knownColumn.DataType} to {currentColumn.DataType}",
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    OldType = knownColumn.DataType,
                    NewType = currentColumn.DataType
                })
            });
        }

        // Check nullable changes
        if (knownColumn.IsNullable != currentColumn.IsNullable)
        {
            changes.Add(new SchemaChange
            {
                Type = SchemaChangeType.ColumnNullableChanged,
                TableName = tableSchema.Name,
                SchemaName = tableSchema.Schema,
                ColumnName = currentColumn.Name,
                Description = $"Column {currentColumn.Name} nullable property changed from {knownColumn.IsNullable} to {currentColumn.IsNullable}",
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    OldNullable = knownColumn.IsNullable,
                    NewNullable = currentColumn.IsNullable
                })
            });
        }

        // Check default value changes
        if (knownColumn.DefaultValue != currentColumn.DefaultValue)
        {
            changes.Add(new SchemaChange
            {
                Type = SchemaChangeType.ColumnDefaultChanged,
                TableName = tableSchema.Name,
                SchemaName = tableSchema.Schema,
                ColumnName = currentColumn.Name,
                Description = $"Column {currentColumn.Name} default value changed",
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    OldDefault = knownColumn.DefaultValue,
                    NewDefault = currentColumn.DefaultValue
                })
            });
        }

        return changes;
    }

    private async Task PublishSchemaChangeEventAsync(SchemaChange change, CancellationToken cancellationToken)
    {
        try
        {
            var changeEvent = ChangeEvent.Create(
                _dbAdapter.Source,
                change.SchemaName,
                "__schema_changes__", // Special table name for schema changes
                "SCHEMA_CHANGE",
                Guid.NewGuid().ToString(),
                null,
                JsonSerializer.SerializeToElement(change),
                new Dictionary<string, string>
                {
                    ["change_type"] = change.Type.ToString(),
                    ["table_name"] = change.TableName,
                    ["column_name"] = change.ColumnName ?? "",
                    ["description"] = change.Description
                });

            await _changePublisher.PublishAsync(changeEvent, cancellationToken);

            _logger.LogInformation("Published schema change event: {ChangeType} for {TableName}", 
                change.Type, change.TableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing schema change event for {TableName}", change.TableName);
        }
    }

    /// <summary>
    /// Disposes the schema change detector.
    /// </summary>
    public void Dispose()
    {
        _detectionTimer?.Dispose();
    }
}

/// <summary>
/// Configuration options for schema change detection.
/// </summary>
public sealed class SchemaChangeDetectorOptions
{
    /// <summary>
    /// Gets or sets whether schema change detection is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the detection interval in seconds.
    /// </summary>
    public int DetectionIntervalSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets the list of tables to monitor (empty means all tables).
    /// </summary>
    public IList<string> MonitoredTables { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of tables to exclude from monitoring.
    /// </summary>
    public IList<string> ExcludedTables { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets whether to monitor column changes.
    /// </summary>
    public bool MonitorColumnChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to monitor table changes.
    /// </summary>
    public bool MonitorTableChanges { get; set; } = true;
}

/// <summary>
/// Represents a schema change.
/// </summary>
public sealed class SchemaChange
{
    /// <summary>
    /// Gets or sets the type of schema change.
    /// </summary>
    public SchemaChangeType Type { get; set; }

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column name (for column-related changes).
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the description of the change.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the change was detected.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional details about the change.
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Types of schema changes.
/// </summary>
public enum SchemaChangeType
{
    /// <summary>
    /// A table was added.
    /// </summary>
    TableAdded,

    /// <summary>
    /// A table was removed.
    /// </summary>
    TableRemoved,

    /// <summary>
    /// A column was added.
    /// </summary>
    ColumnAdded,

    /// <summary>
    /// A column was removed.
    /// </summary>
    ColumnRemoved,

    /// <summary>
    /// A column type was changed.
    /// </summary>
    ColumnTypeChanged,

    /// <summary>
    /// A column nullable property was changed.
    /// </summary>
    ColumnNullableChanged,

    /// <summary>
    /// A column default value was changed.
    /// </summary>
    ColumnDefaultChanged
}

/// <summary>
/// Represents a database table schema.
/// </summary>
public sealed class TableSchema
{
    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of columns.
    /// </summary>
    public IList<ColumnSchema> Columns { get; set; } = new List<ColumnSchema>();
}

/// <summary>
/// Represents a database column schema.
/// </summary>
public sealed class ColumnSchema
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column data type.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the column is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets the column default value.
    /// </summary>
    public string? DefaultValue { get; set; }
}