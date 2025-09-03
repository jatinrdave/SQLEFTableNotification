# Change Context Filtering in SQLDBEntityNotifier

## Overview

SQLDBEntityNotifier now supports **Change Context Filtering** to prevent infinite loops in bi-directional synchronization scenarios. This feature allows you to distinguish between different sources of database changes and filter notifications accordingly.

## The Problem

In bi-directional sync scenarios, when System A pushes data to System B, and System B's database triggers change notifications that push data back to System A, you end up in an infinite loop:

```
System A → Database Change → Notification → System B → Database Change → Notification → System A → ...
```

## The Solution

Change Context Filtering uses SQL Server's `SYS_CHANGE_CONTEXT` feature to identify the source of changes and filter notifications based on configurable rules.

## Key Features

- **Context-Based Filtering**: Filter changes by their source context
- **Bidirectional Sync Support**: Prevent infinite loops between systems
- **Flexible Configuration**: Include or exclude specific change contexts
- **Extended Information**: Get detailed context information in notifications
- **Backward Compatibility**: Existing code continues to work unchanged

## Change Context Types

| Context | Value | Description |
|---------|-------|-------------|
| `Application` | 1 | Application or system changes |
| `UserInterface` | 2 | User interface actions |
| `WebService` | 3 | Web service or API calls |
| `ScheduledJob` | 4 | Scheduled jobs or batch processes |
| `DataMigration` | 5 | Data migration or ETL processes |
| `Replication` | 6 | Replication or sync processes |
| `Maintenance` | 7 | Maintenance scripts or DBA operations |
| `Unknown` | 99 | Unknown or unspecified source |

## Usage Examples

### 1. Prevent Replication Loops

```csharp
// Exclude changes from replication and maintenance processes
var filterOptions = ChangeFilterOptions.Exclude(
    ChangeContext.Replication,      // Exclude sync processes
    ChangeContext.DataMigration,    // Exclude ETL processes
    ChangeContext.Maintenance       // Exclude maintenance scripts
);

var notificationService = new SqlDBNotificationService<YourEntity>(
    changeTableService: new ChangeTableService<YourEntity>(dbContext),
    tableName: "YourTable",
    connectionString: "your_connection_string",
    filterOptions: filterOptions
);
```

### 2. Allow Only Specific Contexts

```csharp
// Only allow changes from user interface and web services
var filterOptions = ChangeFilterOptions.AllowOnly(
    ChangeContext.UserInterface,    // User actions
    ChangeContext.WebService        // API calls
);

var notificationService = new SqlDBNotificationService<YourEntity>(
    changeTableService: new ChangeTableService<YourEntity>(dbContext),
    tableName: "YourTable",
    connectionString: "your_connection_string",
    filterOptions: filterOptions
);
```

### 3. Advanced Filtering with Extended Information

```csharp
var filterOptions = new ChangeFilterOptions
{
    // Exclude replication and maintenance changes
    ExcludedChangeContexts = new List<ChangeContext>
    {
        ChangeContext.Replication,
        ChangeContext.Maintenance
    },
    
    // Include detailed context information
    IncludeChangeContext = true,
    IncludeApplicationName = true,
    IncludeHostName = true,
    IncludeUserInfo = true
};

var notificationService = new SqlDBNotificationService<YourEntity>(
    changeTableService: new ChangeTableService<YourEntity>(dbContext),
    tableName: "YourTable",
    connectionString: "your_connection_string",
    filterOptions: filterOptions
);

// Subscribe to enhanced change events
notificationService.OnChanged += (sender, e) =>
{
    var context = e.ChangeContext;
    Console.WriteLine($"Change from {context?.ApplicationName} on {context?.HostName}");
    Console.WriteLine($"Context: {context?.Context}");
    Console.WriteLine($"Version: {e.ChangeVersion}");
    Console.WriteLine($"Detected at: {e.ChangeDetectedAt}");
    
    // Process the change
    ProcessChange(e.Entities);
};
```

### 4. Multi-System Integration

```csharp
// System 1: Only listen to its own changes
var system1Filter = ChangeFilterOptions.AllowOnly(
    ChangeContext.Application, 
    ChangeContext.UserInterface
);

var system1Service = new SqlDBNotificationService<YourEntity>(
    changeTableService: new ChangeTableService<YourEntity>(dbContext),
    tableName: "YourTable",
    connectionString: "system1_connection_string",
    filterOptions: system1Filter
);

// System 2: Only listen to its own changes
var system2Filter = ChangeFilterOptions.AllowOnly(
    ChangeContext.WebService, 
    ChangeContext.ScheduledJob
);

var system2Service = new SqlDBNotificationService<YourEntity>(
    changeTableService: new ChangeTableService<YourEntity>(dbContext),
    tableName: "YourTable",
    connectionString: "system2_connection_string",
    filterOptions: system2Filter
);

// Each system only processes its own changes, preventing loops
await Task.WhenAll(
    system1Service.StartNotify(),
    system2Service.StartNotify()
);
```

