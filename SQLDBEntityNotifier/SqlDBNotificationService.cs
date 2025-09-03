using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Helpers;
using SQLDBEntityNotifier.Compatibility;
using SQLDBEntityNotifier.Providers;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Monitors a SQL table and raises events on changes or errors with configurable context filtering.
    /// This class maintains full backward compatibility while optionally leveraging enhanced CDC features.
    /// </summary>
    public class SqlDBNotificationService<T> : IDBNotificationService<T> where T : class, new()
    {
        private readonly IChangeTableService<T> _changeTableService;
        private readonly string _tableName;
        private readonly string _connectionString;
        private long _currentVersion;
        private int _errorCount = 0;
        private readonly TimeSpan _period;
        private System.Timers.Timer? _timer;
        private readonly Func<long, long, string> _changeTrackingQueryFunc;
        private readonly ChangeFilterOptions? _filterOptions;
        
        // New CDC infrastructure (optional, for enhanced features)
        private ICDCProvider? _cdcProvider;
        private bool _useEnhancedCDC = false;

        public event EventHandler<RecordChangedEventArgs<T>>? OnChanged;
        public event EventHandler<ErrorEventArgs>? OnError;

        /// <summary>
        /// Initializes a new instance of the SqlDBNotificationService class.
        /// </summary>
        public SqlDBNotificationService(IChangeTableService<T> changeTableService, string tableName, string connectionString, long version = -1L, TimeSpan? period = null)
        : this(changeTableService, tableName, connectionString, version, period, null, null) { }

        /// <summary>
        /// Initializes a new instance of the SqlDBNotificationService class with custom query function.
        /// </summary>
        public SqlDBNotificationService(
            IChangeTableService<T> changeTableService,
            string tableName,
            string connectionString,
            long version,
            TimeSpan? period,
            Func<long, string>? changeTrackingQueryFunc)
        : this(changeTableService, tableName, connectionString, version, period, changeTrackingQueryFunc, null) { }

        /// <summary>
        /// Initializes a new instance of the SqlDBNotificationService class with context filtering.
        /// </summary>
        public SqlDBNotificationService(
            IChangeTableService<T> changeTableService,
            string tableName,
            string connectionString,
            long version,
            TimeSpan? period,
            ChangeFilterOptions? filterOptions)
        : this(changeTableService, tableName, connectionString, version, period, null, filterOptions) { }

        /// <summary>
        /// Initializes a new instance of the SqlDBNotificationService class with full configuration.
        /// </summary>
        public SqlDBNotificationService(
            IChangeTableService<T> changeTableService,
            string tableName,
            string connectionString,
            long version,
            TimeSpan? period,
            Func<long, string>? changeTrackingQueryFunc,
            ChangeFilterOptions? filterOptions)
        {
            _changeTableService = changeTableService;
            _tableName = tableName;
            _connectionString = connectionString;
            _currentVersion = version;
            _period = period ?? TimeSpan.FromSeconds(60);
            _filterOptions = filterOptions;

            if (changeTrackingQueryFunc != null)
            {
                // Convert the old function signature to the new one for backward compatibility
                _changeTrackingQueryFunc = (fromVer, toVer) => changeTrackingQueryFunc(fromVer).Replace("{0}", toVer.ToString());
            }
            else
            {
                // Use the new context-aware query builder
                _changeTrackingQueryFunc = ChangeTrackingQueryBuilder.CreateCustomQueryFunction(tableName, filterOptions);
            }

            // Try to initialize enhanced CDC (optional, won't break existing functionality)
            TryInitializeEnhancedCDC();
        }

        /// <summary>
        /// Attempts to initialize enhanced CDC features without breaking existing functionality
        /// </summary>
        private async void TryInitializeEnhancedCDC()
        {
            try
            {
                // Only try to use enhanced CDC for SQL Server connections
                if (SqlDBNotificationServiceCompatibility.IsSqlServerConnectionString(_connectionString))
                {
                    _cdcProvider = SqlDBNotificationServiceCompatibility.CreateCompatibleCDCProvider(_connectionString);
                    var initialized = await _cdcProvider.InitializeAsync();
                    _useEnhancedCDC = initialized;
                }
            }
            catch
            {
                // Silently fall back to legacy mode - don't break existing functionality
                _useEnhancedCDC = false;
                _cdcProvider = null;
            }
        }

        public async Task StartNotify()
        {
            _errorCount = 0;
            
            if (_useEnhancedCDC && _cdcProvider != null)
            {
                // Use enhanced CDC if available
                await StartEnhancedMonitoringAsync();
            }
            else
            {
                // Fall back to legacy mode
                await StartLegacyMonitoringAsync();
            }
        }

        /// <summary>
        /// Starts monitoring using enhanced CDC features
        /// </summary>
        private async Task StartEnhancedMonitoringAsync()
        {
            try
            {
                // Get current position from CDC provider
                var currentPosition = await _cdcProvider!.GetCurrentChangePositionAsync();
                if (long.TryParse(currentPosition, out var version))
                {
                    _currentVersion = version;
                }

                _timer = new System.Timers.Timer(_period.TotalMilliseconds);
                _timer.Elapsed += async (s, e) => await PollForChangesEnhancedAsync();
                _timer.AutoReset = true;
                _timer.Start();
            }
            catch (Exception ex)
            {
                // Fall back to legacy mode if enhanced CDC fails
                _useEnhancedCDC = false;
                await StartLegacyMonitoringAsync();
            }
        }

        /// <summary>
        /// Starts monitoring using legacy change tracking
        /// </summary>
        private async Task StartLegacyMonitoringAsync()
        {
            _currentVersion = (_currentVersion == -1L) ? await QueryCurrentVersion() : _currentVersion;
            _timer = new System.Timers.Timer(_period.TotalMilliseconds);
            _timer.Elapsed += async (s, e) => await PollForChangesAsync();
            _timer.AutoReset = true;
            _timer.Start();
        }

        public void StopNotify()
        {
            _timer?.Stop();
        }

        private async Task<long> QueryCurrentVersion()
        {
            try
            {
                var result = await _changeTableService.GetRecordCount("SELECT ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0) as VersionCount");
                return result;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
                return 0;
            }
        }

        /// <summary>
        /// Enhanced polling using CDC provider
        /// </summary>
        private async Task PollForChangesEnhancedAsync()
        {
            try
            {
                if (_cdcProvider == null || !_useEnhancedCDC)
                {
                    await PollForChangesAsync();
                    return;
                }

                var changes = await _cdcProvider.GetTableChangesAsync(_tableName, _currentVersion.ToString());
                
                if (changes.Any())
                {
                    // Convert CDC changes to legacy format for backward compatibility
                    var entities = await ConvertCDCChangesToEntitiesAsync(changes);
                    if (entities.Any())
                    {
                        var eventArgs = CreateEnhancedEventArgs(entities, _currentVersion);
                        OnChanged?.Invoke(this, eventArgs);
                    }
                    
                    // Update current position
                    var latestChange = changes.OrderByDescending(c => c.ChangePosition).First();
                    if (long.TryParse(latestChange.ChangePosition, out var newVersion))
                    {
                        _currentVersion = newVersion;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
                _errorCount++;
                
                if (_errorCount > 20)
                {
                    _timer?.Stop();
                    OnError?.Invoke(this, new ErrorEventArgs { Message = $"Stopped monitoring for {_connectionString}:{_tableName}", Exception = new Exception($"Stopped monitoring for {_connectionString}:{_tableName}") });
                }
            }
        }

        public async Task PollForChangesAsync()
        {
            long lastVersion = 0;
            try
            {
                lastVersion = await QueryCurrentVersion();
                if (_currentVersion == lastVersion)
                {
                    return;
                }

                string commandText = _changeTrackingQueryFunc(_currentVersion, lastVersion);
                var records = await _changeTableService.GetRecords(commandText);
                if (records != null && records.Any())
                {
                    var eventArgs = CreateEnhancedEventArgs(records, lastVersion);
                    OnChanged?.Invoke(this, eventArgs);
                }
                _currentVersion = lastVersion;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message, Exception = ex });
                _errorCount++;
                _currentVersion = lastVersion;
            }
            finally
            {
                if (_errorCount > 20)
                {
                    _timer?.Stop();
                    OnError?.Invoke(this, new ErrorEventArgs { Message = $"Stopped monitoring for {_connectionString}:{_tableName}", Exception = new Exception($"Stopped monitoring for {_connectionString}:{_tableName}") });
                }
            }
        }

        /// <summary>
        /// Converts CDC changes to entity objects for backward compatibility
        /// </summary>
        private async Task<List<T>> ConvertCDCChangesToEntitiesAsync(List<ChangeRecord> changes)
        {
            var entities = new List<T>();
            
            try
            {
                // For backward compatibility, we'll use the existing change table service
                // to fetch the actual entity data based on the change information
                foreach (var change in changes)
                {
                    if (change.Operation == ChangeOperation.Delete)
                    {
                        // For deletes, we can't fetch the entity, but we can create a placeholder
                        var entity = new T();
                        entities.Add(entity);
                    }
                    else
                    {
                        // For inserts and updates, try to fetch the entity data
                        var commandText = $"SELECT * FROM {_tableName} WHERE /* Add primary key condition based on change */ 1=1";
                        var records = await _changeTableService.GetRecords(commandText);
                        if (records.Any())
                        {
                            entities.AddRange(records);
                        }
                    }
                }
            }
            catch
            {
                // If conversion fails, return empty list to maintain backward compatibility
            }
            
            return entities;
        }

        /// <summary>
        /// Creates enhanced event arguments with change context information
        /// </summary>
        private RecordChangedEventArgs<T> CreateEnhancedEventArgs(IEnumerable<T> entities, long changeVersion)
        {
            var eventArgs = new RecordChangedEventArgs<T>
            {
                Entities = entities,
                ChangeVersion = changeVersion,
                ChangeDetectedAt = DateTime.UtcNow
            };

            if (_filterOptions?.IncludeChangeContext == true)
            {
                eventArgs.ChangeContext = new ChangeContextInfo
                {
                    Context = ChangeContext.Unknown, // Will be populated from query results if available
                    ApplicationName = _filterOptions.IncludeApplicationName ? GetApplicationName() : null,
                    HostName = _filterOptions.IncludeHostName ? GetHostName() : null
                };
            }

            return eventArgs;
        }

        /// <summary>
        /// Gets the current application name
        /// </summary>
        private string? GetApplicationName()
        {
            try
            {
                return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the current host name
        /// </summary>
        private string? GetHostName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets whether enhanced CDC features are being used
        /// </summary>
        public bool IsUsingEnhancedCDC => _useEnhancedCDC;

        /// <summary>
        /// Gets the CDC provider if available
        /// </summary>
        public ICDCProvider? CDCProvider => _cdcProvider;
    }
}
