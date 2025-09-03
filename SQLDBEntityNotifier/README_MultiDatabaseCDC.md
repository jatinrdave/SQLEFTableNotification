# SQLDBEntityNotifier - Multi-Database CDC Support

## Overview

SQLDBEntityNotifier 2.0 is a comprehensive .NET library that provides Change Data Capture (CDC) functionality across multiple database types:

- **SQL Server** - Using native Change Data Capture
- **MySQL** - Using Binary Log Change Data Capture  
- **PostgreSQL** - Using Logical Replication and WAL

The library provides a unified, database-agnostic interface for monitoring database changes with minimal configuration requirements.

## Features

### 🚀 **Multi-Database Support**
- **SQL Server**: Native CDC with `sys.sp_cdc_enable_table`
- **MySQL**: Binary log monitoring with replication privileges
- **PostgreSQL**: Logical replication with WAL position tracking

### 🔧 **Minimal Configuration**
- Automatic database type detection
- Smart connection string building
- Default settings for common scenarios
- Factory pattern for easy provider creation

### 📊 **Enhanced Change Detection**
- CRUD operation details (Insert, Update, Delete, Schema Change)
- Old and new values for updates
- Affected columns tracking
- Transaction and batch operation support
- Change context and metadata

### 🏥 **Health Monitoring**
- Real-time CDC health status
- Performance metrics and lag monitoring
- Automatic health checks
- Configuration validation

### 🎯 **Multi-Table Support**
- Monitor multiple tables simultaneously
- Table-specific change filtering
- Batch change processing
- Cross-table change correlation

## Quick Start

### 1. Install the Package

```bash
dotnet add package SQLDBEntityNotifier
```

### 2. Basic SQL Server CDC

```csharp
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;

// Minimal configuration
var config = DatabaseConfiguration.CreateSqlServer(
    "Server=localhost;Database=YourDB;Integrated Security=true;"
);

using var service = new UnifiedDBNotificationService<YourEntity>(
    config, "YourTable", TimeSpan.FromSeconds(15)
);

// Subscribe to events
service.OnChanged += (sender, e) =>
{
    Console.WriteLine($"Change: {e.Operation} on {e.TableName}");
    Console.WriteLine($"Database: {e.DatabaseType}");
    Console.WriteLine($"Change ID: {e.ChangeIdentifier}");
};

await service.StartMonitoringAsync();
```

### 3. MySQL CDC

```csharp
var config = DatabaseConfiguration.CreateMySql(
    serverName: "localhost",
    databaseName: "your_db",
    username: "user",
    password: "pass"
);

using var service = new UnifiedDBNotificationService<YourEntity>(
    config, "your_table", TimeSpan.FromSeconds(10)
);

await service.StartMonitoringAsync();
```

### 4. PostgreSQL CDC

```csharp
var config = DatabaseConfiguration.CreatePostgreSql(
    serverName: "localhost",
    databaseName: "your_db",
    username: "user",
    password: "pass",
    schemaName: "public"
);

using var service = new UnifiedDBNotificationService<YourEntity>(
    config, "your_table", TimeSpan.FromSeconds(20)
);

await service.StartMonitoringAsync();
```

## Configuration

### Database Configuration

#### SQL Server
```csharp
var config = DatabaseConfiguration.CreateSqlServer(
    connectionString: "Server=localhost;Database=YourDB;Integrated Security=true;",
    databaseName: "YourDB"
);
```

#### MySQL
```csharp
var config = DatabaseConfiguration.CreateMySql(
    serverName: "localhost",
    databaseName: "your_database",
    username: "your_username",
    password: "your_password",
    port: 3306
);
```

#### PostgreSQL
```csharp
var config = DatabaseConfiguration.CreatePostgreSql(
    serverName: "localhost",
    databaseName: "your_database",
    username: "your_username",
    password: "your_password",
    port: 5432,
    schemaName: "public"
);
```

### Advanced Configuration

```csharp
var config = new DatabaseConfiguration
{
    DatabaseType = DatabaseType.SqlServer,
    ServerName = "localhost",
    DatabaseName = "YourDatabase",
    Username = "sa",
    Password = "password",
    ConnectionTimeout = 60,
    CommandTimeout = 120,
    MaxPoolSize = 200,
    MinPoolSize = 10,
    EnableConnectionPooling = true,
    ApplicationName = "MyCustomApp",
    UseSsl = true,
    AdditionalParameters = new Dictionary<string, string>
    {
        ["TrustServerCertificate"] = "true",
        ["MultipleActiveResultSets"] = "true"
    }
};
```

## Usage Examples

### Multi-Table Monitoring

