# SQLDBEntityNotifier - Examples & Tutorials

## Table of Contents

1. [Basic Examples](#basic-examples)
2. [Advanced Examples](#advanced-examples)
3. [Real-world Scenarios](#real-world-scenarios)
4. [Integration Examples](#integration-examples)
5. [Performance Examples](#performance-examples)

## Basic Examples

### Example 1: Simple Change Detection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Adapters.Postgres;
using SqlDbEntityNotifier.Publisher.Kafka;
using SqlDbEntityNotifier.Serializers.Json;

public class SimpleChangeDetection
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddPostgresAdapter(options =>
                {
                    options.ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass";
                    options.SlotName = "simple_slot";
                    options.PublicationName = "simple_publication";
                });

                services.AddKafkaPublisher(options =>
                {
                    options.BootstrapServers = "localhost:9092";
                    options.Topic = "changes";
                });

                services.AddJsonSerializer();
            })
            .Build();

        var notifier = host.Services.GetRequiredService<IEntityNotifier>();
        var publisher = host.Services.GetRequiredService<IChangePublisher>();

        await notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            Console.WriteLine($"Change: {changeEvent.Operation} on {changeEvent.Table}");
            await publisher.PublishAsync(changeEvent);
        }, CancellationToken.None);
    }
}
```

### Example 2: Multiple Database Support

```csharp
public class MultiDatabaseExample
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // PostgreSQL
                services.AddPostgresAdapter(options =>
                {
                    options.ConnectionString = "Host=localhost;Database=postgres_db;Username=user;Password=pass";
                    options.SlotName = "postgres_slot";
                    options.PublicationName = "postgres_publication";
                });

                // MySQL
                services.AddMySQLAdapter(options =>
                {
                    options.ConnectionString = "Server=localhost;Database=mysql_db;Uid=user;Pwd=pass;";
                    options.ServerId = 1;
                });

                // SQLite
                services.AddSqliteAdapter(options =>
                {
                    options.ConnectionString = "Data Source=sqlite.db";
                    options.EnableWAL = true;
                });

                services.AddKafkaPublisher(options =>
                {
                    options.BootstrapServers = "localhost:9092";
                    options.Topic = "multi-db-changes";
                });

                services.AddJsonSerializer();
            })
            .Build();

        var notifier = host.Services.GetRequiredService<IEntityNotifier>();
        var publisher = host.Services.GetRequiredService<IChangePublisher>();

        await notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            Console.WriteLine($"Database: {changeEvent.Source}, Table: {changeEvent.Table}, Operation: {changeEvent.Operation}");
            await publisher.PublishAsync(changeEvent);
        }, CancellationToken.None);
    }
}
```

### Example 3: Custom Change Processing

```csharp
public class CustomChangeProcessor
{
    private readonly ILogger<CustomChangeProcessor> _logger;

    public CustomChangeProcessor(ILogger<CustomChangeProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        switch (changeEvent.Table.ToLower())
        {
            case "users":
                await ProcessUserChangeAsync(changeEvent, cancellationToken);
                break;
            case "orders":
                await ProcessOrderChangeAsync(changeEvent, cancellationToken);
                break;
            case "products":
                await ProcessProductChangeAsync(changeEvent, cancellationToken);
                break;
            default:
                _logger.LogWarning("Unknown table: {Table}", changeEvent.Table);
                break;
        }
    }

    private async Task ProcessUserChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var user = JsonSerializer.Deserialize<User>(changeEvent.Data.GetRawText());
        
