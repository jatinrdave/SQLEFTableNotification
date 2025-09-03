using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Helpers;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Monitors a SQL table and raises events on changes or errors with configurable context filtering.
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
        }

        public async Task StartNotify()
        {
            _errorCount = 0;
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
    }
}
