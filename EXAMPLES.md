# SQLDBEntityNotifier - Complete Examples

## 🚀 **Overview**

This document provides comprehensive examples for all advanced CDC features implemented in SQLDBEntityNotifier v2.0+.

---

## 🔍 **Phase 2: Advanced Change Processing**

### **Change Analytics & Metrics**

```csharp
using SQLDBEntityNotifier.Models;

// Create analytics engine
var analytics = new ChangeAnalytics();

// Record changes with processing times
analytics.RecordChange("Users", ChangeOperation.Insert, TimeSpan.FromMilliseconds(50));
analytics.RecordChange("Users", ChangeOperation.Update, TimeSpan.FromMilliseconds(30));
analytics.RecordChange("Orders", ChangeOperation.Insert, TimeSpan.FromMilliseconds(75));

// Get comprehensive metrics
var userMetrics = analytics.GetTableMetrics("Users");
var userPerformance = analytics.GetPerformanceMetrics("Users");
var userPattern = analytics.GetChangePattern("Users");
var overallMetrics = analytics.GetAggregatedMetrics();

// Display metrics
Console.WriteLine($"Users Table Metrics:");
Console.WriteLine($"  Total Changes: {userMetrics.TotalChanges}");
Console.WriteLine($"  Average Processing Time: {userPerformance.AverageProcessingTime}");
Console.WriteLine($"  Peak Processing Time: {userPerformance.PeakProcessingTime}");
Console.WriteLine($"  Changes per Hour: {userPattern.ChangesPerHour}");
```

### **Schema Change Detection**

```csharp
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Interfaces;

// Create schema detection engine
var schemaDetection = new SchemaChangeDetection();

// Take initial snapshot
var snapshot = await schemaDetection.TakeTableSnapshotAsync("Users", cdcProvider);

// Monitor for schema changes
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

// Start monitoring
await schemaDetection.StartMonitoringAsync();
```

### **Change Correlation Engine**

```csharp
using SQLDBEntityNotifier.Models;

// Create correlation engine
var correlationEngine = new ChangeCorrelationEngine();

// Record changes across related tables
correlationEngine.RecordChange("Users", userChange);
correlationEngine.RecordChange("Orders", orderChange);
correlationEngine.RecordChange("OrderItems", orderItemChange);

// Analyze correlations
var correlations = await correlationEngine.AnalyzeTableCorrelationsAsync("Users");
var dependencyGraph = correlationEngine.GetDependencyGraph("Users");

// Get related changes
var relatedChanges = correlationEngine.GetRelatedChanges("Users", "Orders");

Console.WriteLine($"Dependency Graph for Users:");
Console.WriteLine($"  Dependent Tables: {string.Join(", ", dependencyGraph.GetDependentTables())}");
Console.WriteLine($"  Dependencies: {string.Join(", ", dependencyGraph.GetDependencies())}");
```

### **Change Context Management**

```csharp
using SQLDBEntityNotifier.Models;

// Create context manager
var contextManager = new ChangeContextManager();

// Create rich change context
var context = contextManager.CreateContext("Users");
context.SetEnvironment("Production");
context.SetApplication("OrderManagement");
context.SetUser("admin@company.com");

// Propagate context
contextManager.SetContext(context);

// Get current context
var currentContext = contextManager.GetCurrentContext();
Console.WriteLine($"Current Context: {currentContext.ApplicationName} on {currentContext.Environment}");
```

---

## 🔧 **Phase 3: Advanced Routing & Filtering**

### **Advanced Change Filters**