        if (changeEvent.Operation == "INSERT")
        {
            _logger.LogInformation("New user created: {UserId}", user.Id);
            // Send welcome email, create user profile, etc.
        }
        else if (changeEvent.Operation == "UPDATE")
        {
            _logger.LogInformation("User updated: {UserId}", user.Id);
            // Update user profile, sync with external systems, etc.
        }
        else if (changeEvent.Operation == "DELETE")
        {
            _logger.LogInformation("User deleted: {UserId}", user.Id);
            // Cleanup user data, send notification, etc.
        }
    }

    private async Task ProcessOrderChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var order = JsonSerializer.Deserialize<Order>(changeEvent.Data.GetRawText());
        
        if (changeEvent.Operation == "INSERT")
        {
            _logger.LogInformation("New order created: {OrderId}", order.Id);
            // Process payment, send confirmation, update inventory, etc.
        }
        else if (changeEvent.Operation == "UPDATE")
        {
            var previousOrder = JsonSerializer.Deserialize<Order>(changeEvent.PreviousData);
            
            if (order.Status != previousOrder.Status)
            {
                _logger.LogInformation("Order status changed: {OrderId} from {OldStatus} to {NewStatus}", 
                    order.Id, previousOrder.Status, order.Status);
                // Send status update, trigger workflow, etc.
            }
        }
    }

    private async Task ProcessProductChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var product = JsonSerializer.Deserialize<Product>(changeEvent.Data.GetRawText());
        
        if (changeEvent.Operation == "INSERT")
        {
            _logger.LogInformation("New product created: {ProductId}", product.Id);
            // Update search index, sync with catalog, etc.
        }
        else if (changeEvent.Operation == "UPDATE")
        {
            _logger.LogInformation("Product updated: {ProductId}", product.Id);
            // Update search index, sync with catalog, etc.
        }
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

## Advanced Examples

### Example 4: Multi-Tenant Application

```csharp
public class MultiTenantExample
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddPostgresAdapter(options =>
                {
                    options.ConnectionString = "Host=localhost;Database=multitenant_db;Username=user;Password=pass";
                    options.SlotName = "multitenant_slot";
                    options.PublicationName = "multitenant_publication";
                });

                services.AddKafkaPublisher(options =>
                {
                    options.BootstrapServers = "localhost:9092";
                    options.Topic = "tenant-changes";
                });

                services.AddJsonSerializer();

                // Multi-tenant services
                services.Configure<TenantManagerOptions>(options =>
                {
                    options.EnableMultiTenancy = true;
                    options.DefaultTenantId = "default";
                });

                services.AddSingleton<TenantManager>();
                services.AddSingleton<ThrottlingManager>();
            })
            .Build();

        var notifier = host.Services.GetRequiredService<IEntityNotifier>();
        var publisher = host.Services.GetRequiredService<IChangePublisher>();
        var tenantManager = host.Services.GetRequiredService<TenantManager>();
        var throttlingManager = host.Services.GetRequiredService<ThrottlingManager>();

        // Set up tenants
        await SetupTenantsAsync(tenantManager);

        await notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            var tenantId = changeEvent.Metadata.GetValueOrDefault("tenant_id", "default");
            
            // Check throttling
            var throttlingResult = await throttlingManager.CheckThrottlingAsync(tenantId, ThrottlingRequestType.EventProcessing);
            if (!throttlingResult.IsAllowed)
            {
                Console.WriteLine($"Throttling limit reached for tenant: {tenantId}");
                return;
            }

            // Process change with tenant context
            await ProcessTenantChangeAsync(changeEvent, tenantId, cancellationToken);
        }, CancellationToken.None);
    }

    private static async Task SetupTenantsAsync(TenantManager tenantManager)
    {
        var tenants = new[]
        {
            new TenantContext
            {
                TenantId = "tenant-1",
                TenantName = "Enterprise Customer",
                IsActive = true,
                ResourceLimits = new TenantResourceLimits
                {
                    MaxConnections = 100,
                    MaxEventsPerSecond = 1000
                }
            },
            new TenantContext
            {
                TenantId = "tenant-2",
                TenantName = "Small Business",
                IsActive = true,
                ResourceLimits = new TenantResourceLimits
                {
                    MaxConnections = 50,
                    MaxEventsPerSecond = 500
                }
            }
        };

        foreach (var tenant in tenants)
        {
            await tenantManager.RegisterTenantAsync(tenant);
            await tenantManager.ActivateTenantAsync(tenant.TenantId);
        }
    }

    private static async Task ProcessTenantChangeAsync(ChangeEvent changeEvent, string tenantId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing change for tenant {tenantId}: {changeEvent.Operation} on {changeEvent.Table}");
        
        // Add tenant context to metadata
        changeEvent.Metadata["tenant_id"] = tenantId;
        
        // Process based on tenant
        if (tenantId == "tenant-1")
        {
            // Enterprise processing
            await ProcessEnterpriseChangeAsync(changeEvent, cancellationToken);
        }
        else
        {
            // Standard processing
            await ProcessStandardChangeAsync(changeEvent, cancellationToken);
        }
    }

    private static async Task ProcessEnterpriseChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        // Enterprise-specific processing
        Console.WriteLine("Enterprise processing: High priority, full audit trail");
    }

    private static async Task ProcessStandardChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        // Standard processing
        Console.WriteLine("Standard processing: Normal priority");
    }
}
```

