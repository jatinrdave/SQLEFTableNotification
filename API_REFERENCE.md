# SQLDBEntityNotifier - Complete API Reference

## 🚀 **Overview**

This document provides the complete API reference for SQLDBEntityNotifier v2.0+, including all advanced CDC features, models, interfaces, and examples.

---

## 📚 **Table of Contents**

1. [Core Interfaces](#core-interfaces)
2. [Models & Data Structures](#models--data-structures)
3. [Advanced CDC Engines](#advanced-cdc-engines)
4. [Configuration & Options](#configuration--options)
5. [Event Arguments](#event-arguments)
6. [Enums & Constants](#enums--constants)
7. [Examples & Usage](#examples--usage)

---

## 🔌 **Core Interfaces**

### **ICDCProvider**

The main interface for CDC operations across different database platforms.

```csharp
public interface ICDProvider : IDisposable
{
    // Core CDC methods
    Task<IEnumerable<ChangeRecord>> GetChangesAsync(string tableName, string? lastPosition = null);
    Task<string> GetLastPositionAsync(string tableName);
    Task<bool> IsTableEnabledAsync(string tableName);
    Task EnableTableAsync(string tableName);
    Task DisableTableAsync(string tableName);
    
    // Schema information methods
    Task<IEnumerable<ColumnDefinition>> GetTableColumnsAsync(string tableName);
    Task<IEnumerable<IndexDefinition>> GetTableIndexesAsync(string tableName);
    Task<IEnumerable<ConstraintDefinition>> GetTableConstraintsAsync(string tableName);
    
    // Health and monitoring
    Task<HealthInfo> GetHealthInfoAsync();
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
    
    // Events
    event EventHandler<ChangeDetectedEventArgs>? OnChangeDetected;
    event EventHandler<ErrorEventArgs>? OnError;
    event EventHandler<HealthCheckEventArgs>? OnHealthCheck;
}
```

### **IDestination**

Interface for change delivery destinations.

```csharp
public interface IDestination : IDisposable
{
    string Name { get; }
    DestinationType Type { get; }
    bool IsEnabled { get; }
    
    Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName);
}
```

### **IFilterRule**

Interface for change filtering rules.

```csharp
public interface IFilterRule
{
    bool Matches(object change);
    IFilterRule Clone();
    string ToString();
}
```

---

## 🏗️ **Models & Data Structures**

### **ChangeRecord**

Base class for all change records.

```csharp
public class ChangeRecord
{
    public string ChangeId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public ChangeOperation Operation { get; set; }
    public DateTime ChangeTimestamp { get; set; }
    public string ChangePosition { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
    
    // Alias properties for backward compatibility
    public ChangeOperation ChangeType => Operation;
    public DateTime Timestamp => ChangeTimestamp;
    public Dictionary<string, object>? Data => Metadata;
}
```

### **DetailedChangeRecord**

Extended change record with old and new values.

```csharp
public class DetailedChangeRecord : ChangeRecord
{
    public Dictionary<string, object>? NewValues { get; set; }
    public Dictionary<string, object>? OldValues { get; set; }
    public List<string>? AffectedColumns { get; set; }
}
```

### **EnhancedChangeContext**

Rich context information for changes.

```csharp
public class EnhancedChangeContext
{
    public string ContextId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public ChangePriority Priority { get; set; }
    public ChangeConfidence Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserId { get; set; }
    public string? ApplicationName { get; set; }
    public string? Environment { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### **SchemaChangeInfo**

Information about schema changes.

```csharp
public class SchemaChangeInfo
{
    public string ChangeId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public SchemaChangeType ChangeType { get; set; }
    public DateTime ChangeTimestamp { get; set; }
    public List<string>? AffectedColumns { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    
    // Alias property for backward compatibility
    public DateTime Timestamp => ChangeTimestamp;
}
```

---

## 🔧 **Advanced CDC Engines**

### **ChangeAnalytics**

Engine for collecting and analyzing change metrics.

```csharp
public class ChangeAnalytics : IDisposable
{
    // Constructor
    public ChangeAnalytics();
    
    // Core methods
    public void RecordChange(string tableName, ChangeOperation operation, TimeSpan processingTime);
    public void RecordBatchChanges(string tableName, int count, TimeSpan processingTime);
    
    // Metrics retrieval
    public TableMetrics GetTableMetrics(string tableName);
    public PerformanceMetrics GetPerformanceMetrics(string tableName);
    public ChangePattern GetChangePattern(string tableName);
    public AggregatedMetrics GetAggregatedMetrics();
    
    // Configuration
    public bool EnableRealTimeMetrics { get; set; }
    public TimeSpan MetricsRetentionPeriod { get; set; }
    
    // Events
    public event EventHandler<PerformanceThresholdExceededEventArgs>? OnPerformanceThresholdExceeded;
    public event EventHandler<ChangePatternDetectedEventArgs>? OnChangePatternDetected;
    public event EventHandler<MetricsAggregatedEventArgs>? OnMetricsAggregated;
    
    // Disposal
    public void Dispose();
    protected virtual void Dispose(bool disposing);
}
```

### **SchemaChangeDetection**

Engine for detecting database schema changes.

```csharp
public class SchemaChangeDetection : IDisposable
{
    // Constructor
    public SchemaChangeDetection();
    
    // Core methods
    public Task<TableSchemaSnapshot> TakeTableSnapshotAsync(string tableName, ICDProvider cdcProvider);
    public Task<List<SchemaChangeInfo>> DetectSchemaChangesAsync(string tableName, ICDProvider cdcProvider);
    public List<SchemaChangeInfo> GetChangeHistory(string tableName);
    
    // Configuration
    public bool EnableAutoDetection { get; set; }
    public TimeSpan DetectionInterval { get; set; }
    public bool IncludeColumnChanges { get; set; }
    public bool IncludeIndexChanges { get; set; }
    public bool IncludeConstraintChanges { get; set; }
    
    // Events
    public event EventHandler<SchemaChangeDetectedEventArgs>? OnSchemaChangeDetected;
    public event EventHandler<SchemaChangeImpactAnalyzedEventArgs>? OnSchemaChangeImpactAnalyzed;
    public event EventHandler<SchemaChangeRiskAssessedEventArgs>? OnSchemaChangeRiskAssessed;
    
    // Disposal
    public void Dispose();
    protected virtual void Dispose(bool disposing);
}
```

### **ChangeCorrelationEngine**

Engine for analyzing change correlations across tables.

```csharp
public class ChangeCorrelationEngine : IDisposable
{
    // Constructor
    public ChangeCorrelationEngine();
    
    // Core methods
    public void RecordChange(string tableName, ChangeRecord change);
    public void RecordBatchChanges(string tableName, List<ChangeRecord> changes);
    public Task<List<CorrelatedChange>> AnalyzeTableCorrelationsAsync(string tableName);
    public TableDependencyGraph GetDependencyGraph(string tableName);
    public List<ChangeRecord> GetRelatedChanges(string sourceTable, string targetTable);
    
    // Configuration
    public bool EnableRealTimeCorrelation { get; set; }
    public TimeSpan CorrelationWindow { get; set; }
    public bool IncludeIndirectDependencies { get; set; }
    
    // Events
    public event EventHandler<ChangeImpactAnalyzedEventArgs>? OnChangeImpactAnalyzed;
    public event EventHandler<ChangeCorrelationDetectedEventArgs>? OnChangeCorrelationDetected;
    
    // Disposal
    public void Dispose();
    protected virtual void Dispose(bool disposing);
}
```

### **ChangeContextManager**

Engine for managing change context information.

```csharp
public class ChangeContextManager : IDisposable
{
    // Constructor
    public ChangeContextManager();
    
    // Core methods
    public EnhancedChangeContext CreateContext(string tableName);
    public void SetContext(EnhancedChangeContext context);
    public EnhancedChangeContext? GetCurrentContext();
    public void ClearContext();
    
    // Context information
    public string GetEnvironment();
    public string GetApplicationName();
    public string GetApplicationVersion();
    public string GetHostName();
    public string GetHostIPAddress();
    
    // Configuration
    public bool EnableContextPropagation { get; set; }
    public bool IncludeHostInformation { get; set; }
    public bool IncludeUserInformation { get; set; }
    
    // Disposal
    public void Dispose();
    protected virtual void Dispose(bool disposing);
}
```

### **AdvancedChangeFilters**

Engine for advanced change filtering capabilities.

```csharp
public class AdvancedChangeFilters : IDisposable
{
    // Constructor
    public AdvancedChangeFilters();
    
    // Filter configuration
    public FilterLogic Logic { get; set; }
    public bool CaseSensitive { get; set; }
    public bool IncludeUnmatched { get; set; }
    public int? MaxResults { get; set; }
    public TimeSpan? MaxAge { get; set; }
    
    // Filter rules
    public IReadOnlyList<IFilterRule> FilterRules { get; }
    public IReadOnlyList<IFilterRule> ExclusionRules { get; }
    
    // Filter methods
    public AdvancedChangeFilters AddFilter(IFilterRule rule);
    public AdvancedChangeFilters AddExclusion(IFilterRule rule);
    public AdvancedChangeFilters AddColumnFilter(string columnName, FilterOperator op, object value);
    public AdvancedChangeFilters AddTimeFilter(TimeFilterType type, DateTime value);
    public AdvancedChangeFilters AddValueFilter(string propertyName, FilterOperator op, object value);
    public AdvancedChangeFilters AddCompositeFilter(CompositeFilterRule rule);
    
    // Management methods
    public AdvancedChangeFilters ClearFilters();
    public AdvancedChangeFilters ClearExclusions();
    public AdvancedChangeFilters Clone();
    
    // Core functionality
    public IEnumerable<T> ApplyFilters<T>(IEnumerable<T> changes) where T : class;
    
    // Utility methods
    public override string ToString();
    
    // Disposal
    public void Dispose();
    protected virtual void Dispose(bool disposing);
}
```

### **ChangeRoutingEngine**

Engine for intelligent change routing to multiple destinations.

```csharp
public class ChangeRoutingEngine : IDisposable
{
    // Constructor
    public ChangeRoutingEngine();
    
    // Configuration
    public IReadOnlyList<IRoutingRule> RoutingRules { get; }
    public IReadOnlyList<IDestination> Destinations { get; }
    public RoutingMetrics Metrics { get; }
    
    // Management methods
    public ChangeRoutingEngine AddRoutingRule(IRoutingRule rule);
    public ChangeRoutingEngine AddDestination(IDestination destination);
    public ChangeRoutingEngine RemoveRoutingRule(string ruleName);
    public ChangeRoutingEngine RemoveDestination(string destinationName);
    
    // Routing methods
    public Task<RoutingResult> RouteChangeAsync(ChangeRecord change, string tableName);
    public Task<List<RoutingResult>> RouteChangesAsync(List<ChangeRecord> changes, string tableName);
    
    // Metrics and statistics
    public DestinationStats GetDestinationStats(string destinationName);
    public OverallRoutingStats GetOverallStats();
    public void ClearMetrics();
    
    // Events
    public event EventHandler<ChangeRoutedEventArgs>? OnChangeRouted;
    public event EventHandler<RoutingFailedEventArgs>? OnRoutingFailed;
    public event EventHandler<RoutingMetricsUpdatedEventArgs>? OnRoutingMetricsUpdated;
    
    // Disposal
    public void Dispose();
    protected virtual void Dispose(bool disposing);
}
```

### **ChangeReplayEngine**

Engine for replaying changes for testing and recovery.

```csharp
public class ChangeReplayEngine : IDisposable
{
    // Constructor
    public ChangeReplayEngine();
    
    // Core methods
    public void RecordChange(string tableName, ChangeRecord change);
    public Task<ReplaySession> StartReplayAsync(string tableName, ReplayOptions options);
    public Task<RecoverySession> StartRecoveryAsync(string tableName, RecoveryOptions options);
    
    // Configuration
    public bool EnableAuditLogging { get; set; }
    public bool EnablePerformanceTracking { get; set; }
    public TimeSpan DefaultProcessingDelay { get; set; }
    
    // Events
    public event EventHandler<ReplayStartedEventArgs>? OnReplayStarted;
    public event EventHandler<ReplayCompletedEventArgs>? OnReplayCompleted;
    public event EventHandler<RecoveryStartedEventArgs>? OnRecoveryStarted;
    public event EventHandler<RecoveryCompletedEventArgs>? OnRecoveryCompleted;
    
    // Disposal
    public void Dispose();
    protected virtual void Dispose(bool disposing);
}
```

---

## ⚙️ **Configuration & Options**

### **ReplayOptions**

Configuration for change replay operations.

```csharp
public class ReplayOptions
{
    public int MaxChanges { get; set; } = 1000;
    public int BatchSize { get; set; } = 100;
    public TimeSpan ProcessingDelay { get; set; } = TimeSpan.FromMilliseconds(50);
    public bool SimulateFailures { get; set; } = false;
    public ReplayMode Mode { get; set; } = ReplayMode.Sequential;
    public bool IncludeMetadata { get; set; } = true;
}
```

### **RecoveryOptions**

Configuration for recovery operations.

```csharp
public class RecoveryOptions
{
    public DateTime RecoveryPoint { get; set; } = DateTime.UtcNow;
    public bool ValidateData { get; set; } = true;
    public bool RollbackOnFailure { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
}
```

### **ColumnChangeFilterOptions**

Configuration for column-level change filtering.

```csharp
public class ColumnChangeFilterOptions
{
    // Factory methods
    public static ColumnChangeFilterOptions MonitorOnly(params string[] columns);
    public static ColumnChangeFilterOptions ExcludeColumns(params string[] columns);
    public static ColumnChangeFilterOptions MonitorAllExcept(params string[] columns);
    
    // Configuration
    public bool IncludeColumnLevelChanges { get; set; } = true;
    public bool IncludeColumnValues { get; set; } = true;
    public int MinimumColumnChanges { get; set; } = 1;
    public bool CaseSensitiveColumnNames { get; set; } = false;
    public bool NormalizeColumnNames { get; set; } = true;
    
    // Column management
    public ColumnChangeFilterOptions AddMonitoredColumns(params string[] columns);
    public ColumnChangeFilterOptions AddExcludedColumns(params string[] columns);
    public ColumnChangeFilterOptions AddColumnMapping(string dbColumn, string entityProperty);
}
```

---

## 📡 **Event Arguments**

### **ChangeDetectedEventArgs**

Event arguments for change detection.

```csharp
public class ChangeDetectedEventArgs : EventArgs
{
    public string TableName { get; }
    public ChangeOperation Operation { get; }
    public DateTime Timestamp { get; }
    public List<ChangeRecord> Changes { get; }
    public List<string>? AffectedColumns { get; }
}
```

### **SchemaChangeDetectedEventArgs**

Event arguments for schema change detection.

```csharp
public class SchemaChangeDetectedEventArgs : EventArgs
{
    public string TableName { get; }
    public SchemaChangeType ChangeType { get; }
    public DateTime Timestamp { get; }
    public List<string>? AffectedColumns { get; }
    public string? Description { get; }
    public Dictionary<string, object>? Metadata { get; }
}
```

### **ChangeRoutedEventArgs**

Event arguments for change routing.

```csharp
public class ChangeRoutedEventArgs : EventArgs
{
    public string TableName { get; }
    public ChangeRecord Change { get; }
    public List<string> RoutedDestinations { get; }
    public TimeSpan ProcessingTime { get; }
    public bool Success { get; }
    public List<string> Errors { get; }
}
```

### **ReplayCompletedEventArgs**

Event arguments for replay completion.

```csharp
public class ReplayCompletedEventArgs : EventArgs
{
    public string TableName { get; }
    public int TotalChanges { get; }
    public int ProcessedChanges { get; }
    public int FailedChanges { get; }
    public TimeSpan TotalTime { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }
}
```

---

## 🔢 **Enums & Constants**

### **ChangeOperation**

Types of database operations.

```csharp
public enum ChangeOperation
{
    Insert = 1,
    Update = 2,
    Delete = 3,
    SchemaChange = 4
}
```

### **SchemaChangeType**

Types of schema changes.

```csharp
public enum SchemaChangeType
{
    ColumnAdded = 1,
    ColumnDropped = 2,
    ColumnDataTypeChanged = 3,
    IndexCreated = 4,
    IndexDropped = 5,
    IndexModified = 6,
    ConstraintAdded = 7,
    ConstraintDropped = 8,
    ConstraintModified = 9,
    TableCreated = 10,
    TableDropped = 11,
    TableRenamed = 12
}
```

### **ChangePriority**

Priority levels for changes.

```csharp
public enum ChangePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}
```

### **FilterLogic**

Logic for combining filter rules.

```csharp
public enum FilterLogic
{
    All = 1,    // All rules must pass (AND logic)
    Any = 2     // Any rule can pass (OR logic)
}
```

### **FilterOperator**

Operators for filter rules.

```csharp
public enum FilterOperator
{
    Equals = 1,
    NotEquals = 2,
    GreaterThan = 3,
    GreaterThanOrEqual = 4,
    LessThan = 5,
    LessThanOrEqual = 6,
    Contains = 7,
    NotContains = 8,
    StartsWith = 9,
    EndsWith = 10,
    IsNull = 11,
    IsNotNull = 12,
    In = 13,
    NotIn = 14,
    Like = 15,
    NotLike = 16
}
```

### **TimeFilterType**

Types of time-based filters.

```csharp
public enum TimeFilterType
{
    After = 1,      // Changes after this time
    Before = 2,     // Changes before this time
    Between = 3,    // Changes between two times
    Within = 4      // Changes within a time range
}
```

### **ReplayMode**

Modes for change replay.

```csharp
public enum ReplayMode
{
    Sequential = 1,  // Process changes in order
    Parallel = 2,    // Process changes concurrently
    Batch = 3,       // Process changes in batches
    Streaming = 4    // Stream changes in real-time
}
```

### **DestinationType**

Types of change destinations.

```csharp
public enum DestinationType
{
    Webhook = 1,
    Database = 2,
    FileSystem = 3,
    MessageQueue = 4,
    Custom = 5
}
```

---

## 💡 **Examples & Usage**

### **Basic Setup and Configuration**

```csharp
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Interfaces;

// Create and configure engines
var analytics = new ChangeAnalytics();
var schemaDetection = new SchemaChangeDetection();
var correlationEngine = new ChangeCorrelationEngine();
var contextManager = new ChangeContextManager();
var filters = new AdvancedChangeFilters();
var routingEngine = new ChangeRoutingEngine();
var replayEngine = new ChangeReplayEngine();

// Configure filters
filters.AddColumnFilter("Status", FilterOperator.Equals, "Active")
       .AddTimeFilter(TimeFilterType.After, DateTime.UtcNow.AddHours(-24))
       .SetMaxResults(100);

// Configure routing
routingEngine.AddDestination(new WebhookDestination("API", "https://api.company.com/webhook"))
             .AddDestination(new DatabaseDestination("AuditDB", auditConnectionString));

// Add routing rules
routingEngine.AddRoutingRule(new TableBasedRoutingRule("UserChanges", 
    new List<string> { "Users" }, 
    new List<string> { "API", "AuditDB" }));
```

### **Event Handling**

```csharp
// Subscribe to events
analytics.OnPerformanceThresholdExceeded += (sender, e) =>
{
    Console.WriteLine($"Performance threshold exceeded: {e.MetricName} = {e.Value}");
};

schemaDetection.OnSchemaChangeDetected += (sender, e) =>
{
    Console.WriteLine($"Schema change detected: {e.ChangeType} on {e.TableName}");
};

correlationEngine.OnChangeImpactAnalyzed += (sender, e) =>
{
    Console.WriteLine($"Change impact analyzed: {e.ImpactLevel} on {e.AffectedTables.Count} tables");
};

routingEngine.OnChangeRouted += (sender, e) =>
{
    Console.WriteLine($"Change routed to {e.RoutedDestinations.Count} destinations");
};

replayEngine.OnReplayCompleted += (sender, e) =>
{
    Console.WriteLine($"Replay completed: {e.ProcessedChanges}/{e.TotalChanges} changes");
};
```

### **Advanced Filtering**

```csharp
// Create complex filter rules
var filters = new AdvancedChangeFilters();

// Add multiple filter conditions
filters.AddColumnFilter("Status", FilterOperator.Equals, "Active")
       .AddColumnFilter("Priority", FilterOperator.GreaterThan, 5)
       .AddTimeFilter(TimeFilterType.After, DateTime.UtcNow.AddHours(-1))
       .AddExclusion(new ColumnFilterRule("InternalField", FilterOperator.Equals, "System"));

// Set filter logic
filters.Logic = FilterLogic.All; // All rules must pass
filters.MaxResults = 50;         // Limit results
filters.MaxAge = TimeSpan.FromMinutes(30); // Only recent changes

// Apply filters
var filteredChanges = filters.ApplyFilters(allChanges);
```

### **Change Replay and Recovery**

```csharp
// Configure replay options
var replayOptions = new ReplayOptions
{
    MaxChanges = 1000,
    BatchSize = 100,
    ProcessingDelay = TimeSpan.FromMilliseconds(50),
    SimulateFailures = false,
    Mode = ReplayMode.Sequential,
    IncludeMetadata = true
};

// Start replay session
var replaySession = await replayEngine.StartReplayAsync("Users", replayOptions);

// Monitor progress
replaySession.OnProgress += (sender, e) =>
{
    Console.WriteLine($"Replay progress: {e.ProcessedChanges}/{e.TotalChanges} ({e.PercentageComplete:F1}%)");
};

// Wait for completion
await replaySession.WaitForCompletionAsync();

// Recovery example
var recoveryOptions = new RecoveryOptions
{
    RecoveryPoint = DateTime.UtcNow.AddHours(-1),
    ValidateData = true,
    RollbackOnFailure = false
};

var recoverySession = await replayEngine.StartRecoveryAsync("Users", recoveryOptions);
await recoverySession.WaitForCompletionAsync();
```

### **Performance Monitoring**

```csharp
// Get performance metrics
var tableMetrics = analytics.GetTableMetrics("Users");
var performanceMetrics = analytics.GetPerformanceMetrics("Users");
var changePattern = analytics.GetChangePattern("Users");
var aggregatedMetrics = analytics.GetAggregatedMetrics();

// Display metrics
Console.WriteLine($"Table: {tableMetrics.TableName}");
Console.WriteLine($"Total Changes: {tableMetrics.TotalChanges}");
Console.WriteLine($"Average Processing Time: {performanceMetrics.AverageProcessingTime}");
Console.WriteLine($"Peak Processing Time: {performanceMetrics.PeakProcessingTime}");
Console.WriteLine($"Changes per Hour: {changePattern.ChangesPerHour}");
```

### **Schema Change Monitoring**

```csharp
// Take initial snapshot
var snapshot = await schemaDetection.TakeTableSnapshotAsync("Users", cdcProvider);

// Monitor for changes
schemaDetection.OnSchemaChangeDetected += async (sender, e) =>
{
    Console.WriteLine($"Schema change detected: {e.ChangeType} on {e.TableName}");
    
    // Get detailed change information
    var changes = await schemaDetection.DetectSchemaChangesAsync(e.TableName, cdcProvider);
    
    foreach (var change in changes)
    {
        Console.WriteLine($"  - {change.ChangeType}: {change.Description}");
        if (change.AffectedColumns?.Any() == true)
        {
            Console.WriteLine($"    Affected columns: {string.Join(", ", change.AffectedColumns)}");
        }
    }
};
```

---

## 🔧 **Configuration Best Practices**

### **Performance Tuning**

```csharp
// Optimize batch sizes
var replayOptions = new ReplayOptions
{
    BatchSize = Environment.ProcessorCount * 10, // Scale with CPU cores
    ProcessingDelay = TimeSpan.FromMilliseconds(10), // Minimal delay
    Mode = ReplayMode.Parallel // Use parallel processing
};

// Configure filter performance
var filters = new AdvancedChangeFilters();
filters.MaxResults = 1000; // Reasonable limit
filters.MaxAge = TimeSpan.FromHours(1); // Recent changes only
```

### **Error Handling**

```csharp
// Subscribe to error events
analytics.OnPerformanceThresholdExceeded += (sender, e) =>
{
    // Log performance issues
    _logger.LogWarning("Performance threshold exceeded: {Metric} = {Value}", e.MetricName, e.Value);
    
    // Send alerts
    _alertService.SendAlert($"Performance issue detected: {e.MetricName}");
};

routingEngine.OnRoutingFailed += (sender, e) =>
{
    // Log routing failures
    _logger.LogError("Routing failed for change {ChangeId}: {Error}", e.Change.ChangeId, e.Error);
    
    // Implement retry logic
    _retryService.ScheduleRetry(e.Change, e.Destination);
};
```

### **Resource Management**

```csharp
// Proper disposal pattern
using var analytics = new ChangeAnalytics();
using var schemaDetection = new SchemaChangeDetection();
using var correlationEngine = new ChangeCorrelationEngine();
using var filters = new AdvancedChangeFilters();
using var routingEngine = new ChangeRoutingEngine();
using var replayEngine = new ChangeReplayEngine();

try
{
    // Use engines
    await ConfigureAndStartEngines();
}
finally
{
    // Dispose is automatic with using statements
}
```

---

## 📊 **Testing & Validation**

### **Unit Testing Examples**

```csharp
[Fact]
public async Task ChangeAnalytics_ShouldRecordChanges()
{
    // Arrange
    var analytics = new ChangeAnalytics();
    
    // Act
    analytics.RecordChange("Users", ChangeOperation.Insert, TimeSpan.FromMilliseconds(50));
    
    // Assert
    var metrics = analytics.GetTableMetrics("Users");
    Assert.Equal(1, metrics.TotalChanges);
    Assert.Equal(ChangeOperation.Insert, metrics.OperationDistribution[ChangeOperation.Insert]);
}

[Fact]
public async Task AdvancedChangeFilters_ShouldFilterChanges()
{
    // Arrange
    var filters = new AdvancedChangeFilters();
    filters.AddColumnFilter("Status", FilterOperator.Equals, "Active");
    
    var changes = new List<ChangeRecord>
    {
        new ChangeRecord { Metadata = new Dictionary<string, object> { { "Status", "Active" } } },
        new ChangeRecord { Metadata = new Dictionary<string, object> { { "Status", "Inactive" } } }
    };
    
    // Act
    var filtered = filters.ApplyFilters(changes);
    
    // Assert
    Assert.Single(filtered);
}
```

---

## 🚀 **Getting Started Checklist**

### **1. Install Package**
```bash
dotnet add package SQLDBEntityNotifier
```

### **2. Create Engines**
```csharp
var analytics = new ChangeAnalytics();
var schemaDetection = new SchemaChangeDetection();
var correlationEngine = new ChangeCorrelationEngine();
var contextManager = new ChangeContextManager();
var filters = new AdvancedChangeFilters();
var routingEngine = new ChangeRoutingEngine();
var replayEngine = new ChangeReplayEngine();
```

### **3. Configure Engines**
```csharp
// Configure filters
filters.AddColumnFilter("Status", FilterOperator.Equals, "Active");

// Configure routing
routingEngine.AddDestination(new WebhookDestination("API", "https://api.company.com/webhook"));

// Configure analytics
analytics.EnableRealTimeMetrics = true;
```

### **4. Subscribe to Events**
```csharp
analytics.OnPerformanceThresholdExceeded += HandlePerformanceAlert;
schemaDetection.OnSchemaChangeDetected += HandleSchemaChange;
routingEngine.OnChangeRouted += HandleChangeRouted;
```

### **5. Start Monitoring**
```csharp
await schemaDetection.StartMonitoringAsync();
```

---

## 📞 **Support & Resources**

- **GitHub Repository**: [https://github.com/jatinrdave/SQLEFTableNotification](https://github.com/jatinrdave/SQLEFTableNotification)
- **NuGet Package**: [https://www.nuget.org/packages/SQLDBEntityNotifier](https://www.nuget.org/packages/SQLDBEntityNotifier)
- **Documentation**: See `README_AdvancedCDCFeatures.md` for comprehensive feature documentation
- **Examples**: See `Examples/` directory for complete usage examples

---

**Happy Advanced Change Detection! 🚀✨**

*Built with ❤️ for the .NET community*