```csharp
using var service = new UnifiedDBNotificationService<YourEntity>(
    config, "Users", TimeSpan.FromSeconds(30)
);

service.OnChanged += async (sender, e) =>
{
    // Get changes for multiple tables
    var multiTableChanges = await service.GetMultiTableChangesAsync(
        new[] { "Users", "Orders", "Products" }
    );
    
    foreach (var tableChange in multiTableChanges)
    {
        Console.WriteLine($"Table {tableChange.Key}: {tableChange.Value.Count} changes");
    }
};
```

### Health Monitoring

```csharp
service.OnHealthCheck += (sender, healthInfo) =>
{
    Console.WriteLine($"Health Status: {healthInfo.Status}");
    Console.WriteLine($"Changes/Hour: {healthInfo.ChangesLastHour}");
    Console.WriteLine($"Errors/Hour: {healthInfo.ErrorsLastHour}");
    Console.WriteLine($"Response Time: {healthInfo.AverageResponseTime}");
    Console.WriteLine($"CDC Lag: {healthInfo.CDCLag}");
};

// Manual health check
var healthInfo = await service.GetHealthInfoAsync();
Console.WriteLine($"Health: {healthInfo.Status}");
```

### Configuration Validation

```csharp
var validation = await service.ValidateConfigurationAsync();
if (validation.IsValid)
{
    Console.WriteLine("Configuration is valid");
    await service.StartMonitoringAsync();
}
else
{
    Console.WriteLine("Configuration validation failed:");
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

### Factory Pattern

```csharp
// Create providers using factory
var sqlServerProvider = CDCProviderFactory.CreateSqlServerProvider(
    "Server=localhost;Database=YourDB;Integrated Security=true;"
);

var mySqlProvider = CDCProviderFactory.CreateMySqlProvider(
    "localhost", "your_db", "user", "pass"
);

var postgreSqlProvider = CDCProviderFactory.CreatePostgreSqlProvider(
    "localhost", "your_db", "user", "pass"
);

// Create services using providers
using var sqlServerService = new UnifiedDBNotificationService<YourEntity>(
    sqlServerProvider, "YourTable"
);