### Example 5: Transactional Grouping

```csharp
public class TransactionalGroupingExample
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddPostgresAdapter(options =>
                {
                    options.ConnectionString = "Host=localhost;Database=transactional_db;Username=user;Password=pass";
                    options.SlotName = "transactional_slot";
                    options.PublicationName = "transactional_publication";
                });

                services.AddKafkaPublisher(options =>
                {
                    options.BootstrapServers = "localhost:9092";
                    options.Topic = "transactional-changes";
                });

                services.AddJsonSerializer();

                // Transactional services
                services.Configure<TransactionalGroupManagerOptions>(options =>
                {
                    options.MaxTransactionSize = 10000;
                    options.TransactionTimeout = TimeSpan.FromMinutes(5);
                    options.CleanupInterval = TimeSpan.FromMinutes(1);
                });

                services.AddSingleton<TransactionalGroupManager>();
            })
            .Build();

        var notifier = host.Services.GetRequiredService<IEntityNotifier>();
        var publisher = host.Services.GetRequiredService<IChangePublisher>();
        var transactionalGroupManager = host.Services.GetRequiredService<TransactionalGroupManager>();

        await notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            var transactionId = changeEvent.Metadata.GetValueOrDefault("transaction_id");
            
            if (!string.IsNullOrEmpty(transactionId))
            {
                // Group changes by transaction
                await ProcessTransactionalChangeAsync(changeEvent, transactionId, cancellationToken);
            }
            else
            {
                // Process individual change
                await publisher.PublishAsync(changeEvent, cancellationToken);
            }
        }, CancellationToken.None);
    }

    private static async Task ProcessTransactionalChangeAsync(ChangeEvent changeEvent, string transactionId, CancellationToken cancellationToken)
    {
        var transactionalGroupManager = host.Services.GetRequiredService<TransactionalGroupManager>();
        
        // Get or create transaction
        var transaction = await transactionalGroupManager.GetTransactionAsync(transactionId);
        if (transaction == null)
        {
            transaction = await transactionalGroupManager.StartTransactionAsync(transactionId, changeEvent.Source);
        }

        // Add change event to transaction
        await transactionalGroupManager.AddChangeEventAsync(transactionId, changeEvent);

        // Check if transaction is complete
        if (IsTransactionComplete(transaction))
        {
            // Commit transaction and publish all changes
            await transactionalGroupManager.CommitTransactionAsync(transactionId);
            
            var committedTransaction = await transactionalGroupManager.GetTransactionAsync(transactionId);
            foreach (var eventInTransaction in committedTransaction.ChangeEvents)
            {
                await publisher.PublishAsync(eventInTransaction, cancellationToken);
            }
        }
    }

    private static bool IsTransactionComplete(TransactionContext transaction)
    {
        // Check if transaction is complete based on your business logic
        // For example, check if all expected tables have been updated
        var expectedTables = new[] { "orders", "order_items", "payments" };
        var updatedTables = transaction.ChangeEvents.Select(e => e.Table).Distinct();
        
        return expectedTables.All(table => updatedTables.Contains(table));
    }
}
```

### Example 6: Exactly-Once Delivery

