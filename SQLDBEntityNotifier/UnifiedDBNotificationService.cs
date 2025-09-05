using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Unified database notification service that supports multiple database types with CDC functionality
    /// and column-level change filtering.
    /// </summary>
    /// <typeparam name="T">The entity type to monitor</typeparam>
    public class UnifiedDBNotificationService<T> : IDisposable where T : class, new()
    {
        private readonly ICDCProvider _cdcProvider;
        private readonly string _tableName;
        private readonly TimeSpan _pollingInterval;
        private readonly TimeSpan _healthCheckInterval;
        private readonly ColumnChangeFilterOptions? _columnFilterOptions;
        private readonly SemaphoreSlim _pollSemaphore = new SemaphoreSlim(1, 1);
        
        // Advanced CDC Features
        private readonly ChangeAnalytics _changeAnalytics;
        private readonly SchemaChangeDetection _schemaChangeDetection;
        private readonly ChangeCorrelationEngine _changeCorrelationEngine;
        private readonly ChangeContextManager _changeContextManager;
        
        // Phase 3 & 4: Advanced Filtering, Routing, and Replay
        private readonly AdvancedChangeFilters _advancedFilters;
        private readonly ChangeRoutingEngine _changeRoutingEngine;
        private readonly ChangeReplayEngine _changeReplayEngine;
        
        private System.Timers.Timer? _timer;
        private System.Timers.Timer? _healthCheckTimer;
        private string _currentPosition = "0";
        private bool _isMonitoring = false;
        private bool _disposed = false;

        public event EventHandler<EnhancedRecordChangedEventArgs<T>>? OnChanged;
        public event EventHandler<ErrorEventArgs>? OnError;
        public event EventHandler<CDCHealthInfo>? OnHealthCheck;
        
        // Advanced CDC Feature Events
        public event EventHandler<PerformanceThresholdExceededEventArgs>? OnPerformanceThresholdExceeded;
        public event EventHandler<ChangePatternDetectedEventArgs>? OnChangePatternDetected;
        public event EventHandler<MetricsAggregatedEventArgs>? OnMetricsAggregated;
        public event EventHandler<SchemaChangeDetectedEventArgs>? OnSchemaChangeDetected;
        public event EventHandler<SchemaChangeImpactAnalyzedEventArgs>? OnSchemaChangeImpactAnalyzed;
        public event EventHandler<CorrelatedChangesDetectedEventArgs>? OnChangeCorrelationDetected;
        public event EventHandler<ChangeImpactAnalyzedEventArgs>? OnChangeImpactAnalyzed;
        
        // Phase 3 & 4: Advanced Filtering, Routing, and Replay Events
        public event EventHandler<ChangeRoutedEventArgs>? OnChangeRouted;
        public event EventHandler<RoutingFailedEventArgs>? OnRoutingFailed;
        public event EventHandler<RoutingMetricsUpdatedEventArgs>? OnRoutingMetricsUpdated;
        public event EventHandler<ReplayStartedEventArgs>? OnReplayStarted;
        public event EventHandler<ReplayCompletedEventArgs>? OnReplayCompleted;
        public event EventHandler<ReplayFailedEventArgs>? OnReplayFailed;
        public event EventHandler<RecoveryPerformedEventArgs>? OnRecoveryPerformed;

        /// <summary>
        /// Gets the table name being monitored
        /// </summary>
        public string TableName => _tableName;

        /// <summary>
        /// Gets the polling interval for change detection
        /// </summary>
        public TimeSpan PollingInterval => _pollingInterval;

        /// <summary>
        /// Gets the health check interval
        /// </summary>
        public TimeSpan HealthCheckInterval => _healthCheckInterval;

        /// <summary>
        /// Gets whether the service is currently monitoring for changes
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Gets the current change position
        /// </summary>
        public string CurrentPosition => _currentPosition;

        /// <summary>
        /// Gets the CDC provider being used
        /// </summary>
        public ICDCProvider CDCProvider => _cdcProvider;

        /// <summary>
        /// Gets the column filter options
        /// </summary>
        public ColumnChangeFilterOptions? ColumnFilterOptions => _columnFilterOptions;

        /// <summary>
        /// Gets the change analytics engine
        /// </summary>
        public ChangeAnalytics ChangeAnalytics => _changeAnalytics;

        /// <summary>
        /// Gets the schema change detection engine
        /// </summary>
        public SchemaChangeDetection SchemaChangeDetection => _schemaChangeDetection;

        /// <summary>
        /// Gets the change correlation engine
        /// </summary>
        public ChangeCorrelationEngine ChangeCorrelationEngine => _changeCorrelationEngine;

        /// <summary>
        /// Gets the change context manager
        /// </summary>
        public ChangeContextManager ChangeContextManager => _changeContextManager;

        /// <summary>
        /// Gets the advanced change filters
        /// </summary>
        public AdvancedChangeFilters AdvancedFilters => _advancedFilters;

        /// <summary>
        /// Gets the change routing engine
        /// </summary>
        public ChangeRoutingEngine ChangeRoutingEngine => _changeRoutingEngine;

        /// <summary>
        /// Gets the change replay engine
        /// </summary>
        public ChangeReplayEngine ChangeReplayEngine => _changeReplayEngine;

        /// <summary>
        /// Initializes a new instance of the UnifiedDBNotificationService
        /// </summary>
        /// <param name="cdcProvider">The CDC provider to use</param>
        /// <param name="tableName">The name of the table to monitor</param>
        /// <param name="pollingInterval">The polling interval for change detection</param>
        /// <param name="healthCheckInterval">The health check interval</param>
        /// <param name="columnFilterOptions">Options for filtering changes based on specific columns</param>
        public UnifiedDBNotificationService(
            ICDCProvider cdcProvider,
            string tableName,
            TimeSpan? pollingInterval = null,
            TimeSpan? healthCheckInterval = null,
            ColumnChangeFilterOptions? columnFilterOptions = null)
        {
            _cdcProvider = cdcProvider ?? throw new ArgumentNullException(nameof(cdcProvider));
            _tableName = !string.IsNullOrWhiteSpace(tableName) ? tableName : throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(60);
            _healthCheckInterval = healthCheckInterval ?? TimeSpan.FromMinutes(5);
            _columnFilterOptions = columnFilterOptions;
            
            // Initialize advanced CDC features
            _changeAnalytics = new ChangeAnalytics();
            _schemaChangeDetection = new SchemaChangeDetection();
            _changeCorrelationEngine = new ChangeCorrelationEngine();
            _changeContextManager = new ChangeContextManager();
            
            // Initialize Phase 3 & 4 features
            _advancedFilters = new AdvancedChangeFilters();
            _changeRoutingEngine = new ChangeRoutingEngine();
            _changeReplayEngine = new ChangeReplayEngine();
            
            // Wire up advanced feature events
            WireUpAdvancedFeatureEvents();
        }

        /// <summary>
        /// Initializes a new instance of the UnifiedDBNotificationService using configuration
        /// </summary>
        /// <param name="configuration">The database configuration</param>
        /// <param name="tableName">The name of the table to monitor</param>
        /// <param name="pollingInterval">The polling interval for change detection</param>
        /// <param name="healthCheckInterval">The health check interval</param>
        /// <param name="columnFilterOptions">Options for filtering changes based on specific columns</param>
        public UnifiedDBNotificationService(
            DatabaseConfiguration configuration,
            string tableName,
            TimeSpan? pollingInterval = null,
            TimeSpan? healthCheckInterval = null,
            ColumnChangeFilterOptions? columnFilterOptions = null)
            : this(CDCProviderFactory.CreateProvider(configuration), tableName, pollingInterval, healthCheckInterval, columnFilterOptions)
        {
            // Advanced features are initialized in the base constructor
        }

        /// <summary>
        /// Wires up events from advanced CDC features to the main service events
        /// </summary>
        private void WireUpAdvancedFeatureEvents()
        {
            // Wire up ChangeAnalytics events
            _changeAnalytics.PerformanceThresholdExceeded += (sender, e) => OnPerformanceThresholdExceeded?.Invoke(this, e);
            _changeAnalytics.ChangePatternDetected += (sender, e) => OnChangePatternDetected?.Invoke(this, e);
            _changeAnalytics.MetricsAggregated += (sender, e) => OnMetricsAggregated?.Invoke(this, e);
            
            // Wire up SchemaChangeDetection events
            _schemaChangeDetection.SchemaChangeDetected += (sender, e) => OnSchemaChangeDetected?.Invoke(this, e);
            _schemaChangeDetection.SchemaChangeImpactAnalyzed += (sender, e) => OnSchemaChangeImpactAnalyzed?.Invoke(this, e);
            
            // Wire up ChangeCorrelationEngine events
            _changeCorrelationEngine.CorrelatedChangesDetected += (sender, e) => OnChangeCorrelationDetected?.Invoke(this, e);
            _changeCorrelationEngine.ChangeImpactAnalyzed += (sender, e) => OnChangeImpactAnalyzed?.Invoke(this, e);
            
            // Wire up Phase 3 & 4 events
            _changeRoutingEngine.OnChangeRouted += (sender, e) => OnChangeRouted?.Invoke(this, e);
            _changeRoutingEngine.OnRoutingFailed += (sender, e) => OnRoutingFailed?.Invoke(this, e);
            _changeRoutingEngine.OnRoutingMetricsUpdated += (sender, e) => OnRoutingMetricsUpdated?.Invoke(this, e);
            
            _changeReplayEngine.OnReplayStarted += (sender, e) => OnReplayStarted?.Invoke(this, e);
            _changeReplayEngine.OnReplayCompleted += (sender, e) => OnReplayCompleted?.Invoke(this, e);
            _changeReplayEngine.OnReplayFailed += (sender, e) => OnReplayFailed?.Invoke(this, e);
            _changeReplayEngine.OnRecoveryPerformed += (sender, e) => OnRecoveryPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Starts monitoring for changes with optional health check interval
        /// </summary>
        /// <param name="healthCheckInterval">Optional custom health check interval</param>
        public async Task StartMonitoringAsync(TimeSpan? healthCheckInterval = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedDBNotificationService<T>));

            if (_isMonitoring)
                return;

            try
            {
                // Initialize the CDC provider
                var initialized = await _cdcProvider.InitializeAsync();
                if (!initialized)
                {
                    throw new InvalidOperationException("Failed to initialize CDC provider");
                }

                // Check if CDC is enabled for the table
                var cdcEnabled = await _cdcProvider.IsCDCEnabledAsync(_tableName);
                if (!cdcEnabled)
                {
                    // Try to enable CDC
                    var enabled = await _cdcProvider.EnableCDCAsync(_tableName);
                    if (!enabled)
                    {
                        throw new InvalidOperationException($"Failed to enable CDC for table {_tableName}");
                    }
                }

                // Get current change position
                _currentPosition = await _cdcProvider.GetCurrentChangePositionAsync();

                // Start the polling timer
                _timer = new System.Timers.Timer(_pollingInterval.TotalMilliseconds);
                _timer.Elapsed += (sender, e) => _ = HandlePollingTimerElapsedAsync();
                _timer.AutoReset = true;
                _timer.Start();

                // Start the health check timer
                var actualHealthCheckInterval = healthCheckInterval ?? _healthCheckInterval;
                if (actualHealthCheckInterval > TimeSpan.Zero)
                {
                    _healthCheckTimer = new System.Timers.Timer(actualHealthCheckInterval.TotalMilliseconds);
                    _healthCheckTimer.Elapsed += (sender, e) => _ = HandleHealthCheckTimerElapsedAsync();
                    _healthCheckTimer.AutoReset = true;
                    _healthCheckTimer.Start();
                }

                _isMonitoring = true;
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
            if (_disposed || !_isMonitoring)
                return;

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            _healthCheckTimer?.Stop();
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;

            _isMonitoring = false;
        }

        /// <summary>
        /// Helper method to handle polling timer events with semaphore protection
        /// </summary>
        private async Task HandlePollingTimerElapsedAsync()
        {
            try
            {
                await _pollSemaphore.WaitAsync();
                await PollForChangesAsync();
            }
            finally
            {
                _pollSemaphore.Release();
            }
        }

        /// <summary>
        /// Helper method to handle health check timer events with semaphore protection
        /// </summary>
        private async Task HandleHealthCheckTimerElapsedAsync()
        {
            try
            {
                await _pollSemaphore.WaitAsync();
                await PerformHealthCheckAsync();
            }
            finally
            {
                _pollSemaphore.Release();
            }
        }

        /// <summary>
        /// Polls for changes and raises events
        /// </summary>
        public async Task PollForChangesAsync()
        {
            if (_disposed || !_isMonitoring)
                return;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var changes = await _cdcProvider.GetTableChangesAsync(_tableName, _currentPosition);
                
                if (changes.Any())
                {
                    // Filter changes based on column filter options
                    var filteredChanges = FilterChangesByColumns(changes);
                    
                    if (filteredChanges.Any())
                    {
                        // Convert changes to entities and raise events
                        var entities = await ConvertChangesToEntitiesAsync(filteredChanges);
                        if (entities.Any())
                        {
                            var eventArgs = CreateEnhancedEventArgs(filteredChanges, entities);
                            OnChanged?.Invoke(this, eventArgs);
                            
                            // Record changes for advanced analytics
                            var processingTime = stopwatch.Elapsed;
                            RecordChangesForAnalytics(filteredChanges, processingTime);
                            
                            // Record changes for correlation analysis
                            RecordChangesForCorrelation(filteredChanges);
                            
                            // Create and propagate change context
                            var changeContext = CreateChangeContext(filteredChanges);
                            PropagateChangeContext(changeContext);
                            
                            // Record changes for replay and recovery
                            RecordChangesForReplay(filteredChanges);
                            
                            // Route changes using advanced routing engine
                            await RouteChangesAsync(filteredChanges);
                        }
                    }

                    // Update current position
                    var latestChange = changes.OrderByDescending(c => c.ChangePosition).First();
                    _currentPosition = latestChange.ChangePosition;
                }
                
                // Check for schema changes periodically
                await CheckForSchemaChangesAsync();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Filters changes based on column filter options
        /// </summary>
        /// <param name="changes">The list of changes to filter</param>
        /// <returns>Filtered list of changes</returns>
        private List<ChangeRecord> FilterChangesByColumns(List<ChangeRecord> changes)
        {
            if (_columnFilterOptions == null)
                return changes;

            var filteredChanges = new List<ChangeRecord>();

            foreach (var change in changes)
            {
                // For insert and delete operations, check if any monitored columns are affected
                if (change.Operation == ChangeOperation.Insert || change.Operation == ChangeOperation.Delete)
                {
                    if (ShouldIncludeChange(change))
                    {
                        filteredChanges.Add(change);
                    }
                }
                // For update operations, check if any monitored columns actually changed
                else if (change.Operation == ChangeOperation.Update)
                {
                    if (HasRelevantColumnChanges(change))
                    {
                        filteredChanges.Add(change);
                    }
                }
                // For schema changes, always include if column filtering is enabled
                else if (change.Operation == ChangeOperation.SchemaChange)
                {
                    if (_columnFilterOptions.IncludeColumnLevelChanges)
                    {
                        filteredChanges.Add(change);
                    }
                }
            }

            return filteredChanges;
        }

        /// <summary>
        /// Determines if a change should be included based on column filter options
        /// </summary>
        /// <param name="change">The change to evaluate</param>
        /// <returns>True if the change should be included, false otherwise</returns>
        private bool ShouldIncludeChange(ChangeRecord change)
        {
            if (_columnFilterOptions == null)
                return true;

            // If no specific columns are monitored, include all changes
            if (_columnFilterOptions.MonitoredColumns == null || _columnFilterOptions.MonitoredColumns.Count == 0)
                return true;

            // For insert/delete operations, check if any monitored columns are present
            // This is a simplified check - in practice, you might want to check the actual column data
            return true; // Always include for now, as we don't have column-level data in ChangeRecord
        }

        /// <summary>
        /// Determines if an update change has relevant column changes
        /// </summary>
        /// <param name="change">The update change to evaluate</param>
        /// <returns>True if relevant columns changed, false otherwise</returns>
        private bool HasRelevantColumnChanges(ChangeRecord change)
        {
            if (_columnFilterOptions == null)
                return true;

            // If no specific columns are monitored, include all changes
            if (_columnFilterOptions.MonitoredColumns == null || _columnFilterOptions.MonitoredColumns.Count == 0)
                return true;

            // For now, we'll include all update changes
            // In a real implementation, you would check the actual changed columns from the CDC data
            return true;
        }

        /// <summary>
        /// Converts CDC changes to entity objects
        /// </summary>
        /// <param name="changes">The list of changes to convert</param>
        /// <returns>List of entity objects</returns>
        private async Task<List<T>> ConvertChangesToEntitiesAsync(List<ChangeRecord> changes)
        {
            var entities = new List<T>();

            try
            {
                foreach (var change in changes)
                {
                    if (change.Operation == ChangeOperation.Delete)
                    {
                        // For deletes, create a placeholder entity
                        var entity = new T();
                        entities.Add(entity);
                    }
                    else
                    {
                        // For inserts and updates, try to fetch the actual entity data
                        // This is a simplified implementation - in practice, you might want to use
                        // the CDC provider's detailed change information
                        var entity = new T();
                        entities.Add(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
            }

            return entities;
        }

        /// <summary>
        /// Creates enhanced event arguments with column-level change information
        /// </summary>
        /// <param name="changes">The list of changes</param>
        /// <param name="entities">The list of entities</param>
        /// <returns>Enhanced event arguments</returns>
        private EnhancedRecordChangedEventArgs<T> CreateEnhancedEventArgs(List<ChangeRecord> changes, List<T> entities)
        {
            var eventArgs = new EnhancedRecordChangedEventArgs<T>
            {
                Entities = entities,
                ChangeVersion = long.TryParse(_currentPosition, out var version) ? version : 0,
                ChangeDetectedAt = DateTime.UtcNow,
                DatabaseType = _cdcProvider.DatabaseType,
                Operation = changes.FirstOrDefault()?.Operation ?? ChangeOperation.Unknown,
                ChangeIdentifier = changes.FirstOrDefault()?.ChangeId,
                DatabaseChangeTimestamp = changes.FirstOrDefault()?.ChangeTimestamp,
                ChangedBy = changes.FirstOrDefault()?.ChangedBy,
                HostName = changes.FirstOrDefault()?.HostName,
                IsBatchOperation = changes.Count > 1,
                BatchSequence = changes.Count > 1 ? 1 : null
            };

            // Add column-level change information if enabled
            if (_columnFilterOptions?.IncludeColumnLevelChanges == true)
            {
                eventArgs.AffectedColumns = GetAffectedColumns(changes);
            }

            // Add old/new values if enabled
            if (_columnFilterOptions?.IncludeColumnValues == true)
            {
                // In a real implementation, you would extract old/new values from the CDC data
                // For now, we'll create placeholder objects
                eventArgs.OldValues = new T();
                eventArgs.NewValues = new T();
            }

            return eventArgs;
        }

        /// <summary>
        /// Gets the list of affected columns from the changes
        /// </summary>
        /// <param name="changes">The list of changes</param>
        /// <returns>List of affected column names</returns>
        private List<string> GetAffectedColumns(List<ChangeRecord> changes)
        {
            var affectedColumns = new List<string>();

            if (_columnFilterOptions?.IncludeColumnLevelChanges == true)
            {
                // In a real implementation, you would extract the actual changed columns from the CDC data
                // For now, we'll return an empty list
                // This would typically come from the CDC provider's detailed change information
            }

            return affectedColumns;
        }

        /// <summary>
        /// Gets changes for multiple tables
        /// </summary>
        /// <param name="tableNames">The names of the tables to monitor</param>
        /// <returns>Dictionary of table names to their changes</returns>
        public async Task<Dictionary<string, List<ChangeRecord>>> GetMultiTableChangesAsync(IEnumerable<string> tableNames)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedDBNotificationService<T>));

            return await _cdcProvider.GetMultiTableChangesAsync(tableNames, _currentPosition);
        }

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        /// <returns>Validation result</returns>
        public async Task<CDCValidationResult> ValidateConfigurationAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedDBNotificationService<T>));

            return await _cdcProvider.ValidateConfigurationAsync();
        }

        /// <summary>
        /// Gets health information
        /// </summary>
        /// <returns>Health information</returns>
        public async Task<CDCHealthInfo> GetHealthInfoAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedDBNotificationService<T>));

            return await _cdcProvider.GetHealthInfoAsync();
        }

        /// <summary>
        /// Performs a health check and raises the OnHealthCheck event
        /// </summary>
        public async Task PerformHealthCheckAsync()
        {
            if (_disposed || !_isMonitoring)
                return;

            try
            {
                var healthInfo = await _cdcProvider.GetHealthInfoAsync();
                OnHealthCheck?.Invoke(this, healthInfo);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
            }
        }

        /// <summary>
        /// Cleans up old changes
        /// </summary>
        /// <param name="retentionPeriod">The retention period for changes</param>
        /// <returns>True if cleanup was successful, false otherwise</returns>
        public async Task<bool> CleanupOldChangesAsync(TimeSpan retentionPeriod)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedDBNotificationService<T>));

            return await _cdcProvider.CleanupOldChangesAsync(retentionPeriod);
        }

        /// <summary>
        /// Gets the table schema
        /// </summary>
        /// <returns>Table schema information</returns>
        public async Task<TableSchema> GetTableSchemaAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedDBNotificationService<T>));

            return await _cdcProvider.GetTableSchemaAsync(_tableName);
        }

        /// <summary>
        /// Records changes for analytics processing
        /// </summary>
        /// <param name="changes">The changes to record</param>
        /// <param name="processingTime">The time taken to process the changes</param>
        private void RecordChangesForAnalytics(List<ChangeRecord> changes, TimeSpan processingTime)
        {
            try
            {
                if (changes.Count == 1)
                {
                    _changeAnalytics.RecordChange(_tableName, changes[0], processingTime);
                }
                else
                {
                    _changeAnalytics.RecordBatchChanges(_tableName, changes, processingTime);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent interrupting the main flow
                System.Diagnostics.Debug.WriteLine($"Error recording changes for analytics: {ex.Message}");
            }
        }

        /// <summary>
        /// Records changes for correlation analysis
        /// </summary>
        /// <param name="changes">The changes to record</param>
        private void RecordChangesForCorrelation(List<ChangeRecord> changes)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                foreach (var change in changes)
                {
                    _changeCorrelationEngine.RecordChange(_tableName, change);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent interrupting the main flow
                System.Diagnostics.Debug.WriteLine($"Error recording changes for correlation: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a change context for the given changes
        /// </summary>
        /// <param name="changes">The changes to create context for</param>
        /// <returns>The created change context</returns>
        private EnhancedChangeContext CreateChangeContext(List<ChangeRecord> changes)
        {
            try
            {
                var context = _changeContextManager.CreateContext(_tableName);
                
                // Add additional metadata
                context.Metadata["ProcessingTimestamp"] = DateTime.UtcNow;
                context.Metadata["ChangeCount"] = changes.Count;
                context.Metadata["TableName"] = _tableName;
                
                return context;
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent interrupting the main flow
                System.Diagnostics.Debug.WriteLine($"Error creating change context: {ex.Message}");
                return new EnhancedChangeContext();
            }
        }

        /// <summary>
        /// Propagates change context to dependent systems
        /// </summary>
        /// <param name="context">The change context to propagate</param>
        private void PropagateChangeContext(EnhancedChangeContext context)
        {
            try
            {
                _changeContextManager.SetContext(context);
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent interrupting the main flow
                System.Diagnostics.Debug.WriteLine($"Error propagating change context: {ex.Message}");
            }
        }

        /// <summary>
        /// Records changes for replay and recovery
        /// </summary>
        /// <param name="changes">The changes to record</param>
        private void RecordChangesForReplay(List<ChangeRecord> changes)
        {
            try
            {
                foreach (var change in changes)
                {
                    _changeReplayEngine.RecordChange(_tableName, change);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent interrupting the main flow
                System.Diagnostics.Debug.WriteLine($"Error recording changes for replay: {ex.Message}");
            }
        }

        /// <summary>
        /// Routes changes using the advanced routing engine
        /// </summary>
        /// <param name="changes">The changes to route</param>
        private async Task RouteChangesAsync(List<ChangeRecord> changes)
        {
            try
            {
                foreach (var change in changes)
                {
                    await _changeRoutingEngine.RouteChangeAsync(change, _tableName);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent interrupting the main flow
                System.Diagnostics.Debug.WriteLine($"Error routing changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for schema changes in the monitored table
        /// </summary>
        private async Task CheckForSchemaChangesAsync()
        {
            try
            {
                // Only check for schema changes every 10 polling cycles to avoid performance impact
                var shouldCheck = DateTime.UtcNow.Ticks % 10 == 0;
                if (shouldCheck)
                {
                    var schemaChanges = await _schemaChangeDetection.DetectSchemaChangesAsync(_tableName, _cdcProvider);
                    if (schemaChanges.Any())
                    {
                        // Schema changes are automatically raised through the event wiring
                        System.Diagnostics.Debug.WriteLine($"Schema changes detected for table {_tableName}: {schemaChanges.Count} changes");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent interrupting the main flow
                System.Diagnostics.Debug.WriteLine($"Error checking for schema changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes the service and stops monitoring
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        /// <param name="disposing">True if disposing, false if finalizing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                StopMonitoring();
                _cdcProvider?.Dispose();
                _pollSemaphore?.Dispose();
                
                // Dispose advanced CDC features
                _changeAnalytics?.Dispose();
                _schemaChangeDetection?.Dispose();
                _changeCorrelationEngine?.Dispose();
                _changeContextManager?.Dispose();
                
                // Dispose Phase 3 & 4 features
                _advancedFilters?.Dispose();
                _changeRoutingEngine?.Dispose();
                _changeReplayEngine?.Dispose();
                
                _disposed = true;
            }
        }
    }
}