using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlDbEntityNotifier.Adapters.Sqlite;
using SqlDbEntityNotifier.Adapters.Sqlite.Models;
using SqlDbEntityNotifier.Core.Extensions;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Publisher.Webhook;
using SqlDbEntityNotifier.Publisher.Webhook.Models;
using System.Text.Json;

namespace WorkerSample;

/// <summary>
/// Sample worker application demonstrating SQLDBEntityNotifier usage.
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

                // Configure SQLite adapter
                services.Configure<SqliteAdapterOptions>(configuration.GetSection("SqlDbEntityNotifier:Adapters:Sqlite"));
                services.AddDbAdapter<SqliteAdapter>();

                // Configure Webhook publisher
                services.Configure<WebhookPublisherOptions>(configuration.GetSection("SqlDbEntityNotifier:Publishers:Webhook"));
                services.AddChangePublisher<WebhookChangePublisher>();

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
    private readonly IConfiguration _configuration;

    public ChangeEventWorker(
        ILogger<ChangeEventWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChangeEventWorker starting...");

        try
        {
            // Initialize the database with sample data
            await InitializeDatabaseAsync(stoppingToken);

            // Get the database adapter
            var adapter = _serviceProvider.GetRequiredService<IDbAdapter>();
            var publisher = _serviceProvider.GetRequiredService<IChangePublisher>();

            // Start the adapter
            await adapter.StartAsync(async (changeEvent, ct) =>
            {
                try
                {
                    _logger.LogInformation("Received change event: {Source}.{Schema}.{Table} - {Operation}", 
                        changeEvent.Source, changeEvent.Schema, changeEvent.Table, changeEvent.Operation);

                    // Publish the change event
                    await publisher.PublishAsync(changeEvent, ct);

                    _logger.LogInformation("Successfully published change event for {Table}", changeEvent.Table);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing change event for {Table}", changeEvent.Table);
                }
            }, stoppingToken);

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

    private async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=sample.db";

        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Create sample tables
        var createOrdersTable = @"
            CREATE TABLE IF NOT EXISTS orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_number TEXT NOT NULL UNIQUE,
                customer_name TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'Pending',
                amount DECIMAL(10,2) NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        var createProductsTable = @"
            CREATE TABLE IF NOT EXISTS products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price DECIMAL(10,2) NOT NULL,
                stock_quantity INTEGER NOT NULL DEFAULT 0,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        using var createOrdersCommand = new Microsoft.Data.Sqlite.SqliteCommand(createOrdersTable, connection);
        await createOrdersCommand.ExecuteNonQueryAsync(cancellationToken);

        using var createProductsCommand = new Microsoft.Data.Sqlite.SqliteCommand(createProductsTable, connection);
        await createProductsCommand.ExecuteNonQueryAsync(cancellationToken);

        // Insert sample data
        await InsertSampleDataAsync(connection, cancellationToken);

        _logger.LogInformation("Database initialized with sample data");
    }

    private async Task InsertSampleDataAsync(Microsoft.Data.Sqlite.SqliteConnection connection, CancellationToken cancellationToken)
    {
        var insertOrders = @"
            INSERT OR IGNORE INTO orders (order_number, customer_name, status, amount)
            VALUES 
                ('ORD-001', 'John Doe', 'Pending', 99.99),
                ('ORD-002', 'Jane Smith', 'Processing', 149.50),
                ('ORD-003', 'Bob Johnson', 'Shipped', 75.25)";

        var insertProducts = @"
            INSERT OR IGNORE INTO products (name, price, stock_quantity)
            VALUES 
                ('Laptop', 999.99, 10),
                ('Mouse', 29.99, 50),
                ('Keyboard', 79.99, 25)";

        using var insertOrdersCommand = new Microsoft.Data.Sqlite.SqliteCommand(insertOrders, connection);
        await insertOrdersCommand.ExecuteNonQueryAsync(cancellationToken);

        using var insertProductsCommand = new Microsoft.Data.Sqlite.SqliteCommand(insertProducts, connection);
        await insertProductsCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Sample data inserted");
    }
}