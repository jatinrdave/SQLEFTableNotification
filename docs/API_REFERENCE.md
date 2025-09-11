# SQLDBEntityNotifier - API Reference

## Table of Contents

1. [Core Interfaces](#core-interfaces)
2. [Models](#models)
3. [Database Adapters](#database-adapters)
4. [Publishers](#publishers)
5. [Serializers](#serializers)
6. [Advanced Features](#advanced-features)
7. [Monitoring](#monitoring)
8. [Configuration Options](#configuration-options)

## Core Interfaces

### IEntityNotifier

Main interface for change data capture.

```csharp
public interface IEntityNotifier
{
    /// <summary>
    /// Starts listening for database changes.
    /// </summary>
    /// <param name="onChange">Callback function to handle change events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening for database changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

### IChangePublisher

Interface for publishing change events.

```csharp
public interface IChangePublisher
{
    /// <summary>
    /// Publishes a change event.
    /// </summary>
    /// <param name="changeEvent">The change event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);
}
```

### ISerializer

Interface for serializing and deserializing objects.

```csharp
public interface ISerializer
{
    /// <summary>
    /// Serializes an object to bytes.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize</typeparam>
    /// <param name="obj">Object to serialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Serialized bytes</returns>
    Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes bytes to an object.
    /// </summary>
    /// <typeparam name="T">Type of object to deserialize</typeparam>
    /// <param name="data">Bytes to deserialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized object</returns>
    Task<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);
}
```

### IDbAdapter

Interface for database adapters.

```csharp
public interface IDbAdapter
{
    /// <summary>
    /// Starts the database adapter.
    /// </summary>
    /// <param name="onChange">Callback function to handle change events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the database adapter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

## Models

### ChangeEvent

Represents a database change event.

```csharp
public class ChangeEvent
{
    /// <summary>
    /// Gets or sets the database source identifier.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the database schema name.
    /// </summary>
    public string Schema { get; set; }

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string Table { get; set; }

    /// <summary>
    /// Gets or sets the operation type (INSERT, UPDATE, DELETE).
    /// </summary>
    public string Operation { get; set; }

    /// <summary>
    /// Gets or sets the unique offset identifier.
    /// </summary>
    public string Offset { get; set; }

    /// <summary>
    /// Gets or sets the previous row data (for UPDATE operations).
    /// </summary>
    public string? PreviousData { get; set; }

    /// <summary>
    /// Gets or sets the current row data.
    /// </summary>
    public JsonElement Data { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Creates a new change event.
    /// </summary>
    /// <param name="source">Database source</param>
    /// <param name="schema">Database schema</param>
    /// <param name="table">Table name</param>
    /// <param name="operation">Operation type</param>
    /// <param name="offset">Unique offset</param>
    /// <param name="previousData">Previous data (for updates)</param>
    /// <param name="data">Current data</param>
    /// <param name="metadata">Additional metadata</param>
    /// <returns>New change event</returns>
    public static ChangeEvent Create(
        string source,
        string schema,
        string table,
        string operation,
        string offset,
        string? previousData,
        JsonElement data,
        Dictionary<string, string> metadata);
}
```

### TenantContext

Represents a tenant in a multi-tenant system.

```csharp
public class TenantContext
{
    /// <summary>
    /// Gets or sets the unique tenant identifier.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// Gets or sets the tenant name.
    /// </summary>
    public string TenantName { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the resource limits for the tenant.
    /// </summary>
    public TenantResourceLimits ResourceLimits { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }
}
```

### TenantResourceLimits

Defines resource limits for a tenant.

```csharp
public class TenantResourceLimits
{
    /// <summary>
    /// Gets or sets the maximum number of connections.
    /// </summary>
    public int MaxConnections { get; set; }

    /// <summary>
    /// Gets or sets the maximum events per second.
    /// </summary>
    public int MaxEventsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the maximum burst limit.
    /// </summary>
    public int MaxBurstLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum transaction size.
    /// </summary>
    public int MaxTransactionSize { get; set; }
}
```

### TransactionContext

Represents a transactional group of change events.

```csharp
public class TransactionContext
{
    /// <summary>
    /// Gets or sets the unique transaction identifier.
    /// </summary>
    public string TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the database source.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the list of change events in the transaction.
    /// </summary>
    public List<ChangeEvent> ChangeEvents { get; set; }

    /// <summary>
    /// Gets or sets the transaction start time.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the transaction commit time.
    /// </summary>
    public DateTime? CommittedAt { get; set; }
}
```

### TransactionStatus

Enumeration of transaction statuses.

```csharp
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is active.
    /// </summary>
    Active,

    /// <summary>
    /// Transaction has been committed.
    /// </summary>
    Committed,

    /// <summary>
    /// Transaction has been rolled back.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Transaction has timed out.
    /// </summary>
    TimedOut
}
```

### DeliveryResult

Represents the result of a delivery operation.

```csharp
public class DeliveryResult
{
    /// <summary>
    /// Gets or sets whether the delivery was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets whether this was a duplicate delivery.
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// Gets or sets the number of delivery attempts.
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the error message if delivery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the delivery timestamp.
    /// </summary>
    public DateTime DeliveredAt { get; set; }
}
```

### BulkOperationEvent

Represents a bulk operation event.

```csharp
public class BulkOperationEvent
{
    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public string Operation { get; set; }

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string Table { get; set; }

    /// <summary>
    /// Gets or sets the number of rows affected.
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Gets or sets the start time of the bulk operation.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the bulk operation.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration of the bulk operation.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
}
```

## Database Adapters

### PostgresAdapter

PostgreSQL adapter using logical replication.

```csharp
public class PostgresAdapter : IDbAdapter
{
    /// <summary>
    /// Initializes a new instance of the PostgresAdapter class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public PostgresAdapter(IOptions<PostgresAdapterOptions> options, ILogger<PostgresAdapter> logger);

    /// <summary>
    /// Starts the PostgreSQL adapter.
    /// </summary>
    /// <param name="onChange">Callback function to handle change events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the PostgreSQL adapter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StopAsync(CancellationToken cancellationToken = default);
}
```

### SqliteAdapter

SQLite adapter using WAL mode and triggers.

```csharp
public class SqliteAdapter : IDbAdapter
{
    /// <summary>
    /// Initializes a new instance of the SqliteAdapter class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public SqliteAdapter(IOptions<SqliteAdapterOptions> options, ILogger<SqliteAdapter> logger);

    /// <summary>
    /// Starts the SQLite adapter.
    /// </summary>
    /// <param name="onChange">Callback function to handle change events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the SQLite adapter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StopAsync(CancellationToken cancellationToken = default);
}
```

### MySQLAdapter

MySQL adapter using binary log replication.

```csharp
public class MySQLAdapter : IDbAdapter
{
    /// <summary>
    /// Initializes a new instance of the MySQLAdapter class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public MySQLAdapter(IOptions<MySQLAdapterOptions> options, ILogger<MySQLAdapter> logger);

    /// <summary>
    /// Starts the MySQL adapter.
    /// </summary>
    /// <param name="onChange">Callback function to handle change events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the MySQL adapter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StopAsync(CancellationToken cancellationToken = default);
}
```

### OracleAdapter

Oracle adapter using redo log mining.

```csharp
public class OracleAdapter : IDbAdapter
{
    /// <summary>
    /// Initializes a new instance of the OracleAdapter class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public OracleAdapter(IOptions<OracleAdapterOptions> options, ILogger<OracleAdapter> logger);

    /// <summary>
    /// Starts the Oracle adapter.
    /// </summary>
    /// <param name="onChange">Callback function to handle change events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the Oracle adapter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task StopAsync(CancellationToken cancellationToken = default);
}
```

## Publishers

### KafkaPublisher

Kafka publisher for high-throughput messaging.

```csharp
public class KafkaPublisher : IChangePublisher
{
    /// <summary>
    /// Initializes a new instance of the KafkaPublisher class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public KafkaPublisher(IOptions<KafkaPublisherOptions> options, ILogger<KafkaPublisher> logger);

    /// <summary>
    /// Publishes a change event to Kafka.
    /// </summary>
    /// <param name="changeEvent">The change event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);
}
```

### RabbitMQPublisher

RabbitMQ publisher for reliable messaging.

```csharp
public class RabbitMQPublisher : IChangePublisher
{
    /// <summary>
    /// Initializes a new instance of the RabbitMQPublisher class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public RabbitMQPublisher(IOptions<RabbitMQPublisherOptions> options, ILogger<RabbitMQPublisher> logger);

    /// <summary>
    /// Publishes a change event to RabbitMQ.
    /// </summary>
    /// <param name="changeEvent">The change event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);
}
```

### WebhookPublisher

Webhook publisher for HTTP-based notifications.

```csharp
public class WebhookPublisher : IChangePublisher
{
    /// <summary>
    /// Initializes a new instance of the WebhookPublisher class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public WebhookPublisher(IOptions<WebhookPublisherOptions> options, ILogger<WebhookPublisher> logger);

    /// <summary>
    /// Publishes a change event via webhook.
    /// </summary>
    /// <param name="changeEvent">The change event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);
}
```

### AzureEventHubsPublisher

Azure Event Hubs publisher for cloud messaging.

```csharp
public class AzureEventHubsPublisher : IChangePublisher
{
    /// <summary>
    /// Initializes a new instance of the AzureEventHubsPublisher class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public AzureEventHubsPublisher(IOptions<AzureEventHubsPublisherOptions> options, ILogger<AzureEventHubsPublisher> logger);

    /// <summary>
    /// Publishes a change event to Azure Event Hubs.
    /// </summary>
    /// <param name="changeEvent">The change event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);
}
```

## Serializers

### JsonSerializer

JSON serializer for human-readable format.

```csharp
public class JsonSerializer : ISerializer
{
    /// <summary>
    /// Initializes a new instance of the JsonSerializer class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    public JsonSerializer(IOptions<JsonSerializerOptions> options);

    /// <summary>
    /// Serializes an object to JSON bytes.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize</typeparam>
    /// <param name="obj">Object to serialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Serialized JSON bytes</returns>
    public Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes JSON bytes to an object.
    /// </summary>
    /// <typeparam name="T">Type of object to deserialize</typeparam>
    /// <param name="data">JSON bytes to deserialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized object</returns>
    public Task<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);
}
```

### ProtobufSerializer

Protobuf serializer for efficient binary format.

```csharp
public class ProtobufSerializer : ISerializer
{
    /// <summary>
    /// Initializes a new instance of the ProtobufSerializer class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    public ProtobufSerializer(IOptions<ProtobufSerializerOptions> options);

    /// <summary>
    /// Serializes an object to Protobuf bytes.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize</typeparam>
    /// <param name="obj">Object to serialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Serialized Protobuf bytes</returns>
    public Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes Protobuf bytes to an object.
    /// </summary>
    /// <typeparam name="T">Type of object to deserialize</typeparam>
    /// <param name="data">Protobuf bytes to deserialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized object</returns>
    public Task<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);
}
```

### AvroSerializer

Avro serializer with schema registry integration.

```csharp
public class AvroSerializer : ISerializer
{
    /// <summary>
    /// Initializes a new instance of the AvroSerializer class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    public AvroSerializer(IOptions<AvroSerializerOptions> options);

    /// <summary>
    /// Serializes an object to Avro bytes.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize</typeparam>
    /// <param name="obj">Object to serialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Serialized Avro bytes</returns>
    public Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes Avro bytes to an object.
    /// </summary>
    /// <typeparam name="T">Type of object to deserialize</typeparam>
    /// <param name="data">Avro bytes to deserialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized object</returns>
    public Task<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);
}
```

## Advanced Features

### TenantManager

Manages tenants in a multi-tenant system.

```csharp
public class TenantManager
{
    /// <summary>
    /// Initializes a new instance of the TenantManager class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public TenantManager(IOptions<TenantManagerOptions> options, ILogger<TenantManager> logger);

    /// <summary>
    /// Gets the active tenants.
    /// </summary>
    public Dictionary<string, TenantContext> ActiveTenants { get; }

    /// <summary>
    /// Registers a new tenant.
    /// </summary>
    /// <param name="tenant">Tenant context</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task RegisterTenantAsync(TenantContext tenant);

    /// <summary>
    /// Activates a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task ActivateTenantAsync(string tenantId);

    /// <summary>
    /// Deactivates a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task DeactivateTenantAsync(string tenantId);

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Tenant context or null if not found</returns>
    public Task<TenantContext?> GetTenantAsync(string tenantId);
}
```

### ThrottlingManager

Manages throttling for tenants.

```csharp
public class ThrottlingManager
{
    /// <summary>
    /// Initializes a new instance of the ThrottlingManager class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public ThrottlingManager(IOptions<ThrottlingManagerOptions> options, ILogger<ThrottlingManager> logger);

    /// <summary>
    /// Checks if a request is allowed based on throttling rules.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="requestType">Type of request</param>
    /// <returns>Throttling result</returns>
    public Task<ThrottlingResult> CheckThrottlingAsync(string tenantId, ThrottlingRequestType requestType);

    /// <summary>
    /// Gets throttling statistics.
    /// </summary>
    /// <returns>Throttling statistics</returns>
    public Task<ThrottlingStatistics> GetThrottlingStatisticsAsync();
}
```

### TransactionalGroupManager

Manages transactional grouping of change events.

```csharp
public class TransactionalGroupManager
{
    /// <summary>
    /// Initializes a new instance of the TransactionalGroupManager class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public TransactionalGroupManager(IOptions<TransactionalGroupManagerOptions> options, ILogger<TransactionalGroupManager> logger);

    /// <summary>
    /// Starts a new transaction.
    /// </summary>
    /// <param name="transactionId">Transaction identifier</param>
    /// <param name="source">Database source</param>
    /// <returns>Transaction context</returns>
    public Task<TransactionContext> StartTransactionAsync(string transactionId, string source);

    /// <summary>
    /// Adds a change event to a transaction.
    /// </summary>
    /// <param name="transactionId">Transaction identifier</param>
    /// <param name="changeEvent">Change event to add</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task AddChangeEventAsync(string transactionId, ChangeEvent changeEvent);

    /// <summary>
    /// Commits a transaction.
    /// </summary>
    /// <param name="transactionId">Transaction identifier</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task CommitTransactionAsync(string transactionId);

    /// <summary>
    /// Rolls back a transaction.
    /// </summary>
    /// <param name="transactionId">Transaction identifier</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task RollbackTransactionAsync(string transactionId);

    /// <summary>
    /// Gets a transaction by ID.
    /// </summary>
    /// <param name="transactionId">Transaction identifier</param>
    /// <returns>Transaction context or null if not found</returns>
    public Task<TransactionContext?> GetTransactionAsync(string transactionId);
}
```

### ExactlyOnceDeliveryManager

Manages exactly-once delivery semantics.

```csharp
public class ExactlyOnceDeliveryManager
{
    /// <summary>
    /// Initializes a new instance of the ExactlyOnceDeliveryManager class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public ExactlyOnceDeliveryManager(IOptions<ExactlyOnceDeliveryManagerOptions> options, ILogger<ExactlyOnceDeliveryManager> logger);

    /// <summary>
    /// Delivers a change event with exactly-once semantics.
    /// </summary>
    /// <param name="changeEvent">Change event to deliver</param>
    /// <param name="publisher">Publisher to use</param>
    /// <returns>Delivery result</returns>
    public Task<DeliveryResult> DeliverExactlyOnceAsync(ChangeEvent changeEvent, IChangePublisher publisher);

    /// <summary>
    /// Checks if an event has already been delivered.
    /// </summary>
    /// <param name="eventId">Event identifier</param>
    /// <returns>True if already delivered, false otherwise</returns>
    public Task<bool> IsEventDeliveredAsync(string eventId);
}
```

### BulkOperationDetector

Detects bulk operations in change events.

```csharp
public class BulkOperationDetector
{
    /// <summary>
    /// Initializes a new instance of the BulkOperationDetector class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public BulkOperationDetector(IOptions<BulkOperationDetectorOptions> options, ILogger<BulkOperationDetector> logger);

    /// <summary>
    /// Event raised when a bulk operation is detected.
    /// </summary>
    public event EventHandler<BulkOperationEvent>? BulkOperationDetected;

    /// <summary>
    /// Processes a change event for bulk operation detection.
    /// </summary>
    /// <param name="changeEvent">Change event to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task ProcessChangeEventAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);
}
```

## Monitoring

### IChangeEventMetrics

Interface for change event metrics.

```csharp
public interface IChangeEventMetrics
{
    /// <summary>
    /// Gets current metrics.
    /// </summary>
    /// <returns>Metrics data</returns>
    Task<ChangeEventMetricsData> GetMetricsAsync();

    /// <summary>
    /// Records a change event.
    /// </summary>
    /// <param name="changeEvent">Change event to record</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task RecordChangeEventAsync(ChangeEvent changeEvent);

    /// <summary>
    /// Records a processing time.
    /// </summary>
    /// <param name="processingTime">Processing time</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task RecordProcessingTimeAsync(TimeSpan processingTime);
}
```

### IHealthCheckService

Interface for health checks.

```csharp
public interface IHealthCheckService
{
    /// <summary>
    /// Gets health check results.
    /// </summary>
    /// <returns>Health check results</returns>
    Task<HealthCheckResults> GetHealthChecksAsync();

    /// <summary>
    /// Registers a health check.
    /// </summary>
    /// <param name="name">Health check name</param>
    /// <param name="check">Health check function</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task RegisterHealthCheckAsync(string name, Func<Task<HealthCheckResult>> check);
}
```

### ChangeEventTracer

Provides distributed tracing for change events.

```csharp
public class ChangeEventTracer
{
    /// <summary>
    /// Initializes a new instance of the ChangeEventTracer class.
    /// </summary>
    /// <param name="options">Configuration options</param>
    public ChangeEventTracer(IOptions<ChangeEventTracerOptions> options);

    /// <summary>
    /// Starts a new trace for a change event.
    /// </summary>
    /// <param name="changeEvent">Change event</param>
    /// <param name="operationName">Operation name</param>
    /// <returns>Trace activity</returns>
    public ChangeEventTraceActivity StartChangeEventTrace(ChangeEvent changeEvent, string operationName);

    /// <summary>
    /// Sets a tag on the current trace.
    /// </summary>
    /// <param name="key">Tag key</param>
    /// <param name="value">Tag value</param>
    public void SetTag(string key, string value);

    /// <summary>
    /// Records an event in the current trace.
    /// </summary>
    /// <param name="eventName">Event name</param>
    public void RecordEvent(string eventName);

    /// <summary>
    /// Sets the status of the current trace.
    /// </summary>
    /// <param name="status">Status code</param>
    public void SetStatus(ActivityStatusCode status);
}
```

## Configuration Options

### PostgresAdapterOptions

```csharp
public class PostgresAdapterOptions
{
    /// <summary>
    /// Gets or sets the PostgreSQL connection string.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the replication slot name.
    /// </summary>
    public string SlotName { get; set; }

    /// <summary>
    /// Gets or sets the publication name.
    /// </summary>
    public string PublicationName { get; set; }

    /// <summary>
    /// Gets or sets the heartbeat interval.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; }

    /// <summary>
    /// Gets or sets the maximum replication lag.
    /// </summary>
    public TimeSpan MaxReplicationLag { get; set; }
}
```

### KafkaPublisherOptions

```csharp
public class KafkaPublisherOptions
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers.
    /// </summary>
    public string BootstrapServers { get; set; }

    /// <summary>
    /// Gets or sets the topic name.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// Gets or sets the acknowledgment level.
    /// </summary>
    public string Acks { get; set; }

    /// <summary>
    /// Gets or sets the retry backoff in milliseconds.
    /// </summary>
    public int RetryBackoffMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; }
}
```

### JsonSerializerOptions

```csharp
public class JsonSerializerOptions
{
    /// <summary>
    /// Gets or sets the property naming policy.
    /// </summary>
    public JsonNamingPolicy PropertyNamingPolicy { get; set; }

    /// <summary>
    /// Gets or sets whether to write indented JSON.
    /// </summary>
    public bool WriteIndented { get; set; }
}
```

### TenantManagerOptions

```csharp
public class TenantManagerOptions
{
    /// <summary>
    /// Gets or sets whether multi-tenancy is enabled.
    /// </summary>
    public bool EnableMultiTenancy { get; set; }

    /// <summary>
    /// Gets or sets the default tenant ID.
    /// </summary>
    public string DefaultTenantId { get; set; }
}
```

### ThrottlingManagerOptions

```csharp
public class ThrottlingManagerOptions
{
    /// <summary>
    /// Gets or sets whether throttling is enabled.
    /// </summary>
    public bool EnableThrottling { get; set; }

    /// <summary>
    /// Gets or sets the default rate limit.
    /// </summary>
    public int DefaultRateLimit { get; set; }

    /// <summary>
    /// Gets or sets the default burst limit.
    /// </summary>
    public int DefaultBurstLimit { get; set; }
}
```

### TransactionalGroupManagerOptions

```csharp
public class TransactionalGroupManagerOptions
{
    /// <summary>
    /// Gets or sets the maximum transaction size.
    /// </summary>
    public int MaxTransactionSize { get; set; }

    /// <summary>
    /// Gets or sets the transaction timeout.
    /// </summary>
    public TimeSpan TransactionTimeout { get; set; }

    /// <summary>
    /// Gets or sets the cleanup interval.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; }
}
```

### ExactlyOnceDeliveryManagerOptions

```csharp
public class ExactlyOnceDeliveryManagerOptions
{
    /// <summary>
    /// Gets or sets whether exactly-once delivery is enabled.
    /// </summary>
    public bool EnableExactlyOnce { get; set; }

    /// <summary>
    /// Gets or sets the delivery timeout.
    /// </summary>
    public TimeSpan DeliveryTimeout { get; set; }

    /// <summary>
    /// Gets or sets the cleanup interval.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; }
}
```

### BulkOperationDetectorOptions

```csharp
public class BulkOperationDetectorOptions
{
    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the batch timeout.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; }

    /// <summary>
    /// Gets or sets the minimum batch size.
    /// </summary>
    public int MinBatchSize { get; set; }
}
```

### ChangeEventMetricsOptions

```csharp
public class ChangeEventMetricsOptions
{
    /// <summary>
    /// Gets or sets whether metrics are enabled.
    /// </summary>
    public bool EnableMetrics { get; set; }

    /// <summary>
    /// Gets or sets the metrics interval.
    /// </summary>
    public TimeSpan MetricsInterval { get; set; }
}
```

### HealthCheckServiceOptions

```csharp
public class HealthCheckServiceOptions
{
    /// <summary>
    /// Gets or sets whether health checks are enabled.
    /// </summary>
    public bool EnableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets the health check interval.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; }
}
```

### ChangeEventTracerOptions

```csharp
public class ChangeEventTracerOptions
{
    /// <summary>
    /// Gets or sets whether tracing is enabled.
    /// </summary>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets the trace level.
    /// </summary>
    public string TraceLevel { get; set; }
}
```

This API reference provides comprehensive documentation for all the interfaces, classes, and configuration options available in SQLDBEntityNotifier. For more detailed examples and usage patterns, please refer to the [Examples](EXAMPLES.md) and [Quick Start Guide](QUICKSTART.md).