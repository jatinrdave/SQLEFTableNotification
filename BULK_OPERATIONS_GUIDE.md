# SQLDBEntityNotifier - Bulk Operations Guide

## Overview

SQLDBEntityNotifier now supports **comprehensive bulk operation detection and filtering** for enterprise scenarios. This enhancement allows you to:

1. **Detect bulk operations** (Bulk Insert, Bulk Update, Bulk Delete)
2. **Filter notifications** based on operation type, row count, duration, and more
3. **Monitor performance** of bulk operations
4. **Get detailed statistics** about bulk operations

---

## 🚀 **Key Features**

### ✅ **Bulk Operation Detection**
- **Automatic Detection**: Detects bulk operations based on transaction patterns, timing, and metadata
- **Multiple Strategies**: PostgreSQL WAL analysis, SQLite timing patterns, transaction grouping
- **Real-time Processing**: Processes bulk operations as they occur
- **Batch Grouping**: Groups related operations into batches for analysis

### ✅ **Advanced Filtering**
- **LINQ-like Expressions**: Use familiar LINQ syntax for filtering
- **Performance-based**: Filter by row count, execution duration, operation type
- **Table-specific**: Include/exclude specific tables
- **Transaction-aware**: Filter by transaction ID or batch ID

### ✅ **Performance Monitoring**
- **Slow Operation Alerts**: Detect operations exceeding duration thresholds
- **Large Operation Alerts**: Detect operations affecting many rows
- **Statistics Generation**: Get comprehensive statistics about bulk operations
- **Performance Metrics**: Track execution times and row counts

---

## 📊 **Bulk Operation Types**

### **BULK_INSERT**
- Multiple rows inserted in a single operation
- Detected via transaction patterns or WAL analysis
- Includes sample data from inserted rows

### **BULK_UPDATE**
- Multiple rows updated in a single operation
- Detected via transaction patterns or WAL analysis
- Includes before/after data samples

### **BULK_DELETE**
- Multiple rows deleted in a single operation
- Detected via transaction patterns or WAL analysis
- Includes before data samples

---

## 🔧 **Configuration**

### **Basic Configuration**
```csharp
services.Configure<BulkOperationDetectorOptions>(options =>
{
    options.Enabled = true;
    options.MinRowCount = 2;                    // Minimum rows to consider as bulk
    options.MaxBatchSize = 1000;                // Maximum batch size
    options.BatchTimeoutSeconds = 5;            // Batch timeout
    options.MaxSampleSize = 10;                 // Sample data size
    options.IncludeSampleData = true;           // Include sample data
    options.GroupByTransaction = true;          // Group by transaction
});
```

### **Filtering Configuration**
```csharp
services.Configure<BulkOperationDetectorOptions>(options =>
{
    // Include only specific tables
    options.IncludedTables.Add("orders");
    options.IncludedTables.Add("customers");
    
    // Exclude specific tables
    options.ExcludedTables.Add("temp_table");
    options.ExcludedTables.Add("audit_log");
    
    // Exclude specific operation types
    options.ExcludedOperations.Add(BulkOperationType.BULK_DELETE);
});
```

### **Performance Monitoring**
```csharp
services.Configure<BulkOperationDetectorOptions>(options =>
{
    options.PerformanceMonitoring.Enabled = true;
    options.PerformanceMonitoring.SlowOperationThresholdMs = 1000;    // 1 second
    options.PerformanceMonitoring.LargeOperationThreshold = 10000;    // 10k rows
    options.PerformanceMonitoring.AlertOnSlowOperations = true;
    options.PerformanceMonitoring.AlertOnLargeOperations = true;
});
```

---

## 🎯 **Usage Examples**

### **1. Basic Bulk Operation Detection**
```csharp
// Configure services
services.AddSqlDbEntityNotifier();
services.AddSingleton<BulkOperationDetector>();
services.AddSingleton<BulkOperationFilterEngine>();

// Subscribe to bulk operations
var subscription = await entityNotifier.SubscribeAsync<BulkOperationEvent>(
    new SubscriptionOptions
    {
        TableName = "__schema_changes__" // Special table for bulk operations
    },
    async (changeEvent, bulkEvent, cancellationToken) =>
    {
        Console.WriteLine($"Bulk {bulkEvent.Operation} on {bulkEvent.Table} affecting {bulkEvent.AffectedRowCount} rows");
    });
```

