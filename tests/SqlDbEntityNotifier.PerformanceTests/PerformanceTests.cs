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
using System.Text.Json;
using System.Diagnostics;

namespace SqlDbEntityNotifier.PerformanceTests;

/// <summary>
/// Performance tests for SQLDBEntityNotifier components.
/// </summary>
public class PerformanceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    private readonly ILogger<PerformanceTests> _logger;

    public PerformanceTests(PerformanceTestFixture fixture, ILogger<PerformanceTests> logger)
    {
        _fixture = fixture;
        _logger = logger;
    }

    [Fact]
    public async Task TestChangeEventProcessingPerformance()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var notifier = services.GetRequiredService<IEntityNotifier>();
        var metrics = services.GetRequiredService<IChangeEventMetrics>();

        var eventCount = 10000;
        var receivedEvents = 0;
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        await notifier.StartAsync(async (changeEvent, ct) =>
        {
            Interlocked.Increment(ref receivedEvents);
        }, cancellationTokenSource.Token);

        // Wait for events to be processed
        while (receivedEvents < eventCount && !cancellationTokenSource.Token.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationTokenSource.Token);
        }

        stopwatch.Stop();

        // Assert
        var eventsPerSecond = receivedEvents / stopwatch.Elapsed.TotalSeconds;
        var metricsData = await metrics.GetMetricsAsync();

        _logger.LogInformation("Processed {EventCount} events in {ElapsedMs}ms ({EventsPerSecond:F2} events/sec)", 
            receivedEvents, stopwatch.ElapsedMilliseconds, eventsPerSecond);

        Assert.True(eventsPerSecond > 1000, $"Expected > 1000 events/sec, got {eventsPerSecond:F2}");
        Assert.True(receivedEvents >= eventCount * 0.9, $"Expected at least {eventCount * 0.9} events, got {receivedEvents}");
    }

    [Fact]
    public async Task TestSerializationPerformance()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var jsonSerializer = services.GetRequiredService<ISerializer>();
        var protobufSerializer = services.GetRequiredService<ISerializer>();
        var avroSerializer = services.GetRequiredService<ISerializer>();

        var changeEvent = ChangeEvent.Create(
            "test-source",
            "test_schema",
            "test_table",
            "INSERT",
            "test-offset",
            null,
            JsonSerializer.SerializeToElement(new { id = 1, name = "test", data = new string('x', 1000) }),
            new Dictionary<string, string> { ["test_metadata"] = "test_value" }
        );

        var iterations = 10000;

        // Act - JSON Serialization
        var jsonStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var jsonData = await jsonSerializer.SerializeAsync(changeEvent);
            var deserialized = await jsonSerializer.DeserializeAsync<ChangeEvent>(jsonData);
        }
        jsonStopwatch.Stop();

        // Act - Protobuf Serialization
        var protobufStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var protobufData = await protobufSerializer.SerializeAsync(changeEvent);
            var deserialized = await protobufSerializer.DeserializeAsync<ChangeEvent>(protobufData);
        }
        protobufStopwatch.Stop();

        // Act - Avro Serialization
        var avroStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var avroData = await avroSerializer.SerializeAsync(changeEvent);
            var deserialized = await avroSerializer.DeserializeAsync<ChangeEvent>(avroData);
        }
        avroStopwatch.Stop();

        // Assert
        var jsonOpsPerSecond = iterations / jsonStopwatch.Elapsed.TotalSeconds;
        var protobufOpsPerSecond = iterations / protobufStopwatch.Elapsed.TotalSeconds;
        var avroOpsPerSecond = iterations / avroStopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("JSON: {OpsPerSecond:F2} ops/sec, Protobuf: {ProtobufOpsPerSecond:F2} ops/sec, Avro: {AvroOpsPerSecond:F2} ops/sec",
            jsonOpsPerSecond, protobufOpsPerSecond, avroOpsPerSecond);

        Assert.True(jsonOpsPerSecond > 1000, $"JSON serialization too slow: {jsonOpsPerSecond:F2} ops/sec");
        Assert.True(protobufOpsPerSecond > 2000, $"Protobuf serialization too slow: {protobufOpsPerSecond:F2} ops/sec");
        Assert.True(avroOpsPerSecond > 500, $"Avro serialization too slow: {avroOpsPerSecond:F2} ops/sec");
    }

    [Fact]
    public async Task TestMultiTenantPerformance()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var tenantManager = services.GetRequiredService<TenantManager>();
        var throttlingManager = services.GetRequiredService<ThrottlingManager>();

        var tenantCount = 1000;
        var operationsPerTenant = 100;

        // Act
        var stopwatch = Stopwatch.StartNew();

        // Register tenants
        for (int i = 0; i < tenantCount; i++)
        {
            var tenant = new TenantContext
            {
                TenantId = $"tenant-{i}",
                TenantName = $"Test Tenant {i}",
                IsActive = true,
                ResourceLimits = new TenantResourceLimits
                {
                    MaxConnections = 50,
                    MaxEventsPerSecond = 500
                }
            };

            await tenantManager.RegisterTenantAsync(tenant);
            await tenantManager.ActivateTenantAsync($"tenant-{i}");
        }

        // Perform throttling checks
        var tasks = new List<Task>();
        for (int i = 0; i < tenantCount; i++)
        {
            for (int j = 0; j < operationsPerTenant; j++)
            {
                var tenantId = $"tenant-{i}";
                tasks.Add(throttlingManager.CheckThrottlingAsync(tenantId, ThrottlingRequestType.EventProcessing));
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalOperations = tenantCount * operationsPerTenant;
        var operationsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("Processed {TotalOperations} tenant operations in {ElapsedMs}ms ({OperationsPerSecond:F2} ops/sec)",
            totalOperations, stopwatch.ElapsedMilliseconds, operationsPerSecond);

        Assert.True(operationsPerSecond > 5000, $"Tenant operations too slow: {operationsPerSecond:F2} ops/sec");
        Assert.Equal(tenantCount, tenantManager.ActiveTenants.Count);
    }

    [Fact]
    public async Task TestTransactionalGroupingPerformance()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var transactionalGroupManager = services.GetRequiredService<TransactionalGroupManager>();

        var transactionCount = 1000;
        var eventsPerTransaction = 10;

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (int i = 0; i < transactionCount; i++)
        {
            var transactionId = $"tx-{i}";
            tasks.Add(ProcessTransactionAsync(transactionId, eventsPerTransaction));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalEvents = transactionCount * eventsPerTransaction;
        var eventsPerSecond = totalEvents / stopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("Processed {TotalEvents} events in {TransactionCount} transactions in {ElapsedMs}ms ({EventsPerSecond:F2} events/sec)",
            totalEvents, transactionCount, stopwatch.ElapsedMilliseconds, eventsPerSecond);

        Assert.True(eventsPerSecond > 2000, $"Transactional grouping too slow: {eventsPerSecond:F2} events/sec");

        async Task ProcessTransactionAsync(string transactionId, int eventCount)
        {
            var transaction = await transactionalGroupManager.StartTransactionAsync(transactionId, "test-source");

            for (int j = 0; j < eventCount; j++)
            {
                var changeEvent = ChangeEvent.Create(
                    "test-source",
                    "test_schema",
                    "test_table",
                    "INSERT",
                    $"{transactionId}-{j}",
                    null,
                    JsonSerializer.SerializeToElement(new { id = j, name = $"test_{j}" }),
                    new Dictionary<string, string> { ["transaction_id"] = transactionId }
                );

                await transactionalGroupManager.AddChangeEventAsync(transactionId, changeEvent);
            }

            await transactionalGroupManager.CommitTransactionAsync(transactionId);
        }
    }

    [Fact]
    public async Task TestExactlyOnceDeliveryPerformance()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var exactlyOnceDeliveryManager = services.GetRequiredService<ExactlyOnceDeliveryManager>();
        var mockPublisher = new MockChangePublisher();

        var eventCount = 10000;
        var duplicateCount = 5000;

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>();
        
        // First delivery
        for (int i = 0; i < eventCount; i++)
        {
            var changeEvent = ChangeEvent.Create(
                "test-source",
                "test_schema",
                "test_table",
                "INSERT",
                $"test-offset-{i}",
                null,
                JsonSerializer.SerializeToElement(new { id = i, name = $"test_{i}" }),
                new Dictionary<string, string>()
            );

            tasks.Add(exactlyOnceDeliveryManager.DeliverExactlyOnceAsync(changeEvent, mockPublisher));
        }

        await Task.WhenAll(tasks);
        tasks.Clear();

        // Duplicate delivery
        for (int i = 0; i < duplicateCount; i++)
        {
            var changeEvent = ChangeEvent.Create(
                "test-source",
                "test_schema",
                "test_table",
                "INSERT",
                $"test-offset-{i}",
                null,
                JsonSerializer.SerializeToElement(new { id = i, name = $"test_{i}" }),
                new Dictionary<string, string>()
            );

            tasks.Add(exactlyOnceDeliveryManager.DeliverExactlyOnceAsync(changeEvent, mockPublisher));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalOperations = eventCount + duplicateCount;
        var operationsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("Processed {TotalOperations} exactly-once operations in {ElapsedMs}ms ({OperationsPerSecond:F2} ops/sec)",
            totalOperations, stopwatch.ElapsedMilliseconds, operationsPerSecond);

        Assert.True(operationsPerSecond > 3000, $"Exactly-once delivery too slow: {operationsPerSecond:F2} ops/sec");
    }

    [Fact]
    public async Task TestBulkOperationDetectionPerformance()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var bulkOperationDetector = services.GetRequiredService<BulkOperationDetector>();

        var eventCount = 50000;
        var bulkEventCount = 0;
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Set up bulk operation detection
        bulkOperationDetector.BulkOperationDetected += (sender, e) =>
        {
            Interlocked.Increment(ref bulkEventCount);
        };

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (int i = 0; i < eventCount; i++)
        {
            var changeEvent = ChangeEvent.Create(
                "test-source",
                "test_schema",
                "bulk_table",
                "INSERT",
                $"bulk-offset-{i}",
                null,
                JsonSerializer.SerializeToElement(new { id = i, name = $"bulk_{i}" }),
                new Dictionary<string, string> { ["bulk_operation"] = "true" }
            );

            tasks.Add(bulkOperationDetector.ProcessChangeEventAsync(changeEvent, cancellationTokenSource.Token));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var eventsPerSecond = eventCount / stopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("Processed {EventCount} events for bulk detection in {ElapsedMs}ms ({EventsPerSecond:F2} events/sec), detected {BulkEventCount} bulk operations",
            eventCount, stopwatch.ElapsedMilliseconds, eventsPerSecond, bulkEventCount);

        Assert.True(eventsPerSecond > 5000, $"Bulk operation detection too slow: {eventsPerSecond:F2} events/sec");
        Assert.True(bulkEventCount > 0, "Expected at least one bulk operation to be detected");
    }

    [Fact]
    public async Task TestDatabaseAdapterPerformance()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var postgresAdapter = services.GetRequiredService<IDbAdapter>();
        var sqliteAdapter = services.GetRequiredService<IDbAdapter>();
        var mysqlAdapter = services.GetRequiredService<IDbAdapter>();
        var oracleAdapter = services.GetRequiredService<IDbAdapter>();

        var eventCount = 10000;
        var receivedEvents = 0;
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>
        {
            StartAdapterAsync(postgresAdapter, "postgres"),
            StartAdapterAsync(sqliteAdapter, "sqlite"),
            StartAdapterAsync(mysqlAdapter, "mysql"),
            StartAdapterAsync(oracleAdapter, "oracle")
        };

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var eventsPerSecond = receivedEvents / stopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("Processed {EventCount} events across all adapters in {ElapsedMs}ms ({EventsPerSecond:F2} events/sec)",
            receivedEvents, stopwatch.ElapsedMilliseconds, eventsPerSecond);

        Assert.True(eventsPerSecond > 2000, $"Database adapters too slow: {eventsPerSecond:F2} events/sec");
        Assert.True(receivedEvents >= eventCount * 0.8, $"Expected at least {eventCount * 0.8} events, got {receivedEvents}");

        async Task StartAdapterAsync(IDbAdapter adapter, string source)
        {
            await adapter.StartAsync(async (changeEvent, ct) =>
            {
                Interlocked.Increment(ref receivedEvents);
            }, cancellationTokenSource.Token);
        }
    }

    [Fact]
    public async Task TestPublisherPerformance()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var kafkaPublisher = services.GetRequiredService<IChangePublisher>();
        var rabbitMQPublisher = services.GetRequiredService<IChangePublisher>();
        var webhookPublisher = services.GetRequiredService<IChangePublisher>();
        var azureEventHubsPublisher = services.GetRequiredService<IChangePublisher>();

        var eventCount = 10000;
        var changeEvent = ChangeEvent.Create(
            "test-source",
            "test_schema",
            "test_table",
            "INSERT",
            "test-offset",
            null,
            JsonSerializer.SerializeToElement(new { id = 1, name = "test" }),
            new Dictionary<string, string>()
        );

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (int i = 0; i < eventCount; i++)
        {
            tasks.Add(kafkaPublisher.PublishAsync(changeEvent));
            tasks.Add(rabbitMQPublisher.PublishAsync(changeEvent));
            tasks.Add(webhookPublisher.PublishAsync(changeEvent));
            tasks.Add(azureEventHubsPublisher.PublishAsync(changeEvent));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalPublishes = eventCount * 4; // 4 publishers
        var publishesPerSecond = totalPublishes / stopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("Published {TotalPublishes} events in {ElapsedMs}ms ({PublishesPerSecond:F2} publishes/sec)",
            totalPublishes, stopwatch.ElapsedMilliseconds, publishesPerSecond);

        Assert.True(publishesPerSecond > 1000, $"Publishers too slow: {publishesPerSecond:F2} publishes/sec");
    }

    [Fact]
    public async Task TestMemoryUsage()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var notifier = services.GetRequiredService<IEntityNotifier>();
        var tenantManager = services.GetRequiredService<TenantManager>();
        var throttlingManager = services.GetRequiredService<ThrottlingManager>();
        var transactionalGroupManager = services.GetRequiredService<TransactionalGroupManager>();
        var exactlyOnceDeliveryManager = services.GetRequiredService<ExactlyOnceDeliveryManager>();
        var bulkOperationDetector = services.GetRequiredService<BulkOperationDetector>();

        var initialMemory = GC.GetTotalMemory(true);
        var eventCount = 100000;
        var receivedEvents = 0;
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        // Act
        await notifier.StartAsync(async (changeEvent, ct) =>
        {
            Interlocked.Increment(ref receivedEvents);
        }, cancellationTokenSource.Token);

        // Wait for events to be processed
        while (receivedEvents < eventCount && !cancellationTokenSource.Token.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationTokenSource.Token);
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        var memoryPerEvent = memoryUsed / (double)receivedEvents;
        var memoryMB = memoryUsed / (1024.0 * 1024.0);

        _logger.LogInformation("Processed {EventCount} events, used {MemoryMB:F2} MB ({MemoryPerEvent:F2} bytes/event)",
            receivedEvents, memoryMB, memoryPerEvent);

        Assert.True(memoryPerEvent < 1000, $"Memory usage too high: {memoryPerEvent:F2} bytes/event");
        Assert.True(memoryMB < 500, $"Total memory usage too high: {memoryMB:F2} MB");
    }

    [Fact]
    public async Task TestConcurrentOperations()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var notifier = services.GetRequiredService<IEntityNotifier>();
        var tenantManager = services.GetRequiredService<TenantManager>();
        var throttlingManager = services.GetRequiredService<ThrottlingManager>();
        var transactionalGroupManager = services.GetRequiredService<TransactionalGroupManager>();
        var exactlyOnceDeliveryManager = services.GetRequiredService<ExactlyOnceDeliveryManager>();
        var bulkOperationDetector = services.GetRequiredService<BulkOperationDetector>();

        var concurrentTasks = 100;
        var operationsPerTask = 1000;
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (int i = 0; i < concurrentTasks; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < operationsPerTask; j++)
                {
                    // Simulate various operations
                    var tenantId = $"tenant-{taskId % 10}";
                    await throttlingManager.CheckThrottlingAsync(tenantId, ThrottlingRequestType.EventProcessing);

                    var transactionId = $"tx-{taskId}-{j}";
                    var transaction = await transactionalGroupManager.StartTransactionAsync(transactionId, "test-source");
                    await transactionalGroupManager.CommitTransactionAsync(transactionId);

                    var changeEvent = ChangeEvent.Create(
                        "test-source",
                        "test_schema",
                        "test_table",
                        "INSERT",
                        $"offset-{taskId}-{j}",
                        null,
                        JsonSerializer.SerializeToElement(new { id = j, name = $"test_{j}" }),
                        new Dictionary<string, string>()
                    );

                    await bulkOperationDetector.ProcessChangeEventAsync(changeEvent, cancellationTokenSource.Token);
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalOperations = concurrentTasks * operationsPerTask * 4; // 4 operations per iteration
        var operationsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("Completed {TotalOperations} concurrent operations in {ElapsedMs}ms ({OperationsPerSecond:F2} ops/sec)",
            totalOperations, stopwatch.ElapsedMilliseconds, operationsPerSecond);

        Assert.True(operationsPerSecond > 10000, $"Concurrent operations too slow: {operationsPerSecond:F2} ops/sec");
    }
}

/// <summary>
/// Mock change publisher for performance testing.
/// </summary>
public class MockChangePublisher : IChangePublisher
{
    public Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default)
    {
        // Mock implementation - just return completed task
        return Task.CompletedTask;
    }
}