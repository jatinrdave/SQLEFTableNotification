# SQLDBEntityNotifier - Comprehensive Developer Guide

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [NuGet Packages](#nuget-packages)
4. [Core Features](#core-features)
5. [Database Adapters](#database-adapters)
6. [Publishers](#publishers)
7. [Serializers](#serializers)
8. [Advanced Features](#advanced-features)
9. [Monitoring & Observability](#monitoring--observability)
10. [Examples & Tutorials](#examples--tutorials)
11. [Troubleshooting](#troubleshooting)
12. [API Reference](#api-reference)

## Overview

SQLDBEntityNotifier is a comprehensive .NET library for Change Data Capture (CDC) that provides real-time database change notifications with support for multiple databases, publishers, and advanced features like multi-tenancy, transactional grouping, and exactly-once delivery.

### Key Features

- **Multi-Database Support**: PostgreSQL, SQLite, MySQL, Oracle
- **Multiple Publishers**: Kafka, RabbitMQ, Webhooks, Azure Event Hubs
- **Serialization Formats**: JSON, Protobuf, Avro
- **Advanced Features**: Multi-tenancy, transactional grouping, exactly-once delivery
- **Monitoring**: Comprehensive metrics, health checks, and tracing
- **Performance**: High-throughput, low-latency change detection

## Quick Start

### Installation

```bash
# Core package
dotnet add package SqlDbEntityNotifier.Core

# Database adapters
dotnet add package SqlDbEntityNotifier.Adapters.Postgres
dotnet add package SqlDbEntityNotifier.Adapters.Sqlite
dotnet add package SqlDbEntityNotifier.Adapters.MySQL
dotnet add package SqlDbEntityNotifier.Adapters.Oracle

# Publishers
dotnet add package SqlDbEntityNotifier.Publisher.Kafka
dotnet add package SqlDbEntityNotifier.Publisher.RabbitMQ
dotnet add package SqlDbEntityNotifier.Publisher.Webhook
dotnet add package SqlDbEntityNotifier.Publisher.AzureEventHubs

# Serializers
dotnet add package SqlDbEntityNotifier.Serializers.Json
dotnet add package SqlDbEntityNotifier.Serializers.Protobuf
dotnet add package SqlDbEntityNotifier.Serializers.Avro

# Advanced features
dotnet add package SqlDbEntityNotifier.MultiTenant
dotnet add package SqlDbEntityNotifier.Transactional
dotnet add package SqlDbEntityNotifier.Delivery

# Monitoring
dotnet add package SqlDbEntityNotifier.Monitoring
dotnet add package SqlDbEntityNotifier.Tracing
```

### Basic Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Adapters.Postgres;
using SqlDbEntityNotifier.Publisher.Kafka;
using SqlDbEntityNotifier.Serializers.Json;

// Configure services
var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddPostgresAdapter(options =>
        {
            options.ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass";
            options.SlotName = "my_slot";
            options.PublicationName = "my_publication";
        });

        services.AddKafkaPublisher(options =>
        {
            options.BootstrapServers = "localhost:9092";
            options.Topic = "my-topic";
        });

        services.AddJsonSerializer();
    })
    .Build();

// Get services
var notifier = host.Services.GetRequiredService<IEntityNotifier>();
var publisher = host.Services.GetRequiredService<IChangePublisher>();

// Start listening for changes
await notifier.StartAsync(async (changeEvent, cancellationToken) =>
{
    // Process the change event
    Console.WriteLine($"Change detected: {changeEvent.Operation} on {changeEvent.Table}");
    
    // Publish to Kafka
    await publisher.PublishAsync(changeEvent);
}, CancellationToken.None);
```

## NuGet Packages

### Core Packages

#### SqlDbEntityNotifier.Core
The main package containing core interfaces and models.

```xml
<PackageReference Include="SqlDbEntityNotifier.Core" Version="1.0.0" />
```

**Key Classes:**
- `IEntityNotifier` - Main interface for change detection
- `IChangePublisher` - Interface for publishing changes
- `ISerializer` - Interface for serialization
- `ChangeEvent` - Core change event model

### Database Adapters

#### SqlDbEntityNotifier.Adapters.Postgres
PostgreSQL adapter using logical replication.

```xml
<PackageReference Include="SqlDbEntityNotifier.Adapters.Postgres" Version="1.0.0" />
```

**Features:**
- Logical replication slots
- WAL-based change detection
- Automatic slot management
- Heartbeat monitoring

#### SqlDbEntityNotifier.Adapters.Sqlite
SQLite adapter using WAL mode and triggers.

```xml
<PackageReference Include="SqlDbEntityNotifier.Adapters.Sqlite" Version="1.0.0" />
```

**Features:**
- WAL mode support
- Trigger-based change detection
- In-memory database support
- Lightweight implementation

#### SqlDbEntityNotifier.Adapters.MySQL
MySQL adapter using binary log replication.

```xml
<PackageReference Include="SqlDbEntityNotifier.Adapters.MySQL" Version="1.0.0" />
```

**Features:**
- Binary log replication
- GTID support
- Automatic failover
- Row-based replication

#### SqlDbEntityNotifier.Adapters.Oracle
Oracle adapter using redo log mining.

```xml
<PackageReference Include="SqlDbEntityNotifier.Adapters.Oracle" Version="1.0.0" />
```

**Features:**
- Redo log mining
- Flashback query support
- Automatic log switching
- Enterprise features

### Publishers

#### SqlDbEntityNotifier.Publisher.Kafka
Kafka publisher for high-throughput messaging.

```xml
<PackageReference Include="SqlDbEntityNotifier.Publisher.Kafka" Version="1.0.0" />
```

**Features:**
- High-throughput publishing
- Automatic partitioning
- Exactly-once semantics
- Schema registry integration

#### SqlDbEntityNotifier.Publisher.RabbitMQ
RabbitMQ publisher for reliable messaging.

```xml
<PackageReference Include="SqlDbEntityNotifier.Publisher.RabbitMQ" Version="1.0.0" />
```

**Features:**
- Reliable message delivery
- Exchange routing
- Dead letter queues
- Clustering support

#### SqlDbEntityNotifier.Publisher.Webhook
Webhook publisher for HTTP-based notifications.

```xml
<PackageReference Include="SqlDbEntityNotifier.Publisher.Webhook" Version="1.0.0" />
```

**Features:**
- HTTP/HTTPS support
- Retry mechanisms
- Authentication
- Custom headers

#### SqlDbEntityNotifier.Publisher.AzureEventHubs
Azure Event Hubs publisher for cloud messaging.

```xml
<PackageReference Include="SqlDbEntityNotifier.Publisher.AzureEventHubs" Version="1.0.0" />
```

**Features:**
- Azure integration
- Automatic scaling
- Event batching
- Managed identity

### Serializers

#### SqlDbEntityNotifier.Serializers.Json
JSON serializer for human-readable format.

```xml
<PackageReference Include="SqlDbEntityNotifier.Serializers.Json" Version="1.0.0" />
```

**Features:**
- Human-readable format
- Schema evolution
- Compression support
- Custom converters

#### SqlDbEntityNotifier.Serializers.Protobuf
Protobuf serializer for efficient binary format.

```xml
<PackageReference Include="SqlDbEntityNotifier.Serializers.Protobuf" Version="1.0.0" />
```

**Features:**
- Efficient binary format
- Schema evolution
- Compression support
- Cross-language compatibility

#### SqlDbEntityNotifier.Serializers.Avro
Avro serializer with schema registry integration.

```xml
<PackageReference Include="SqlDbEntityNotifier.Serializers.Avro" Version="1.0.0" />
```

**Features:**
- Schema registry integration
- Schema evolution
- Compression support
- Apache ecosystem integration

### Advanced Features

#### SqlDbEntityNotifier.MultiTenant
Multi-tenant support with tenant isolation.

```xml
<PackageReference Include="SqlDbEntityNotifier.MultiTenant" Version="1.0.0" />
```

**Features:**
- Tenant isolation
- Resource limits
- Throttling per tenant
- Tenant management

#### SqlDbEntityNotifier.Transactional
Transactional grouping for exactly-once semantics.

```xml
<PackageReference Include="SqlDbEntityNotifier.Transactional" Version="1.0.0" />
```

**Features:**
- Transactional grouping
- Exactly-once delivery
- Transaction management
- Rollback support

#### SqlDbEntityNotifier.Delivery
Delivery guarantees and retry mechanisms.

```xml
<PackageReference Include="SqlDbEntityNotifier.Delivery" Version="1.0.0" />
```

**Features:**
- Delivery guarantees
- Retry mechanisms
- Dead letter queues
- Delivery tracking

### Monitoring

#### SqlDbEntityNotifier.Monitoring
Comprehensive monitoring and metrics.

```xml
<PackageReference Include="SqlDbEntityNotifier.Monitoring" Version="1.0.0" />
```

**Features:**
- Performance metrics
- Health checks
- Custom dashboards
- Alerting

#### SqlDbEntityNotifier.Tracing
Distributed tracing with OpenTelemetry.

```xml
<PackageReference Include="SqlDbEntityNotifier.Tracing" Version="1.0.0" />
```

**Features:**
- OpenTelemetry integration
- Distributed tracing
- Performance profiling
- Error tracking

## Core Features

### Change Event Model

The `ChangeEvent` class represents a database change:

```csharp
public class ChangeEvent
{
    public string Source { get; set; }           // Database source
    public string Schema { get; set; }           // Database schema
    public string Table { get; set; }            // Table name
    public string Operation { get; set; }        // INSERT, UPDATE, DELETE
    public string Offset { get; set; }           // Unique identifier
    public string? PreviousData { get; set; }    // Previous row data
    public JsonElement Data { get; set; }        // Current row data
    public Dictionary<string, string> Metadata { get; set; } // Additional metadata
    public DateTime Timestamp { get; set; }      // Event timestamp
}
```

### Entity Notifier Interface

```csharp
public interface IEntityNotifier
{
    Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

### Change Publisher Interface

```csharp
public interface IChangePublisher
{
    Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);
}
```

### Serializer Interface

```csharp
public interface ISerializer
{
    Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);
    Task<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);
}
```

## Database Adapters

### PostgreSQL Adapter

```csharp
using SqlDbEntityNotifier.Adapters.Postgres;

// Configuration
services.Configure<PostgresAdapterOptions>(options =>
{
    options.ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass";
    options.SlotName = "my_slot";
    options.PublicationName = "my_publication";
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
    options.MaxReplicationLag = TimeSpan.FromMinutes(5);
});

// Usage
var adapter = services.GetRequiredService<IDbAdapter>();
await adapter.StartAsync(async (changeEvent, cancellationToken) =>
{
    Console.WriteLine($"PostgreSQL change: {changeEvent.Operation} on {changeEvent.Table}");
}, CancellationToken.None);
```

### SQLite Adapter

```csharp
using SqlDbEntityNotifier.Adapters.Sqlite;

// Configuration
services.Configure<SqliteAdapterOptions>(options =>
{
    options.ConnectionString = "Data Source=mydb.db";
    options.EnableWAL = true;
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
});

// Usage
var adapter = services.GetRequiredService<IDbAdapter>();
await adapter.StartAsync(async (changeEvent, cancellationToken) =>
{
    Console.WriteLine($"SQLite change: {changeEvent.Operation} on {changeEvent.Table}");
}, CancellationToken.None);
```

### MySQL Adapter

```csharp
using SqlDbEntityNotifier.Adapters.MySQL;

// Configuration
services.Configure<MySQLAdapterOptions>(options =>
{
    options.ConnectionString = "Server=localhost;Database=mydb;Uid=user;Pwd=pass;";
    options.ServerId = 1;
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
    options.MaxReplicationLag = TimeSpan.FromMinutes(5);
});

// Usage
var adapter = services.GetRequiredService<IDbAdapter>();
await adapter.StartAsync(async (changeEvent, cancellationToken) =>
{
    Console.WriteLine($"MySQL change: {changeEvent.Operation} on {changeEvent.Table}");
}, CancellationToken.None);
```

### Oracle Adapter

```csharp
using SqlDbEntityNotifier.Adapters.Oracle;

// Configuration
services.Configure<OracleAdapterOptions>(options =>
{
    options.ConnectionString = "Data Source=localhost:1521/XE;User Id=user;Password=pass;";
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
    options.MaxReplicationLag = TimeSpan.FromMinutes(5);
});

// Usage
var adapter = services.GetRequiredService<IDbAdapter>();
await adapter.StartAsync(async (changeEvent, cancellationToken) =>
{
    Console.WriteLine($"Oracle change: {changeEvent.Operation} on {changeEvent.Table}");
}, CancellationToken.None);
```

## Publishers

### Kafka Publisher

```csharp
using SqlDbEntityNotifier.Publisher.Kafka;

// Configuration
services.Configure<KafkaPublisherOptions>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.Topic = "my-topic";
    options.Acks = "all";
    options.RetryBackoffMs = 100;
    options.MaxRetries = 3;
});

// Usage
var publisher = services.GetRequiredService<IChangePublisher>();
await publisher.PublishAsync(changeEvent);
```

### RabbitMQ Publisher

```csharp
using SqlDbEntityNotifier.Publisher.RabbitMQ;

// Configuration
services.Configure<RabbitMQPublisherOptions>(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.Exchange = "my-exchange";
    options.RoutingKey = "my-routing-key";
});

// Usage
var publisher = services.GetRequiredService<IChangePublisher>();
await publisher.PublishAsync(changeEvent);
```

### Webhook Publisher

```csharp
using SqlDbEntityNotifier.Publisher.Webhook;

// Configuration
services.Configure<WebhookPublisherOptions>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Endpoint = "/webhook";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.RetryCount = 3;
    options.RetryDelay = TimeSpan.FromSeconds(1);
});

// Usage
var publisher = services.GetRequiredService<IChangePublisher>();
await publisher.PublishAsync(changeEvent);
```

### Azure Event Hubs Publisher

```csharp
using SqlDbEntityNotifier.Publisher.AzureEventHubs;

// Configuration
services.Configure<AzureEventHubsPublisherOptions>(options =>
{
    options.ConnectionString = "Endpoint=sb://myhub.servicebus.windows.net/;SharedAccessKeyName=mykey;SharedAccessKey=mysecret";
    options.EventHubName = "my-hub";
    options.BatchSize = 100;
    options.FlushInterval = TimeSpan.FromSeconds(1);
});

// Usage
var publisher = services.GetRequiredService<IChangePublisher>();
await publisher.PublishAsync(changeEvent);
```

## Serializers

### JSON Serializer

```csharp
using SqlDbEntityNotifier.Serializers.Json;

// Configuration
services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = false;
});

// Usage
var serializer = services.GetRequiredService<ISerializer>();
var data = await serializer.SerializeAsync(changeEvent);
var deserialized = await serializer.DeserializeAsync<ChangeEvent>(data);
```

### Protobuf Serializer

```csharp
using SqlDbEntityNotifier.Serializers.Protobuf;

// Configuration
services.Configure<ProtobufSerializerOptions>(options =>
{
    options.UseCompression = true;
    options.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});

// Usage
var serializer = services.GetRequiredService<ISerializer>();
var data = await serializer.SerializeAsync(changeEvent);
var deserialized = await serializer.DeserializeAsync<ChangeEvent>(data);
```

### Avro Serializer

```csharp
using SqlDbEntityNotifier.Serializers.Avro;

// Configuration
services.Configure<AvroSerializerOptions>(options =>
{
    options.SchemaRegistryUrl = "http://localhost:8081";
    options.UseCompression = true;
    options.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});

// Usage
var serializer = services.GetRequiredService<ISerializer>();
var data = await serializer.SerializeAsync(changeEvent);
var deserialized = await serializer.DeserializeAsync<ChangeEvent>(data);
```

## Advanced Features

### Multi-Tenant Support

```csharp
using SqlDbEntityNotifier.MultiTenant;

// Configuration
services.Configure<TenantManagerOptions>(options =>
{
    options.EnableMultiTenancy = true;
    options.DefaultTenantId = "default";
});

// Usage
var tenantManager = services.GetRequiredService<TenantManager>();

// Register tenant
var tenant = new TenantContext
{
    TenantId = "tenant-1",
    TenantName = "My Tenant",
    IsActive = true,
    ResourceLimits = new TenantResourceLimits
    {
        MaxConnections = 100,
        MaxEventsPerSecond = 1000
    }
};

await tenantManager.RegisterTenantAsync(tenant);
await tenantManager.ActivateTenantAsync("tenant-1");

// Check throttling
var throttlingManager = services.GetRequiredService<ThrottlingManager>();
var result = await throttlingManager.CheckThrottlingAsync("tenant-1", ThrottlingRequestType.EventProcessing);
```

### Transactional Grouping

```csharp
using SqlDbEntityNotifier.Transactional;

// Configuration
services.Configure<TransactionalGroupManagerOptions>(options =>
{
    options.MaxTransactionSize = 10000;
    options.TransactionTimeout = TimeSpan.FromMinutes(5);
    options.CleanupInterval = TimeSpan.FromMinutes(1);
});

// Usage
var transactionalGroupManager = services.GetRequiredService<TransactionalGroupManager>();

// Start transaction
var transactionId = Guid.NewGuid().ToString();
var transaction = await transactionalGroupManager.StartTransactionAsync(transactionId, "my-source");

// Add change events
await transactionalGroupManager.AddChangeEventAsync(transactionId, changeEvent1);
await transactionalGroupManager.AddChangeEventAsync(transactionId, changeEvent2);

// Commit transaction
await transactionalGroupManager.CommitTransactionAsync(transactionId);
```

### Exactly-Once Delivery

```csharp
using SqlDbEntityNotifier.Delivery;

// Configuration
services.Configure<ExactlyOnceDeliveryManagerOptions>(options =>
{
    options.EnableExactlyOnce = true;
    options.DeliveryTimeout = TimeSpan.FromMinutes(5);
    options.CleanupInterval = TimeSpan.FromMinutes(1);
});

// Usage
var exactlyOnceDeliveryManager = services.GetRequiredService<ExactlyOnceDeliveryManager>();
var publisher = services.GetRequiredService<IChangePublisher>();

var result = await exactlyOnceDeliveryManager.DeliverExactlyOnceAsync(changeEvent, publisher);
if (result.IsSuccess)
{
    Console.WriteLine($"Event delivered successfully in {result.DeliveryAttempts} attempts");
}
else
{
    Console.WriteLine($"Event delivery failed: {result.ErrorMessage}");
}
```

## Monitoring & Observability

### Metrics

```csharp
using SqlDbEntityNotifier.Monitoring;

// Configuration
services.Configure<ChangeEventMetricsOptions>(options =>
{
    options.EnableMetrics = true;
    options.MetricsInterval = TimeSpan.FromSeconds(10);
});

// Usage
var metrics = services.GetRequiredService<IChangeEventMetrics>();
var metricsData = await metrics.GetMetricsAsync();

Console.WriteLine($"Total events: {metricsData.TotalEvents}");
Console.WriteLine($"Events per second: {metricsData.EventsPerSecond}");
Console.WriteLine($"Average latency: {metricsData.AverageLatency}ms");
```

### Health Checks

```csharp
using SqlDbEntityNotifier.Monitoring;

// Configuration
services.Configure<HealthCheckServiceOptions>(options =>
{
    options.EnableHealthChecks = true;
    options.HealthCheckInterval = TimeSpan.FromSeconds(30);
});

// Usage
var healthCheckService = services.GetRequiredService<IHealthCheckService>();
var healthChecks = await healthCheckService.GetHealthChecksAsync();

Console.WriteLine($"Overall status: {healthChecks.OverallStatus}");
foreach (var check in healthChecks.Checks)
{
    Console.WriteLine($"{check.Name}: {check.Status} - {check.Description}");
}
```

### Tracing

```csharp
using SqlDbEntityNotifier.Tracing;

// Configuration
services.Configure<ChangeEventTracerOptions>(options =>
{
    options.EnableTracing = true;
    options.TraceLevel = System.Diagnostics.ActivitySource.DefaultActivitySourceName;
});

// Usage
var tracer = services.GetRequiredService<ChangeEventTracer>();

using var activity = tracer.StartChangeEventTrace(changeEvent, "process_change");
tracer.SetTag("table", changeEvent.Table);
tracer.SetTag("operation", changeEvent.Operation);
tracer.RecordEvent("change_processed");
tracer.SetStatus(ActivityStatusCode.Ok);
```

## Examples & Tutorials

### Complete Example: E-commerce Order Processing

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Adapters.Postgres;
using SqlDbEntityNotifier.Publisher.Kafka;
using SqlDbEntityNotifier.Serializers.Json;
using SqlDbEntityNotifier.MultiTenant;
using SqlDbEntityNotifier.Transactional;

public class OrderProcessingService
{
    private readonly IEntityNotifier _notifier;
    private readonly IChangePublisher _publisher;
    private readonly TenantManager _tenantManager;
    private readonly TransactionalGroupManager _transactionalGroupManager;

    public OrderProcessingService(
        IEntityNotifier notifier,
        IChangePublisher publisher,
        TenantManager tenantManager,
        TransactionalGroupManager transactionalGroupManager)
    {
        _notifier = notifier;
        _publisher = publisher;
        _tenantManager = tenantManager;
        _transactionalGroupManager = transactionalGroupManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Set up tenant
        var tenant = new TenantContext
        {
            TenantId = "ecommerce-tenant",
            TenantName = "E-commerce Platform",
            IsActive = true,
            ResourceLimits = new TenantResourceLimits
            {
                MaxConnections = 200,
                MaxEventsPerSecond = 2000
            }
        };

        await _tenantManager.RegisterTenantAsync(tenant);
        await _tenantManager.ActivateTenantAsync("ecommerce-tenant");

        // Start listening for changes
        await _notifier.StartAsync(async (changeEvent, ct) =>
        {
            await ProcessOrderChangeAsync(changeEvent, ct);
        }, cancellationToken);
    }

    private async Task ProcessOrderChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        if (changeEvent.Table == "orders")
        {
            // Process order changes
            await ProcessOrderEventAsync(changeEvent, cancellationToken);
        }
        else if (changeEvent.Table == "order_items")
        {
            // Process order item changes
            await ProcessOrderItemEventAsync(changeEvent, cancellationToken);
        }
    }

    private async Task ProcessOrderEventAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var orderData = JsonSerializer.Deserialize<Order>(changeEvent.Data.GetRawText());
        
        if (changeEvent.Operation == "INSERT")
        {
            // New order created
            await _publisher.PublishAsync(changeEvent, cancellationToken);
        }
        else if (changeEvent.Operation == "UPDATE")
        {
            // Order updated
            var previousOrder = JsonSerializer.Deserialize<Order>(changeEvent.PreviousData);
            
            if (orderData.Status != previousOrder.Status)
            {
                // Status changed - publish status update
                await _publisher.PublishAsync(changeEvent, cancellationToken);
            }
        }
    }

    private async Task ProcessOrderItemEventAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        // Process order item changes
        await _publisher.PublishAsync(changeEvent, cancellationToken);
    }
}

public class Order
{
    public int Id { get; set; }
    public string CustomerId { get; set; }
    public string Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Configuration Example

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Database adapter
                services.AddPostgresAdapter(options =>
                {
                    options.ConnectionString = context.Configuration.GetConnectionString("PostgreSQL");
                    options.SlotName = "order_processing_slot";
                    options.PublicationName = "order_processing_publication";
                    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
                    options.MaxReplicationLag = TimeSpan.FromMinutes(5);
                });

                // Publisher
                services.AddKafkaPublisher(options =>
                {
                    options.BootstrapServers = context.Configuration["Kafka:BootstrapServers"];
                    options.Topic = "order-events";
                    options.Acks = "all";
                    options.RetryBackoffMs = 100;
                    options.MaxRetries = 3;
                });

                // Serializer
                services.AddJsonSerializer(options =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.WriteIndented = false;
                });

                // Multi-tenant
                services.Configure<TenantManagerOptions>(options =>
                {
                    options.EnableMultiTenancy = true;
                    options.DefaultTenantId = "default";
                });

                // Transactional grouping
                services.Configure<TransactionalGroupManagerOptions>(options =>
                {
                    options.MaxTransactionSize = 10000;
                    options.TransactionTimeout = TimeSpan.FromMinutes(5);
                    options.CleanupInterval = TimeSpan.FromMinutes(1);
                });

                // Monitoring
                services.Configure<ChangeEventMetricsOptions>(options =>
                {
                    options.EnableMetrics = true;
                    options.MetricsInterval = TimeSpan.FromSeconds(10);
                });

                // Health checks
                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    options.EnableHealthChecks = true;
                    options.HealthCheckInterval = TimeSpan.FromSeconds(30);
                });

                // Tracing
                services.Configure<ChangeEventTracerOptions>(options =>
                {
                    options.EnableTracing = true;
                    options.TraceLevel = System.Diagnostics.ActivitySource.DefaultActivitySourceName;
                });

                // Register services
                services.AddSingleton<OrderProcessingService>();
                services.AddHostedService<OrderProcessingService>();
            });
    }
}
```

## Troubleshooting

### Common Issues

#### 1. PostgreSQL Replication Slot Issues

**Problem**: Replication slot not found or already exists.

**Solution**:
```sql
-- Check existing slots
SELECT * FROM pg_replication_slots;

