using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Advanced change replay and recovery engine for CDC changes
    /// </summary>
    public class ChangeReplayEngine : IDisposable
    {
        private readonly Dictionary<string, List<ChangeRecord>> _changeHistory = new();
        private readonly Dictionary<string, ReplaySession> _activeSessions = new();
        private readonly ChangeReplayMetrics _metrics = new();
        private readonly object _lock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Event raised when replay starts
        /// </summary>
        public event EventHandler<ReplayStartedEventArgs>? OnReplayStarted;

        /// <summary>
        /// Event raised when replay completes
        /// </summary>
        public event EventHandler<ReplayCompletedEventArgs>? OnReplayCompleted;

        /// <summary>
        /// Event raised when replay fails
        /// </summary>
        public event EventHandler<ReplayFailedEventArgs>? OnReplayFailed;

        /// <summary>
        /// Event raised when recovery is performed
        /// </summary>
        public event EventHandler<RecoveryPerformedEventArgs>? OnRecoveryPerformed;

        /// <summary>
        /// Gets the change replay metrics
        /// </summary>
        public ChangeReplayMetrics Metrics => _metrics;

        /// <summary>
        /// Records a change for potential replay
        /// </summary>
        public void RecordChange(string tableName, ChangeRecord change)
        {
            if (_disposed) return;
            if (string.IsNullOrEmpty(tableName)) return;
            if (change == null) return;

            lock (_lock)
            {
                if (!_changeHistory.ContainsKey(tableName))
                {
                    _changeHistory[tableName] = new List<ChangeRecord>();
                }

                _changeHistory[tableName].Add(change);

                // Keep only last 10000 changes per table to prevent memory issues
                if (_changeHistory[tableName].Count > 10000)
                {
                    _changeHistory[tableName].RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Starts a replay session for a specific table
        /// </summary>
        public async Task<ReplaySession> StartReplayAsync(string tableName, ReplayOptions options)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeReplayEngine));

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            lock (_lock)
            {
                if (_activeSessions.ContainsKey(tableName))
                {
                    throw new InvalidOperationException($"Replay session already active for table {tableName}");
                }
            }

            var session = new ReplaySession(tableName, options);
            _activeSessions[tableName] = session;

            OnReplayStarted?.Invoke(this, new ReplayStartedEventArgs
            {
                TableName = tableName,
                Options = options,
                SessionId = session.SessionId
            });

            try
            {
                await session.StartAsync();
                return session;
            }
            catch (Exception ex)
            {
                _activeSessions.Remove(tableName);
                OnReplayFailed?.Invoke(this, new ReplayFailedEventArgs
                {
                    TableName = tableName,
                    Error = ex.Message,
                    SessionId = session.SessionId
                });
                throw;
            }
        }

        /// <summary>
        /// Stops a replay session
        /// </summary>
        public async Task StopReplayAsync(string tableName)
        {
            if (_disposed) return;

            lock (_lock)
            {
                if (_activeSessions.TryGetValue(tableName, out var session))
                {
                    _activeSessions.Remove(tableName);
                    _ = Task.Run(async () => await session.StopAsync());
                }
            }
        }

        /// <summary>
        /// Gets the replay status for a table
        /// </summary>
        public ReplayStatus GetReplayStatus(string tableName)
        {
            lock (_lock)
            {
                if (_activeSessions.TryGetValue(tableName, out var session))
                {
                    return session.Status;
                }
                return ReplayStatus.NotStarted;
            }
        }

        /// <summary>
        /// Gets available changes for replay
        /// </summary>
        public List<ChangeRecord> GetAvailableChanges(string tableName, DateTime? fromTime = null, DateTime? toTime = null)
        {
            lock (_lock)
            {
                if (!_changeHistory.ContainsKey(tableName))
                    return new List<ChangeRecord>();

                var changes = _changeHistory[tableName];

                if (fromTime.HasValue)
                {
                    changes = changes.Where(c => c.ChangeTimestamp >= fromTime.Value).ToList();
                }

                if (toTime.HasValue)
                {
                    changes = changes.Where(c => c.ChangeTimestamp <= toTime.Value).ToList();
                }

                return changes.OrderBy(c => c.ChangeTimestamp).ToList();
            }
        }

        /// <summary>
        /// Performs recovery for a table
        /// </summary>
        public async Task<RecoveryResult> PerformRecoveryAsync(string tableName, RecoveryOptions options)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeReplayEngine));

            var startTime = DateTime.UtcNow;
            var result = new RecoveryResult
            {
                TableName = tableName,
                StartTime = startTime,
                Success = true
            };

            try
            {
                // Get changes since the last known good state
                var changes = GetAvailableChanges(tableName, options.FromTime, options.ToTime);
                var filteredChanges = FilterChangesForRecovery(changes, options);

                result.TotalChanges = changes.Count;
                result.RecoveredChanges = filteredChanges.Count;

                // Create recovery session
                var recoverySession = new RecoverySession(tableName, options, filteredChanges);
                await recoverySession.ExecuteAsync();

                result.CompletedTime = DateTime.UtcNow;
                result.ProcessingTime = result.CompletedTime.Value - startTime;
                result.RecoverySessionId = recoverySession.SessionId;

                OnRecoveryPerformed?.Invoke(this, new RecoveryPerformedEventArgs
                {
                    TableName = tableName,
                    Result = result,
                    SessionId = recoverySession.SessionId
                });

                _metrics.RecordRecovery(result);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedTime = DateTime.UtcNow;
                result.ProcessingTime = result.CompletedTime.Value - startTime;

                _metrics.RecordRecoveryError(tableName, ex.Message, startTime);
                return result;
            }
        }

        /// <summary>
        /// Gets recovery recommendations for a table
        /// </summary>
        public List<RecoveryRecommendation> GetRecoveryRecommendations(string tableName)
        {
            var recommendations = new List<RecoveryRecommendation>();

            lock (_lock)
            {
                if (!_changeHistory.ContainsKey(tableName))
                {
                    recommendations.Add(new RecoveryRecommendation
                    {
                        Type = RecoveryRecommendationType.NoHistory,
                        Description = "No change history available for this table",
                        Priority = RecoveryPriority.Low
                    });
                    return recommendations;
                }

                var changes = _changeHistory[tableName];
                var recentChanges = changes.Where(c => c.ChangeTimestamp >= DateTime.UtcNow.AddHours(-1)).ToList();

                if (recentChanges.Count == 0)
                {
                    recommendations.Add(new RecoveryRecommendation
                    {
                        Type = RecoveryRecommendationType.NoRecentActivity,
                        Description = "No recent changes detected",
                        Priority = RecoveryPriority.Low
                    });
                }
                else if (recentChanges.Count > 100)
                {
                    recommendations.Add(new RecoveryRecommendation
                    {
                        Type = RecoveryRecommendationType.HighActivity,
                        Description = $"High change activity detected: {recentChanges.Count} changes in the last hour",
                        Priority = RecoveryPriority.Medium
                    });
                }

                // Check for failed operations
                var failedChanges = changes.Where(c => c.Metadata?.ContainsKey("Failed") == true).ToList();
                if (failedChanges.Any())
                {
                    recommendations.Add(new RecoveryRecommendation
                    {
                        Type = RecoveryRecommendationType.FailedOperations,
                        Description = $"{failedChanges.Count} failed operations detected",
                        Priority = RecoveryPriority.High
                    });
                }
            }

            return recommendations;
        }

        /// <summary>
        /// Clears change history for a table
        /// </summary>
        public void ClearHistory(string tableName)
        {
            lock (_lock)
            {
                if (_changeHistory.ContainsKey(tableName))
                {
                    _changeHistory[tableName].Clear();
                }
            }
        }

        /// <summary>
        /// Gets overall replay statistics
        /// </summary>
        public ReplayStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new ReplayStatistics
                {
                    TotalTables = _changeHistory.Count,
                    ActiveSessions = _activeSessions.Count,
                    TotalChanges = _changeHistory.Values.Sum(c => c.Count),
                    AverageChangesPerTable = _changeHistory.Count > 0 ? _changeHistory.Values.Average(c => c.Count) : 0
                };
            }
        }

        /// <summary>
        /// Filters changes for recovery based on options
        /// </summary>
        private List<ChangeRecord> FilterChangesForRecovery(List<ChangeRecord> changes, RecoveryOptions options)
        {
            var filteredChanges = changes;

            // Filter by operation type
            if (options.IncludeOperations?.Any() == true)
            {
                filteredChanges = filteredChanges.Where(c => options.IncludeOperations.Contains(c.Operation)).ToList();
            }

            // Filter by priority
            if (options.MinimumPriority.HasValue)
            {
                filteredChanges = filteredChanges.Where(c => 
                {
                    if (c.Metadata?.ContainsKey("Priority") == true && c.Metadata["Priority"] is ChangePriority priority)
                    {
                        return priority >= options.MinimumPriority.Value;
                    }
                    return true; // Include if no priority specified
                }).ToList();
            }

            // Filter by metadata
            if (options.MetadataFilters?.Any() == true)
            {
                foreach (var filter in options.MetadataFilters)
                {
                    filteredChanges = filteredChanges.Where(c => 
                    {
                        if (c.Metadata?.ContainsKey(filter.Key) == true)
                        {
                            var value = c.Metadata[filter.Key];
                            return filter.Value == null || Equals(value, filter.Value);
                        }
                        return false;
                    }).ToList();
                }
            }

            return filteredChanges;
        }

        /// <summary>
        /// Disposes the replay engine
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the replay engine
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lock)
                {
                    foreach (var session in _activeSessions.Values)
                    {
                        session?.Dispose();
                    }
                    _activeSessions.Clear();
                    _changeHistory.Clear();
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Replay session for a specific table
    /// </summary>
    public class ReplaySession : IDisposable
    {
        public string SessionId { get; }
        public string TableName { get; }
        public ReplayOptions Options { get; }
        public ReplayStatus Status { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public int ProcessedChanges { get; private set; }
        public int FailedChanges { get; private set; }
        public List<string> Errors { get; } = new();

        private bool _disposed = false;
        private bool _cancellationRequested = false;

        public ReplaySession(string tableName, ReplayOptions options)
        {
            SessionId = Guid.NewGuid().ToString();
            TableName = tableName;
            Options = options;
            Status = ReplayStatus.NotStarted;
        }

        public async Task StartAsync()
        {
            if (_disposed) return;

            Status = ReplayStatus.Running;
            StartTime = DateTime.UtcNow;

            try
            {
                // Simulate replay processing
                await ProcessReplayAsync();
                
                Status = ReplayStatus.Completed;
                EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Status = ReplayStatus.Failed;
                                 EndTime = DateTime.UtcNow;
                Errors.Add(ex.Message);
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_disposed) return;

            _cancellationRequested = true;
            Status = ReplayStatus.Stopped;
            EndTime = DateTime.UtcNow;

            await Task.CompletedTask;
        }

        private async Task ProcessReplayAsync()
        {
            // Simulate processing changes
            var totalChanges = Options.MaxChanges ?? 1000;
            var batchSize = Options.BatchSize ?? 100;

            for (int i = 0; i < totalChanges && !_cancellationRequested; i += batchSize)
            {
                var batch = Math.Min(batchSize, totalChanges - i);
                
                // Simulate processing delay
                await Task.Delay(Options.ProcessingDelay ?? TimeSpan.FromMilliseconds(10));
                
                ProcessedChanges += batch;

                // Simulate occasional failures
                if (Options.SimulateFailures && i % 200 == 0)
                {
                    FailedChanges++;
                    Errors.Add($"Simulated failure at change {i}");
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationRequested = true;
                Status = ReplayStatus.Disposed;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Recovery session for performing table recovery
    /// </summary>
    public class RecoverySession
    {
        public string SessionId { get; }
        public string TableName { get; }
        public RecoveryOptions Options { get; }
        public List<ChangeRecord> Changes { get; }
        public DateTime StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public RecoveryStatus Status { get; private set; }

        public RecoverySession(string tableName, RecoveryOptions options, List<ChangeRecord> changes)
        {
            SessionId = Guid.NewGuid().ToString();
            TableName = tableName;
            Options = options;
            Changes = changes;
            Status = RecoveryStatus.NotStarted;
        }

        public async Task ExecuteAsync()
        {
            Status = RecoveryStatus.Running;
            StartTime = DateTime.UtcNow;

            try
            {
                // Simulate recovery execution
                await Task.Delay(Options.SimulatedProcessingTime ?? TimeSpan.FromMilliseconds(100));
                
                Status = RecoveryStatus.Completed;
                EndTime = DateTime.UtcNow;
            }
            catch (Exception)
            {
                Status = RecoveryStatus.Failed;
                EndTime = DateTime.UtcNow;
                throw;
            }
        }
    }

    /// <summary>
    /// Replay options
    /// </summary>
    public class ReplayOptions
    {
        public int? MaxChanges { get; set; }
        public int? BatchSize { get; set; }
        public TimeSpan? ProcessingDelay { get; set; }
        public bool SimulateFailures { get; set; } = false;
        public ReplayMode Mode { get; set; } = ReplayMode.Sequential;
        public bool IncludeMetadata { get; set; } = true;
    }

    /// <summary>
    /// Recovery options
    /// </summary>
    public class RecoveryOptions
    {
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public List<ChangeOperation>? IncludeOperations { get; set; }
        public ChangePriority? MinimumPriority { get; set; }
        public Dictionary<string, object>? MetadataFilters { get; set; }
        public TimeSpan? SimulatedProcessingTime { get; set; }
        public bool ValidateBeforeRecovery { get; set; } = true;
        public bool CreateBackup { get; set; } = false;
    }

    /// <summary>
    /// Replay status
    /// </summary>
    public enum ReplayStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Stopped,
        Disposed
    }

    /// <summary>
    /// Recovery status
    /// </summary>
    public enum RecoveryStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed
    }

    /// <summary>
    /// Replay mode
    /// </summary>
    public enum ReplayMode
    {
        Sequential,
        Parallel,
        Batched
    }

    /// <summary>
    /// Recovery priority
    /// </summary>
    public enum RecoveryPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Recovery recommendation type
    /// </summary>
    public enum RecoveryRecommendationType
    {
        NoHistory,
        NoRecentActivity,
        HighActivity,
        FailedOperations,
        DataInconsistency,
        PerformanceIssue
    }

    /// <summary>
    /// Recovery recommendation
    /// </summary>
    public class RecoveryRecommendation
    {
        public RecoveryRecommendationType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public RecoveryPriority Priority { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Recovery result
    /// </summary>
    public class RecoveryResult
    {
        public string TableName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalChanges { get; set; }
        public int RecoveredChanges { get; set; }
        public string? RecoverySessionId { get; set; }
    }

    /// <summary>
    /// Replay statistics
    /// </summary>
    public class ReplayStatistics
    {
        public int TotalTables { get; set; }
        public int ActiveSessions { get; set; }
        public int TotalChanges { get; set; }
        public double AverageChangesPerTable { get; set; }
    }

    /// <summary>
    /// Event arguments for replay started events
    /// </summary>
    public class ReplayStartedEventArgs : EventArgs
    {
        public string TableName { get; set; } = string.Empty;
        public ReplayOptions Options { get; set; } = new();
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event arguments for replay completed events
    /// </summary>
    public class ReplayCompletedEventArgs : EventArgs
    {
        public string TableName { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public int ProcessedChanges { get; set; }
        public int FailedChanges { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Event arguments for replay failed events
    /// </summary>
    public class ReplayFailedEventArgs : EventArgs
    {
        public string TableName { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event arguments for recovery performed events
    /// </summary>
    public class RecoveryPerformedEventArgs : EventArgs
    {
        public string TableName { get; set; } = string.Empty;
        public RecoveryResult Result { get; set; } = new();
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Change replay metrics
    /// </summary>
    public class ChangeReplayMetrics
    {
        private readonly object _lock = new object();
        private int _totalReplays;
        private int _totalRecoveries;
        private int _totalReplayErrors;
        private int _totalRecoveryErrors;
        private TimeSpan _totalReplayTime;
        private TimeSpan _totalRecoveryTime;

        public int TotalReplays => _totalReplays;
        public int TotalRecoveries => _totalRecoveries;
        public int TotalReplayErrors => _totalReplayErrors;
        public int TotalRecoveryErrors => _totalRecoveryErrors;
        public TimeSpan TotalReplayTime => _totalReplayTime;
        public TimeSpan TotalRecoveryTime => _totalRecoveryTime;

        public TimeSpan AverageReplayTime => _totalReplays > 0 ? TimeSpan.FromTicks(_totalReplayTime.Ticks / _totalReplays) : TimeSpan.Zero;
        public TimeSpan AverageRecoveryTime => _totalRecoveries > 0 ? TimeSpan.FromTicks(_totalRecoveryTime.Ticks / _totalRecoveries) : TimeSpan.Zero;

        public void RecordReplay(TimeSpan duration)
        {
            lock (_lock)
            {
                _totalReplays++;
                _totalReplayTime += duration;
            }
        }

        public void RecordReplayError()
        {
            lock (_lock)
            {
                _totalReplayErrors++;
            }
        }

        public void RecordRecovery(RecoveryResult result)
        {
            lock (_lock)
            {
                _totalRecoveries++;
                _totalRecoveryTime += result.ProcessingTime;
            }
        }

        public void RecordRecoveryError(string tableName, string error, DateTime timestamp)
        {
            lock (_lock)
            {
                _totalRecoveryErrors++;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _totalReplays = 0;
                _totalRecoveries = 0;
                _totalReplayErrors = 0;
                _totalRecoveryErrors = 0;
                _totalReplayTime = TimeSpan.Zero;
                _totalRecoveryTime = TimeSpan.Zero;
            }
        }
    }
}
