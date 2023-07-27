
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Abstractions;
using SQLEFTableNotification.Console.Services;
using SQLEFTableNotification.Entity.Entity;
using SQLEFTableNotificationLib.Interfaces;
using SQLEFTableNotificationLib.Services;

public class MainProgram
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    //[STAThread]
    public static void Main()
    {
        var serviceProvider = new ServiceCollection()
                 .AddScoped(typeof(IChangeTableService<>), typeof(ChangeTableService<,>))

        .BuildServiceProvider();

        string connectionString = "";//From Config file.
        var changeTableService = serviceProvider.GetRequiredService<IChangeTableService<UserChangeTable>>();
        IDBNotificationService<UserChangeTable> sqlDBNotificationService = new SqlDBNotificationService<UserChangeTable>("User", connectionString, changeTableService, -1, TimeSpan.FromHours(1), "API");
        sqlDBNotificationService.StartNotify();
    }
}