-- Drop existing slot if needed
SELECT pg_drop_replication_slot('my_slot');

-- Create new slot
SELECT pg_create_logical_replication_slot('my_slot', 'pgoutput');
```

#### 2. MySQL Binary Log Issues

**Problem**: Binary logging not enabled.

**Solution**:
```sql
-- Check binary log status
SHOW VARIABLES LIKE 'log_bin';

-- Enable binary logging
SET GLOBAL log_bin = ON;
SET GLOBAL binlog_format = 'ROW';
```

#### 3. Oracle Redo Log Issues

**Problem**: Redo log mining not working.

**Solution**:
```sql
-- Check redo log status
SELECT * FROM V$LOG;

-- Enable supplemental logging
ALTER DATABASE ADD SUPPLEMENTAL LOG DATA;
```

#### 4. Kafka Connection Issues

**Problem**: Cannot connect to Kafka.

**Solution**:
```csharp
// Check Kafka configuration
services.Configure<KafkaPublisherOptions>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.SecurityProtocol = SecurityProtocol.Plaintext;
    options.SaslMechanism = SaslMechanism.Plain;
    options.SaslUsername = "username";
    options.SaslPassword = "password";
});
```

#### 5. Memory Issues

**Problem**: High memory usage.

**Solution**:
```csharp
// Configure memory limits
services.Configure<TransactionalGroupManagerOptions>(options =>
{
    options.MaxTransactionSize = 1000; // Reduce transaction size
    options.CleanupInterval = TimeSpan.FromSeconds(30); // More frequent cleanup
});
```

### Performance Tuning

#### 1. Database Connection Pooling

```csharp
services.Configure<PostgresAdapterOptions>(options =>
{
    options.ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass;Pooling=true;MinPoolSize=5;MaxPoolSize=100;";
});
```

#### 2. Batch Processing

```csharp
services.Configure<BulkOperationDetectorOptions>(options =>
{
    options.BatchSize = 1000; // Increase batch size
    options.BatchTimeout = TimeSpan.FromSeconds(1); // Reduce timeout
});
```

#### 3. Serialization Optimization

```csharp
services.Configure<ProtobufSerializerOptions>(options =>
{
    options.UseCompression = true;
    options.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});