### **2. Advanced Filtering**
```csharp
var filterEngine = serviceProvider.GetRequiredService<BulkOperationFilterEngine>();

// Filter by operation type
var insertFilter = filterEngine.CreateOperationTypeFilter(BulkOperationType.BULK_INSERT);

// Filter by minimum row count
var largeOperationFilter = filterEngine.CreateMinRowCountFilter(1000);

// Filter by execution duration
var slowOperationFilter = filterEngine.CreateMinDurationFilter(5000);

// Complex filter combining multiple conditions
var complexFilter = filterEngine.CreateComplexFilter(
    tableName: "orders",
    operationType: BulkOperationType.BULK_UPDATE,
    minRowCount: 100,
    maxRowCount: 10000,
    minDurationMs: 1000
);

// High-impact operations filter
var highImpactFilter = filterEngine.CreateHighImpactFilter(
    minRowCount: 1000,
    minDurationMs: 5000
);
```

### **3. Statistics and Analytics**
```csharp
var bulkEvents = await GetBulkOperationsAsync();
var stats = filterEngine.GetStatistics(bulkEvents);

Console.WriteLine($"Total Operations: {stats.TotalOperations}");
Console.WriteLine($"Total Affected Rows: {stats.TotalAffectedRows}");
Console.WriteLine($"Average Rows per Operation: {stats.AverageAffectedRows:F2}");
Console.WriteLine($"Max Rows in Single Operation: {stats.MaxAffectedRows}");
Console.WriteLine($"Average Duration: {stats.AverageExecutionDuration:F2}ms");
Console.WriteLine($"Max Duration: {stats.MaxExecutionDuration}ms");

// Operations by type
foreach (var kvp in stats.OperationsByType)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value} operations");
}

// Operations by table
foreach (var kvp in stats.OperationsByTable)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value} operations");
}
```

### **4. Performance Monitoring**
```csharp
// Subscribe to high-impact operations
var highImpactFilter = filterEngine.CreateHighImpactFilter(1000, 5000);

var subscription = await entityNotifier.SubscribeAsync<BulkOperationEvent>(
    new SubscriptionOptions { TableName = "__schema_changes__" },
    async (changeEvent, bulkEvent, cancellationToken) =>
    {
        if (highImpactFilter(bulkEvent))
        {
            // Alert on high-impact operations
            await SendAlertAsync($"High-impact bulk operation: {bulkEvent.Operation} on {bulkEvent.Table} affecting {bulkEvent.AffectedRowCount} rows in {bulkEvent.ExecutionDurationMs}ms");
        }
    });
```

---

## 🗄️ **Database-Specific Implementation**

### **PostgreSQL**
- **WAL Analysis**: Analyzes Write-Ahead Log entries to detect bulk operations
- **Transaction Grouping**: Groups operations by transaction ID
- **Row Count Detection**: Uses WAL metadata to determine affected row counts
- **Sample Data**: Extracts sample data from WAL entries

```csharp
// PostgreSQL-specific configuration
services.Configure<PostgresAdapterOptions>(options =>
{
    options.Source = "postgres-orders";
    options.ConnectionString = "Host=localhost;Database=orders;Username=user;Password=pass";
    options.SlotName = "orders_replication_slot";
    options.Plugin = "wal2json";
    options.IncludeBefore = true;
    options.IncludeAfter = true;
});
```

### **SQLite**
- **Timing Patterns**: Detects bulk operations based on timing patterns
- **Trigger-based**: Uses database triggers to capture changes
- **Batch Detection**: Groups changes that occur within short time windows
- **Transaction Awareness**: Tracks transaction boundaries

```csharp
// SQLite-specific configuration
services.Configure<SqliteAdapterOptions>(options =>
{
    options.Source = "sqlite-orders";
    options.FilePath = "orders.db";
    options.ChangeTable = "change_log";
    options.IncludeBefore = true;
    options.IncludeAfter = true;
    options.PollingIntervalSeconds = 1;
});
```

---

## 📈 **Performance Considerations**

### **Batch Processing**
- **Configurable Batch Size**: Adjust `MaxBatchSize` based on your needs
- **Timeout Handling**: Set appropriate `BatchTimeoutSeconds` for your workload
- **Memory Usage**: Monitor memory usage with large batch sizes