```csharp
using SQLDBEntityNotifier.Models;

// Create advanced filters
var filters = new AdvancedChangeFilters();

// Add sophisticated filter rules
filters.AddColumnFilter("Status", FilterOperator.Equals, "Active")
       .AddColumnFilter("Priority", FilterOperator.GreaterThan, 5)
       .AddTimeFilter(TimeFilterType.After, DateTime.UtcNow.AddHours(-24))
       .AddExclusion(new ColumnFilterRule("AuditField", FilterOperator.Equals, "System"));

// Configure filter behavior
filters.Logic = FilterLogic.All; // All rules must pass
filters.MaxResults = 100;        // Limit results
filters.MaxAge = TimeSpan.FromHours(1); // Only recent changes
filters.CaseSensitive = false;  // Case-insensitive matching

// Apply filters
var filteredChanges = filters.ApplyFilters(allChanges);

Console.WriteLine($"Filtered {filteredChanges.Count()} changes from {allChanges.Count()} total");
Console.WriteLine($"Filter configuration: {filters}");
```

### **Change Routing Engine**

```csharp
using SQLDBEntityNotifier.Models;

// Create routing engine
var routingEngine = new ChangeRoutingEngine();

// Add multiple destinations
routingEngine.AddDestination(new WebhookDestination("API", "https://api.company.com/webhook"))
             .AddDestination(new DatabaseDestination("AuditDB", auditConnectionString))
             .AddDestination(new FileSystemDestination("Logs", "/var/logs/changes"));

// Add intelligent routing rules
routingEngine.AddRoutingRule(new TableBasedRoutingRule("UserChanges", 
    new List<string> { "Users" }, 
    new List<string> { "API", "AuditDB" }));

routingEngine.AddRoutingRule(new OperationBasedRoutingRule("CriticalUpdates", 
    new List<ChangeOperation> { ChangeOperation.Update }, 
    new List<string> { "API", "Logs" }));

// Route changes
var result = await routingEngine.RouteChangeAsync(change, "Users");

Console.WriteLine($"Routing result: {result.Success}");
Console.WriteLine($"Routed to: {string.Join(", ", result.RoutedDestinations)}");
if (result.Errors.Any())
{
    Console.WriteLine($"Errors: {string.Join(", ", result.Errors)}");
}
```

---

## 🔄 **Phase 4: Change Replay & Recovery**

### **Change Replay Engine**

```csharp
using SQLDBEntityNotifier.Models;

// Create replay engine
var replayEngine = new ChangeReplayEngine();

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
var session = await replayEngine.StartReplayAsync("Users", replayOptions);

// Monitor replay progress
session.OnProgress += (sender, e) =>
{
    Console.WriteLine($"Replay progress: {e.ProcessedChanges}/{e.TotalChanges} ({e.PercentageComplete:F1}%)");
};

// Wait for completion
await session.WaitForCompletionAsync();

Console.WriteLine($"Replay completed: {session.ProcessedChanges} changes processed in {session.TotalTime}");
```

### **Recovery Mechanisms**

```csharp
using SQLDBEntityNotifier.Models;

// Create recovery session
var recoverySession = await replayEngine.StartRecoveryAsync("Users", new RecoveryOptions
{
    RecoveryPoint = DateTime.UtcNow.AddHours(-1),
    ValidateData = true,
    RollbackOnFailure = false,
    MaxRetries = 3,
    RetryDelay = TimeSpan.FromSeconds(5)
});

// Monitor recovery progress
recoverySession.OnRecoveryProgress += (sender, e) =>
{
    Console.WriteLine($"Recovery progress: {e.PercentageComplete:F1}%");
};

// Wait for recovery completion
await recoverySession.WaitForCompletionAsync();

Console.WriteLine($"Recovery completed: {recoverySession.Status}");
```

---

## 🔌 **Integration Examples**

### **Dependency Injection Setup**

```csharp
// Program.cs or Startup.cs
services.AddSingleton<IChangeAnalytics, ChangeAnalytics>();
services.AddSingleton<ISchemaChangeDetection, SchemaChangeDetection>();
services.AddSingleton<IChangeCorrelationEngine, ChangeCorrelationEngine>();
services.AddSingleton<IAdvancedChangeFilters, AdvancedChangeFilters>();
services.AddSingleton<IChangeRoutingEngine, ChangeRoutingEngine>();
services.AddSingleton<IChangeReplayEngine, ChangeReplayEngine>();

// Configure options
services.Configure<ChangeAnalyticsOptions>(configuration.GetSection("ChangeAnalytics"));
services.Configure<SchemaDetectionOptions>(configuration.GetSection("SchemaDetection"));
```