```csharp
public class ExactlyOnceDeliveryExample
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddPostgresAdapter(options =>
                {
                    options.ConnectionString = "Host=localhost;Database=exactlyonce_db;Username=user;Password=pass";
                    options.SlotName = "exactlyonce_slot";
                    options.PublicationName = "exactlyonce_publication";
                });

                services.AddKafkaPublisher(options =>
                {
                    options.BootstrapServers = "localhost:9092";
                    options.Topic = "exactlyonce-changes";
                });

                services.AddJsonSerializer();

                // Exactly-once delivery services
                services.Configure<ExactlyOnceDeliveryManagerOptions>(options =>
                {
                    options.EnableExactlyOnce = true;
                    options.DeliveryTimeout = TimeSpan.FromMinutes(5);
                    options.CleanupInterval = TimeSpan.FromMinutes(1);
                });

                services.AddSingleton<ExactlyOnceDeliveryManager>();
            })
            .Build();

        var notifier = host.Services.GetRequiredService<IEntityNotifier>();
        var publisher = host.Services.GetRequiredService<IChangePublisher>();
        var exactlyOnceDeliveryManager = host.Services.GetRequiredService<ExactlyOnceDeliveryManager>();

        await notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            // Deliver with exactly-once semantics
            var result = await exactlyOnceDeliveryManager.DeliverExactlyOnceAsync(changeEvent, publisher);
            
            if (result.IsSuccess)
            {
                Console.WriteLine($"Event delivered successfully in {result.DeliveryAttempts} attempts");
            }
            else
            {
                Console.WriteLine($"Event delivery failed: {result.ErrorMessage}");
            }
        }, CancellationToken.None);
    }
}
```

## Real-world Scenarios

### Example 7: E-commerce Order Processing

```csharp
public class EcommerceOrderProcessing
{
    private readonly IEntityNotifier _notifier;
    private readonly IChangePublisher _publisher;
    private readonly ILogger<EcommerceOrderProcessing> _logger;

    public EcommerceOrderProcessing(
        IEntityNotifier notifier,
        IChangePublisher publisher,
        ILogger<EcommerceOrderProcessing> logger)
    {
        _notifier = notifier;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            switch (changeEvent.Table.ToLower())
            {
                case "orders":
                    await ProcessOrderChangeAsync(changeEvent, cancellationToken);
                    break;
                case "order_items":
                    await ProcessOrderItemChangeAsync(changeEvent, cancellationToken);
                    break;
                case "payments":
                    await ProcessPaymentChangeAsync(changeEvent, cancellationToken);
                    break;
                case "inventory":
                    await ProcessInventoryChangeAsync(changeEvent, cancellationToken);
                    break;
            }
        }, cancellationToken);
    }

    private async Task ProcessOrderChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var order = JsonSerializer.Deserialize<Order>(changeEvent.Data.GetRawText());
        
        if (changeEvent.Operation == "INSERT")
        {
            _logger.LogInformation("New order created: {OrderId}", order.Id);
            
            // Publish order created event
            await _publisher.PublishAsync(changeEvent, cancellationToken);
            
            // Trigger order processing workflow
            await TriggerOrderProcessingWorkflowAsync(order, cancellationToken);
        }
        else if (changeEvent.Operation == "UPDATE")
        {
            var previousOrder = JsonSerializer.Deserialize<Order>(changeEvent.PreviousData);
            
            if (order.Status != previousOrder.Status)
            {
                _logger.LogInformation("Order status changed: {OrderId} from {OldStatus} to {NewStatus}", 
                    order.Id, previousOrder.Status, order.Status);
                
                // Publish status change event
                await _publisher.PublishAsync(changeEvent, cancellationToken);
                
                // Handle status-specific logic
                await HandleOrderStatusChangeAsync(order, previousOrder, cancellationToken);
            }
        }
    }

    private async Task ProcessOrderItemChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var orderItem = JsonSerializer.Deserialize<OrderItem>(changeEvent.Data.GetRawText());
        
        if (changeEvent.Operation == "INSERT")
        {
            _logger.LogInformation("Order item added: {OrderId} - {ProductId}", orderItem.OrderId, orderItem.ProductId);
            
            // Update inventory
            await UpdateInventoryAsync(orderItem.ProductId, -orderItem.Quantity, cancellationToken);
            
            // Publish order item event
            await _publisher.PublishAsync(changeEvent, cancellationToken);
        }
    }

    private async Task ProcessPaymentChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var payment = JsonSerializer.Deserialize<Payment>(changeEvent.Data.GetRawText());
        
        if (changeEvent.Operation == "INSERT")
        {
            _logger.LogInformation("Payment processed: {PaymentId} for order {OrderId}", payment.Id, payment.OrderId);
            
            // Update order status
            await UpdateOrderStatusAsync(payment.OrderId, "paid", cancellationToken);
            
            // Publish payment event
            await _publisher.PublishAsync(changeEvent, cancellationToken);
        }
    }

    private async Task ProcessInventoryChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var inventory = JsonSerializer.Deserialize<Inventory>(changeEvent.Data.GetRawText());
        
        if (changeEvent.Operation == "UPDATE")
        {
            var previousInventory = JsonSerializer.Deserialize<Inventory>(changeEvent.PreviousData);
            
            if (inventory.Stock < previousInventory.Stock)
            {
                _logger.LogInformation("Inventory decreased: {ProductId} from {OldStock} to {NewStock}", 
                    inventory.ProductId, previousInventory.Stock, inventory.Stock);
                
                // Check if stock is low
                if (inventory.Stock < inventory.ReorderLevel)
                {
                    await TriggerReorderAsync(inventory.ProductId, cancellationToken);
                }
            }
        }
    }

    private async Task TriggerOrderProcessingWorkflowAsync(Order order, CancellationToken cancellationToken)
    {
        // Implement order processing workflow
        // This could include inventory checks, payment processing, etc.
    }

    private async Task HandleOrderStatusChangeAsync(Order order, Order previousOrder, CancellationToken cancellationToken)
    {
        switch (order.Status)
        {
            case "shipped":
                await SendShippingNotificationAsync(order, cancellationToken);
                break;
            case "delivered":
                await SendDeliveryNotificationAsync(order, cancellationToken);
                break;
            case "cancelled":
                await HandleOrderCancellationAsync(order, cancellationToken);
                break;
        }
    }

    private async Task UpdateInventoryAsync(int productId, int quantityChange, CancellationToken cancellationToken)
    {
        // Update inventory in database
    }

    private async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken)
    {
        // Update order status in database
    }

    private async Task TriggerReorderAsync(int productId, CancellationToken cancellationToken)
    {
        // Trigger reorder process
    }

    private async Task SendShippingNotificationAsync(Order order, CancellationToken cancellationToken)
    {
        // Send shipping notification
    }

    private async Task SendDeliveryNotificationAsync(Order order, CancellationToken cancellationToken)
    {
        // Send delivery notification
    }

    private async Task HandleOrderCancellationAsync(Order order, CancellationToken cancellationToken)
    {
        // Handle order cancellation
    }
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Stock { get; set; }
    public int ReorderLevel { get; set; }
}
```

