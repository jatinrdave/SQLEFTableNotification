using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Core.Extensions;
using SqlDbEntityNotifier.Core.MultiTenant;
using SqlDbEntityNotifier.Core.Throttling;
using SqlDbEntityNotifier.Core.Transactional;
using SqlDbEntityNotifier.Core.Delivery;
using SqlDbEntityNotifier.Core.BulkOperations;
using SqlDbEntityNotifier.Adapters.Postgres;
using SqlDbEntityNotifier.Adapters.Sqlite;
using SqlDbEntityNotifier.Adapters.MySQL;
using SqlDbEntityNotifier.Adapters.Oracle;
using SqlDbEntityNotifier.Publisher.Kafka;
using SqlDbEntityNotifier.Publisher.RabbitMQ;
using SqlDbEntityNotifier.Publisher.Webhook;
using SqlDbEntityNotifier.Publisher.AzureEventHubs;
using SqlDbEntityNotifier.Serializers.Json;
using SqlDbEntityNotifier.Serializers.Protobuf;
using SqlDbEntityNotifier.Serializers.Avro;
using SqlDbEntityNotifier.Monitoring;
using SqlDbEntityNotifier.Tracing;

namespace SqlDbEntityNotifier.PerformanceTests;

/// <summary>
/// Performance test fixture for SQLDBEntityNotifier components.
/// </summary>
public class PerformanceTestFixture : IDisposable
{
    private readonly IHost _host;
    private bool _disposed = false;

    public PerformanceTestFixture()
    {
        _host = CreateHost();
    }

    public IHost CreateHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Core services
                services.AddSingleton<IEntityNotifier, MockEntityNotifier>();
                services.AddSingleton<IChangePublisher, MockChangePublisher>();
                services.AddSingleton<ISerializer, JsonSerializer>();
                services.AddSingleton<IDbAdapter, MockDbAdapter>();

                // Multi-tenant services
                services.AddSingleton<TenantManager>();
                services.AddSingleton<ThrottlingManager>();

                // Transactional services
                services.AddSingleton<TransactionalGroupManager>();
                services.AddSingleton<ExactlyOnceDeliveryManager>();

                // Bulk operation services
                services.AddSingleton<BulkOperationDetector>();
                services.AddSingleton<BulkOperationFilterEngine>();

                // Database adapters
                services.AddSingleton<IDbAdapter, PostgresAdapter>();
                services.AddSingleton<IDbAdapter, SqliteAdapter>();
                services.AddSingleton<IDbAdapter, MySQLAdapter>();
                services.AddSingleton<IDbAdapter, OracleAdapter>();

                // Publishers
                services.AddSingleton<IChangePublisher, KafkaPublisher>();
                services.AddSingleton<IChangePublisher, RabbitMQPublisher>();
                services.AddSingleton<IChangePublisher, WebhookPublisher>();
                services.AddSingleton<IChangePublisher, AzureEventHubsPublisher>();

                // Serializers
                services.AddSingleton<ISerializer, JsonSerializer>();
                services.AddSingleton<ISerializer, ProtobufSerializer>();
                services.AddSingleton<ISerializer, AvroSerializer>();

                // Monitoring
                services.AddSingleton<IChangeEventMetrics, ChangeEventMetrics>();
                services.AddSingleton<IHealthCheckService, HealthCheckService>();

                // Tracing
                services.AddSingleton<ChangeEventTracer>();

                // Configuration
                services.Configure<PostgresAdapterOptions>(options =>
                {
                    options.ConnectionString = "Host=localhost;Database=testdb;Username=testuser;Password=testpass";
                    options.SlotName = "test_slot";
                    options.PublicationName = "test_publication";
                    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
                    options.MaxReplicationLag = TimeSpan.FromMinutes(5);
                });

                services.Configure<SqliteAdapterOptions>(options =>
                {
                    options.ConnectionString = "Data Source=:memory:";
                    options.EnableWAL = true;
                    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
                });

                services.Configure<MySQLAdapterOptions>(options =>
                {
                    options.ConnectionString = "Server=localhost;Database=testdb;Uid=testuser;Pwd=testpass;";
                    options.ServerId = 1;
                    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
                    options.MaxReplicationLag = TimeSpan.FromMinutes(5);
                });

                services.Configure<OracleAdapterOptions>(options =>
                {
                    options.ConnectionString = "Data Source=localhost:1521/XE;User Id=testuser;Password=testpass;";
                    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
                    options.MaxReplicationLag = TimeSpan.FromMinutes(5);
                });

                services.Configure<KafkaPublisherOptions>(options =>
                {
                    options.BootstrapServers = "localhost:9092";
                    options.Topic = "test-topic";
                    options.Acks = "all";
                    options.RetryBackoffMs = 100;
                    options.MaxRetries = 3;
                });

                services.Configure<RabbitMQPublisherOptions>(options =>
                {
                    options.HostName = "localhost";
                    options.Port = 5672;
                    options.UserName = "guest";
                    options.Password = "guest";
                    options.Exchange = "test-exchange";
                    options.RoutingKey = "test-routing-key";
                });

