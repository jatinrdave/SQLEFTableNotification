using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.CodeGen.Interfaces;
using SqlDbEntityNotifier.CodeGen.Models;

namespace SqlDbEntityNotifier.Core.Schema;

/// <summary>
/// Enhanced schema change detector with real database schema reading capabilities.
/// </summary>
public class EnhancedSchemaChangeDetector
{
    private readonly ILogger<EnhancedSchemaChangeDetector> _logger;
    private readonly SchemaChangeDetectorOptions _options;
    private readonly IChangePublisher _changePublisher;
    private readonly ISchemaReader _schemaReader;
    private readonly Dictionary<string, DatabaseSchema> _lastKnownSchemas;
    private readonly Dictionary<string, DateTime> _lastDetectionTimes;
    private readonly Timer _detectionTimer;
    private readonly SemaphoreSlim _detectionSemaphore;

    /// <summary>
    /// Initializes a new instance of the EnhancedSchemaChangeDetector class.
    /// </summary>
    public EnhancedSchemaChangeDetector(
        ILogger<EnhancedSchemaChangeDetector> logger,
        IOptions<SchemaChangeDetectorOptions> options,
        IChangePublisher changePublisher,
        ISchemaReader schemaReader)
    {
        _logger = logger;
        _options = options.Value;
        _changePublisher = changePublisher;
        _schemaReader = schemaReader;
        _lastKnownSchemas = new Dictionary<string, DatabaseSchema>();
        _lastDetectionTimes = new Dictionary<string, DateTime>();
        _detectionSemaphore = new SemaphoreSlim(1, 1);

        // Start detection timer
        _detectionTimer = new Timer(DetectSchemaChanges, null, TimeSpan.Zero, TimeSpan.FromSeconds(_options.DetectionIntervalSeconds));
    }

