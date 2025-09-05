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
    /// Engine for analyzing and correlating changes across multiple tables
    /// </summary>
    public class ChangeCorrelationEngine : IDisposable
    {
        private readonly ConcurrentDictionary<string, List<ChangeRecord>> _correlations;
        private readonly ConcurrentDictionary<string, TableDependencyGraph> _dependencyGraphs;
        private readonly ConcurrentDictionary<string, List<ForeignKeyRelationship>> _foreignKeyRelationships;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        /// <summary>
        /// Event raised when correlated changes are detected
        /// </summary>
        public event EventHandler<CorrelatedChangesDetectedEventArgs>? CorrelatedChangesDetected;

        /// <summary>
        /// Event raised when change impact is analyzed
        /// </summary>
        public event EventHandler<ChangeImpactAnalyzedEventArgs>? ChangeImpactAnalyzed;

        public ChangeCorrelationEngine()
        {
            _correlations = new ConcurrentDictionary<string, List<ChangeRecord>>();
            _dependencyGraphs = new ConcurrentDictionary<string, TableDependencyGraph>();
            _foreignKeyRelationships = new ConcurrentDictionary<string, List<ForeignKeyRelationship>>();
        }

        /// <summary>
        /// Records a change for correlation analysis
        /// </summary>
        public void RecordChange(string tableName, ChangeRecord changeRecord)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeCorrelationEngine));

            if (string.IsNullOrWhiteSpace(tableName))
                tableName = "unknown";

            if (changeRecord == null)
                return;

            _correlations.AddOrUpdate(tableName, 
                new List<ChangeRecord> { changeRecord },
                (key, existing) => 
                {
                    lock (existing)
                    {
                        existing.Add(changeRecord);
                    }
                    return existing;
                });

            // Analyze correlations
            AnalyzeCorrelations(tableName, changeRecord);
        }

        /// <summary>
        /// Records multiple changes for batch correlation analysis
        /// </summary>
        public void RecordBatchChanges(string tableName, IEnumerable<ChangeRecord> changes)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeCorrelationEngine));

            if (string.IsNullOrWhiteSpace(tableName))
                tableName = "unknown";

            if (changes == null)
                return;

            var changeList = changes.ToList();
            if (!changeList.Any())
                return;

            _correlations.AddOrUpdate(tableName,
                new List<ChangeRecord>(changeList),
                (key, existing) =>
                {
                    lock (existing)
                    {
                        existing.AddRange(changeList);
                    }
                    return existing;
                });

            // Analyze correlations for each change
            foreach (var change in changeList)
            {
                AnalyzeCorrelations(tableName, change);
            }
        }

        /// <summary>
        /// Gets correlated changes for a specific table
        /// </summary>
        public List<ChangeRecord> GetCorrelatedChanges(string tableName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeCorrelationEngine));

            if (string.IsNullOrWhiteSpace(tableName))
                tableName = "unknown";

            return _correlations.TryGetValue(tableName, out var changes) ? 
                new List<ChangeRecord>(changes) : 
                new List<ChangeRecord>();
        }

        /// <summary>
        /// Gets the dependency graph for a table
        /// </summary>
        public TableDependencyGraph GetDependencyGraph(string tableName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeCorrelationEngine));

            if (string.IsNullOrWhiteSpace(tableName))
                return new TableDependencyGraph("unknown");

            return _dependencyGraphs.GetOrAdd(tableName, _ => new TableDependencyGraph(tableName));
        }

        /// <summary>
        /// Adds a foreign key relationship
        /// </summary>
        public void AddForeignKeyRelationship(ForeignKeyRelationship relationship)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeCorrelationEngine));

            if (relationship == null)
                return;

            var key = $"{relationship.SourceTable}_{relationship.TargetTable}";
            _foreignKeyRelationships.AddOrUpdate(key,
                new List<ForeignKeyRelationship> { relationship },
                (k, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(relationship);
                    }
                    return existing;
                });

            // Update dependency graphs
            UpdateDependencyGraph(relationship.SourceTable, relationship.TargetTable, relationship);
        }

        /// <summary>
        /// Clears correlation data for a specific table
        /// </summary>
        public void ClearTableCorrelations(string tableName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeCorrelationEngine));

            if (string.IsNullOrWhiteSpace(tableName))
                return;

            _correlations.TryRemove(tableName, out _);
            _dependencyGraphs.TryRemove(tableName, out _);

            // Remove from foreign key relationships
            foreach (var kvp in _foreignKeyRelationships.ToList())
            {
                lock (kvp.Value)
                {
                    kvp.Value.RemoveAll(r => r.SourceTable == tableName || r.TargetTable == tableName);
                }
            }
        }

        /// <summary>
        /// Clears all correlation data
        /// </summary>
        public void ClearAllCorrelations()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeCorrelationEngine));

            _correlations.Clear();
            _dependencyGraphs.Clear();
            _foreignKeyRelationships.Clear();
        }

        private void AnalyzeCorrelations(string tableName, ChangeRecord change)
        {
            // Basic correlation analysis
            var relatedChanges = FindRelatedChanges(tableName, change);
            
            if (relatedChanges.Any())
            {
                CorrelatedChangesDetected?.Invoke(this, 
                    new CorrelatedChangesDetectedEventArgs(tableName, change, relatedChanges));
            }

            // Analyze impact
            var impact = AnalyzeChangeImpact(tableName, change);
            if (impact != null)
            {
                ChangeImpactAnalyzed?.Invoke(this,
                    new ChangeImpactAnalyzedEventArgs(tableName, impact));
            }
        }

        private List<ChangeRecord> FindRelatedChanges(string tableName, ChangeRecord change)
        {
            var relatedChanges = new List<ChangeRecord>();

            // Find changes in related tables based on foreign key relationships
            foreach (var kvp in _foreignKeyRelationships)
            {
                var relationships = kvp.Value;
                lock (relationships)
                {
                    foreach (var relationship in relationships)
                    {
                        if (relationship.SourceTable == tableName)
                        {
                            var targetChanges = GetCorrelatedChanges(relationship.TargetTable);
                            relatedChanges.AddRange(targetChanges.Where(c => 
                                IsRelatedChange(change, c, relationship)));
                        }
                        else if (relationship.TargetTable == tableName)
                        {
                            var sourceChanges = GetCorrelatedChanges(relationship.SourceTable);
                            relatedChanges.AddRange(sourceChanges.Where(c => 
                                IsRelatedChange(c, change, relationship)));
                        }
                    }
                }
            }

            return relatedChanges;
        }

        private bool IsRelatedChange(ChangeRecord sourceChange, ChangeRecord targetChange, ForeignKeyRelationship relationship)
        {
            // Simple correlation logic - can be enhanced
            return sourceChange.Timestamp.AddMinutes(5) >= targetChange.Timestamp &&
                   targetChange.Timestamp >= sourceChange.Timestamp.AddMinutes(-5);
        }

        private ChangeImpactAnalysis? AnalyzeChangeImpact(string tableName, ChangeRecord change)
        {
            var graph = GetDependencyGraph(tableName);
            var dependentTables = graph.GetDependentTables();

            return new ChangeImpactAnalysis
            {
                SourceTable = tableName,
                ChangeType = change.ChangeType,
                ImpactedTables = dependentTables,
                Severity = dependentTables.Count > 5 ? ImpactSeverity.High : 
                          dependentTables.Count > 2 ? ImpactSeverity.Medium : ImpactSeverity.Low,
                AnalysisTime = DateTime.UtcNow
            };
        }

        private void UpdateDependencyGraph(string sourceTable, string targetTable, ForeignKeyRelationship relationship)
        {
            var sourceGraph = _dependencyGraphs.GetOrAdd(sourceTable, _ => new TableDependencyGraph(sourceTable));
            var targetGraph = _dependencyGraphs.GetOrAdd(targetTable, _ => new TableDependencyGraph(targetTable));

            sourceGraph.AddDependency(targetTable, relationship);
            targetGraph.AddDependent(sourceTable, relationship);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _correlations.Clear();
                _dependencyGraphs.Clear();
                _foreignKeyRelationships.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a foreign key relationship between tables
    /// </summary>
    public class ForeignKeyRelationship
    {
        public string SourceTable { get; set; } = string.Empty;
        public string TargetTable { get; set; } = string.Empty;
        public string SourceColumn { get; set; } = string.Empty;
        public string TargetColumn { get; set; } = string.Empty;
        public string RelationshipName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a table dependency graph
    /// </summary>
    public class TableDependencyGraph
    {
        public string TableName { get; }
        private readonly List<string> _dependencies;
        private readonly List<string> _dependents;
        private readonly List<ForeignKeyRelationship> _relationships;

        public TableDependencyGraph(string tableName)
        {
            TableName = tableName;
            _dependencies = new List<string>();
            _dependents = new List<string>();
            _relationships = new List<ForeignKeyRelationship>();
        }

        public void AddDependency(string tableName, ForeignKeyRelationship relationship)
        {
            if (!_dependencies.Contains(tableName))
            {
                _dependencies.Add(tableName);
                _relationships.Add(relationship);
            }
        }

        public void AddDependent(string tableName, ForeignKeyRelationship relationship)
        {
            if (!_dependents.Contains(tableName))
            {
                _dependents.Add(tableName);
                _relationships.Add(relationship);
            }
        }

        public List<string> GetDependentTables()
        {
            return new List<string>(_dependents);
        }

        public List<string> GetDependencyTables()
        {
            return new List<string>(_dependencies);
        }
    }

    /// <summary>
    /// Represents a correlated change
    /// </summary>
    public class CorrelatedChange
    {
        public ChangeRecord RelatedChange { get; set; } = null!;
        public string RelationshipType { get; set; } = string.Empty;
        public double CorrelationScore { get; set; }
    }

    /// <summary>
    /// Represents change impact analysis
    /// </summary>
    public class ChangeImpactAnalysis
    {
        public string SourceTable { get; set; } = string.Empty;
        public ChangeType ChangeType { get; set; }
        public List<string> ImpactedTables { get; set; } = new List<string>();
        public ImpactSeverity Severity { get; set; }
        public DateTime AnalysisTime { get; set; }
    }

    /// <summary>
    /// Impact severity levels
    /// </summary>
    public enum ImpactSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Event arguments for correlated changes detected
    /// </summary>
    public class CorrelatedChangesDetectedEventArgs : EventArgs
    {
        public string TableName { get; }
        public ChangeRecord SourceChange { get; }
        public List<ChangeRecord> RelatedChanges { get; }

        public CorrelatedChangesDetectedEventArgs(string tableName, ChangeRecord sourceChange, List<ChangeRecord> relatedChanges)
        {
            TableName = tableName;
            SourceChange = sourceChange;
            RelatedChanges = relatedChanges;
        }
    }

    /// <summary>
    /// Event arguments for change impact analyzed
    /// </summary>
    public class ChangeImpactAnalyzedEventArgs : EventArgs
    {
        public string TableName { get; }
        public ChangeImpactAnalysis ImpactAnalysis { get; }

        public ChangeImpactAnalyzedEventArgs(string tableName, ChangeImpactAnalysis impactAnalysis)
        {
            TableName = tableName;
            ImpactAnalysis = impactAnalysis;
        }
    }
}