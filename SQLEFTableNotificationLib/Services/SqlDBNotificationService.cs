using SQLEFTableNotification.Delegates;
using SQLEFTableNotification.Interfaces;
using SQLEFTableNotification.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Services
{
    /// <summary>
    /// Monitor table record changes at specified interval and notify changes to subscriber.
    /// </summary>
    /// <typeparam name="TChangeTableEntity"></typeparam>
    public class SqlDBNotificationService<TChangeTableEntity> : IDBNotificationService<TChangeTableEntity>, IDisposable where TChangeTableEntity : class, new()
    {

        #region members
        private const string Sql_CurrentVersion = @"SELECT ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0) as VersionCount";
        private const string Sql_TrackingOnChangeTable = @"SELECT ct.* FROM CHANGETABLE(CHANGES {0},{1}) ct WHERE  ct.SYS_CHANGE_VERSION <= {2}";
        private readonly string _changeContextName;
        private long _currentVersion;
        private ScheduledJobTimer _timer;
        private readonly TimeSpan _period;
        private readonly IChangeTableService<TChangeTableEntity> _changeTableService;
        private readonly string _tableName;
        private int _errorCount = 0;
        private readonly string? _connectionString = null;

        //private List<TableInfo> _model;
        #endregion

        #region Events
        public event Delegates.ErrorEventHandler OnError;

        public event ChangedEventHandler<TChangeTableEntity> OnChanged;

        //public event StatusEventHandler OnStatusChanged;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tableName">database table name to monitor</param>
        /// <param name="connectionString">database connection string with credentials</param>
        /// <param name="changeTableService">ChangeTableService which can read data from database.</param>
        /// <param name="version">Specify initial SYS_CHANGE_VERSION</param>
        /// <param name="period">Specify interval</param>
        /// <param name="recordIdentifier">record identifier to avoid database changes for own(not implemented).</param>
        public SqlDBNotificationService(string tableName, string connectionString, IChangeTableService<TChangeTableEntity> changeTableService, long version = -1L, TimeSpan? period = null, string recordIdentifier = "WMWebAPI")
        {
            _changeContextName = recordIdentifier;
            _currentVersion = version;
            _period = period.GetValueOrDefault(TimeSpan.FromSeconds(60D));
            _changeTableService = changeTableService;
            _tableName = tableName;
            _errorCount = 0;
            _connectionString = connectionString;
        }

        private async Task<long> QueryCurrentVersion()
        {
            return await _changeTableService.GetRecordCount(Sql_CurrentVersion);
        }
        #endregion
        private async Task SqlDBNotificationService_OnError(object sender, Models.ErrorEventArgs e)
        {
            if (OnError != null)
                OnError(sender, e);
        }

        private async Task SqlDBNotificationService_OnChanged(object sender, RecordChangedEventArgs<TChangeTableEntity> e)
        {
            if (OnChanged != null)
                OnChanged(sender, e);
        }

        public void Dispose()
        {

        }

        public async Task StartNotify()
        {
            _errorCount = 0;
            _currentVersion = _currentVersion == -1L ? await QueryCurrentVersion() : _currentVersion;
            _timer = new ScheduledJobTimer(JobContent, _period);
            _timer.Start();

        }

        private void EnableChangeTracking()
        {
            //Execute Query or stored procedure to enable change tracking on database and table level.
        }

        public async Task StopNotify()
        {
            await Task.Run(() => _timer.Stop());
        }

        private async void JobContent(object state)
        {
            try
            {
                long lastVersion = await QueryCurrentVersion();
                if (_currentVersion == lastVersion)
                {
                    return;
                }

                var buffer = new StringBuilder();

                var commandText = string.Format(Sql_TrackingOnChangeTable, _tableName, _currentVersion, lastVersion, _changeContextName);
                List<TChangeTableEntity> records = await _changeTableService.GetRecords(commandText);
                if (records != null && records.Count > 0)
                {
                    RecordChangedEventArgs<TChangeTableEntity> recordChangedEventArgs = new RecordChangedEventArgs<TChangeTableEntity>(records);// obj);
                    await SqlDBNotificationService_OnChanged(this, recordChangedEventArgs);
                }
                _currentVersion = lastVersion;
            }
            catch (Exception ex)
            {
                await SqlDBNotificationService_OnError(this, new Models.ErrorEventArgs(ex));
                _errorCount++;
            }
            finally
            {
                if (_errorCount > 20)
                {
                    _timer.Stop();
                    await SqlDBNotificationService_OnError(this, new Models.ErrorEventArgs(new Exception($"Stopped monitoring for {_connectionString}:{_tableName}")));
                }
                //do nothing.
            }
        }
    }
}