                services.Configure<WebhookPublisherOptions>(options =>
                {
                    options.BaseUrl = "http://localhost:8080";
                    options.Endpoint = "/webhook";
                    options.Timeout = TimeSpan.FromSeconds(30);
                    options.RetryCount = 3;
                    options.RetryDelay = TimeSpan.FromSeconds(1);
                });

                services.Configure<AzureEventHubsPublisherOptions>(options =>
                {
                    options.ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test";
                    options.EventHubName = "test-hub";
                    options.BatchSize = 100;
                    options.FlushInterval = TimeSpan.FromSeconds(1);
                });

                services.Configure<JsonSerializerOptions>(options =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.WriteIndented = false;
                });

                services.Configure<ProtobufSerializerOptions>(options =>
                {
                    options.UseCompression = true;
                    options.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
                });

                services.Configure<AvroSerializerOptions>(options =>
                {
                    options.SchemaRegistryUrl = "http://localhost:8081";
                    options.UseCompression = true;
                    options.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
                });

                services.Configure<BulkOperationDetectorOptions>(options =>
                {
                    options.BatchSize = 1000;
                    options.BatchTimeout = TimeSpan.FromSeconds(5);
                    options.MinBatchSize = 10;
                });

                services.Configure<BulkOperationFilterEngineOptions>(options =>
                {
                    options.EnableFiltering = true;
                    options.FilterThreshold = 100;
                    options.FilterTimeout = TimeSpan.FromSeconds(10);
                });

                services.Configure<TransactionalGroupManagerOptions>(options =>
                {
                    options.MaxTransactionSize = 10000;
                    options.TransactionTimeout = TimeSpan.FromMinutes(5);
                    options.CleanupInterval = TimeSpan.FromMinutes(1);
                });

                services.Configure<ExactlyOnceDeliveryManagerOptions>(options =>
                {
                    options.EnableExactlyOnce = true;
                    options.DeliveryTimeout = TimeSpan.FromMinutes(5);
                    options.CleanupInterval = TimeSpan.FromMinutes(1);
                });

                services.Configure<ThrottlingManagerOptions>(options =>
                {
                    options.EnableThrottling = true;
                    options.DefaultRateLimit = 1000;
                    options.DefaultBurstLimit = 2000;
                });

                services.Configure<TenantManagerOptions>(options =>
                {
                    options.EnableMultiTenancy = true;
                    options.DefaultTenantId = "default";
                });

                services.Configure<ChangeEventMetricsOptions>(options =>
                {
                    options.EnableMetrics = true;
                    options.MetricsInterval = TimeSpan.FromSeconds(10);
                });

                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    options.EnableHealthChecks = true;
                    options.HealthCheckInterval = TimeSpan.FromSeconds(30);
                });

                services.Configure<ChangeEventTracerOptions>(options =>
                {
                    options.EnableTracing = true;
                    options.TraceLevel = System.Diagnostics.ActivitySource.DefaultActivitySourceName;
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning); // Reduce logging for performance tests
            });

        return builder.Build();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _host?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Mock entity notifier for performance testing.
/// </summary>
public class MockEntityNotifier : IEntityNotifier
{
    private readonly ILogger<MockEntityNotifier> _logger;
    private readonly Random _random = new Random();

    public MockEntityNotifier(ILogger<MockEntityNotifier> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            var eventCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var changeEvent = ChangeEvent.Create(
                        "mock-source",
                        "mock_schema",
                        "mock_table",
                        "INSERT",
                        $"mock-offset-{eventCount}",
                        null,
                        System.Text.Json.JsonSerializer.SerializeToElement(new { id = eventCount, name = $"test_{eventCount}" }),
                        new Dictionary<string, string>()
                    );

                    await onChange(changeEvent, cancellationToken);
                    eventCount++;

                    // Simulate some processing time
                    await Task.Delay(1, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in mock entity notifier");
                }
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Mock change publisher for performance testing.
/// </summary>
public class MockChangePublisher : IChangePublisher
{
    public Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default)
    {
        // Simulate some processing time
        return Task.Delay(1, cancellationToken);
    }
}

/// <summary>
/// Mock database adapter for performance testing.
/// </summary>
public class MockDbAdapter : IDbAdapter
{
    private readonly ILogger<MockDbAdapter> _logger;
    private readonly Random _random = new Random();

    public MockDbAdapter(ILogger<MockDbAdapter> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            var eventCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var changeEvent = ChangeEvent.Create(
                        "mock-db",
                        "mock_schema",
                        "mock_table",
                        "INSERT",
                        $"mock-db-offset-{eventCount}",
                        null,
                        System.Text.Json.JsonSerializer.SerializeToElement(new { id = eventCount, name = $"test_{eventCount}" }),
                        new Dictionary<string, string>()
                    );

                    await onChange(changeEvent, cancellationToken);
                    eventCount++;

                    // Simulate some processing time
                    await Task.Delay(1, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in mock database adapter");
                }
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}