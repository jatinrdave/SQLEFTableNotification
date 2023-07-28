
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Abstractions;
using SQLEFTableNotification.Console.Services;
using SQLEFTableNotification.Entity.Entity;
using SQLEFTableNotification.Interfaces;
using SQLEFTableNotification.Services;

public class MainProgram
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    //[STAThread]
    public static async void Main()
    {
        var serviceProvider = new ServiceCollection()
                 .AddScoped(typeof(IChangeTableService<>), typeof(ChangeTableService<,>))
                 .AddScoped<ISQLTableMonitorManager, SQLTableMonitorManager>()
        .BuildServiceProvider();

        ISQLTableMonitorManager tableMonitorManager = serviceProvider.GetRequiredService<ISQLTableMonitorManager>();
        await tableMonitorManager.Invoke();
    }

   
}