using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlDbEntityNotifier.Core.Extensions;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifierWorker;

/// <summary>
/// SQLDBEntityNotifier Worker Service
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Application terminated unexpectedly");
            throw;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Add SQLDBEntityNotifier core services
                services.AddSqlDbEntityNotifier();

                // Configure database adapter
                // TODO: Configure your database adapter here
                // services.Configure<YourAdapterOptions>(configuration.GetSection("SqlDbEntityNotifier:Adapters:YourAdapter"));
                // services.AddDbAdapter<YourAdapter>();

                // Configure publisher
                // TODO: Configure your publisher here
                // services.Configure<YourPublisherOptions>(configuration.GetSection("SqlDbEntityNotifier:Publishers:YourPublisher"));
                // services.AddChangePublisher<YourPublisher>();

                // Add the worker service
                services.AddHostedService<ChangeEventWorker>();
            });
}

/// <summary>
/// Worker service that processes change events.
/// </summary>
public class ChangeEventWorker : BackgroundService
{
    private readonly ILogger<ChangeEventWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ChangeEventWorker(
        ILogger<ChangeEventWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChangeEventWorker starting...");

        try
        {
            // TODO: Initialize your database here
            // await InitializeDatabaseAsync(stoppingToken);

            // Get the database adapter and publisher
            // var adapter = _serviceProvider.GetRequiredService<IDbAdapter>();
            // var publisher = _serviceProvider.GetRequiredService<IChangePublisher>();

            // Start the adapter
            // await adapter.StartAsync(async (changeEvent, ct) =>
            // {
            //     try
            //     {
            //         _logger.LogInformation("Received change event: {Source}.{Schema}.{Table} - {Operation}", 
            //             changeEvent.Source, changeEvent.Schema, changeEvent.Table, changeEvent.Operation);

            //         // Publish the change event
            //         await publisher.PublishAsync(changeEvent, ct);

            //         _logger.LogInformation("Successfully published change event for {Table}", changeEvent.Table);
            //     }
            //     catch (Exception ex)
            //     {
            //         _logger.LogError(ex, "Error processing change event for {Table}", changeEvent.Table);
            //     }
            // }, stoppingToken);

            _logger.LogInformation("ChangeEventWorker started successfully");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ChangeEventWorker is stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangeEventWorker encountered an error");
            throw;
        }
    }

    // TODO: Implement your database initialization logic here
    // private async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    // {
    //     // Your database initialization code
    // }
}