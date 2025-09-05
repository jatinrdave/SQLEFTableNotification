using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Schema change detection engine for monitoring database schema changes and analyzing their impact
    /// </summary>
    public class SchemaChangeDetection : IDisposable
    {
        private readonly ConcurrentDictionary<string, TableSchemaSnapshot> _tableSnapshots = new();
        private readonly ConcurrentDictionary<string, SchemaChangeHistory> _changeHistory = new();
        private readonly Timer _schemaCheckTimer;
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        /// <summary>
        /// Event raised when schema changes are detected
        /// </summary>
        public event EventHandler<SchemaChangeDetectedEventArgs>? SchemaChangeDetected;

        /// <summary>
        /// Event raised when schema change impact analysis is completed
        /// </summary>
        public event EventHandler<SchemaChangeImpactAnalyzedEventArgs>? SchemaChangeImpactAnalyzed;

        /// <summary>
        /// Event raised when schema change risk assessment is completed
        /// </summary>
        public event EventHandler<SchemaChangeRiskAssessedEventArgs>? SchemaChangeRiskAssessed;

        public SchemaChangeDetection()
        {
            _schemaCheckTimer = new Timer(CheckForSchemaChanges, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            _cleanupTimer = new Timer(CleanupOldData, null, TimeSpan.FromHours(6), TimeSpan.FromHours(6));
        }

        /// <summary>
        /// Takes a snapshot of the current table schema
        /// </summary>
        public async Task<TableSchemaSnapshot> TakeTableSnapshotAsync(string tableName, ICDCProvider cdcProvider)
        {
            if (_disposed) return new TableSchemaSnapshot { TableName = tableName };
            if (string.IsNullOrEmpty(tableName)) return new TableSchemaSnapshot { TableName = "unknown" };
            if (cdcProvider == null) throw new ArgumentNullException(nameof(cdcProvider));

            try
            {
                var columns = await cdcProvider.GetTableColumnsAsync(tableName);
                var indexes = await cdcProvider.GetTableIndexesAsync(tableName);
                var constraints = await cdcProvider.GetTableConstraintsAsync(tableName);

                var snapshot = new TableSchemaSnapshot
                {
                    TableName = tableName,
                    Timestamp = DateTime.UtcNow,
                    Columns = columns?.Select(c => c.Clone()).ToList() ?? new List<ColumnDefinition>(),
                    Indexes = indexes?.ToList() ?? new List<IndexDefinition>(),
                    Constraints = constraints?.ToList() ?? new List<ConstraintDefinition>(),
                    TableDefinition = new TableDefinition
                    {
                        Name = tableName,
                        SchemaName = "dbo", // Default schema, should be configurable
                        CreationDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    }
                };

                _tableSnapshots.AddOrUpdate(tableName, snapshot, (_, _) => snapshot);
                return snapshot;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error taking schema snapshot for table {tableName}: {ex.Message}");
                return new TableSchemaSnapshot { TableName = tableName };
            }
        }

        /// <summary>
        /// Compares current schema with previous snapshot and detects changes
        /// </summary>
        public async Task<List<SchemaChangeInfo>> DetectSchemaChangesAsync(string tableName, ICDCProvider cdcProvider)
        {
            if (_disposed) return new List<SchemaChangeInfo>();
            if (string.IsNullOrEmpty(tableName)) return new List<SchemaChangeInfo>();
            if (cdcProvider == null) throw new ArgumentNullException(nameof(cdcProvider));

            // Get current schema without overwriting the previous snapshot
            var columns = await cdcProvider.GetTableColumnsAsync(tableName);
            var indexes = await cdcProvider.GetTableIndexesAsync(tableName);
            var constraints = await cdcProvider.GetTableConstraintsAsync(tableName);

            var currentSnapshot = new TableSchemaSnapshot
            {
                TableName = tableName,
                Timestamp = DateTime.UtcNow,
                Columns = columns?.Select(c => c.Clone()).ToList() ?? new List<ColumnDefinition>(),
                Indexes = indexes?.ToList() ?? new List<IndexDefinition>(),
                Constraints = constraints?.ToList() ?? new List<ConstraintDefinition>(),
                TableDefinition = new TableDefinition
                {
                    Name = tableName,
                    SchemaName = "dbo", // Default schema, should be configurable
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                }
            };

            var previousSnapshot = _tableSnapshots.GetValueOrDefault(tableName);

            if (previousSnapshot == null)
            {
                // First time snapshot, no changes to detect
                return new List<SchemaChangeInfo>();
            }

            var changes = new List<SchemaChangeInfo>();

            // Detect column changes
            var columnChanges = DetectColumnChanges(previousSnapshot, currentSnapshot);
            changes.AddRange(columnChanges);

            // Detect index changes
            var indexChanges = DetectIndexChanges(previousSnapshot, currentSnapshot);
            changes.AddRange(indexChanges);

            // Detect constraint changes
            var constraintChanges = DetectConstraintChanges(previousSnapshot, currentSnapshot);
            changes.AddRange(constraintChanges);

            // Detect table-level changes
            var tableChanges = DetectTableChanges(previousSnapshot, currentSnapshot);
            changes.AddRange(tableChanges);

            if (changes.Any())
            {
                // Record changes in history
                var history = _changeHistory.GetOrAdd(tableName, _ => new SchemaChangeHistory());
                history.RecordChanges(changes);

                // Raise schema change detected event
                SchemaChangeDetected?.Invoke(this, new SchemaChangeDetectedEventArgs(tableName, changes));

                // Perform impact analysis
                var impactAnalysis = await AnalyzeSchemaChangeImpactAsync(tableName, changes);
                SchemaChangeImpactAnalyzed?.Invoke(this, new SchemaChangeImpactAnalyzedEventArgs(tableName, changes, impactAnalysis));

                // Perform risk assessment
                var riskAssessment = await AssessSchemaChangeRiskAsync(tableName, changes);
                SchemaChangeRiskAssessed?.Invoke(this, new SchemaChangeRiskAssessedEventArgs(tableName, changes, riskAssessment));
            }

            return changes;
        }

        /// <summary>
        /// Gets schema change history for a specific table
        /// </summary>
        public SchemaChangeHistory GetChangeHistory(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return new SchemaChangeHistory();
            return _changeHistory.TryGetValue(tableName, out var history) ? history : new SchemaChangeHistory();
        }

        /// <summary>
        /// Gets all schema change history
        /// </summary>
        public Dictionary<string, SchemaChangeHistory> GetAllChangeHistory()
        {
            return _changeHistory.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Clears schema change history for a specific table
        /// </summary>
        public void ClearTableHistory(string tableName)
        {
            _changeHistory.TryRemove(tableName, out _);
            _tableSnapshots.TryRemove(tableName, out _);
        }

        /// <summary>
        /// Clears all schema change history
        /// </summary>
        public void ClearAllHistory()
        {
            _changeHistory.Clear();
            _tableSnapshots.Clear();
        }

        private List<SchemaChangeInfo> DetectColumnChanges(TableSchemaSnapshot previous, TableSchemaSnapshot current)
        {
            var changes = new List<SchemaChangeInfo>();
            var previousColumns = previous.Columns.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
            var currentColumns = current.Columns.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);

            // Detect added columns
            foreach (var currentColumn in current.Columns)
            {
                if (!previousColumns.ContainsKey(currentColumn.ColumnName))
                {
                    changes.Add(new SchemaChangeInfo
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        TableName = current.TableName,
                        ChangeType = SchemaChangeType.ColumnAdded,
                        ChangeTimestamp = DateTime.UtcNow,
                        Description = $"Column '{currentColumn.ColumnName}' was added",
                        Impact = SchemaChangeImpact.Low,
                        Risk = SchemaChangeRisk.Low,
                        AffectedColumns = new List<string> { currentColumn.ColumnName }
                    });
                }
            }

            // Detect removed columns
            foreach (var previousColumn in previous.Columns)
            {
                if (!currentColumns.ContainsKey(previousColumn.ColumnName))
                {
                    changes.Add(new SchemaChangeInfo
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        TableName = current.TableName,
                        ChangeType = SchemaChangeType.ColumnDropped,
                        ChangeTimestamp = DateTime.UtcNow,
                        Description = $"Column '{previousColumn.ColumnName}' was removed",
                        Impact = SchemaChangeImpact.High,
                        Risk = SchemaChangeRisk.High,
                        AffectedColumns = new List<string> { previousColumn.ColumnName }
                    });
                }
            }

            // Detect modified columns
            foreach (var currentColumn in current.Columns)
            {
                if (previousColumns.TryGetValue(currentColumn.ColumnName, out var previousColumn))
                {
                    if (!ColumnsAreEqual(previousColumn, currentColumn))
                    {
                        changes.Add(new SchemaChangeInfo
                        {
                            ChangeId = Guid.NewGuid().ToString(),
                            TableName = current.TableName,
                            ChangeType = SchemaChangeType.ColumnDataTypeChanged,
                            ChangeTimestamp = DateTime.UtcNow,
                            Description = $"Column '{currentColumn.ColumnName}' was modified",
                            Impact = SchemaChangeImpact.Medium,
                            Risk = SchemaChangeRisk.Medium,
                            AffectedColumns = new List<string> { currentColumn.ColumnName }
                        });
                    }
                }
            }

            return changes;
        }

        private List<SchemaChangeInfo> DetectIndexChanges(TableSchemaSnapshot previous, TableSchemaSnapshot current)
        {
            var changes = new List<SchemaChangeInfo>();
            var previousIndexes = previous.Indexes.ToDictionary(i => i.IndexName, StringComparer.OrdinalIgnoreCase);
            var currentIndexes = current.Indexes.ToDictionary(i => i.IndexName, StringComparer.OrdinalIgnoreCase);

            // Detect added indexes
            foreach (var currentIndex in current.Indexes)
            {
                if (!previousIndexes.ContainsKey(currentIndex.IndexName))
                {
                    changes.Add(new SchemaChangeInfo
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        TableName = current.TableName,
                        ChangeType = SchemaChangeType.IndexCreated,
                        ChangeTimestamp = DateTime.UtcNow,
                        Description = $"Index '{currentIndex.IndexName}' was added",
                        Impact = SchemaChangeImpact.Low,
                        Risk = SchemaChangeRisk.Low
                    });
                }
            }

            // Detect removed indexes
            foreach (var previousIndex in previous.Indexes)
            {
                if (!currentIndexes.ContainsKey(previousIndex.IndexName))
                {
                    changes.Add(new SchemaChangeInfo
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        TableName = current.TableName,
                        ChangeType = SchemaChangeType.IndexDropped,
                        ChangeTimestamp = DateTime.UtcNow,
                        Description = $"Index '{previousIndex.IndexName}' was removed",
                        Impact = SchemaChangeImpact.Medium,
                        Risk = SchemaChangeRisk.Medium
                    });
                }
            }

            return changes;
        }

        private List<SchemaChangeInfo> DetectConstraintChanges(TableSchemaSnapshot previous, TableSchemaSnapshot current)
        {
            var changes = new List<SchemaChangeInfo>();
            var previousConstraints = previous.Constraints.ToDictionary(c => c.ConstraintName, StringComparer.OrdinalIgnoreCase);
            var currentConstraints = current.Constraints.ToDictionary(c => c.ConstraintName, StringComparer.OrdinalIgnoreCase);

            // Detect added constraints
            foreach (var currentConstraint in current.Constraints)
            {
                if (!previousConstraints.ContainsKey(currentConstraint.ConstraintName))
                {
                    changes.Add(new SchemaChangeInfo
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        TableName = current.TableName,
                        ChangeType = SchemaChangeType.ConstraintAdded,
                        ChangeTimestamp = DateTime.UtcNow,
                        Description = $"Constraint '{currentConstraint.ConstraintName}' was added",
                        Impact = SchemaChangeImpact.Medium,
                        Risk = SchemaChangeRisk.Medium
                    });
                }
            }

            // Detect removed constraints
            foreach (var previousConstraint in previous.Constraints)
            {
                if (!currentConstraints.ContainsKey(previousConstraint.ConstraintName))
                {
                    changes.Add(new SchemaChangeInfo
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        TableName = current.TableName,
                        ChangeType = SchemaChangeType.ConstraintDropped,
                        ChangeTimestamp = DateTime.UtcNow,
                        Description = $"Constraint '{previousConstraint.ConstraintName}' was removed",
                        Impact = SchemaChangeImpact.High,
                        Risk = SchemaChangeRisk.High
                    });
                }
            }

            return changes;
        }

        private List<SchemaChangeInfo> DetectTableChanges(TableSchemaSnapshot previous, TableSchemaSnapshot current)
        {
            var changes = new List<SchemaChangeInfo>();

            // Detect table-level changes (e.g., table properties, partitioning, etc.)
            if (previous.TableDefinition != null && current.TableDefinition != null)
            {
                if (previous.TableDefinition.SchemaName != current.TableDefinition.SchemaName)
                {
                    changes.Add(new SchemaChangeInfo
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        TableName = current.TableName,
                        ChangeType = SchemaChangeType.TableRenamed,
                        ChangeTimestamp = DateTime.UtcNow,
                        Description = $"Table schema changed from '{previous.TableDefinition.SchemaName}' to '{current.TableDefinition.SchemaName}'",
                        Impact = SchemaChangeImpact.High,
                        Risk = SchemaChangeRisk.High
                    });
                }
            }

            return changes;
        }

        private bool ColumnsAreEqual(ColumnDefinition col1, ColumnDefinition col2)
        {
            return col1.ColumnName.Equals(col2.ColumnName, StringComparison.OrdinalIgnoreCase) &&
                   col1.DataType.Equals(col2.DataType, StringComparison.OrdinalIgnoreCase) &&
                   col1.IsNullable == col2.IsNullable &&
                   col1.MaxLength == col2.MaxLength &&
                   col1.Precision == col2.Precision &&
                   col1.Scale == col2.Scale;
        }

        private async Task<SchemaChangeImpact> AnalyzeSchemaChangeImpactAsync(string tableName, List<SchemaChangeInfo> changes)
        {
            // Simple impact analysis based on change types
            var maxImpact = SchemaChangeImpact.Low;

            foreach (var change in changes)
            {
                if (change.Impact > maxImpact)
                {
                    maxImpact = change.Impact;
                }
            }

            return maxImpact;
        }

        private async Task<SchemaChangeRisk> AssessSchemaChangeRiskAsync(string tableName, List<SchemaChangeInfo> changes)
        {
            // Simple risk assessment based on change types
            var maxRisk = SchemaChangeRisk.Low;

            foreach (var change in changes)
            {
                if (change.Risk > maxRisk)
                {
                    maxRisk = change.Risk;
                }
            }

            return maxRisk;
        }

        private void CheckForSchemaChanges(object? state)
        {
            if (_disposed) return;

            // This method would be called by the timer to periodically check for schema changes
            // In a real implementation, this would iterate through monitored tables and check for changes
        }

        private void CleanupOldData(object? state)
        {
            if (_disposed) return;

            try
            {
                var cutoffTime = DateTime.UtcNow.AddDays(-30); // Keep last 30 days

                foreach (var history in _changeHistory.Values)
                {
                    history.CleanupOldData(cutoffTime);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in schema change cleanup: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _schemaCheckTimer?.Dispose();
                _cleanupTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a snapshot of a table's schema at a specific point in time
    /// </summary>
    public class TableSchemaSnapshot
    {
        public string TableName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
        public List<IndexDefinition> Indexes { get; set; } = new List<IndexDefinition>();
        public List<ConstraintDefinition> Constraints { get; set; } = new List<ConstraintDefinition>();
        public TableDefinition? TableDefinition { get; set; }
    }

    /// <summary>
    /// Manages the history of schema changes for a specific table
    /// </summary>
    public class SchemaChangeHistory
    {
        private readonly List<SchemaChangeInfo> _changes = new();
        private readonly object _lockObject = new object();

        public IReadOnlyList<SchemaChangeInfo> Changes => _changes.AsReadOnly();

        public void RecordChanges(List<SchemaChangeInfo> changes)
        {
            lock (_lockObject)
            {
                _changes.AddRange(changes);
            }
        }

        public void CleanupOldData(DateTime cutoffTime)
        {
            lock (_lockObject)
            {
                _changes.RemoveAll(c => c.Timestamp < cutoffTime);
            }
        }

        public List<SchemaChangeInfo> GetChangesInTimeRange(DateTime startTime, DateTime endTime)
        {
            lock (_lockObject)
            {
                return _changes.Where(c => c.Timestamp >= startTime && c.Timestamp <= endTime).ToList();
            }
        }

        public List<SchemaChangeInfo> GetChangesByType(SchemaChangeType changeType)
        {
            lock (_lockObject)
            {
                return _changes.Where(c => c.ChangeType == changeType).ToList();
            }
        }
    }
}

#region Event Arguments

namespace SQLDBEntityNotifier.Models
{
    public class SchemaChangeDetectedEventArgs : EventArgs
    {
        public string TableName { get; }
        public List<SchemaChangeInfo> Changes { get; }

        public SchemaChangeDetectedEventArgs(string tableName, List<SchemaChangeInfo> changes)
        {
            TableName = tableName;
            Changes = changes;
        }
    }

    public class SchemaChangeImpactAnalyzedEventArgs : EventArgs
    {
        public string TableName { get; }
        public List<SchemaChangeInfo> Changes { get; }
        public SchemaChangeImpact Impact { get; }

        public SchemaChangeImpactAnalyzedEventArgs(string tableName, List<SchemaChangeInfo> changes, SchemaChangeImpact impact)
        {
            TableName = tableName;
            Changes = changes;
            Impact = impact;
        }
    }

    public class SchemaChangeRiskAssessedEventArgs : EventArgs
    {
        public string TableName { get; }
        public List<SchemaChangeInfo> Changes { get; }
        public SchemaChangeRisk Risk { get; }

        public SchemaChangeRiskAssessedEventArgs(string tableName, List<SchemaChangeInfo> changes, SchemaChangeRisk risk)
        {
            TableName = tableName;
            Changes = changes;
            Risk = risk;
        }
    }
}

#endregion