    /// <summary>
    /// Detects schema changes for a specific database.
    /// </summary>
    /// <param name="source">The database source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the detection operation.</returns>
    public async Task DetectSchemaChangesAsync(string source, CancellationToken cancellationToken = default)
    {
        try
        {
            await _detectionSemaphore.WaitAsync(cancellationToken);

            _logger.LogInformation("Detecting schema changes for source: {Source}", source);

            // Check if enough time has passed since last detection
            if (_lastDetectionTimes.TryGetValue(source, out var lastDetection) &&
                DateTime.UtcNow - lastDetection < TimeSpan.FromSeconds(_options.DetectionIntervalSeconds))
            {
                _logger.LogDebug("Skipping schema detection for source: {Source} - too soon since last detection", source);
                return;
            }

            // Get current schema from database
            var currentSchema = await _schemaReader.ReadSchemaAsync(_options.SchemaName, cancellationToken);
            currentSchema.Source = source;

            var lastKnownSchema = _lastKnownSchemas.GetValueOrDefault(source);

            if (lastKnownSchema != null)
            {
                var changes = CompareSchemas(lastKnownSchema, currentSchema);
                if (changes.Any())
                {
                    await PublishSchemaChangeEventAsync(source, changes, cancellationToken);
                    _logger.LogInformation("Detected {ChangeCount} schema changes for source: {Source}", changes.Count, source);
                }
                else
                {
                    _logger.LogDebug("No schema changes detected for source: {Source}", source);
                }
            }
            else
            {
                _logger.LogInformation("Initial schema snapshot captured for source: {Source}", source);
            }

            _lastKnownSchemas[source] = currentSchema;
            _lastDetectionTimes[source] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting schema changes for source: {Source}", source);
        }
        finally
        {
            _detectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current schema for a specific source.
    /// </summary>
    /// <param name="source">The database source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current database schema.</returns>
    public async Task<DatabaseSchema?> GetCurrentSchemaAsync(string source, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _schemaReader.ReadSchemaAsync(_options.SchemaName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current schema for source: {Source}", source);
            return null;
        }
    }

    /// <summary>
    /// Gets the last known schema for a specific source.
    /// </summary>
    /// <param name="source">The database source.</param>
    /// <returns>The last known database schema or null if not available.</returns>
    public DatabaseSchema? GetLastKnownSchema(string source)
    {
        return _lastKnownSchemas.GetValueOrDefault(source);
    }

    /// <summary>
    /// Forces a schema change detection for a specific source.
    /// </summary>
    /// <param name="source">The database source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the forced detection operation.</returns>
    public async Task ForceSchemaDetectionAsync(string source, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Forcing schema detection for source: {Source}", source);

            // Remove last detection time to force immediate detection
            _lastDetectionTimes.Remove(source);

            await DetectSchemaChangesAsync(source, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing schema detection for source: {Source}", source);
            throw;
        }
    }

    /// <summary>
    /// Gets schema change statistics.
    /// </summary>
    /// <returns>Schema change statistics.</returns>
    public SchemaChangeStatistics GetStatistics()
    {
        return new SchemaChangeStatistics
        {
            MonitoredSources = _lastKnownSchemas.Count,
            LastDetectionTimes = new Dictionary<string, DateTime>(_lastDetectionTimes),
            TotalTables = _lastKnownSchemas.Values.Sum(s => s.Tables.Count),
            TotalColumns = _lastKnownSchemas.Values.Sum(s => s.Tables.Sum(t => t.Columns.Count)),
            LastUpdated = DateTime.UtcNow
        };
    }

    private IList<SchemaChange> CompareSchemas(DatabaseSchema oldSchema, DatabaseSchema newSchema)
    {
        var changes = new List<SchemaChange>();

        // Compare tables
        var oldTables = oldSchema.Tables.ToDictionary(t => t.TableName);
        var newTables = newSchema.Tables.ToDictionary(t => t.TableName);

        // Check for new tables
        foreach (var newTable in newTables.Values)
        {
            if (!oldTables.ContainsKey(newTable.TableName))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.TableAdded,
                    TableName = newTable.TableName,
                    SchemaName = newTable.SchemaName,
                    Description = $"Table {newTable.TableName} was added",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        TableType = newTable.TableType,
                        ColumnCount = newTable.Columns.Count,
                        Comment = newTable.Comment
                    })
                });
            }
        }

        // Check for removed tables
        foreach (var oldTable in oldTables.Values)
        {
            if (!newTables.ContainsKey(oldTable.TableName))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.TableRemoved,
                    TableName = oldTable.TableName,
                    SchemaName = oldTable.SchemaName,
                    Description = $"Table {oldTable.TableName} was removed",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        TableType = oldTable.TableType,
                        ColumnCount = oldTable.Columns.Count,
                        Comment = oldTable.Comment
                    })
                });
            }
        }

        // Check for modified tables
        foreach (var newTable in newTables.Values)
        {
            if (oldTables.TryGetValue(newTable.TableName, out var oldTable))
            {
                var tableChanges = CompareTables(oldTable, newTable);
                changes.AddRange(tableChanges);
            }
        }

        return changes;
    }

    private IList<SchemaChange> CompareTables(TableSchema oldTable, TableSchema newTable)
    {
        var changes = new List<SchemaChange>();

        // Compare columns
        var oldColumns = oldTable.Columns.ToDictionary(c => c.ColumnName);
        var newColumns = newTable.Columns.ToDictionary(c => c.ColumnName);

        // Check for new columns
        foreach (var newColumn in newColumns.Values)
        {
            if (!oldColumns.ContainsKey(newColumn.ColumnName))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.ColumnAdded,
                    TableName = newTable.TableName,
                    SchemaName = newTable.SchemaName,
                    ColumnName = newColumn.ColumnName,
                    Description = $"Column {newColumn.ColumnName} was added to table {newTable.TableName}",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        DataType = newColumn.DataType,
                        IsNullable = newColumn.IsNullable,
                        ColumnDefault = newColumn.ColumnDefault,
                        Comment = newColumn.Comment
                    })
                });
            }
        }

        // Check for removed columns
        foreach (var oldColumn in oldColumns.Values)
        {
            if (!newColumns.ContainsKey(oldColumn.ColumnName))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.ColumnRemoved,
                    TableName = newTable.TableName,
                    SchemaName = newTable.SchemaName,
                    ColumnName = oldColumn.ColumnName,
                    Description = $"Column {oldColumn.ColumnName} was removed from table {newTable.TableName}",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        DataType = oldColumn.DataType,
                        IsNullable = oldColumn.IsNullable,
                        ColumnDefault = oldColumn.ColumnDefault,
                        Comment = oldColumn.Comment
                    })
                });
            }
        }

        // Check for modified columns
        foreach (var newColumn in newColumns.Values)
        {
            if (oldColumns.TryGetValue(newColumn.ColumnName, out var oldColumn))
            {
                var columnChanges = CompareColumns(newTable.TableName, newTable.SchemaName, oldColumn, newColumn);
                changes.AddRange(columnChanges);
            }
        }

        // Compare indexes
        var indexChanges = CompareIndexes(oldTable, newTable);
        changes.AddRange(indexChanges);

        // Compare foreign keys
        var foreignKeyChanges = CompareForeignKeys(oldTable, newTable);
        changes.AddRange(foreignKeyChanges);

        return changes;
    }

    private IList<SchemaChange> CompareColumns(string tableName, string schemaName, ColumnSchema oldColumn, ColumnSchema newColumn)
    {
        var changes = new List<SchemaChange>();

        // Check data type changes
        if (oldColumn.DataType != newColumn.DataType)
        {
            changes.Add(new SchemaChange
            {
                Type = SchemaChangeType.ColumnTypeChanged,
                TableName = tableName,
                SchemaName = schemaName,
                ColumnName = newColumn.ColumnName,
                Description = $"Column {newColumn.ColumnName} data type changed from {oldColumn.DataType} to {newColumn.DataType}",
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    OldDataType = oldColumn.DataType,
                    NewDataType = newColumn.DataType,
                    OldNullable = oldColumn.IsNullable,
                    NewNullable = newColumn.IsNullable
                })
            });
        }

        // Check nullable changes
        if (oldColumn.IsNullable != newColumn.IsNullable)
        {
            changes.Add(new SchemaChange
            {
                Type = SchemaChangeType.ColumnNullableChanged,
                TableName = tableName,
                SchemaName = schemaName,
                ColumnName = newColumn.ColumnName,
                Description = $"Column {newColumn.ColumnName} nullable property changed from {oldColumn.IsNullable} to {newColumn.IsNullable}",
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    OldNullable = oldColumn.IsNullable,
                    NewNullable = newColumn.IsNullable
                })
            });
        }

        // Check default value changes
        if (oldColumn.ColumnDefault != newColumn.ColumnDefault)
        {
            changes.Add(new SchemaChange
            {
                Type = SchemaChangeType.ColumnDefaultChanged,
                TableName = tableName,
                SchemaName = schemaName,
                ColumnName = newColumn.ColumnName,
                Description = $"Column {newColumn.ColumnName} default value changed from {oldColumn.ColumnDefault} to {newColumn.ColumnDefault}",
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    OldDefault = oldColumn.ColumnDefault,
                    NewDefault = newColumn.ColumnDefault
                })
            });
        }

        return changes;
    }

    private IList<SchemaChange> CompareIndexes(TableSchema oldTable, TableSchema newTable)
    {
        var changes = new List<SchemaChange>();

        var oldIndexes = oldTable.Indexes.ToDictionary(i => i.IndexName);
        var newIndexes = newTable.Indexes.ToDictionary(i => i.IndexName);

        // Check for new indexes
        foreach (var newIndex in newIndexes.Values)
        {
            if (!oldIndexes.ContainsKey(newIndex.IndexName))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.IndexAdded,
                    TableName = newTable.TableName,
                    SchemaName = newTable.SchemaName,
                    IndexName = newIndex.IndexName,
                    Description = $"Index {newIndex.IndexName} was added to table {newTable.TableName}",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        IsUnique = newIndex.IsUnique,
                        IsPrimary = newIndex.IsPrimary,
                        ColumnNames = newIndex.ColumnNames
                    })
                });
            }
        }

        // Check for removed indexes
        foreach (var oldIndex in oldIndexes.Values)
        {
            if (!newIndexes.ContainsKey(oldIndex.IndexName))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.IndexRemoved,
                    TableName = newTable.TableName,
                    SchemaName = newTable.SchemaName,
                    IndexName = oldIndex.IndexName,
                    Description = $"Index {oldIndex.IndexName} was removed from table {newTable.TableName}",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        IsUnique = oldIndex.IsUnique,
                        IsPrimary = oldIndex.IsPrimary,
                        ColumnNames = oldIndex.ColumnNames
                    })
                });
            }
        }

        return changes;
    }

    private IList<SchemaChange> CompareForeignKeys(TableSchema oldTable, TableSchema newTable)
    {
        var changes = new List<SchemaChange>();

        var oldForeignKeys = oldTable.ForeignKeys.ToDictionary(fk => fk.ConstraintName);
        var newForeignKeys = newTable.ForeignKeys.ToDictionary(fk => fk.ConstraintName);

        // Check for new foreign keys
        foreach (var newForeignKey in newForeignKeys.Values)
        {
            if (!oldForeignKeys.ContainsKey(newForeignKey.ConstraintName))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.ForeignKeyAdded,
                    TableName = newTable.TableName,
                    SchemaName = newTable.SchemaName,
                    ConstraintName = newForeignKey.ConstraintName,
                    Description = $"Foreign key {newForeignKey.ConstraintName} was added to table {newTable.TableName}",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        ColumnName = newForeignKey.ColumnName,
                        ForeignTableName = newForeignKey.ForeignTableName,
                        ForeignColumnName = newForeignKey.ForeignColumnName,
                        UpdateRule = newForeignKey.UpdateRule,
                        DeleteRule = newForeignKey.DeleteRule
                    })
                });
            }
        }

        // Check for removed foreign keys
        foreach (var oldForeignKey in oldForeignKeys.Values)
        {
            if (!newForeignKeys.ContainsKey(oldForeignKey.ConstraintName))
            {
                changes.Add(new SchemaChange
                {
                    Type = SchemaChangeType.ForeignKeyRemoved,
                    TableName = newTable.TableName,
                    SchemaName = newTable.SchemaName,
                    ConstraintName = oldForeignKey.ConstraintName,
                    Description = $"Foreign key {oldForeignKey.ConstraintName} was removed from table {newTable.TableName}",
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        ColumnName = oldForeignKey.ColumnName,
                        ForeignTableName = oldForeignKey.ForeignTableName,
                        ForeignColumnName = oldForeignKey.ForeignColumnName,
                        UpdateRule = oldForeignKey.UpdateRule,
                        DeleteRule = oldForeignKey.DeleteRule
                    })
                });
            }
        }

        return changes;
    }

    private async Task PublishSchemaChangeEventAsync(string source, IList<SchemaChange> changes, CancellationToken cancellationToken)
    {
        try
        {
            var changeEvent = ChangeEvent.Create(
                source,
                "schema",
                "schema_changes",
                "SCHEMA_CHANGE",
                DateTime.UtcNow.Ticks.ToString(),
                null,
                JsonSerializer.SerializeToElement(changes),
                new Dictionary<string, string>
                {
                    ["change_count"] = changes.Count.ToString(),
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["change_types"] = string.Join(",", changes.Select(c => c.Type.ToString()).Distinct())
                });

            await _changePublisher.PublishAsync(changeEvent, cancellationToken);

            _logger.LogInformation("Published schema change event for source: {Source} with {ChangeCount} changes", 
                source, changes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing schema change event for source: {Source}", source);
        }
    }

    private void DetectSchemaChanges(object? state)
    {
        // This method is called by the timer
        // In a real implementation, you would iterate through all known sources
        // and detect schema changes for each one
        _ = Task.Run(async () =>
        {
            try
            {
                // Get all monitored sources
                var sources = _lastKnownSchemas.Keys.ToList();
                
                foreach (var source in sources)
                {
                    await DetectSchemaChangesAsync(source, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled schema change detection");
            }
        });
    }

    /// <summary>
    /// Disposes the enhanced schema change detector.
    /// </summary>
    public void Dispose()
    {
        _detectionTimer?.Dispose();
        _detectionSemaphore?.Dispose();
    }
}

/// <summary>
/// Schema change statistics.
/// </summary>
public sealed class SchemaChangeStatistics
{
    /// <summary>
    /// Gets or sets the number of monitored sources.
    /// </summary>
    public int MonitoredSources { get; set; }

    /// <summary>
    /// Gets or sets the last detection times for each source.
    /// </summary>
    public IDictionary<string, DateTime> LastDetectionTimes { get; set; } = new Dictionary<string, DateTime>();

    /// <summary>
    /// Gets or sets the total number of tables across all sources.
    /// </summary>
    public int TotalTables { get; set; }

    /// <summary>
    /// Gets or sets the total number of columns across all sources.
    /// </summary>
    public int TotalColumns { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}