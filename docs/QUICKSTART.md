# SQLDBEntityNotifier - Quick Start Guide

## Installation

### 1. Install Core Package

```bash
dotnet add package SqlDbEntityNotifier.Core
```

### 2. Install Database Adapter

Choose one or more database adapters:

```bash
# PostgreSQL
dotnet add package SqlDbEntityNotifier.Adapters.Postgres

# SQLite
dotnet add package SqlDbEntityNotifier.Adapters.Sqlite

# MySQL
dotnet add package SqlDbEntityNotifier.Adapters.MySQL

# Oracle
dotnet add package SqlDbEntityNotifier.Adapters.Oracle
```

### 3. Install Publisher

Choose one or more publishers:

```bash
# Kafka
dotnet add package SqlDbEntityNotifier.Publisher.Kafka

# RabbitMQ
dotnet add package SqlDbEntityNotifier.Publisher.RabbitMQ

# Webhook
dotnet add package SqlDbEntityNotifier.Publisher.Webhook

# Azure Event Hubs
dotnet add package SqlDbEntityNotifier.Publisher.AzureEventHubs
```

### 4. Install Serializer

Choose one or more serializers:

```bash
# JSON
dotnet add package SqlDbEntityNotifier.Serializers.Json

# Protobuf
dotnet add package SqlDbEntityNotifier.Serializers.Protobuf

# Avro
dotnet add package SqlDbEntityNotifier.Serializers.Avro
```

## Basic Setup

### 1. Configure Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Adapters.Postgres;
using SqlDbEntityNotifier.Publisher.Kafka;
using SqlDbEntityNotifier.Serializers.Json;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        // Database adapter
        services.AddPostgresAdapter(options =>
        {
            options.ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass";
            options.SlotName = "my_slot";
            options.PublicationName = "my_publication";
        });

        // Publisher
        services.AddKafkaPublisher(options =>
        {
            options.BootstrapServers = "localhost:9092";
            options.Topic = "my-topic";
        });

        // Serializer
        services.AddJsonSerializer();
    })
    .Build();
```

### 2. Start Listening for Changes

```csharp
var notifier = host.Services.GetRequiredService<IEntityNotifier>();
var publisher = host.Services.GetRequiredService<IChangePublisher>();

await notifier.StartAsync(async (changeEvent, cancellationToken) =>
{
    Console.WriteLine($"Change detected: {changeEvent.Operation} on {changeEvent.Table}");
    
    // Publish the change event
    await publisher.PublishAsync(changeEvent);
}, CancellationToken.None);
```

## Common Use Cases

### 1. Real-time Data Sync

```csharp
await notifier.StartAsync(async (changeEvent, cancellationToken) =>
{
    if (changeEvent.Operation == "INSERT")
    {
        // Handle new record
        await SyncNewRecordAsync(changeEvent);
    }
    else if (changeEvent.Operation == "UPDATE")
    {
        // Handle updated record
        await SyncUpdatedRecordAsync(changeEvent);
    }
    else if (changeEvent.Operation == "DELETE")
    {
        // Handle deleted record
        await SyncDeletedRecordAsync(changeEvent);
    }
}, CancellationToken.None);
```

### 2. Event Sourcing

```csharp
await notifier.StartAsync(async (changeEvent, cancellationToken) =>
{
    // Store event in event store
    await eventStore.AppendEventAsync(changeEvent);
    
    // Update read models
    await updateReadModelsAsync(changeEvent);
}, CancellationToken.None);
```

### 3. Audit Logging

```csharp
await notifier.StartAsync(async (changeEvent, cancellationToken) =>
{
    var auditLog = new AuditLog
    {
        TableName = changeEvent.Table,
        Operation = changeEvent.Operation,
        Data = changeEvent.Data,
        Timestamp = changeEvent.Timestamp,
        UserId = changeEvent.Metadata.GetValueOrDefault("user_id")
    };
    
    await auditService.LogAsync(auditLog);
}, CancellationToken.None);
```

## Configuration Examples

### appsettings.json

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=mydb;Username=user;Password=pass"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "my-topic"
  },
  "PostgresAdapter": {
    "SlotName": "my_slot",
    "PublicationName": "my_publication",
    "HeartbeatInterval": "00:00:30"
  }
}
```

### Program.cs

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
                var configuration = context.Configuration;
                
                // Database adapter
                services.AddPostgresAdapter(options =>
                {
                    options.ConnectionString = configuration.GetConnectionString("PostgreSQL");
                    options.SlotName = configuration["PostgresAdapter:SlotName"];
                    options.PublicationName = configuration["PostgresAdapter:PublicationName"];
                    options.HeartbeatInterval = TimeSpan.Parse(configuration["PostgresAdapter:HeartbeatInterval"]);
                });

                // Publisher
                services.AddKafkaPublisher(options =>
                {
                    options.BootstrapServers = configuration["Kafka:BootstrapServers"];
                    options.Topic = configuration["Kafka:Topic"];
                });

                // Serializer
                services.AddJsonSerializer();

                // Register your service
                services.AddHostedService<ChangeEventProcessor>();
            });
    }
}
```

### ChangeEventProcessor Service

```csharp
public class ChangeEventProcessor : BackgroundService
{
    private readonly IEntityNotifier _notifier;
    private readonly IChangePublisher _publisher;
    private readonly ILogger<ChangeEventProcessor> _logger;

    public ChangeEventProcessor(
        IEntityNotifier notifier,
        IChangePublisher publisher,
        ILogger<ChangeEventProcessor> logger)
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
```

## Next Steps

1. **Explore Advanced Features**: Check out multi-tenancy, transactional grouping, and exactly-once delivery
2. **Add Monitoring**: Integrate metrics, health checks, and tracing
3. **Optimize Performance**: Configure batch processing and connection pooling
4. **Handle Errors**: Implement retry mechanisms and dead letter queues

For more detailed information, see the [Comprehensive Developer Guide](README.md).