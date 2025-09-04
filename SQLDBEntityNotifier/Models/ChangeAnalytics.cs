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
    /// Change analytics and metrics engine for monitoring CDC performance and analyzing change patterns
    /// </summary>
    public class ChangeAnalytics : IDisposable
    {
        private readonly ConcurrentDictionary<string, ChangeMetrics> _tableMetrics = new();
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _performanceMetrics = new();
        private readonly ConcurrentDictionary<string, ChangePattern> _changePatterns = new();
        private readonly Timer _metricsAggregationTimer;
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        /// <summary>
        /// Event raised when performance thresholds are exceeded
        /// </summary>
        public event EventHandler<PerformanceThresholdExceededEventArgs>? PerformanceThresholdExceeded;

        /// <summary>
        /// Event raised when change patterns are detected
        /// </summary>
        public event EventHandler<ChangePatternDetectedEventArgs>? ChangePatternDetected;

        /// <summary>
        /// Event raised when metrics are aggregated
        /// </summary>
        public event EventHandler<MetricsAggregatedEventArgs>? MetricsAggregated;

        public ChangeAnalytics()
        {
            _metricsAggregationTimer = new Timer(AggregateMetrics, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _cleanupTimer = new Timer(CleanupOldMetrics, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        /// <summary>
        /// Records a change event for analytics
        /// </summary>
        public void RecordChange(string tableName, ChangeRecord changeRecord, TimeSpan processingTime)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeAnalytics));

            var metrics = _tableMetrics.GetOrAdd(tableName, _ => new ChangeMetrics());
            metrics.RecordChange(changeRecord, processingTime);

            var perfMetrics = _performanceMetrics.GetOrAdd(tableName, _ => new PerformanceMetrics());
            perfMetrics.RecordProcessingTime(processingTime);

            var pattern = _changePatterns.GetOrAdd(tableName, _ => new ChangePattern());
            pattern.RecordChange(changeRecord);
        }

        /// <summary>
        /// Records a batch of changes for analytics
        /// </summary>
        public void RecordBatchChanges(string tableName, IEnumerable<ChangeRecord> changeRecords, TimeSpan processingTime)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeAnalytics));

            var metrics = _tableMetrics.GetOrAdd(tableName, _ => new ChangeMetrics());
            metrics.RecordBatchChanges(changeRecords, processingTime);

            var perfMetrics = _performanceMetrics.GetOrAdd(tableName, _ => new PerformanceMetrics());
            perfMetrics.RecordBatchProcessingTime(processingTime, changeRecords.Count());

            var pattern = _changePatterns.GetOrAdd(tableName, _ => new ChangePattern());
            pattern.RecordBatchChanges(changeRecords);
        }

        /// <summary>
        /// Gets current metrics for a specific table
        /// </summary>
        public ChangeMetrics GetTableMetrics(string tableName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeAnalytics));
            return _tableMetrics.TryGetValue(tableName, out var metrics) ? metrics : new ChangeMetrics();
        }

        /// <summary>
        /// Gets performance metrics for a specific table
        /// </summary>
        public PerformanceMetrics GetPerformanceMetrics(string tableName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeAnalytics));
            return _performanceMetrics.TryGetValue(tableName, out var metrics) ? metrics : new PerformanceMetrics();
        }

        /// <summary>
        /// Gets change patterns for a specific table
        /// </summary>
        public ChangePattern GetChangePattern(string tableName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeAnalytics));
            return _changePatterns.TryGetValue(tableName, out var pattern) ? pattern : new ChangePattern();
        }

        /// <summary>
        /// Gets aggregated metrics across all tables
        /// </summary>
        public AggregatedMetrics GetAggregatedMetrics()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeAnalytics));
            var totalChanges = _tableMetrics.Values.Sum(m => m.TotalChanges);
            var totalProcessingTimeTicks = _performanceMetrics.Values.Sum(m => m.TotalProcessingTime.Ticks);
            var totalProcessingTime = TimeSpan.FromTicks(totalProcessingTimeTicks);
            var averageProcessingTime = totalChanges > 0 ? TimeSpan.FromTicks(totalProcessingTimeTicks / totalChanges) : TimeSpan.Zero;

            return new AggregatedMetrics
            {
                TotalTables = _tableMetrics.Count,
                TotalChanges = totalChanges,
                TotalProcessingTime = totalProcessingTime,
                AverageProcessingTime = averageProcessingTime,
                PeakProcessingTime = _performanceMetrics.Values.Any() ? _performanceMetrics.Values.Max(m => m.PeakProcessingTime) : TimeSpan.Zero,
                TablesWithHighActivity = _tableMetrics.Values.Count(m => m.ChangesPerMinute > 100)
            };
        }

        /// <summary>
        /// Sets performance thresholds for monitoring
        /// </summary>
        public void SetPerformanceThresholds(string tableName, PerformanceThresholds thresholds)
        {
            var perfMetrics = _performanceMetrics.GetOrAdd(tableName, _ => new PerformanceMetrics());
            perfMetrics.SetThresholds(thresholds);
        }

        /// <summary>
        /// Clears metrics for a specific table
        /// </summary>
        public void ClearTableMetrics(string tableName)
        {
            _tableMetrics.TryRemove(tableName, out _);
            _performanceMetrics.TryRemove(tableName, out _);
            _changePatterns.TryRemove(tableName, out _);
        }

        /// <summary>
        /// Clears all metrics
        /// </summary>
        public void ClearAllMetrics()
        {
            _tableMetrics.Clear();
            _performanceMetrics.Clear();
            _changePatterns.Clear();
        }

        private void AggregateMetrics(object? state)
        {
            if (_disposed) return;

            try
            {
                var aggregatedMetrics = GetAggregatedMetrics();
                MetricsAggregated?.Invoke(this, new MetricsAggregatedEventArgs(aggregatedMetrics));

                // Check for performance threshold violations
                foreach (var kvp in _performanceMetrics)
                {
                    var tableName = kvp.Key;
                    var metrics = kvp.Value;

                    if (metrics.HasThresholdViolations(out var violations))
                    {
                        PerformanceThresholdExceeded?.Invoke(this, new PerformanceThresholdExceededEventArgs(tableName, violations));
                    }
                }

                // Check for change patterns
                foreach (var kvp in _changePatterns)
                {
                    var tableName = kvp.Key;
                    var pattern = kvp.Value;

                    if (pattern.HasSignificantPatterns(out var detectedPatterns))
                    {
                        ChangePatternDetected?.Invoke(this, new ChangePatternDetectedEventArgs(tableName, detectedPatterns));
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent timer from stopping
                System.Diagnostics.Debug.WriteLine($"Error in metrics aggregation: {ex.Message}");
            }
        }

        private void CleanupOldMetrics(object? state)
        {
            if (_disposed) return;

            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-24); // Keep last 24 hours

                foreach (var metrics in _tableMetrics.Values)
                {
                    metrics.CleanupOldData(cutoffTime);
                }

                foreach (var metrics in _performanceMetrics.Values)
                {
                    metrics.CleanupOldData(cutoffTime);
                }

                foreach (var pattern in _changePatterns.Values)
                {
                    pattern.CleanupOldData(cutoffTime);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in metrics cleanup: {ex.Message}");
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
                _metricsAggregationTimer?.Dispose();
                _cleanupTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents change metrics for a specific table
    /// </summary>
    public class ChangeMetrics
    {
        private readonly ConcurrentQueue<ChangeEvent> _recentChanges = new();
        private readonly object _lockObject = new object();
        private long _totalChanges = 0;
        private long _inserts = 0;
        private long _updates = 0;
        private long _deletes = 0;

        public long TotalChanges => _totalChanges;
        public long Inserts => _inserts;
        public long Updates => _updates;
        public long Deletes => _deletes;
        public double ChangesPerMinute => CalculateChangesPerMinute();

        public void RecordChange(ChangeRecord changeRecord, TimeSpan processingTime)
        {
            Interlocked.Increment(ref _totalChanges);

            switch (changeRecord.ChangeType)
            {
                case ChangeType.Insert:
                    Interlocked.Increment(ref _inserts);
                    break;
                case ChangeType.Update:
                    Interlocked.Increment(ref _updates);
                    break;
                case ChangeType.Delete:
                    Interlocked.Increment(ref _deletes);
                    break;
            }

            var changeEvent = new ChangeEvent
            {
                Timestamp = DateTime.UtcNow,
                ChangeType = changeRecord.ChangeType,
                ProcessingTime = processingTime,
                RecordCount = 1
            };

            _recentChanges.Enqueue(changeEvent);

            // Keep only last 1000 changes for memory management
            while (_recentChanges.Count > 1000)
            {
                _recentChanges.TryDequeue(out _);
            }
        }

        public void RecordBatchChanges(IEnumerable<ChangeRecord> changeRecords, TimeSpan processingTime)
        {
            var records = changeRecords.ToList();
            var recordCount = records.Count;

            Interlocked.Add(ref _totalChanges, recordCount);

            foreach (var record in records)
            {
                switch (record.ChangeType)
                {
                    case ChangeType.Insert:
                        Interlocked.Increment(ref _inserts);
                        break;
                    case ChangeType.Update:
                        Interlocked.Increment(ref _updates);
                        break;
                    case ChangeType.Delete:
                        Interlocked.Increment(ref _deletes);
                        break;
                }
            }

            var changeEvent = new ChangeEvent
            {
                Timestamp = DateTime.UtcNow,
                ChangeType = ChangeType.Batch,
                ProcessingTime = processingTime,
                RecordCount = recordCount
            };

            _recentChanges.Enqueue(changeEvent);

            while (_recentChanges.Count > 1000)
            {
                _recentChanges.TryDequeue(out _);
            }
        }

        private double CalculateChangesPerMinute()
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var recentChanges = _recentChanges.Count(e => e.Timestamp >= oneMinuteAgo);
            return recentChanges;
        }

        public void CleanupOldData(DateTime cutoffTime)
        {
            while (_recentChanges.TryPeek(out var oldestEvent) && oldestEvent.Timestamp < cutoffTime)
            {
                _recentChanges.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    /// Represents performance metrics for a specific table
    /// </summary>
    public class PerformanceMetrics
    {
        private readonly ConcurrentQueue<PerformanceEvent> _recentEvents = new();
        private readonly object _lockObject = new object();
        private long _totalProcessingTimeTicks = 0;
        private TimeSpan _peakProcessingTime = TimeSpan.Zero;
        private long _totalEvents = 0;
        private PerformanceThresholds? _thresholds;

        public TimeSpan TotalProcessingTime => TimeSpan.FromTicks(_totalProcessingTimeTicks);
        public TimeSpan PeakProcessingTime => _peakProcessingTime;
        public long TotalEvents => _totalEvents;
        public TimeSpan AverageProcessingTime => _totalEvents > 0 ? TimeSpan.FromTicks(_totalProcessingTimeTicks / _totalEvents) : TimeSpan.Zero;

        public void RecordProcessingTime(TimeSpan processingTime)
        {
            Interlocked.Add(ref _totalProcessingTimeTicks, processingTime.Ticks);
            Interlocked.Increment(ref _totalEvents);

            if (processingTime > _peakProcessingTime)
            {
                lock (_lockObject)
                {
                    if (processingTime > _peakProcessingTime)
                    {
                        _peakProcessingTime = processingTime;
                    }
                }
            }

            var performanceEvent = new PerformanceEvent
            {
                Timestamp = DateTime.UtcNow,
                ProcessingTime = processingTime
            };

            _recentEvents.Enqueue(performanceEvent);

            while (_recentEvents.Count > 1000)
            {
                _recentEvents.TryDequeue(out _);
            }
        }

        public void RecordBatchProcessingTime(TimeSpan processingTime, int recordCount)
        {
            var perRecordTime = TimeSpan.FromTicks(processingTime.Ticks / Math.Max(1, recordCount));
            RecordProcessingTime(perRecordTime);
        }

        public void SetThresholds(PerformanceThresholds thresholds)
        {
            _thresholds = thresholds;
        }

        public bool HasThresholdViolations(out List<ThresholdViolation> violations)
        {
            violations = new List<ThresholdViolation>();

            if (_thresholds == null) return false;

            var currentAvg = AverageProcessingTime;
            var currentPeak = PeakProcessingTime;

            if (currentAvg > _thresholds.MaxAverageProcessingTime)
            {
                violations.Add(new ThresholdViolation
                {
                    ThresholdType = ThresholdType.AverageProcessingTime,
                    CurrentValue = currentAvg,
                    ThresholdValue = _thresholds.MaxAverageProcessingTime,
                    Severity = ThresholdSeverity.High
                });
            }

            if (currentPeak > _thresholds.MaxPeakProcessingTime)
            {
                violations.Add(new ThresholdViolation
                {
                    ThresholdType = ThresholdType.PeakProcessingTime,
                    CurrentValue = currentPeak,
                    ThresholdValue = _thresholds.MaxPeakProcessingTime,
                    Severity = ThresholdSeverity.Critical
                });
            }

            return violations.Count > 0;
        }

        public void CleanupOldData(DateTime cutoffTime)
        {
            while (_recentEvents.TryPeek(out var oldestEvent) && oldestEvent.Timestamp < cutoffTime)
            {
                _recentEvents.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    /// Represents change patterns for a specific table
    /// </summary>
    public class ChangePattern
    {
        private readonly ConcurrentQueue<ChangeEvent> _recentChanges = new();
        private readonly Dictionary<ChangeType, int> _changeTypeCounts = new();
        private readonly Dictionary<TimeSpan, int> _timeIntervalCounts = new();

        public void RecordChange(ChangeRecord changeRecord)
        {
            var changeEvent = new ChangeEvent
            {
                Timestamp = DateTime.UtcNow,
                ChangeType = changeRecord.ChangeType,
                ProcessingTime = TimeSpan.Zero,
                RecordCount = 1
            };

            _recentChanges.Enqueue(changeEvent);

            // Update change type counts
            lock (_changeTypeCounts)
            {
                if (_changeTypeCounts.ContainsKey(changeRecord.ChangeType))
                    _changeTypeCounts[changeRecord.ChangeType]++;
                else
                    _changeTypeCounts[changeRecord.ChangeType] = 1;
            }

            // Analyze time intervals
            AnalyzeTimeIntervals();

            while (_recentChanges.Count > 1000)
            {
                _recentChanges.TryDequeue(out _);
            }
        }

        public void RecordBatchChanges(IEnumerable<ChangeRecord> changeRecords)
        {
            var records = changeRecords.ToList();
            var changeEvent = new ChangeEvent
            {
                Timestamp = DateTime.UtcNow,
                ChangeType = ChangeType.Batch,
                ProcessingTime = TimeSpan.Zero,
                RecordCount = records.Count
            };

            _recentChanges.Enqueue(changeEvent);

            // Update change type counts for individual changes
            lock (_changeTypeCounts)
            {
                foreach (var record in records)
                {
                    if (_changeTypeCounts.ContainsKey(record.ChangeType))
                        _changeTypeCounts[record.ChangeType]++;
                    else
                        _changeTypeCounts[record.ChangeType] = 1;
                }
            }

            AnalyzeTimeIntervals();

            while (_recentChanges.Count > 1000)
            {
                _recentChanges.TryDequeue(out _);
            }
        }

        private void AnalyzeTimeIntervals()
        {
            if (_recentChanges.Count < 2) return;

            var changes = _recentChanges.OrderBy(e => e.Timestamp).ToList();
            
            for (int i = 1; i < changes.Count; i++)
            {
                var interval = changes[i].Timestamp - changes[i - 1].Timestamp;
                var roundedInterval = TimeSpan.FromSeconds(Math.Round(interval.TotalSeconds));

                lock (_timeIntervalCounts)
                {
                    if (_timeIntervalCounts.ContainsKey(roundedInterval))
                        _timeIntervalCounts[roundedInterval]++;
                    else
                        _timeIntervalCounts[roundedInterval] = 1;
                }
            }
        }

        public bool HasSignificantPatterns(out List<DetectedPattern> patterns)
        {
            patterns = new List<DetectedPattern>();

            // Detect change type patterns
            lock (_changeTypeCounts)
            {
                var totalChanges = _changeTypeCounts.Values.Sum();
                if (totalChanges > 0)
                {
                    foreach (var kvp in _changeTypeCounts)
                    {
                        var percentage = (double)kvp.Value / totalChanges * 100;
                        if (percentage > 80) // If one change type represents >80% of changes
                        {
                            patterns.Add(new DetectedPattern
                            {
                                PatternType = PatternType.ChangeTypeDominance,
                                Description = $"{kvp.Key} represents {percentage:F1}% of all changes",
                                Confidence = Math.Min(percentage / 100.0, 1.0),
                                Severity = PatternSeverity.Medium
                            });
                        }
                    }
                }
            }

            // Detect time interval patterns
            lock (_timeIntervalCounts)
            {
                var mostCommonInterval = _timeIntervalCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                if (mostCommonInterval.Value > 10) // If an interval appears more than 10 times
                {
                    patterns.Add(new DetectedPattern
                    {
                        PatternType = PatternType.TimeIntervalPattern,
                        Description = $"Most common interval: {mostCommonInterval.Key} (appears {mostCommonInterval.Value} times)",
                        Confidence = Math.Min((double)mostCommonInterval.Value / _recentChanges.Count, 1.0),
                        Severity = PatternSeverity.Low
                    });
                }
            }

            return patterns.Count > 0;
        }

        public void CleanupOldData(DateTime cutoffTime)
        {
            while (_recentChanges.TryPeek(out var oldestEvent) && oldestEvent.Timestamp < cutoffTime)
            {
                _recentChanges.TryDequeue(out _);
            }
        }
    }

    #region Supporting Classes and Enums

    public class ChangeEvent
    {
        public DateTime Timestamp { get; set; }
        public ChangeType ChangeType { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int RecordCount { get; set; }
    }

    public class PerformanceEvent
    {
        public DateTime Timestamp { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class PerformanceThresholds
    {
        public TimeSpan MaxAverageProcessingTime { get; set; } = TimeSpan.FromMilliseconds(100);
        public TimeSpan MaxPeakProcessingTime { get; set; } = TimeSpan.FromMilliseconds(500);
        public int MaxChangesPerMinute { get; set; } = 1000;
    }

    public class ThresholdViolation
    {
        public ThresholdType ThresholdType { get; set; }
        public TimeSpan CurrentValue { get; set; }
        public TimeSpan ThresholdValue { get; set; }
        public ThresholdSeverity Severity { get; set; }
    }

    public class DetectedPattern
    {
        public PatternType PatternType { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public PatternSeverity Severity { get; set; }
    }

    public class AggregatedMetrics
    {
        public int TotalTables { get; set; }
        public long TotalChanges { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public TimeSpan PeakProcessingTime { get; set; }
        public int TablesWithHighActivity { get; set; }
    }

    public enum ThresholdType
    {
        AverageProcessingTime,
        PeakProcessingTime,
        ChangesPerMinute
    }

    public enum ThresholdSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum PatternType
    {
        ChangeTypeDominance,
        TimeIntervalPattern,
        BatchSizePattern,
        ProcessingTimePattern
    }

    public enum PatternSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}

#region Event Arguments

namespace SQLDBEntityNotifier.Models
{
    public class PerformanceThresholdExceededEventArgs : EventArgs
    {
        public string TableName { get; }
        public List<ThresholdViolation> Violations { get; }

        public PerformanceThresholdExceededEventArgs(string tableName, List<ThresholdViolation> violations)
        {
            TableName = tableName;
            Violations = violations;
        }
    }

    public class ChangePatternDetectedEventArgs : EventArgs
    {
        public string TableName { get; }
        public List<DetectedPattern> Patterns { get; }

        public ChangePatternDetectedEventArgs(string tableName, List<DetectedPattern> patterns)
        {
            TableName = tableName;
            Patterns = patterns;
        }
    }

    public class MetricsAggregatedEventArgs : EventArgs
    {
        public AggregatedMetrics Metrics { get; }

        public MetricsAggregatedEventArgs(AggregatedMetrics metrics)
        {
            Metrics = metrics;
        }
    }
}

#endregion
