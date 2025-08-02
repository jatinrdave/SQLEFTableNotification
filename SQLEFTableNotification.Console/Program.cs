using Microsoft.Extensions.DependencyInjection;
using SQLEFTableNotification.Console.Services;
using SQLEFTableNotification.Entity.Entity;
using SQLEFTableNotification.Interfaces;
using SQLEFTableNotification.Services;
using System.Threading.Tasks;
using Scrutor;
using SQLEFTableNotification.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SQLEFTableNotification.Entity.Context;
using SQLEFTableNotification.Console;
public class MainProgram
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <summary>
    /// Application entry point that configures services, loads application settings, sets up the database context, and starts the SQL table monitor manager.
    /// </summary>
    public static void Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SQLEFTableNotification.Console"))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var services = new ServiceCollection()
            .AddDbContext<SQLEFTableNotificationContext>(options =>
                options.UseSqlServer(connectionString))
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IChangeTableService<UserChangeTable>, ChangeTableService<UserChangeTable, UserViewModel>>()
            .AddScoped<ISQLTableMonitorManager, SQLTableMonitorManager>();

        var serviceProvider = services.BuildServiceProvider();

        ISQLTableMonitorManager tableMonitorManager = serviceProvider.GetRequiredService<ISQLTableMonitorManager>();
        Task.Run(async () => await tableMonitorManager.Invoke()).Wait();
    }
}