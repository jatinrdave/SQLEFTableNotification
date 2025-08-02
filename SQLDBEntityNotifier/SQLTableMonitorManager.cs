using System;
using System.Threading.Tasks;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Coordinates monitoring of a SQL table and handles notifications.
    /// </summary>
    public class SQLTableMonitorManager : ISQLTableMonitorManager
    {
        private readonly IChangeTableService<object> _changeTableService;
        private readonly IDBNotificationService<object> _notificationService;

        public SQLTableMonitorManager(IChangeTableService<object> changeTableService, IDBNotificationService<object> notificationService)
        {
            _changeTableService = changeTableService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Starts monitoring and subscribes to change/error events.
        /// </summary>
        public async Task Invoke()
        {
            _notificationService.OnChanged += NotificationService_OnChanged;
            _notificationService.OnError += NotificationService_OnError;
            await _notificationService.StartNotify();
        }

        private void NotificationService_OnChanged(object? sender, RecordChangedEventArgs<object> e)
        {
            // Handle changed records
        }

        private void NotificationService_OnError(object? sender, ErrorEventArgs e)
        {
            // Handle errors
        }
    }
}