### **Filtering Performance**
- **Compiled Filters**: Filters are compiled for optimal performance
- **Indexing**: Use appropriate database indexes for better performance
- **Caching**: Consider caching frequently used filters

### **Monitoring Overhead**
- **Sampling**: Use `MaxSampleSize` to limit sample data size
- **Selective Monitoring**: Enable monitoring only for critical tables
- **Performance Thresholds**: Set appropriate thresholds to avoid noise

---

## 🔍 **Troubleshooting**

### **Common Issues**

#### **Bulk Operations Not Detected**
```csharp
// Check configuration
options.Enabled = true;
options.MinRowCount = 2; // Lower threshold
options.BatchTimeoutSeconds = 10; // Longer timeout
```

#### **Too Many Notifications**
```csharp
// Increase thresholds
options.MinRowCount = 100;
options.PerformanceMonitoring.SlowOperationThresholdMs = 5000;
options.PerformanceMonitoring.LargeOperationThreshold = 10000;
```

#### **Memory Usage Issues**
```csharp
// Reduce batch size and sample size
options.MaxBatchSize = 100;
options.MaxSampleSize = 5;
options.IncludeSampleData = false;
```

### **Debugging**
```csharp
// Enable detailed logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Add custom logging in filters
var filter = filterEngine.CompileFilter(be =>
{
    _logger.LogDebug("Processing bulk operation: {Operation} on {Table}", be.Operation, be.Table);
    return be.AffectedRowCount > 100;
});
```

---

## 🚀 **Advanced Use Cases**

### **1. Data Warehouse ETL Monitoring**
```csharp
// Monitor ETL operations
var etlFilter = filterEngine.CreateComplexFilter(
    tableName: "fact_sales",
    operationType: BulkOperationType.BULK_INSERT,
    minRowCount: 10000
);

// Alert on slow ETL operations
var slowEtlFilter = filterEngine.CreateMinDurationFilter(30000); // 30 seconds
```

### **2. Audit Trail for Bulk Operations**
```csharp
// Track all bulk operations for audit
var auditFilter = filterEngine.CreateComplexFilter(
    minRowCount: 1 // All bulk operations
);

// Store in audit database
await auditService.LogBulkOperationAsync(bulkEvent);
```

### **3. Performance Optimization**
```csharp
// Identify tables with frequent bulk operations
var stats = filterEngine.GetStatistics(bulkEvents);
var frequentTables = stats.OperationsByTable
    .Where(kvp => kvp.Value > 100)
    .OrderByDescending(kvp => kvp.Value);

// Optimize indexes for frequently bulk-updated tables
foreach (var table in frequentTables)
{
    await optimizeIndexesAsync(table.Key);
}
```

### **4. Capacity Planning**
```csharp
// Monitor bulk operation trends
var monthlyStats = bulkEvents
    .Where(be => be.TimestampUtc >= DateTime.UtcNow.AddMonths(-1))
    .GroupBy(be => be.TimestampUtc.Date)
    .Select(g => new
    {
        Date = g.Key,
        TotalOperations = g.Count(),
        TotalRows = g.Sum(be => be.AffectedRowCount),
        AvgDuration = g.Average(be => be.ExecutionDurationMs)
    });
```

---

## 📋 **Best Practices**

### **1. Configuration**
- Start with conservative thresholds and adjust based on your workload
- Enable performance monitoring for critical tables
- Use table-specific filtering to reduce noise

### **2. Filtering**
- Use compiled filters for better performance
- Combine multiple filters for complex scenarios
- Cache frequently used filters

### **3. Monitoring**
- Set appropriate alert thresholds
- Monitor both row count and execution duration
- Track trends over time for capacity planning

### **4. Performance**
- Monitor memory usage with large batch sizes
- Use sampling to limit data volume
- Consider asynchronous processing for heavy operations

---

## 🎉 **Summary**

The bulk operations enhancement makes SQLDBEntityNotifier a **comprehensive solution for enterprise CDC scenarios**:

- ✅ **Automatic Detection** of bulk operations across multiple database types
- ✅ **Advanced Filtering** with LINQ-like expressions
- ✅ **Performance Monitoring** with configurable thresholds
- ✅ **Detailed Statistics** for analytics and capacity planning
- ✅ **Production Ready** with proper error handling and logging

**This enhancement significantly improves the platform's ability to handle enterprise-scale bulk operations while providing the flexibility and control needed for complex scenarios.**