```

## API Reference

### Core Interfaces

#### IEntityNotifier

```csharp
public interface IEntityNotifier
{
    Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

#### IChangePublisher

```csharp
public interface IChangePublisher
{
    Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);
}
```

#### ISerializer

```csharp
public interface ISerializer
{
    Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);
    Task<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);
}
```

### Models

#### ChangeEvent

```csharp
public class ChangeEvent
{
    public string Source { get; set; }
    public string Schema { get; set; }
    public string Table { get; set; }
    public string Operation { get; set; }
    public string Offset { get; set; }
    public string? PreviousData { get; set; }
    public JsonElement Data { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### TenantContext

```csharp
public class TenantContext
{
    public string TenantId { get; set; }
    public string TenantName { get; set; }
    public bool IsActive { get; set; }
    public TenantResourceLimits ResourceLimits { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

#### TransactionContext

```csharp
public class TransactionContext
{
    public string TransactionId { get; set; }
    public string Source { get; set; }
    public TransactionStatus Status { get; set; }
    public List<ChangeEvent> ChangeEvents { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CommittedAt { get; set; }
}
```

### Configuration Options

#### PostgresAdapterOptions

```csharp
public class PostgresAdapterOptions
{
    public string ConnectionString { get; set; }
    public string SlotName { get; set; }
    public string PublicationName { get; set; }
    public TimeSpan HeartbeatInterval { get; set; }
    public TimeSpan MaxReplicationLag { get; set; }
}
```

#### KafkaPublisherOptions

```csharp
public class KafkaPublisherOptions
{
    public string BootstrapServers { get; set; }
    public string Topic { get; set; }
    public string Acks { get; set; }
    public int RetryBackoffMs { get; set; }
    public int MaxRetries { get; set; }
}
```

#### JsonSerializerOptions

```csharp
public class JsonSerializerOptions
{
    public JsonNamingPolicy PropertyNamingPolicy { get; set; }
    public bool WriteIndented { get; set; }
}
```

---

This comprehensive developer guide provides everything you need to get started with SQLDBEntityNotifier. For more information, please refer to the individual package documentation or contact our support team.