### Example 8: Real-time Analytics

```csharp
public class RealTimeAnalytics
{
    private readonly IEntityNotifier _notifier;
    private readonly IChangePublisher _publisher;
    private readonly ILogger<RealTimeAnalytics> _logger;
    private readonly Dictionary<string, int> _eventCounts = new();
    private readonly Timer _analyticsTimer;

    public RealTimeAnalytics(
        IEntityNotifier notifier,
        IChangePublisher publisher,
        ILogger<RealTimeAnalytics> logger)
    {
        _notifier = notifier;
        _publisher = publisher;
        _logger = logger;
        
        // Set up analytics timer
        _analyticsTimer = new Timer(GenerateAnalyticsReport, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            // Track event counts
            var key = $"{changeEvent.Source}.{changeEvent.Table}.{changeEvent.Operation}";
            _eventCounts[key] = _eventCounts.GetValueOrDefault(key, 0) + 1;

            // Process for real-time analytics
            await ProcessAnalyticsEventAsync(changeEvent, cancellationToken);
        }, cancellationToken);
    }

    private async Task ProcessAnalyticsEventAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        // Create analytics event
        var analyticsEvent = new AnalyticsEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Source = changeEvent.Source,
            Table = changeEvent.Table,
            Operation = changeEvent.Operation,
            Timestamp = changeEvent.Timestamp,
            Data = changeEvent.Data,
            Metadata = changeEvent.Metadata
        };

        // Publish to analytics stream
        await _publisher.PublishAsync(analyticsEvent, cancellationToken);

        // Update real-time metrics
        await UpdateRealTimeMetricsAsync(analyticsEvent, cancellationToken);
    }

    private async Task UpdateRealTimeMetricsAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken)
    {
        // Update real-time metrics
        // This could include updating dashboards, sending alerts, etc.
        
        if (analyticsEvent.Operation == "INSERT" && analyticsEvent.Table == "orders")
        {
            var order = JsonSerializer.Deserialize<Order>(analyticsEvent.Data.GetRawText());
            
            // Update order metrics
            await UpdateOrderMetricsAsync(order, cancellationToken);
        }
    }

    private async Task UpdateOrderMetricsAsync(Order order, CancellationToken cancellationToken)
    {
        // Update order metrics
        // This could include updating counters, calculating averages, etc.
    }

    private void GenerateAnalyticsReport(object state)
    {
        _logger.LogInformation("Analytics Report - Event Counts:");
        foreach (var kvp in _eventCounts)
        {
            _logger.LogInformation("  {Key}: {Count}", kvp.Key, kvp.Value);
        }
        
        // Clear counts for next period
        _eventCounts.Clear();
    }

    public void Dispose()
    {
        _analyticsTimer?.Dispose();
    }
}

public class AnalyticsEvent
{
    public string EventId { get; set; }
    public string Source { get; set; }
    public string Table { get; set; }
    public string Operation { get; set; }
    public DateTime Timestamp { get; set; }
    public JsonElement Data { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

## Integration Examples

### Example 9: ASP.NET Core Integration

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        
        // Add SQLDBEntityNotifier
        services.AddPostgresAdapter(options =>
        {
            options.ConnectionString = Configuration.GetConnectionString("PostgreSQL");
            options.SlotName = "webapp_slot";
            options.PublicationName = "webapp_publication";
        });

        services.AddKafkaPublisher(options =>
        {
            options.BootstrapServers = Configuration["Kafka:BootstrapServers"];
            options.Topic = "webapp-changes";
        });

        services.AddJsonSerializer();

        // Add hosted service
        services.AddHostedService<ChangeEventService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

public class ChangeEventService : BackgroundService
{
    private readonly IEntityNotifier _notifier;
    private readonly IChangePublisher _publisher;
    private readonly ILogger<ChangeEventService> _logger;

    public ChangeEventService(
        IEntityNotifier notifier,
        IChangePublisher publisher,
        ILogger<ChangeEventService> logger)
    {
        _notifier = notifier;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            _logger.LogInformation("Processing change: {Operation} on {Table}", 
                changeEvent.Operation, changeEvent.Table);
            
            await _publisher.PublishAsync(changeEvent, cancellationToken);
        }, stoppingToken);
    }
}

[ApiController]
[Route("api/[controller]")]
public class ChangeEventsController : ControllerBase
{
    private readonly IChangeEventMetrics _metrics;
    private readonly IHealthCheckService _healthCheckService;

    public ChangeEventsController(
        IChangeEventMetrics metrics,
        IHealthCheckService healthCheckService)
    {
        _metrics = metrics;
        _healthCheckService = healthCheckService;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _metrics.GetMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        var health = await _healthCheckService.GetHealthChecksAsync();
        return Ok(health);
    }
}
```