using var mySqlService = new UnifiedDBNotificationService<YourEntity>(
    mySqlProvider, "your_table"
);
```

## Database Setup Requirements

### SQL Server
1. Enable CDC at database level:
   ```sql
   EXEC sys.sp_cdc_enable_db
   ```

2. Enable CDC for specific tables:
   ```sql
   EXEC sys.sp_cdc_enable_table
       @source_schema = 'dbo',
       @source_name = 'YourTable',
       @role_name = NULL
   ```

### MySQL
1. Enable binary logging in `my.cnf`:
   ```ini
   [mysqld]
   log-bin=mysql-bin
   binlog-format=ROW
   ```

2. Grant replication privileges:
   ```sql
   GRANT REPLICATION SLAVE ON *.* TO 'your_user'@'%';
   FLUSH PRIVILEGES;
   ```

### PostgreSQL
1. Enable logical replication in `postgresql.conf`:
   ```ini
   wal_level = logical
   max_replication_slots = 10
   max_wal_senders = 10
   ```

2. Grant replication privileges:
   ```sql
   ALTER USER your_user REPLICATION;
   ```

## Event Arguments

### EnhancedRecordChangedEventArgs<T>

```csharp
public class EnhancedRecordChangedEventArgs<T> : RecordChangedEventArgs<T>
{
    public ChangeOperation Operation { get; set; }
    public DatabaseType DatabaseType { get; set; }
    public string? ChangeIdentifier { get; set; }
    public DateTime? DatabaseChangeTimestamp { get; set; }
    public string? ChangedBy { get; set; }
    public string? ApplicationName { get; set; }
    public string? HostName { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public T? OldValues { get; set; }
    public T? NewValues { get; set; }
    public List<string>? AffectedColumns { get; set; }
    public string? TransactionId { get; set; }
    public bool IsBatchOperation { get; set; }
    public int? BatchSequence { get; set; }
}
```

### ChangeOperation Enum

```csharp
public enum ChangeOperation
{
    Insert = 1,
    Update = 2,
    Delete = 3,
    SchemaChange = 4,
    Unknown = 99
}
```

### DatabaseType Enum

```csharp
public enum DatabaseType
{
    SqlServer = 1,
    MySql = 2,
    PostgreSql = 3
}
```

## Error Handling

### Subscribe to Error Events

```csharp
service.OnError += (sender, e) =>
{
    Console.WriteLine($"Error: {e.Message}");
    Console.WriteLine($"Exception: {e.Exception?.GetType().Name}");
    
    // Implement retry logic
    if (e.Message.Contains("connection"))
    {
        Console.WriteLine("Attempting to reconnect...");
        // Implement reconnection logic
    }
};
```

### Try-Catch with Fallback

```csharp
try
{
    await service.StartMonitoringAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to start: {ex.Message}");
    
    // Try with different configuration
    var fallbackConfig = DatabaseConfiguration.CreateSqlServer(
        "Server=fallback_server;Database=YourDB;Integrated Security=true;"
    );
    
    service = new UnifiedDBNotificationService<YourEntity>(fallbackConfig, "YourTable");
    await service.StartMonitoringAsync();
}
```

## Performance Considerations

### Polling Intervals
- **SQL Server**: 15-30 seconds (CDC is very efficient)
- **MySQL**: 10-15 seconds (binary log parsing overhead)
- **PostgreSQL**: 20-30 seconds (WAL processing overhead)

### Connection Pooling
```csharp
var config = new DatabaseConfiguration
{
    // ... other settings
    EnableConnectionPooling = true,
    MaxPoolSize = 100,
    MinPoolSize = 5
};
```

### Batch Processing
```csharp
// Process changes in batches
service.OnChanged += (sender, e) =>
{
    if (e.IsBatchOperation)
    {
        Console.WriteLine($"Processing batch of {e.Metadata?["ChangeCount"]} changes");
        // Implement batch processing logic
    }
};
```

## Best Practices

### 1. **Resource Management**
```csharp
using var service = new UnifiedDBNotificationService<YourEntity>(config, "YourTable");
// Service automatically disposed when using statement ends
```

### 2. **Error Recovery**
```csharp
service.OnError += (sender, e) =>
{
    // Log error
    _logger.LogError(e.Exception, e.Message);
    
    // Implement circuit breaker pattern
    if (_errorCount++ > 5)
    {
        service.StopMonitoring();
        // Implement exponential backoff retry
    }
};
```

### 3. **Health Monitoring**
```csharp
// Subscribe to health events
service.OnHealthCheck += (sender, healthInfo) =>
{
    if (healthInfo.Status == CDCHealthStatus.Unhealthy)
    {
        // Alert operations team
        _alertService.SendAlert($"CDC Health: {healthInfo.Status}");
    }
};
```

### 4. **Configuration Validation**
```csharp
// Always validate before starting
var validation = await service.ValidateConfigurationAsync();
if (!validation.IsValid)
{
    throw new InvalidOperationException(
        $"CDC configuration invalid: {string.Join(", ", validation.Errors)}"
    );
}
```

## Troubleshooting

### Common Issues

#### SQL Server
- **CDC not enabled**: Run `EXEC sys.sp_cdc_enable_db`
- **Table not tracked**: Run `EXEC sys.sp_cdc_enable_table`
- **Permission denied**: Ensure user has `db_owner` or `cdc_admin` role

#### MySQL
- **Binary log disabled**: Set `log-bin=mysql-bin` in `my.cnf`
- **Replication privilege missing**: Grant `REPLICATION SLAVE` privilege
- **Connection timeout**: Increase `connect_timeout` in MySQL configuration

#### PostgreSQL
- **Logical replication disabled**: Set `wal_level = logical`
- **Replication privilege missing**: Grant `REPLICATION` privilege
- **Replication slots exhausted**: Increase `max_replication_slots`

### Debug Information

```csharp
// Enable detailed logging
var healthInfo = await service.GetHealthInfoAsync();
foreach (var metric in healthInfo.Metrics)
{
    Console.WriteLine($"{metric.Key}: {metric.Value}");
}

// Validate configuration
var validation = await service.ValidateConfigurationAsync();
foreach (var message in validation.Messages)
{
    Console.WriteLine($"Info: {message}");
}
foreach (var warning in validation.Warnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

## Migration from v1.x

### Old Code (v1.x)
```csharp
var changeService = new ChangeTableService<User>(dbContext);
var notificationService = new SqlDBNotificationService<User>(
    changeService, "Users", connectionString, -1L, null
);
```

### New Code (v2.0)
```csharp
var config = DatabaseConfiguration.CreateSqlServer(connectionString);
using var service = new UnifiedDBNotificationService<User>(config, "Users");
await service.StartMonitoringAsync();
```

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues and questions:
- GitHub Issues: [https://github.com/jatinrdave/SQLEFTableNotification/issues](https://github.com/jatinrdave/SQLEFTableNotification/issues)
- NuGet Package: [https://www.nuget.org/packages/SQLDBEntityNotifier](https://www.nuget.org/packages/SQLDBEntityNotifier)