## Setting Change Context in Your Application

To use change context filtering, you need to set the `SYS_CHANGE_CONTEXT` when making database changes:

### Using Entity Framework

```csharp
// Set change context before making changes
using (var transaction = dbContext.Database.BeginTransaction())
{
    try
    {
        // Set the change context for this transaction
        dbContext.Database.ExecuteSqlRaw("SET CHANGE_TRACKING_CONTEXT 1"); // Application context
        
        // Make your changes
        dbContext.YourEntities.Add(newEntity);
        await dbContext.SaveChangesAsync();
        
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

### Using Raw SQL

```sql
-- Set change context for the current session
SET CHANGE_TRACKING_CONTEXT 1; -- Application context

-- Make your changes
INSERT INTO YourTable (Column1, Column2) VALUES ('Value1', 'Value2');
UPDATE YourTable SET Column1 = 'NewValue' WHERE Id = 1;
DELETE FROM YourTable WHERE Id = 1;
```

### Using Stored Procedures

```sql
CREATE PROCEDURE UpdateYourTable
    @Id INT,
    @Column1 NVARCHAR(100)
AS
BEGIN
    SET CHANGE_TRACKING_CONTEXT 1; -- Application context
    
    UPDATE YourTable 
    SET Column1 = @Column1 
    WHERE Id = @Id;
END
```

## Configuration Options

### ChangeFilterOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AllowedChangeContexts` | `List<ChangeContext>?` | `null` | Only these contexts trigger notifications |
| `ExcludedChangeContexts` | `List<ChangeContext>?` | `null` | These contexts are excluded (takes precedence) |
| `IncludeChangeContext` | `bool` | `true` | Include context info in notifications |
| `IncludeChangeQuery` | `bool` | `false` | Include the SQL query that caused the change |
| `IncludeUserInfo` | `bool` | `false` | Include user information if available |
| `IncludeApplicationName` | `bool` | `false` | Include application name |
| `IncludeHostName` | `bool` | `false` | Include host name |

### Factory Methods

```csharp
// Exclude specific contexts
var excludeFilter = ChangeFilterOptions.Exclude(
    ChangeContext.Replication, 
    ChangeContext.Maintenance
);

// Allow only specific contexts
var allowFilter = ChangeFilterOptions.AllowOnly(
    ChangeContext.Application, 
    ChangeContext.UserInterface
);

// Custom configuration
var customFilter = new ChangeFilterOptions
{
    ExcludedChangeContexts = new List<ChangeContext> { ChangeContext.Replication },
    IncludeChangeContext = true,
    IncludeApplicationName = true
};
```

## Best Practices

1. **Set Change Context Early**: Set the change context at the beginning of your database operations
2. **Use Consistent Values**: Use the same context values across your application
3. **Document Your Contexts**: Document what each context value represents in your system
4. **Test Your Filters**: Verify that your filters work as expected in your environment
5. **Monitor Performance**: Change context filtering adds minimal overhead but monitor in high-volume scenarios

## Migration Guide

### From Previous Versions

Existing code continues to work without changes:

```csharp
// This still works exactly as before
var notificationService = new SqlDBNotificationService<YourEntity>(
    changeTableService: new ChangeTableService<YourEntity>(dbContext),
    tableName: "YourTable",
    connectionString: "your_connection_string"
);
```

### Adding Context Filtering

To add context filtering to existing code:

```csharp
// Before (existing code)
var notificationService = new SqlDBNotificationService<YourEntity>(...);

// After (with context filtering)
var filterOptions = ChangeFilterOptions.Exclude(ChangeContext.Replication);
var notificationService = new SqlDBNotificationService<YourEntity>(
    changeTableService: new ChangeTableService<YourEntity>(dbContext),
    tableName: "YourTable",
    connectionString: "your_connection_string",
    filterOptions: filterOptions
);
```

## Troubleshooting

### Common Issues

1. **No Notifications**: Check if your filter is too restrictive
2. **Still Getting Loops**: Verify that all systems are setting appropriate change contexts
3. **Performance Issues**: Ensure your change tracking queries are optimized

### Debugging

Enable extended context information to debug filtering issues:

```csharp
var filterOptions = new ChangeFilterOptions
{
    IncludeChangeContext = true,
    IncludeApplicationName = true,
    IncludeHostName = true
};

notificationService.OnChanged += (sender, e) =>
{
    var context = e.ChangeContext;
    Console.WriteLine($"Change Context: {context?.Context}");
    Console.WriteLine($"Application: {context?.ApplicationName}");
    Console.WriteLine($"Host: {context?.HostName}");
};
```

## Conclusion

Change Context Filtering provides a robust solution for preventing infinite loops in bi-directional synchronization scenarios while maintaining backward compatibility. By properly configuring change contexts and filters, you can ensure that your systems only process relevant changes and avoid the pitfalls of circular data synchronization.
