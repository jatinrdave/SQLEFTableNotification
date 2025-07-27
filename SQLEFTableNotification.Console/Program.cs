
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

public class MainProgram
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    //[STAThread]
    public static void Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var serviceProvider = new ServiceCollection()
            .AddDbContext<SQLEFTableNotificationContext>(options =>
                options.UseSqlServer(connectionString))
            .AddScoped<IChangeTableService<UserChangeTable>, ChangeTableService<UserChangeTable, UserViewModel>>()
            .Scan(scan => scan
                .FromApplicationDependencies()
                .AddClasses()
                .AsImplementedInterfaces()
                .WithScopedLifetime())
            .BuildServiceProvider();

        ISQLTableMonitorManager tableMonitorManager = serviceProvider.GetRequiredService<ISQLTableMonitorManager>();
        Task.Run(async () => await tableMonitorManager.Invoke()).Wait();
    }
}