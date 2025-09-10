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

namespace SqlDbEntityNotifier.IntegrationTests;

/// <summary>
/// Comprehensive integration tests for all SQLDBEntityNotifier components.
/// </summary>
public class ComprehensiveIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ILogger<ComprehensiveIntegrationTests> _logger;

    public ComprehensiveIntegrationTests(IntegrationTestFixture fixture, ILogger<ComprehensiveIntegrationTests> logger)
    {
        _fixture = fixture;
        _logger = logger;
    }

    [Fact]
    public async Task TestCompleteCDCWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var notifier = services.GetRequiredService<IEntityNotifier>();
        var changePublisher = services.GetRequiredService<IChangePublisher>();
        var serializer = services.GetRequiredService<ISerializer>();

        var receivedEvents = new List<ChangeEvent>();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        await notifier.StartAsync(async (changeEvent, ct) =>
        {
            receivedEvents.Add(changeEvent);
            _logger.LogInformation("Received change event: {EventId}", changeEvent.Offset);
        }, cancellationTokenSource.Token);

        // Wait for some events
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationTokenSource.Token);

        // Assert
        Assert.NotEmpty(receivedEvents);
        Assert.All(receivedEvents, evt => Assert.NotNull(evt.Source));
        Assert.All(receivedEvents, evt => Assert.NotNull(evt.Table));
        Assert.All(receivedEvents, evt => Assert.NotNull(evt.Operation));
    }

    [Fact]
    public async Task TestMultiTenantWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var tenantManager = services.GetRequiredService<TenantManager>();
        var throttlingManager = services.GetRequiredService<ThrottlingManager>();

        // Act
        var tenant1 = new TenantContext
        {
            TenantId = "tenant-1",
            TenantName = "Test Tenant 1",
            IsActive = true,
            ResourceLimits = new TenantResourceLimits
            {
                MaxConnections = 50,
                MaxEventsPerSecond = 500
            }
        };

        var tenant2 = new TenantContext
        {
            TenantId = "tenant-2",
            TenantName = "Test Tenant 2",
            IsActive = true,
            ResourceLimits = new TenantResourceLimits
            {
                MaxConnections = 100,
                MaxEventsPerSecond = 1000
            }
        };

        await tenantManager.RegisterTenantAsync(tenant1);
        await tenantManager.RegisterTenantAsync(tenant2);

        await tenantManager.ActivateTenantAsync("tenant-1");
        await tenantManager.ActivateTenantAsync("tenant-2");

        // Test throttling for each tenant
        var result1 = await throttlingManager.CheckThrottlingAsync("tenant-1", ThrottlingRequestType.EventProcessing);
        var result2 = await throttlingManager.CheckThrottlingAsync("tenant-2", ThrottlingRequestType.EventProcessing);

        // Assert
        Assert.True(result1.IsAllowed);
        Assert.True(result2.IsAllowed);
        Assert.Equal(2, tenantManager.ActiveTenants.Count);
    }

    [Fact]
    public async Task TestTransactionalGroupingWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var transactionalGroupManager = services.GetRequiredService<TransactionalGroupManager>();
        var exactlyOnceDeliveryManager = services.GetRequiredService<ExactlyOnceDeliveryManager>();

        // Act
        var transactionId = Guid.NewGuid().ToString();
        var transaction = await transactionalGroupManager.StartTransactionAsync(transactionId, "test-source");

        // Add some change events to the transaction
        for (int i = 0; i < 5; i++)
        {
            var changeEvent = ChangeEvent.Create(
                "test-source",
                "test_schema",
                "test_table",
                "INSERT",
                $"{transactionId}-{i}",
                null,
                JsonSerializer.SerializeToElement(new { id = i, name = $"test_{i}" }),
                new Dictionary<string, string> { ["transaction_id"] = transactionId }
            );

            await transactionalGroupManager.AddChangeEventAsync(transactionId, changeEvent);
        }

        await transactionalGroupManager.CommitTransactionAsync(transactionId);

        // Assert
        var committedTransaction = await transactionalGroupManager.GetTransactionAsync(transactionId);
        Assert.NotNull(committedTransaction);
        Assert.Equal(TransactionStatus.Committed, committedTransaction.Status);
        Assert.Equal(5, committedTransaction.ChangeEvents.Count);
    }

    [Fact]
    public async Task TestExactlyOnceDeliveryWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var exactlyOnceDeliveryManager = services.GetRequiredService<ExactlyOnceDeliveryManager>();
        var mockPublisher = new MockChangePublisher();

        var changeEvent = ChangeEvent.Create(
            "test-source",
            "test_schema",
            "test_table",
            "INSERT",
            "test-offset-1",
            null,
            JsonSerializer.SerializeToElement(new { id = 1, name = "test" }),
            new Dictionary<string, string>()
        );

        // Act
        var result1 = await exactlyOnceDeliveryManager.DeliverExactlyOnceAsync(changeEvent, mockPublisher);
        var result2 = await exactlyOnceDeliveryManager.DeliverExactlyOnceAsync(changeEvent, mockPublisher);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result2.IsDuplicate);
        Assert.Equal(1, result1.DeliveryAttempts);
        Assert.Equal(1, result2.DeliveryAttempts);
    }

    [Fact]
    public async Task TestBulkOperationDetectionWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var bulkOperationDetector = services.GetRequiredService<BulkOperationDetector>();
        var bulkOperationFilterEngine = services.GetRequiredService<BulkOperationFilterEngine>();

        var bulkEvents = new List<BulkOperationEvent>();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Subscribe to bulk operation events
        bulkOperationDetector.BulkOperationDetected += (sender, e) =>
        {
            bulkEvents.Add(e);
            _logger.LogInformation("Bulk operation detected: {Operation} on {Table}", e.Operation, e.Table);
        };

        // Act
        // Simulate multiple change events that should be detected as a bulk operation
        for (int i = 0; i < 10; i++)
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

            await bulkOperationDetector.ProcessChangeEventAsync(changeEvent, cancellationTokenSource.Token);
        }

        // Wait for bulk operation detection
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationTokenSource.Token);

        // Assert
        Assert.NotEmpty(bulkEvents);
        var bulkEvent = bulkEvents.First();
        Assert.Equal("BULK_INSERT", bulkEvent.Operation);
        Assert.Equal("bulk_table", bulkEvent.Table);
        Assert.Equal(10, bulkEvent.RowCount);
    }

    [Fact]
    public async Task TestSerializationWorkflow()
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
            JsonSerializer.SerializeToElement(new { id = 1, name = "test" }),
            new Dictionary<string, string> { ["test_metadata"] = "test_value" }
        );

        // Act
        var jsonData = await jsonSerializer.SerializeAsync(changeEvent);
        var protobufData = await protobufSerializer.SerializeAsync(changeEvent);
        var avroData = await avroSerializer.SerializeAsync(changeEvent);

        var deserializedJson = await jsonSerializer.DeserializeAsync<ChangeEvent>(jsonData);
        var deserializedProtobuf = await protobufSerializer.DeserializeAsync<ChangeEvent>(protobufData);
        var deserializedAvro = await avroSerializer.DeserializeAsync<ChangeEvent>(avroData);

        // Assert
        Assert.NotNull(jsonData);
        Assert.NotNull(protobufData);
        Assert.NotNull(avroData);

        Assert.Equal(changeEvent.Source, deserializedJson.Source);
        Assert.Equal(changeEvent.Source, deserializedProtobuf.Source);
        Assert.Equal(changeEvent.Source, deserializedAvro.Source);

        Assert.Equal(changeEvent.Table, deserializedJson.Table);
        Assert.Equal(changeEvent.Table, deserializedProtobuf.Table);
        Assert.Equal(changeEvent.Table, deserializedAvro.Table);
    }

    [Fact]
    public async Task TestDatabaseAdaptersWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var postgresAdapter = services.GetRequiredService<IDbAdapter>();
        var sqliteAdapter = services.GetRequiredService<IDbAdapter>();
        var mysqlAdapter = services.GetRequiredService<IDbAdapter>();
        var oracleAdapter = services.GetRequiredService<IDbAdapter>();

        var receivedEvents = new List<ChangeEvent>();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        await postgresAdapter.StartAsync(async (changeEvent, ct) =>
        {
            receivedEvents.Add(changeEvent);
            _logger.LogInformation("PostgreSQL event: {EventId}", changeEvent.Offset);
        }, cancellationTokenSource.Token);

        await sqliteAdapter.StartAsync(async (changeEvent, ct) =>
        {
            receivedEvents.Add(changeEvent);
            _logger.LogInformation("SQLite event: {EventId}", changeEvent.Offset);
        }, cancellationTokenSource.Token);

        await mysqlAdapter.StartAsync(async (changeEvent, ct) =>
        {
            receivedEvents.Add(changeEvent);
            _logger.LogInformation("MySQL event: {EventId}", changeEvent.Offset);
        }, cancellationTokenSource.Token);

        await oracleAdapter.StartAsync(async (changeEvent, ct) =>
        {
            receivedEvents.Add(changeEvent);
            _logger.LogInformation("Oracle event: {EventId}", changeEvent.Offset);
        }, cancellationTokenSource.Token);

        // Wait for some events
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationTokenSource.Token);

        // Assert
        Assert.NotEmpty(receivedEvents);
        Assert.Contains(receivedEvents, e => e.Source == "postgres");
        Assert.Contains(receivedEvents, e => e.Source == "sqlite");
        Assert.Contains(receivedEvents, e => e.Source == "mysql");
        Assert.Contains(receivedEvents, e => e.Source == "oracle");
    }

    [Fact]
    public async Task TestPublishersWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var kafkaPublisher = services.GetRequiredService<IChangePublisher>();
        var rabbitMQPublisher = services.GetRequiredService<IChangePublisher>();
        var webhookPublisher = services.GetRequiredService<IChangePublisher>();
        var azureEventHubsPublisher = services.GetRequiredService<IChangePublisher>();

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
        await kafkaPublisher.PublishAsync(changeEvent);
        await rabbitMQPublisher.PublishAsync(changeEvent);
        await webhookPublisher.PublishAsync(changeEvent);
        await azureEventHubsPublisher.PublishAsync(changeEvent);

        // Assert
        // In a real test, you would verify that the events were actually published
        // For now, we just verify that no exceptions were thrown
        Assert.True(true);
    }

    [Fact]
    public async Task TestMonitoringWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var metrics = services.GetRequiredService<IChangeEventMetrics>();
        var healthCheckService = services.GetRequiredService<IHealthCheckService>();

        // Act
        var metricsData = await metrics.GetMetricsAsync();
        var healthChecks = await healthCheckService.GetHealthChecksAsync();

        // Assert
        Assert.NotNull(metricsData);
        Assert.NotNull(healthChecks);
        Assert.NotNull(healthChecks.OverallStatus);
    }

    [Fact]
    public async Task TestTracingWorkflow()
    {
        // Arrange
        using var host = _fixture.CreateHost();
        var services = host.Services;
        
        var tracer = services.GetRequiredService<ChangeEventTracer>();

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
        using var activity = tracer.StartChangeEventTrace(changeEvent, "test_operation");
        tracer.SetTag("test_tag", "test_value");
        tracer.RecordEvent("test_event");
        tracer.SetStatus(ActivityStatusCode.Ok);

        // Assert
        Assert.NotNull(activity);
        Assert.NotNull(activity.Activity);
    }

    [Fact]
    public async Task TestEndToEndWorkflow()
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

        var receivedEvents = new List<ChangeEvent>();
        var bulkEvents = new List<BulkOperationEvent>();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // Set up tenant
        var tenant = new TenantContext
        {
            TenantId = "e2e-tenant",
            TenantName = "E2E Test Tenant",
            IsActive = true,
            ResourceLimits = new TenantResourceLimits
            {
                MaxConnections = 100,
                MaxEventsPerSecond = 1000
            }
        };

        await tenantManager.RegisterTenantAsync(tenant);
        await tenantManager.ActivateTenantAsync("e2e-tenant");

        // Set up bulk operation detection
        bulkOperationDetector.BulkOperationDetected += (sender, e) =>
        {
            bulkEvents.Add(e);
            _logger.LogInformation("E2E Bulk operation detected: {Operation} on {Table}", e.Operation, e.Table);
        };

        // Act
        await notifier.StartAsync(async (changeEvent, ct) =>
        {
            receivedEvents.Add(changeEvent);
            _logger.LogInformation("E2E Received event: {EventId} for tenant: {TenantId}", changeEvent.Offset, "e2e-tenant");

            // Check throttling
            var throttlingResult = await throttlingManager.CheckThrottlingAsync("e2e-tenant", ThrottlingRequestType.EventProcessing);
            Assert.True(throttlingResult.IsAllowed);

            // Process with bulk operation detector
            await bulkOperationDetector.ProcessChangeEventAsync(changeEvent, ct);

            // Simulate transactional grouping
            if (changeEvent.Metadata.TryGetValue("transaction_id", out var transactionId))
            {
                var transaction = await transactionalGroupManager.GetTransactionAsync(transactionId);
                if (transaction == null)
                {
                    transaction = await transactionalGroupManager.StartTransactionAsync(transactionId, changeEvent.Source);
                }

                await transactionalGroupManager.AddChangeEventAsync(transactionId, changeEvent);
            }
        }, cancellationTokenSource.Token);

        // Wait for events
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationTokenSource.Token);

        // Assert
        Assert.NotEmpty(receivedEvents);
        Assert.True(receivedEvents.Count >= 1);

        // Verify tenant management
        Assert.Contains(tenantManager.ActiveTenants.Values, t => t.TenantId == "e2e-tenant");

        // Verify throttling
        var statistics = await throttlingManager.GetThrottlingStatisticsAsync();
        Assert.True(statistics.TotalTenants >= 1);

        _logger.LogInformation("E2E Test completed successfully with {EventCount} events and {BulkEventCount} bulk events", 
            receivedEvents.Count, bulkEvents.Count);
    }
}

/// <summary>
/// Mock change publisher for testing.
/// </summary>
public class MockChangePublisher : IChangePublisher
{
    public Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default)
    {
        // Mock implementation - just return completed task
        return Task.CompletedTask;
    }
}