### **Event-Driven Architecture**

```csharp
// Subscribe to all engine events
analytics.OnPerformanceThresholdExceeded += HandlePerformanceAlert;
schemaDetection.OnSchemaChangeDetected += HandleSchemaChange;
correlationEngine.OnChangeImpactAnalyzed += HandleChangeImpact;
routingEngine.OnChangeRouted += HandleChangeRouted;
routingEngine.OnRoutingFailed += HandleRoutingFailure;
replayEngine.OnReplayCompleted += HandleReplayCompletion;

// Event handlers
private void HandlePerformanceAlert(object sender, PerformanceThresholdExceededEventArgs e)
{
    _logger.LogWarning("Performance threshold exceeded: {Metric} = {Value}", e.MetricName, e.Value);
    _alertService.SendAlert($"Performance issue: {e.MetricName}");
}

private void HandleSchemaChange(object sender, SchemaChangeDetectedEventArgs e)
{
    _logger.LogInformation("Schema change: {Type} on {Table}", e.ChangeType, e.TableName);
    _notificationService.NotifySchemaChange(e);
}

private void HandleChangeRouted(object sender, ChangeRoutedEventArgs e)
{
    _logger.LogInformation("Change routed: {Table} to {Destinations}", 
        e.TableName, string.Join(", ", e.RoutedDestinations));
}
```

### **Custom Extensions**

```csharp
// Custom filter rule
public class BusinessRuleFilter : IFilterRule
{
    private readonly string _businessRule;
    
    public BusinessRuleFilter(string businessRule)
    {
        _businessRule = businessRule;
    }
    
    public bool Matches(object change)
    {
        if (change is not ChangeRecord record) return false;
        
        // Apply business logic
        return _businessRule switch
        {
            "HighValueOrders" => IsHighValueOrder(record),
            "CriticalUsers" => IsCriticalUser(record),
            _ => false
        };
    }
    
    public IFilterRule Clone() => new BusinessRuleFilter(_businessRule);
    public override string ToString() => $"BusinessRule({_businessRule})";
}

// Custom destination
public class SlackDestination : IDestination
{
    private readonly string _webhookUrl;
    private readonly string _channel;
    
    public SlackDestination(string name, string webhookUrl, string channel)
    {
        Name = name;
        _webhookUrl = webhookUrl;
        _channel = channel;
    }
    
    public string Name { get; }
    public DestinationType Type => DestinationType.Custom;
    public bool IsEnabled => true;
    
    public async Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
    {
        try
        {
            var message = FormatSlackMessage(change, tableName);
            await SendSlackMessage(_webhookUrl, _channel, message);
            
            return new DeliveryResult
            {
                Success = true,
                DeliveryTime = TimeSpan.FromMilliseconds(100)
            };
        }
        catch (Exception ex)
        {
            return new DeliveryResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                DeliveryTime = TimeSpan.Zero
            };
        }
    }
    
    public void Dispose() { }
}
```

---

## 📊 **Performance Monitoring Examples**

### **Real-time Metrics Dashboard**

```csharp
// Create metrics collection
var metrics = new Dictionary<string, object>();

// Collect metrics from all engines
metrics["Analytics"] = analytics.GetAggregatedMetrics();
metrics["Routing"] = routingEngine.GetOverallStats();
metrics["Schema"] = schemaDetection.GetChangeHistory("Users");

// Display dashboard
Console.WriteLine("=== CDC Performance Dashboard ===");
Console.WriteLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine();

foreach (var (engine, metric) in metrics)
{
    Console.WriteLine($"{engine} Engine:");
    DisplayMetrics(metric);
    Console.WriteLine();
}

// Performance alerts
analytics.OnPerformanceThresholdExceeded += (sender, e) =>
{
    Console.WriteLine($"🚨 PERFORMANCE ALERT: {e.MetricName} = {e.Value}");
    Console.WriteLine($"   Threshold: {e.Threshold}");
    Console.WriteLine($"   Table: {e.TableName}");
};
```

