# Advanced CDC Features - Phase 2 Implementation

This document describes the advanced Change Data Capture (CDC) features implemented in Phase 2 of the SQLDBEntityNotifier project. These features provide enterprise-grade capabilities for monitoring, analyzing, and managing database changes.

## Table of Contents

1. [Overview](#overview)
2. [Change Analytics & Metrics Engine](#change-analytics--metrics-engine)
3. [Schema Change Detection](#schema-change-detection)
4. [Change Correlation Engine](#change-correlation-engine)
5. [Enhanced Change Context Management](#enhanced-change-context-management)
6. [Integration with UnifiedDBNotificationService](#integration-with-unifieddbnotificationservice)
7. [Usage Examples](#usage-examples)
8. [Configuration Options](#configuration-options)
9. [Performance Considerations](#performance-considerations)
10. [Best Practices](#best-practices)

## Overview

Phase 2 introduces four major advanced CDC features that extend the basic change notification capabilities:

- **Change Analytics & Metrics Engine**: Monitors performance and detects patterns in change processing
- **Schema Change Detection**: Automatically detects and analyzes database schema changes
- **Change Correlation Engine**: Identifies relationships and dependencies between changes across tables
- **Enhanced Change Context Management**: Provides rich context and metadata for change events

These features are automatically integrated into the `UnifiedDBNotificationService<T>` and provide real-time insights into database change patterns, performance metrics, and impact analysis.

## Change Analytics & Metrics Engine

The `ChangeAnalytics` class provides comprehensive monitoring and analysis of CDC performance and change patterns.

### Key Features

- **Performance Metrics**: Tracks processing times, throughput, and resource utilization
- **Change Patterns**: Detects recurring patterns in change types and timing
- **Threshold Monitoring**: Configurable alerts for performance violations
- **Real-time Aggregation**: Automatic metrics aggregation and reporting

### Metrics Collected

- **Change Metrics**: Total changes, inserts, updates, deletes, changes per minute
- **Performance Metrics**: Processing times, peak performance, average performance
- **Pattern Metrics**: Change type dominance, time interval patterns, batch size patterns

### Events Raised

- `PerformanceThresholdExceeded`: When performance thresholds are exceeded
- `ChangePatternDetected`: When significant change patterns are identified
- `MetricsAggregated`: When metrics are aggregated (every minute by default)

### Example Usage

```csharp
// Set performance thresholds
var thresholds = new PerformanceThresholds
{
    MaxAverageProcessingTime = TimeSpan.FromMilliseconds(50),
    MaxPeakProcessingTime = TimeSpan.FromMilliseconds(200),
    MaxChangesPerMinute = 500
};

service.ChangeAnalytics.SetPerformanceThresholds("Users", thresholds);

// Get current metrics
var tableMetrics = service.ChangeAnalytics.GetTableMetrics("Users");
var perfMetrics = service.ChangeAnalytics.GetPerformanceMetrics("Users");
var aggregatedMetrics = service.ChangeAnalytics.GetAggregatedMetrics();
```

## Schema Change Detection

The `SchemaChangeDetection` class automatically monitors database schema changes and provides impact analysis.

### Key Features

- **Automatic Snapshotting**: Takes periodic snapshots of table schemas
- **Change Detection**: Identifies column, index, and constraint changes
- **Impact Analysis**: Assesses the impact and risk of schema changes
- **Historical Tracking**: Maintains a complete history of schema changes

### Schema Elements Monitored

- **Columns**: Added, removed, or modified columns
- **Indexes**: New or dropped indexes
- **Constraints**: Foreign keys, check constraints, unique constraints
- **Table Properties**: Schema changes, partitioning changes

### Change Types Detected

- `ColumnAdded`: New columns added to tables
- `ColumnRemoved`: Existing columns removed
- `ColumnModified`: Column data type or properties changed
- `IndexAdded`: New indexes created
- `IndexRemoved`: Indexes dropped
- `ConstraintAdded`: New constraints added
- `ConstraintRemoved`: Constraints removed
- `TableModified`: Table-level properties changed

### Events Raised

- `SchemaChangeDetected`: When schema changes are detected
- `SchemaChangeImpactAnalyzed`: When impact analysis is completed
- `SchemaChangeRiskAssessed`: When risk assessment is completed

### Example Usage

```csharp
// Take initial schema snapshot
var snapshot = await service.SchemaChangeDetection.TakeTableSnapshotAsync("Users", cdcProvider);

// Detect schema changes
var changes = await service.SchemaChangeDetection.DetectSchemaChangesAsync("Users", cdcProvider);

// Get change history
var history = service.SchemaChangeDetection.GetChangeHistory("Users");
var recentChanges = history.GetChangesInTimeRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
```

## Change Correlation Engine

The `ChangeCorrelationEngine` identifies relationships and dependencies between changes across multiple tables.

### Key Features

- **Foreign Key Tracking**: Monitors relationships between related tables
- **Change Correlation**: Identifies causally related changes
- **Impact Analysis**: Analyzes the impact of changes on dependent tables
- **Dependency Graphs**: Builds and maintains table dependency graphs

### Correlation Types

- `General`: General correlation between changes
- `CascadingDelete`: Delete operations that trigger related deletes
- `CascadingUpdate`: Update operations that trigger related updates
- `BulkInsert`: Related insert operations
- `ReferentialIntegrity`: Changes that affect referential integrity

### Impact Levels

- `Low`: Minimal impact on dependent systems
- `Medium`: Moderate impact with some dependent tables affected
- `High`: Significant impact with many dependent tables affected
- `Critical`: Critical impact with potential data integrity issues

### Events Raised

- `ChangeCorrelationDetected`: When correlations between changes are detected
- `DependencyCycleDetected`: When circular dependencies are identified
- `ChangeImpactAnalyzed`: When impact analysis is completed

### Example Usage

```csharp
// Register foreign key relationships
service.ChangeCorrelationEngine.RegisterForeignKeyRelationship(
    "Users", "UserProfiles", "Id", "UserId", "FK_UserProfiles_Users");

// Record changes for correlation analysis
service.ChangeCorrelationEngine.RecordChange("Users", changeRecord, DateTime.UtcNow);

// Get correlated changes
var correlatedChanges = service.ChangeCorrelationEngine.GetCorrelatedChanges("Users", TimeSpan.FromMinutes(5));

// Analyze change impact
var impactAnalysis = await service.ChangeCorrelationEngine.AnalyzeChangeImpactAsync("Users", changeRecord);
```

## Enhanced Change Context Management

The `ChangeContextManager` provides rich context and metadata for change events, enabling better traceability and debugging.

### Key Features

- **Context Creation**: Creates rich context for change events
- **Metadata Management**: Attaches business and technical metadata to changes
- **Context Propagation**: Propagates context across system boundaries
- **Validation**: Validates context integrity and completeness

### Context Properties

- **ChangeId**: Unique identifier for the change context
- **TableName**: Name of the affected table
- **Timestamp**: When the context was created
- **Metadata**: Custom key-value pairs for additional context
- **Validation**: Context validation status and results

### Events Raised

- `ContextCreated`: When a new change context is created
- `ContextModified`: When context is modified
- `ContextValidated`: When context validation is completed
- `ContextProcessed`: When context processing is completed

### Example Usage

```csharp
// Create change context
var context = service.ChangeContextManager.CreateContext("Users", changes);
context.Metadata["BusinessProcess"] = "UserRegistration";
context.Metadata["UserId"] = "12345";

// Validate context
var validationResult = await service.ChangeContextManager.ValidateContextAsync(context);

// Propagate context
service.ChangeContextManager.PropagateContext(context);
```

## Integration with UnifiedDBNotificationService

All advanced features are automatically integrated into the `UnifiedDBNotificationService<T>` and can be accessed through public properties.

### Service Properties

```csharp
public class UnifiedDBNotificationService<T> where T : class, new()
{
    // Advanced CDC Features
    public ChangeAnalytics ChangeAnalytics { get; }
    public SchemaChangeDetection SchemaChangeDetection { get; }
    public ChangeCorrelationEngine ChangeCorrelationEngine { get; }
    public ChangeContextManager ChangeContextManager { get; }
}
```

### New Events

```csharp
// Advanced CDC Feature Events
public event EventHandler<PerformanceThresholdExceededEventArgs>? OnPerformanceThresholdExceeded;
public event EventHandler<ChangePatternDetectedEventArgs>? OnChangePatternDetected;
public event EventHandler<MetricsAggregatedEventArgs>? OnMetricsAggregated;
public event EventHandler<SchemaChangeDetectedEventArgs>? OnSchemaChangeDetected;
public event EventHandler<ChangeCorrelationDetectedEventArgs>? OnChangeCorrelationDetected;
public event EventHandler<ChangeImpactAnalyzedEventArgs>? OnChangeImpactAnalyzed;
```

### Automatic Integration

The service automatically:

1. **Records Changes**: All changes are automatically recorded for analytics and correlation
2. **Monitors Performance**: Processing times are tracked for performance analysis
3. **Detects Patterns**: Change patterns are automatically analyzed
4. **Checks Schema**: Schema changes are periodically detected
5. **Creates Context**: Rich change context is automatically created
6. **Raises Events**: All advanced feature events are automatically raised

## Usage Examples

### Basic Setup

```csharp
// Create service with advanced features
var service = new UnifiedDBNotificationService<User>(
    databaseConfig,
    "Users",
    TimeSpan.FromSeconds(30),
    TimeSpan.FromMinutes(2),
    columnFilterOptions);

// Wire up advanced feature events
service.OnPerformanceThresholdExceeded += (sender, e) => 
    Console.WriteLine($"Performance threshold exceeded: {e.TableName}");
service.OnSchemaChangeDetected += (sender, e) => 
    Console.WriteLine($"Schema change detected: {e.TableName}");
service.OnChangeCorrelationDetected += (sender, e) => 
    Console.WriteLine($"Change correlation detected: {e.TableName}");

// Start monitoring
await service.StartMonitoringAsync();
```

### Advanced Configuration

```csharp
// Set performance thresholds
var thresholds = new PerformanceThresholds
{
    MaxAverageProcessingTime = TimeSpan.FromMilliseconds(50),
    MaxPeakProcessingTime = TimeSpan.FromMilliseconds(200),
    MaxChangesPerMinute = 500
};
service.ChangeAnalytics.SetPerformanceThresholds("Users", thresholds);

// Register table relationships
service.ChangeCorrelationEngine.RegisterForeignKeyRelationship(
    "Users", "UserProfiles", "Id", "UserId", "FK_UserProfiles_Users");

// Take initial schema snapshot
var snapshot = await service.SchemaChangeDetection.TakeTableSnapshotAsync("Users", service.CDCProvider);
```

### Monitoring and Analysis

```csharp
// Get current metrics
var metrics = service.ChangeAnalytics.GetTableMetrics("Users");
Console.WriteLine($"Total changes: {metrics.TotalChanges}");

// Get performance metrics
var perfMetrics = service.ChangeAnalytics.GetPerformanceMetrics("Users");
Console.WriteLine($"Average processing time: {perfMetrics.AverageProcessingTime}");

// Get dependency graph
var deps = service.ChangeCorrelationEngine.GetDependencyGraph("Users");
Console.WriteLine($"Dependent tables: {string.Join(", ", deps.DependentTables)}");

// Get schema change history
var history = service.SchemaChangeDetection.GetChangeHistory("Users");
var recentChanges = history.GetChangesInTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
```

## Configuration Options

### Performance Thresholds

```csharp
public class PerformanceThresholds
{
    public TimeSpan MaxAverageProcessingTime { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan MaxPeakProcessingTime { get; set; } = TimeSpan.FromMilliseconds(500);
    public int MaxChangesPerMinute { get; set; } = 1000;
}
```

### Column Filter Options

```csharp
public class ColumnChangeFilterOptions
{
    public List<string> MonitoredColumns { get; set; } = new();
    public bool IncludeColumnLevelChanges { get; set; } = false;
    public bool CaseSensitive { get; set; } = false;
}
```

### Change Context Options

```csharp
public class EnhancedChangeContext
{
    public string ChangeId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool ValidateContext { get; set; } = true;
}
```

## Performance Considerations

### Memory Usage

- **Change Analytics**: Keeps last 1000 changes per table (configurable)
- **Schema Detection**: Keeps last 30 days of schema changes
- **Correlation Engine**: Keeps last 24 hours of correlation data
- **Context Manager**: Contexts are automatically cleaned up after processing

### Processing Overhead

- **Analytics Recording**: Minimal overhead (~1-2ms per change)
- **Schema Detection**: Only runs every 10 polling cycles
- **Correlation Analysis**: Runs asynchronously in background
- **Context Creation**: Minimal overhead (~0.5ms per context)

### Scalability

- **Concurrent Processing**: All features are thread-safe
- **Background Processing**: Heavy operations run asynchronously
- **Configurable Intervals**: Polling and health check intervals are configurable
- **Resource Management**: Automatic cleanup of old data

## Best Practices

### 1. Event Handling

- **Handle All Events**: Wire up all relevant events for comprehensive monitoring
- **Async Event Handlers**: Use async event handlers for non-blocking processing
- **Error Handling**: Always handle exceptions in event handlers
- **Logging**: Log important events for debugging and monitoring

### 2. Performance Monitoring

- **Set Realistic Thresholds**: Base thresholds on actual system performance
- **Monitor Trends**: Watch for performance degradation over time
- **Alert on Violations**: Set up alerts for threshold violations
- **Regular Review**: Review and adjust thresholds based on system changes

### 3. Schema Change Management

- **Baseline Snapshots**: Take initial snapshots for all monitored tables
- **Review Changes**: Regularly review detected schema changes
- **Impact Assessment**: Use impact analysis to understand change effects
- **Documentation**: Document schema changes and their business impact

### 4. Correlation Analysis

- **Register Relationships**: Register all foreign key relationships
- **Monitor Dependencies**: Watch for circular dependencies
- **Impact Analysis**: Use impact analysis for change planning
- **Testing**: Test correlation logic with known change patterns

### 5. Context Management

- **Rich Metadata**: Include business context in change metadata
- **Validation**: Use context validation for data integrity
- **Propagation**: Propagate context to downstream systems
- **Cleanup**: Implement context cleanup for long-running processes

## Conclusion

The advanced CDC features in Phase 2 provide enterprise-grade capabilities for monitoring, analyzing, and managing database changes. These features are automatically integrated into the `UnifiedDBNotificationService<T>` and provide real-time insights into database change patterns, performance metrics, and impact analysis.

By following the best practices outlined in this document, you can effectively use these features to:

- Monitor system performance and detect issues early
- Track schema changes and understand their impact
- Identify relationships between changes across tables
- Maintain rich context for change events
- Build robust, scalable change notification systems

For more examples and detailed implementation guidance, see the `AdvancedCDCFeaturesExample.cs` file in the Examples folder.
