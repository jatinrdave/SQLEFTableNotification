using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Unified database notification service that works with SQL Server, MySQL, and PostgreSQL
    /// </summary>
    public class UnifiedDBNotificationService<T> : IDisposable where T : class, new()
    {
        private readonly ICDCProvider _cdcProvider;
        private readonly string _tableName;
        private readonly TimeSpan _pollingInterval;
        private readonly System.Timers.Timer? _timer;
        private string _currentPosition = "0";
        private bool _disposed = false;
        private readonly object _lockObject = new object();

        public event EventHandler<EnhancedRecordChangedEventArgs<T>>? OnChanged;
        public event EventHandler<ErrorEventArgs>? OnError;
        public event EventHandler<CDCHealthInfo>? OnHealthCheck;

        /// <summary>
        /// Initializes a new instance of the UnifiedDBNotificationService class
        /// </summary>
        public UnifiedDBNotificationService(ICDCProvider cdcProvider, string tableName, TimeSpan? pollingInterval = null)
        {
            _cdcProvider = cdcProvider ?? throw new ArgumentNullException(nameof(cdcProvider));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(30);
            _timer = new System.Timers.Timer(_pollingInterval.TotalMilliseconds);
        }

        /// <summary>
        /// Initializes a new instance of the UnifiedDBNotificationService class with database configuration
        /// </summary>
        public UnifiedDBNotificationService(DatabaseConfiguration configuration, string tableName, TimeSpan? pollingInterval = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _cdcProvider = CDCProviderFactory.CreateProvider(configuration);
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(30);
            _timer = new System.Timers.Timer(_pollingInterval.TotalMilliseconds);
        }

        /// <summary>
        /// Starts monitoring for changes
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            try
            {
                // Initialize the CDC provider
                var initialized = await _cdcProvider.InitializeAsync();
                if (!initialized)
                {
                    throw new InvalidOperationException("Failed to initialize CDC provider");
                }

                // Check if CDC is enabled for the table
                var isCDCEnabled = await _cdcProvider.IsCDCEnabledAsync(_tableName);
                if (!isCDCEnabled)
                {
                    // Try to enable CDC
                    var enabled = await _cdcProvider.EnableCDCAsync(_tableName);
                    if (!enabled)
                    {
                        throw new InvalidOperationException($"Failed to enable CDC for table {_tableName}");
                    }
                }

                // Get current position
                _currentPosition = await _cdcProvider.GetCurrentChangePositionAsync();

                // Set up timer
                _timer!.Elapsed += async (s, e) => await PollForChangesAsync();
                _timer.AutoReset = true;
                _timer.Start();

                // Perform initial health check
                await PerformHealthCheckAsync();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
                throw;
            }
        }

        /// <summary>
        /// Stops monitoring for changes
        /// </summary>
        public void StopMonitoring()
        {
            _timer?.Stop();
        }

        /// <summary>
        /// Polls for changes and raises events
        /// </summary>
        private async Task PollForChangesAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_disposed) return;
                }

                var changes = await _cdcProvider.GetTableChangesAsync(_tableName, _currentPosition);
                
                if (changes.Any())
                {
                    // Get detailed changes if available
                    var detailedChanges = await _cdcProvider.GetDetailedChangesAsync(_currentPosition);
                    
                    // Create enhanced event args
                    var eventArgs = CreateEnhancedEventArgs(detailedChanges);
                    OnChanged?.Invoke(this, eventArgs);
                    
                    // Update current position
                    var latestChange = changes.OrderByDescending(c => c.ChangePosition).First();
                    _currentPosition = latestChange.ChangePosition;
                }

                // Perform periodic health check
                if (DateTime.UtcNow.Second % 60 == 0) // Every minute
                {
                    await PerformHealthCheckAsync();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
            }
        }

        /// <summary>
        /// Manually polls for changes
        /// </summary>
        public async Task<List<EnhancedRecordChangedEventArgs<T>>> PollForChangesManuallyAsync()
        {
            try
            {
                var changes = await _cdcProvider.GetTableChangesAsync(_tableName, _currentPosition);
                var detailedChanges = await _cdcProvider.GetDetailedChangesAsync(_currentPosition);
                
                var eventArgs = CreateEnhancedEventArgs(detailedChanges);
                
                if (changes.Any())
                {
                    var latestChange = changes.OrderByDescending(c => c.ChangePosition).First();
                    _currentPosition = latestChange.ChangePosition;
                }
                
                return new List<EnhancedRecordChangedEventArgs<T>> { eventArgs };
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
                return new List<EnhancedRecordChangedEventArgs<T>>();
            }
        }

        /// <summary>
        /// Gets changes for multiple tables
        /// </summary>
        public async Task<Dictionary<string, List<EnhancedRecordChangedEventArgs<T>>>> GetMultiTableChangesAsync(IEnumerable<string> tableNames)
        {
            var result = new Dictionary<string, List<EnhancedRecordChangedEventArgs<T>>>();
            
            foreach (var tableName in tableNames)
            {
                try
                {
                    var changes = await _cdcProvider.GetTableChangesAsync(tableName, _currentPosition);
                    var detailedChanges = await _cdcProvider.GetDetailedChangesAsync(_currentPosition);
                    
                    var eventArgs = CreateEnhancedEventArgs(detailedChanges);
                    result[tableName] = new List<EnhancedRecordChangedEventArgs<T>> { eventArgs };
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, new ErrorEventArgs { Message = $"Failed to get changes for table {tableName}: {ex.Message}", Exception = ex });
                    result[tableName] = new List<EnhancedRecordChangedEventArgs<T>>();
                }
            }
            
            return result;
        }

        /// <summary>
        /// Validates the CDC configuration
        /// </summary>
        public async Task<CDCValidationResult> ValidateConfigurationAsync()
        {
            try
            {
                return await _cdcProvider.ValidateConfigurationAsync();
            }
            catch (Exception ex)
            {
                return new CDCValidationResult
                {
                    IsValid = false,
                    Errors = { $"Validation failed: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// Gets CDC health information
        /// </summary>
        public async Task<CDCHealthInfo> GetHealthInfoAsync()
        {
            try
            {
                return await _cdcProvider.GetHealthInfoAsync();
            }
            catch (Exception ex)
            {
                return new CDCHealthInfo
                {
                    Status = CDCHealthStatus.Unhealthy,
                    ErrorsLastHour = 1
                };
            }
        }

        /// <summary>
        /// Gets table schema information
        /// </summary>
        public async Task<TableSchema> GetTableSchemaAsync()
        {
            try
            {
                return await _cdcProvider.GetTableSchemaAsync(_tableName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get schema for table {_tableName}", ex);
            }
        }

        /// <summary>
        /// Cleans up old change data
        /// </summary>
        public async Task<bool> CleanupOldChangesAsync(TimeSpan retentionPeriod)
        {
            try
            {
                return await _cdcProvider.CleanupOldChangesAsync(retentionPeriod);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = $"Failed to cleanup old changes: {ex.Message}", Exception = ex });
                return false;
            }
        }

        /// <summary>
        /// Performs a health check and raises the OnHealthCheck event
        /// </summary>
        private async Task PerformHealthCheckAsync()
        {
            try
            {
                var healthInfo = await _cdcProvider.GetHealthInfoAsync();
                OnHealthCheck?.Invoke(this, healthInfo);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = $"Health check failed: {ex.Message}", Exception = ex });
            }
        }

        /// <summary>
        /// Creates enhanced event arguments from change records
        /// </summary>
        private EnhancedRecordChangedEventArgs<T> CreateEnhancedEventArgs(List<DetailedChangeRecord> changes)
        {
            var eventArgs = new EnhancedRecordChangedEventArgs<T>
            {
                Entities = new List<T>(), // This would be populated with actual entity data
                ChangeVersion = long.Parse(_currentPosition),
                ChangeDetectedAt = DateTime.UtcNow,
                DatabaseType = _cdcProvider.DatabaseType,
                ChangeIdentifier = _currentPosition,
                DatabaseChangeTimestamp = DateTime.UtcNow,
                ApplicationName = Environment.GetEnvironmentVariable("APP_NAME") ?? "UnifiedDBNotificationService",
                HostName = Environment.MachineName,
                Metadata = new Dictionary<string, object>
                {
                    ["ProviderType"] = _cdcProvider.GetType().Name,
                    ["TableName"] = _tableName,
                    ["ChangeCount"] = changes.Count
                }
            };

            // Process changes and populate operation-specific information
            if (changes.Any())
            {
                var firstChange = changes.First();
                eventArgs.Operation = firstChange.Operation;
                eventArgs.TransactionId = firstChange.TransactionId;
                eventArgs.IsBatchOperation = changes.Count > 1;
                eventArgs.BatchSequence = 1;

                // Group changes by operation type
                var operationGroups = changes.GroupBy(c => c.Operation).ToList();
                eventArgs.Metadata["OperationBreakdown"] = operationGroups.ToDictionary(g => g.Key.ToString(), g => g.Count());
            }

            return eventArgs;
        }

        /// <summary>
        /// Gets the current change position
        /// </summary>
        public string GetCurrentPosition()
        {
            return _currentPosition;
        }

        /// <summary>
        /// Sets the current change position
        /// </summary>
        public void SetCurrentPosition(string position)
        {
            if (string.IsNullOrWhiteSpace(position))
                throw new ArgumentException("Position cannot be null or empty", nameof(position));

            _currentPosition = position;
        }

        /// <summary>
        /// Resets the monitoring to start from the current database position
        /// </summary>
        public async Task ResetToCurrentPositionAsync()
        {
            try
            {
                _currentPosition = await _cdcProvider.GetCurrentChangePositionAsync();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = $"Failed to reset to current position: {ex.Message}", Exception = ex });
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
                lock (_lockObject)
                {
                    _disposed = true;
                }

                _timer?.Stop();
                _timer?.Dispose();
                _cdcProvider?.Dispose();
            }
        }
    }
}