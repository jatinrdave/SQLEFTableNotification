using Microsoft.Extensions.DependencyInjection;
using SQLEFTableNotification.Domain;
using SQLEFTableNotification.Domain.Service;
using SQLEFTableNotification.Entity;
using SQLEFTableNotification.Entity.Entity;
using SQLEFTableNotification.Interfaces;
using SQLEFTableNotification.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Console.Services
{
    public interface ISQLTableMonitorManager
    {
        Task Invoke();
    }

    public class SQLTableMonitorManager : ISQLTableMonitorManager
    {
        private readonly IChangeTableService<UserChangeTable> _changeTableService;
        private readonly UserServiceAsync<UserViewModel, User> _userService;

        public SQLTableMonitorManager(IChangeTableService<UserChangeTable> changeTableService,UserServiceAsync<UserViewModel,User> userService)
        {
            _changeTableService = changeTableService;
            _userService = userService;
        }

        public async Task Invoke()
        {
            string connectionString = "";//From Config file.
            IDBNotificationService<UserChangeTable> sqlDBNotificationService = new SqlDBNotificationService<UserChangeTable>("User", connectionString, _changeTableService, -1, TimeSpan.FromHours(1), "API");
            await sqlDBNotificationService.StartNotify();
            sqlDBNotificationService.OnChanged += SqlDBNotificationService_OnChanged;
            sqlDBNotificationService.OnError += SqlDBNotificationService_OnError; 
        }

        private async void SqlDBNotificationService_OnError(object sender, SQLEFTableNotification.Models.ErrorEventArgs e)
        {
            //log error
        }

        private async void SqlDBNotificationService_OnChanged(object sender, SQLEFTableNotification.Models.RecordChangedEventArgs<UserChangeTable> e)
        {
            foreach (var record in e.Entities)
            {
                var user = await _userService.GetOne(record.Id);
                //Perform action.
            }
        }
    }
}