### Example 10: Azure Functions Integration

```csharp
public class ChangeEventFunction
{
    private readonly IEntityNotifier _notifier;
    private readonly IChangePublisher _publisher;
    private readonly ILogger<ChangeEventFunction> _logger;

    public ChangeEventFunction(
        IEntityNotifier notifier,
        IChangePublisher publisher,
        ILogger<ChangeEventFunction> logger)
    {
        _notifier = notifier;
        _publisher = publisher;
        _logger = logger;
    }

    [FunctionName("ProcessChangeEvent")]
    public async Task ProcessChangeEvent(
        [ServiceBusTrigger("change-events", Connection = "ServiceBusConnection")] string message,
        ILogger log)
    {
        try
        {
            var changeEvent = JsonSerializer.Deserialize<ChangeEvent>(message);
            
            log.LogInformation("Processing change event: {Operation} on {Table}", 
                changeEvent.Operation, changeEvent.Table);
            
            // Process the change event
            await ProcessChangeEventAsync(changeEvent);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error processing change event");
            throw;
        }
    }

    private async Task ProcessChangeEventAsync(ChangeEvent changeEvent)
    {
        // Process the change event
        // This could include updating external systems, sending notifications, etc.
        
        await _publisher.PublishAsync(changeEvent);
    }
}
```

