using System;
using System.Threading.Tasks;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Monitors a SQL table and raises events on changes or errors.
    /// </summary>
    public class SqlDBNotificationService<T> : IDBNotificationService<T> where T : class, new()
    {
        private readonly IChangeTableService<T> _changeTableService;
        private readonly string _tableName;
        private readonly string _connectionString;
        private long _currentVersion;
        private int _errorCount = 0;
        private readonly TimeSpan _period;
        private System.Timers.Timer _timer;

        public event EventHandler<RecordChangedEventArgs<T>> OnChanged;
        public event EventHandler<ErrorEventArgs> OnError;

        public SqlDBNotificationService(IChangeTableService<T> changeTableService, string tableName, string connectionString, long version = -1L, TimeSpan? period = null)
        {
            _changeTableService = changeTableService;
            _tableName = tableName;
            _connectionString = connectionString;
            _currentVersion = version;
            _period = period ?? TimeSpan.FromSeconds(60);
        }

        public async Task StartNotify()
        {
            _errorCount = 0;
            _currentVersion = (_currentVersion == -1L) ? await QueryCurrentVersion() : _currentVersion;
            _timer = new System.Timers.Timer(_period.TotalMilliseconds);
            _timer.Elapsed += async (s, e) => await PollForChanges();
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

        private async Task PollForChanges()
        {
            long lastVersion = 0;
            try
            {
                lastVersion = await QueryCurrentVersion();
                if (_currentVersion == lastVersion)
                {
                    return;
                }

                string commandText = $"SELECT ct.* FROM CHANGETABLE(CHANGES {_tableName},{_currentVersion}) ct WHERE ct.SYS_CHANGE_VERSION <= {lastVersion}";
                var records = await _changeTableService.GetRecords(commandText);
                if (records != null && records.Count > 0)
                {
                    OnChanged?.Invoke(this, new RecordChangedEventArgs<T> { Entities = records });
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
                    _timer.Stop();
                    OnError?.Invoke(this, new ErrorEventArgs { Message = $"Stopped monitoring for {_connectionString}:{_tableName}", Exception = new Exception($"Stopped monitoring for {_connectionString}:{_tableName}") });
                }
            }
        }
    }
}
