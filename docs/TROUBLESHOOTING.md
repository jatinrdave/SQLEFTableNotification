# SQLDBEntityNotifier - Troubleshooting Guide

## Table of Contents

1. [Common Issues](#common-issues)
2. [Database-Specific Issues](#database-specific-issues)
3. [Publisher Issues](#publisher-issues)
4. [Performance Issues](#performance-issues)
5. [Configuration Issues](#configuration-issues)
6. [Monitoring and Debugging](#monitoring-and-debugging)
7. [Error Codes](#error-codes)
8. [Best Practices](#best-practices)

## Common Issues

### Issue 1: Connection String Problems

**Problem**: Cannot connect to database.

**Symptoms**:
- `SqlException` or connection timeout errors
- "Connection refused" errors
- Authentication failures

**Solutions**:

1. **Verify Connection String**:
```csharp
// PostgreSQL
"Host=localhost;Database=mydb;Username=user;Password=pass;Port=5432;"

// MySQL
"Server=localhost;Database=mydb;Uid=user;Pwd=pass;Port=3306;"

// SQLite
"Data Source=mydb.db;"

// Oracle
"Data Source=localhost:1521/XE;User Id=user;Password=pass;"
```

2. **Check Network Connectivity**:
```bash
# Test PostgreSQL
telnet localhost 5432

# Test MySQL
telnet localhost 3306

# Test Oracle
telnet localhost 1521
```

3. **Verify Credentials**:
```sql
-- PostgreSQL
SELECT current_user;

-- MySQL
SELECT USER();

-- Oracle
SELECT USER FROM DUAL;
```

### Issue 2: Replication Slot Issues

**Problem**: PostgreSQL replication slot not found or already exists.

**Symptoms**:
- `PostgresException: replication slot "my_slot" does not exist`
- `PostgresException: replication slot "my_slot" already exists`

**Solutions**:

1. **Check Existing Slots**:
```sql
SELECT * FROM pg_replication_slots;
```

2. **Drop Existing Slot**:
```sql
SELECT pg_drop_replication_slot('my_slot');
```

3. **Create New Slot**:
```sql
SELECT pg_create_logical_replication_slot('my_slot', 'pgoutput');
```

4. **Check Slot Status**:
```sql
SELECT slot_name, active, xmin, restart_lsn, confirmed_flush_lsn 
FROM pg_replication_slots 
WHERE slot_name = 'my_slot';
```

### Issue 3: Binary Log Issues (MySQL)

**Problem**: MySQL binary logging not enabled or configured incorrectly.

**Symptoms**:
- `MySqlException: Binary logging is not enabled`
- No change events are detected

**Solutions**:

1. **Enable Binary Logging**:
```sql
SET GLOBAL log_bin = ON;
SET GLOBAL binlog_format = 'ROW';
```

2. **Check Binary Log Status**:
```sql
SHOW VARIABLES LIKE 'log_bin';
SHOW VARIABLES LIKE 'binlog_format';
```

3. **Grant Replication Privileges**:
```sql
GRANT REPLICATION SLAVE ON *.* TO 'replication_user'@'%';
FLUSH PRIVILEGES;
```

4. **Check Binary Log Files**:
```sql
SHOW BINARY LOGS;
```

### Issue 4: Oracle Redo Log Issues

**Problem**: Oracle redo log mining not working.

**Symptoms**:
- `OracleException: ORA-00942: table or view does not exist`
- No change events are detected

**Solutions**:

1. **Enable Supplemental Logging**:
```sql
ALTER DATABASE ADD SUPPLEMENTAL LOG DATA;
```

2. **Grant Required Privileges**:
```sql
GRANT SELECT ON V_$DATABASE TO your_user;
GRANT SELECT ON V_$LOG TO your_user;
GRANT SELECT ON V_$LOGFILE TO your_user;
GRANT SELECT ON V_$ARCHIVED_LOG TO your_user;
GRANT SELECT ON V_$ARCHIVE_DEST_STATUS TO your_user;
```

3. **Check Redo Log Status**:
```sql
SELECT * FROM V$LOG;
SELECT * FROM V$LOGFILE;
```

## Database-Specific Issues

### PostgreSQL Issues

#### Issue: WAL Files Not Being Generated

**Problem**: No WAL files are being generated, preventing logical replication.

**Solutions**:

1. **Check WAL Settings**:
```sql
SHOW wal_level;
SHOW max_wal_senders;
SHOW max_replication_slots;
```

2. **Configure WAL Settings**:
```sql
-- In postgresql.conf
wal_level = logical
max_wal_senders = 10
max_replication_slots = 10
```

3. **Restart PostgreSQL**:
```bash
sudo systemctl restart postgresql
```

#### Issue: Publication Not Found

**Problem**: Publication does not exist for logical replication.

**Solutions**:

1. **Create Publication**:
```sql
CREATE PUBLICATION my_publication FOR ALL TABLES;
```

2. **Check Publications**:
```sql
SELECT * FROM pg_publication;
```

3. **Add Tables to Publication**:
```sql
ALTER PUBLICATION my_publication ADD TABLE my_table;
```

### MySQL Issues

#### Issue: GTID Not Enabled

**Problem**: Global Transaction Identifier (GTID) not enabled.

**Solutions**:

1. **Enable GTID**:
```sql
SET GLOBAL gtid_mode = ON;
SET GLOBAL enforce_gtid_consistency = ON;
```

2. **Check GTID Status**:
```sql
SHOW VARIABLES LIKE 'gtid_mode';
SHOW VARIABLES LIKE 'enforce_gtid_consistency';
```

#### Issue: Server ID Not Set

**Problem**: MySQL server ID not configured for replication.

**Solutions**:

1. **Set Server ID**:
```sql
SET GLOBAL server_id = 1;
```

2. **Check Server ID**:
```sql
SHOW VARIABLES LIKE 'server_id';
```

### SQLite Issues

#### Issue: WAL Mode Not Enabled

**Problem**: SQLite WAL mode not enabled for change detection.

**Solutions**:

1. **Enable WAL Mode**:
```sql
PRAGMA journal_mode = WAL;
```

2. **Check WAL Mode**:
```sql
PRAGMA journal_mode;
```

3. **Configure WAL Settings**:
```sql
PRAGMA wal_autocheckpoint = 1000;
PRAGMA synchronous = NORMAL;
```

### Oracle Issues

#### Issue: Flashback Not Enabled

**Problem**: Oracle Flashback not enabled for change detection.

**Solutions**:

1. **Enable Flashback**:
```sql
ALTER DATABASE FLASHBACK ON;
```

2. **Check Flashback Status**:
```sql
SELECT flashback_on FROM V$DATABASE;
```

3. **Set Flashback Retention**:
```sql
ALTER SYSTEM SET db_flashback_retention_target = 1440;
```

## Publisher Issues

### Kafka Issues

#### Issue: Cannot Connect to Kafka

**Problem**: Cannot connect to Kafka broker.

**Symptoms**:
- `KafkaException: Failed to connect to broker`
- Connection timeout errors

**Solutions**:

1. **Check Kafka Configuration**:
```csharp
services.Configure<KafkaPublisherOptions>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.SecurityProtocol = SecurityProtocol.Plaintext;
    options.SaslMechanism = SaslMechanism.Plain;
    options.SaslUsername = "username";
    options.SaslPassword = "password";
});
```

2. **Test Kafka Connectivity**:
```bash
# Test if Kafka is running
telnet localhost 9092

# List topics
kafka-topics.sh --bootstrap-server localhost:9092 --list
```

3. **Check Kafka Logs**:
```bash
tail -f /var/log/kafka/server.log
```

#### Issue: Topic Does Not Exist

**Problem**: Kafka topic does not exist.

**Solutions**:

1. **Create Topic**:
```bash
kafka-topics.sh --bootstrap-server localhost:9092 --create --topic my-topic --partitions 3 --replication-factor 1
```

2. **Check Topic Exists**:
```bash
kafka-topics.sh --bootstrap-server localhost:9092 --list
```

### RabbitMQ Issues

#### Issue: Cannot Connect to RabbitMQ

**Problem**: Cannot connect to RabbitMQ broker.

**Solutions**:

1. **Check RabbitMQ Configuration**:
```csharp
services.Configure<RabbitMQPublisherOptions>(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.Exchange = "my-exchange";
    options.RoutingKey = "my-routing-key";
});
```

2. **Test RabbitMQ Connectivity**:
```bash
# Test if RabbitMQ is running
telnet localhost 5672

# Check RabbitMQ status
rabbitmqctl status
```

3. **Create Exchange**:
```bash
rabbitmqctl eval 'rabbit_exchange:declare({resource, <<"/">>, exchange, <<"my-exchange">>}, topic, true, false, false, []).'
```

### Webhook Issues

#### Issue: Webhook Endpoint Not Responding

**Problem**: Webhook endpoint is not responding or returning errors.

**Solutions**:

1. **Check Webhook Configuration**:
```csharp
services.Configure<WebhookPublisherOptions>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Endpoint = "/webhook";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.RetryCount = 3;
    options.RetryDelay = TimeSpan.FromSeconds(1);
});
```

2. **Test Webhook Endpoint**:
```bash
curl -X POST https://api.example.com/webhook \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

3. **Check SSL/TLS Issues**:
```csharp
// For self-signed certificates
services.Configure<WebhookPublisherOptions>(options =>
{
    options.IgnoreSslErrors = true;
});
```

### Azure Event Hubs Issues

#### Issue: Cannot Connect to Azure Event Hubs

**Problem**: Cannot connect to Azure Event Hubs.

**Solutions**:

1. **Check Connection String**:
```csharp
services.Configure<AzureEventHubsPublisherOptions>(options =>
{
    options.ConnectionString = "Endpoint=sb://myhub.servicebus.windows.net/;SharedAccessKeyName=mykey;SharedAccessKey=mysecret";
    options.EventHubName = "my-hub";
});
```

2. **Verify Event Hub Exists**:
```bash
az eventhubs eventhub show --resource-group myResourceGroup --namespace-name myNamespace --name myHub
```

3. **Check Access Policies**:
```bash
az eventhubs namespace authorization-rule list --resource-group myResourceGroup --namespace-name myNamespace
```

## Performance Issues

### Issue 1: High Memory Usage

**Problem**: Application is using too much memory.

**Solutions**:

1. **Reduce Transaction Size**:
```csharp
services.Configure<TransactionalGroupManagerOptions>(options =>
{
    options.MaxTransactionSize = 1000; // Reduce from default
    options.CleanupInterval = TimeSpan.FromSeconds(30); // More frequent cleanup
});
```

2. **Enable Compression**:
```csharp
services.Configure<ProtobufSerializerOptions>(options =>
{
    options.UseCompression = true;
    options.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});
```

3. **Limit Batch Size**:
```csharp
services.Configure<BulkOperationDetectorOptions>(options =>
{
    options.BatchSize = 500; // Reduce from default
    options.BatchTimeout = TimeSpan.FromSeconds(1); // Reduce timeout
});
```

### Issue 2: Slow Processing

**Problem**: Change events are being processed slowly.

**Solutions**:

1. **Increase Connection Pool**:
```csharp
services.Configure<PostgresAdapterOptions>(options =>
{
    options.ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass;Pooling=true;MinPoolSize=10;MaxPoolSize=100;";
});
```

2. **Use Async Processing**:
```csharp
await notifier.StartAsync(async (changeEvent, cancellationToken) =>
{
    // Use async/await for all operations
    await ProcessChangeEventAsync(changeEvent, cancellationToken);
}, CancellationToken.None);
```

3. **Implement Batching**:
```csharp
public class BatchProcessor
{
    private readonly List<ChangeEvent> _batch = new();
    private readonly Timer _batchTimer;

    public BatchProcessor()
    {
        _batchTimer = new Timer(ProcessBatch, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private void ProcessBatch(object state)
    {
        if (_batch.Count > 0)
        {
            var batchToProcess = new List<ChangeEvent>(_batch);
            _batch.Clear();
            
            // Process batch
            _ = Task.Run(() => ProcessBatchAsync(batchToProcess));
        }
    }
}
```

### Issue 3: High CPU Usage

**Problem**: Application is using too much CPU.

**Solutions**:

1. **Reduce Polling Frequency**:
```csharp
services.Configure<PostgresAdapterOptions>(options =>
{
    options.HeartbeatInterval = TimeSpan.FromSeconds(60); // Increase from default
});
```

2. **Use Efficient Serialization**:
```csharp
// Use Protobuf instead of JSON for better performance
services.AddProtobufSerializer();
```

3. **Limit Concurrent Operations**:
```csharp
public class ThrottledProcessor
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(Environment.ProcessorCount);

    public async Task ProcessAsync(ChangeEvent changeEvent)
    {
        await _semaphore.WaitAsync();
        try
        {
            await ProcessChangeEventAsync(changeEvent);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

## Configuration Issues

### Issue 1: Configuration Not Loading

**Problem**: Configuration options are not being loaded correctly.

**Solutions**:

1. **Check Configuration Binding**:
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Make sure to call Configure
        services.Configure<PostgresAdapterOptions>(Configuration.GetSection("PostgresAdapter"));
    }
}
```

2. **Verify appsettings.json**:
```json
{
  "PostgresAdapter": {
    "ConnectionString": "Host=localhost;Database=mydb;Username=user;Password=pass",
    "SlotName": "my_slot",
    "PublicationName": "my_publication"
  }
}
```

3. **Use IOptions Pattern**:
```csharp
public class MyService
{
    private readonly PostgresAdapterOptions _options;

    public MyService(IOptions<PostgresAdapterOptions> options)
    {
        _options = options.Value;
    }
}
```

### Issue 2: Environment Variables Not Working

**Problem**: Environment variables are not being read.

**Solutions**:

1. **Check Environment Variable Names**:
```bash
# Use double underscores for nested configuration
export PostgresAdapter__ConnectionString="Host=localhost;Database=mydb;Username=user;Password=pass"
export PostgresAdapter__SlotName="my_slot"
```

2. **Verify Environment Variable Loading**:
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Environment variables are loaded automatically
        services.Configure<PostgresAdapterOptions>(Configuration.GetSection("PostgresAdapter"));
    }
}
```

## Monitoring and Debugging

### Issue 1: No Logs Being Generated

**Problem**: Application is not generating logs.

**Solutions**:

1. **Configure Logging**:
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            });
    }
}
```

2. **Use Structured Logging**:
```csharp
_logger.LogInformation("Processing change event: {Operation} on {Table} at {Timestamp}", 
    changeEvent.Operation, changeEvent.Table, changeEvent.Timestamp);
```

### Issue 2: Metrics Not Available

**Problem**: Metrics are not being collected or displayed.

**Solutions**:

1. **Enable Metrics**:
```csharp
services.Configure<ChangeEventMetricsOptions>(options =>
{
    options.EnableMetrics = true;
    options.MetricsInterval = TimeSpan.FromSeconds(10);
});
```

2. **Expose Metrics Endpoint**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly IChangeEventMetrics _metrics;

    public MetricsController(IChangeEventMetrics metrics)
    {
        _metrics = metrics;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _metrics.GetMetricsAsync();
        return Ok(metrics);
    }
}
```

### Issue 3: Health Checks Failing

**Problem**: Health checks are returning unhealthy status.

**Solutions**:

1. **Enable Health Checks**:
```csharp
services.Configure<HealthCheckServiceOptions>(options =>
{
    options.EnableHealthChecks = true;
    options.HealthCheckInterval = TimeSpan.FromSeconds(30);
});
```

2. **Add Custom Health Checks**:
```csharp
services.AddSingleton<IHealthCheckService>(provider =>
{
    var service = new HealthCheckService();
    service.RegisterHealthCheckAsync("database", async () =>
    {
        // Check database connectivity
        return new HealthCheckResult { Status = HealthStatus.Healthy };
    });
    return service;
});
```

## Error Codes

### Database Errors

| Error Code | Description | Solution |
|------------|-------------|----------|
| `08006` | Connection failure | Check connection string and network connectivity |
| `08001` | SQL client unable to establish connection | Verify database server is running |
| `08003` | Connection does not exist | Check connection pooling configuration |
| `08004` | SQL server rejected connection | Verify credentials and permissions |

### Publisher Errors

| Error Code | Description | Solution |
|------------|-------------|----------|
| `KAFKA_001` | Cannot connect to Kafka broker | Check Kafka configuration and connectivity |
| `KAFKA_002` | Topic does not exist | Create the required topic |
| `RABBITMQ_001` | Cannot connect to RabbitMQ broker | Check RabbitMQ configuration and connectivity |
| `RABBITMQ_002` | Exchange does not exist | Create the required exchange |
| `WEBHOOK_001` | Webhook endpoint not responding | Check endpoint URL and connectivity |
| `WEBHOOK_002` | SSL/TLS certificate error | Check certificate configuration |

### Serialization Errors

| Error Code | Description | Solution |
|------------|-------------|----------|
| `SERIALIZE_001` | Object cannot be serialized | Check object structure and serialization attributes |
| `SERIALIZE_002` | Deserialization failed | Verify data format and schema compatibility |
| `SERIALIZE_003` | Schema registry error | Check schema registry connectivity and configuration |

## Best Practices

### 1. Connection Management

```csharp
// Use connection pooling
services.Configure<PostgresAdapterOptions>(options =>
{
    options.ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass;Pooling=true;MinPoolSize=5;MaxPoolSize=100;";
});

// Implement proper disposal
public class MyService : IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        if (!_disposed)
        {
            // Cleanup resources
            _disposed = true;
        }
    }
}
```

### 2. Error Handling

```csharp
await notifier.StartAsync(async (changeEvent, cancellationToken) =>
{
    try
    {
        await ProcessChangeEventAsync(changeEvent, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing change event: {EventId}", changeEvent.Offset);
        
        // Implement retry logic or dead letter queue
        await HandleErrorAsync(changeEvent, ex, cancellationToken);
    }
}, CancellationToken.None);
```

### 3. Performance Optimization

```csharp
// Use efficient serialization
services.AddProtobufSerializer();

// Implement batching
services.Configure<BulkOperationDetectorOptions>(options =>
{
    options.BatchSize = 1000;
    options.BatchTimeout = TimeSpan.FromSeconds(5);
});

// Use async/await properly
public async Task ProcessChangeEventAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
{
    await Task.Run(() => ProcessEvent(changeEvent), cancellationToken);
}
```

### 4. Monitoring and Observability

```csharp
// Enable comprehensive logging
services.Configure<ChangeEventTracerOptions>(options =>
{
    options.EnableTracing = true;
    options.TraceLevel = System.Diagnostics.ActivitySource.DefaultActivitySourceName;
});

// Use structured logging
_logger.LogInformation("Processing change event: {Operation} on {Table} for tenant {TenantId}", 
    changeEvent.Operation, changeEvent.Table, changeEvent.Metadata.GetValueOrDefault("tenant_id"));
```

### 5. Configuration Management

```csharp
// Use configuration validation
services.Configure<PostgresAdapterOptions>(options =>
{
    Configuration.GetSection("PostgresAdapter").Bind(options);
    
    if (string.IsNullOrEmpty(options.ConnectionString))
    {
        throw new InvalidOperationException("PostgreSQL connection string is required");
    }
});

// Use environment-specific configuration
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        if (Environment.IsDevelopment())
        {
            // Development configuration
            services.Configure<PostgresAdapterOptions>(options =>
            {
                options.ConnectionString = "Host=localhost;Database=devdb;Username=devuser;Password=devpass";
            });
        }
        else
        {
            // Production configuration
            services.Configure<PostgresAdapterOptions>(options =>
            {
                options.ConnectionString = Configuration.GetConnectionString("PostgreSQL");
            });
        }
    }
}
```

This troubleshooting guide should help you resolve most common issues with SQLDBEntityNotifier. If you encounter issues not covered here, please check the logs for more detailed error information and consider opening an issue on the project repository.