## Performance Examples

### Example 11: High-Throughput Processing

```csharp
public class HighThroughputProcessing
{
    private readonly IEntityNotifier _notifier;
    private readonly IChangePublisher _publisher;
    private readonly ILogger<HighThroughputProcessing> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly Channel<ChangeEvent> _channel;

    public HighThroughputProcessing(
        IEntityNotifier notifier,
        IChangePublisher publisher,
        ILogger<HighThroughputProcessing> logger)
    {
        _notifier = notifier;
        _publisher = publisher;
        _logger = logger;
        
        // Limit concurrent processing
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);
        
        // Create channel for batching
        _channel = Channel.CreateUnbounded<ChangeEvent>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Start producer
        _ = Task.Run(() => ProduceEventsAsync(cancellationToken), cancellationToken);
        
        // Start consumers
        var consumerTasks = Enumerable.Range(0, Environment.ProcessorCount)
            .Select(_ => Task.Run(() => ConsumeEventsAsync(cancellationToken), cancellationToken))
            .ToArray();

        // Start change detection
        await _notifier.StartAsync(async (changeEvent, ct) =>
        {
            await _channel.Writer.WriteAsync(changeEvent, ct);
        }, cancellationToken);

        await Task.WhenAll(consumerTasks);
    }

    private async Task ProduceEventsAsync(CancellationToken cancellationToken)
    {
        await _notifier.StartAsync(async (changeEvent, ct) =>
        {
            await _channel.Writer.WriteAsync(changeEvent, ct);
        }, cancellationToken);
    }

    private async Task ConsumeEventsAsync(CancellationToken cancellationToken)
    {
        await foreach (var changeEvent in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                await ProcessChangeEventAsync(changeEvent, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    private async Task ProcessChangeEventAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        // Process the change event
        await _publisher.PublishAsync(changeEvent, cancellationToken);
    }
}
```

### Example 12: Batch Processing

```csharp
public class BatchProcessing
{
    private readonly IEntityNotifier _notifier;
    private readonly IChangePublisher _publisher;
    private readonly ILogger<BatchProcessing> _logger;
    private readonly List<ChangeEvent> _batch = new();
    private readonly Timer _batchTimer;
    private readonly object _batchLock = new object();

    public BatchProcessing(
        IEntityNotifier notifier,
        IChangePublisher publisher,
        ILogger<BatchProcessing> logger)
    {
        _notifier = notifier;
        _publisher = publisher;
        _logger = logger;
        
        // Set up batch timer
        _batchTimer = new Timer(ProcessBatch, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _notifier.StartAsync(async (changeEvent, cancellationToken) =>
        {
            lock (_batchLock)
            {
                _batch.Add(changeEvent);
                
                // Process batch if it's full
                if (_batch.Count >= 100)
                {
                    ProcessBatch(null);
                }
            }
        }, cancellationToken);
    }

    private void ProcessBatch(object state)
    {
        List<ChangeEvent> batchToProcess;
        
        lock (_batchLock)
        {
            if (_batch.Count == 0)
                return;
                
            batchToProcess = new List<ChangeEvent>(_batch);
            _batch.Clear();
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessBatchAsync(batchToProcess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch of {Count} events", batchToProcess.Count);
            }
        });
    }

    private async Task ProcessBatchAsync(List<ChangeEvent> batch)
    {
        _logger.LogInformation("Processing batch of {Count} events", batch.Count);
        
        // Process batch
        foreach (var changeEvent in batch)
        {
            await _publisher.PublishAsync(changeEvent);
        }
        
        _logger.LogInformation("Completed processing batch of {Count} events", batch.Count);
    }

    public void Dispose()
    {
        _batchTimer?.Dispose();
    }
}
```

These examples demonstrate various ways to use SQLDBEntityNotifier in different scenarios, from simple change detection to complex real-world applications. Each example includes proper error handling, logging, and performance considerations.