### **Health Monitoring**

```csharp
// Health check for all engines
public async Task<HealthReport> CheckSystemHealthAsync()
{
    var report = new HealthReport();
    
    try
    {
        // Check analytics engine
        var analyticsHealth = analytics.GetAggregatedMetrics();
        report.AnalyticsStatus = analyticsHealth.TotalChanges > 0 ? HealthStatus.Healthy : HealthStatus.Warning;
        
        // Check schema detection
        var schemaHealth = schemaDetection.GetChangeHistory("Users");
        report.SchemaStatus = schemaHealth.Any() ? HealthStatus.Healthy : HealthStatus.Warning;
        
        // Check routing engine
        var routingHealth = routingEngine.GetOverallStats();
        report.RoutingStatus = routingHealth.TotalChanges > 0 ? HealthStatus.Healthy : HealthStatus.Warning;
        
        report.OverallStatus = DetermineOverallHealth(report);
    }
    catch (Exception ex)
    {
        report.OverallStatus = HealthStatus.Unhealthy;
        report.ErrorMessage = ex.Message;
    }
    
    return report;
}
```

---

## 🧪 **Testing Examples**

### **Unit Tests**

```csharp
[Fact]
public async Task ChangeAnalytics_ShouldRecordAndRetrieveMetrics()
{
    // Arrange
    var analytics = new ChangeAnalytics();
    
    // Act
    analytics.RecordChange("Users", ChangeOperation.Insert, TimeSpan.FromMilliseconds(50));
    analytics.RecordChange("Users", ChangeOperation.Update, TimeSpan.FromMilliseconds(30));
    
    // Assert
    var metrics = analytics.GetTableMetrics("Users");
    Assert.Equal(2, metrics.TotalChanges);
    Assert.Equal(1, metrics.OperationDistribution[ChangeOperation.Insert]);
    Assert.Equal(1, metrics.OperationDistribution[ChangeOperation.Update]);
}

[Fact]
public async Task AdvancedChangeFilters_ShouldApplyComplexFilters()
{
    // Arrange
    var filters = new AdvancedChangeFilters();
    filters.AddColumnFilter("Status", FilterOperator.Equals, "Active")
           .AddTimeFilter(TimeFilterType.After, DateTime.UtcNow.AddHours(-1))
           .SetMaxResults(10);
    
    var changes = CreateTestChanges();
    
    // Act
    var filtered = filters.ApplyFilters(changes);
    
    // Assert
    Assert.True(filtered.Count() <= 10);
    Assert.All(filtered, change => 
        change.Metadata?.ContainsKey("Status") == true && 
        change.Metadata["Status"].ToString() == "Active");
}

[Fact]
public async Task ChangeRoutingEngine_ShouldRouteToMultipleDestinations()
{
    // Arrange
    var routingEngine = new ChangeRoutingEngine();
    var mockDestination = new MockSuccessfulDestination("Test");
    
    routingEngine.AddDestination(mockDestination);
    routingEngine.AddRoutingRule(new TableBasedRoutingRule("Test", 
        new List<string> { "Users" }, 
        new List<string> { "Test" }));
    
    var change = new ChangeRecord { TableName = "Users" };
    
    // Act
    var result = await routingEngine.RouteChangeAsync(change, "Users");
    
    // Assert
    Assert.True(result.Success);
    Assert.Single(result.RoutedDestinations);
    Assert.Contains("Test", result.RoutedDestinations);
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

## 📚 **Additional Resources**

- **API Reference**: See `API_REFERENCE.md` for complete API documentation
- **Advanced Features**: See `README_AdvancedCDCFeatures.md` for feature documentation
- **Source Code**: [GitHub Repository](https://github.com/jatinrdave/SQLEFTableNotification)
- **NuGet Package**: [SQLDBEntityNotifier](https://www.nuget.org/packages/SQLDBEntityNotifier)

---

**Happy Advanced Change Detection! 🚀✨**

*Built with ❤️ for the .NET community*
