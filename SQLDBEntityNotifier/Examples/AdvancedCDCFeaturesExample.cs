using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLDBEntityNotifier;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using System.Linq; // Added for .Select()

namespace SQLDBEntityNotifier.Examples
{
    /// <summary>
    /// Example demonstrating the advanced CDC features implemented in Phase 2
    /// </summary>
    public class AdvancedCDCFeaturesExample
    {
        private UnifiedDBNotificationService<User> _notificationService;
        private bool _isRunning = false;

        /// <summary>
        /// Demonstrates how to set up and use advanced CDC features
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== Advanced CDC Features Example ===");
            Console.WriteLine("This example demonstrates the new advanced features:");
            Console.WriteLine("1. Change Analytics & Metrics");
            Console.WriteLine("2. Schema Change Detection");
            Console.WriteLine("3. Change Correlation Engine");
            Console.WriteLine("4. Enhanced Change Context Management");
            Console.WriteLine();

            try
            {
                // Setup the notification service with advanced features
                await SetupNotificationServiceAsync();

                // Wire up all the advanced feature events
                WireUpAdvancedFeatureEvents();

                // Start monitoring
                await StartMonitoringAsync();

                // Demonstrate advanced features
                await DemonstrateAdvancedFeaturesAsync();

                // Keep running for a while to see events
                Console.WriteLine("Monitoring for changes... Press any key to stop.");
                Console.ReadKey();

                // Stop monitoring
                await StopMonitoringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in example: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up the notification service with advanced CDC features
        /// </summary>
        private async Task SetupNotificationServiceAsync()
        {
            Console.WriteLine("Setting up notification service...");

            // Create database configuration
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                "TestDB");

            // Create column filter options for monitoring specific columns
            var columnFilters = new ColumnChangeFilterOptions
            {
                MonitoredColumns = new List<string> { "Name", "Email", "Status" },
                IncludeColumnLevelChanges = true,
                CaseSensitiveColumnNames = false
            };

            // Create the notification service with advanced features
            _notificationService = new UnifiedDBNotificationService<User>(
                config,
                "Users",
                TimeSpan.FromSeconds(30),  // Poll every 30 seconds
                TimeSpan.FromMinutes(2),   // Health check every 2 minutes
                columnFilters);

            Console.WriteLine("✓ Notification service created with advanced features");
        }

        /// <summary>
        /// Wires up all the advanced feature events
        /// </summary>
        private void WireUpAdvancedFeatureEvents()
        {
            Console.WriteLine("Wiring up advanced feature events...");

            // Basic change events
            _notificationService.OnChanged += (sender, e) =>
            {
                var entities = e.Entities?.ToList() ?? new List<User>();
                Console.WriteLine($"📊 Change detected: {entities.Count} records changed");
                foreach (var record in entities)
                {
                    Console.WriteLine($"   - {record.GetType().Name}: {record}");
                }
            };

            _notificationService.OnError += (sender, e) =>
            {
                Console.WriteLine($"❌ Error: {e.Message}");
            };

            _notificationService.OnHealthCheck += (sender, e) =>
            {
                Console.WriteLine($"💓 Health check: {e.Status}");
            };

            // Advanced CDC Feature Events
            _notificationService.OnPerformanceThresholdExceeded += (sender, e) =>
            {
                Console.WriteLine($"🚨 Performance threshold exceeded for table {e.TableName}:");
                foreach (var violation in e.Violations)
                {
                    Console.WriteLine($"   - {violation.ThresholdType}: {violation.CurrentValue} > {violation.ThresholdValue} ({violation.Severity})");
                }
            };

            _notificationService.OnChangePatternDetected += (sender, e) =>
            {
                Console.WriteLine($"🔍 Change pattern detected for table {e.TableName}:");
                foreach (var pattern in e.Patterns)
                {
                    Console.WriteLine($"   - {pattern.PatternType}: {pattern.Description} (Confidence: {pattern.Confidence:P0}, Severity: {pattern.Severity})");
                }
            };

            _notificationService.OnMetricsAggregated += (sender, e) =>
            {
                Console.WriteLine($"📈 Metrics aggregated:");
                Console.WriteLine($"   - Total tables: {e.Metrics.TotalTables}");
                Console.WriteLine($"   - Total changes: {e.Metrics.TotalChanges}");
                Console.WriteLine($"   - Average processing time: {e.Metrics.AverageProcessingTime.TotalMilliseconds:F2}ms");
                Console.WriteLine($"   - Tables with high activity: {e.Metrics.TablesWithHighActivity}");
            };

            _notificationService.OnSchemaChangeDetected += (sender, e) =>
            {
                Console.WriteLine($"🏗️ Schema change detected for table {e.TableName}:");
                foreach (var change in e.Changes)
                {
                    Console.WriteLine($"   - {change.ChangeType}: {change.Description} (Impact: {change.Impact}, Risk: {change.Risk})");
                }
            };

            _notificationService.OnChangeCorrelationDetected += (sender, e) =>
            {
                Console.WriteLine($"🔗 Change correlation detected for table {e.TableName}:");
                var correlation = e.CorrelatedChange;
                Console.WriteLine($"   - Type: {correlation.CorrelationType}");
                Console.WriteLine($"   - Confidence: {correlation.Confidence:P0}");
                Console.WriteLine($"   - Primary change: {correlation.PrimaryChange.ChangeType} at {correlation.PrimaryChange.Timestamp}");
                Console.WriteLine($"   - Related change: {correlation.RelatedChange.ChangeType} at {correlation.RelatedChange.Timestamp}");
            };

            _notificationService.OnChangeImpactAnalyzed += (sender, e) =>
            {
                Console.WriteLine($"📊 Change impact analyzed for table {e.TableName}:");
                var impact = e.ImpactAnalysis;
                Console.WriteLine($"   - Impact level: {impact.ImpactLevel}");
                Console.WriteLine($"   - Affected tables: {string.Join(", ", impact.AffectedTables)}");
                Console.WriteLine($"   - Dependency chain: {string.Join(" → ", impact.DependencyChain)}");
            };

            Console.WriteLine("✓ All advanced feature events wired up");
        }

        /// <summary>
        /// Starts monitoring for changes
        /// </summary>
        private async Task StartMonitoringAsync()
        {
            Console.WriteLine("Starting monitoring...");
            await _notificationService.StartMonitoringAsync();
            _isRunning = true;
            Console.WriteLine("✓ Monitoring started");
        }

        /// <summary>
        /// Demonstrates the advanced features
        /// </summary>
        private async Task DemonstrateAdvancedFeaturesAsync()
        {
            Console.WriteLine("\n=== Demonstrating Advanced Features ===");

            // Set performance thresholds
            var thresholds = new PerformanceThresholds
            {
                MaxAverageProcessingTime = TimeSpan.FromMilliseconds(50),
                MaxPeakProcessingTime = TimeSpan.FromMilliseconds(200),
                MaxChangesPerMinute = 500
            };

            _notificationService.ChangeAnalytics.SetPerformanceThresholds("Users", thresholds);
            Console.WriteLine("✓ Performance thresholds set");

            // Register foreign key relationships for correlation analysis
            _notificationService.ChangeCorrelationEngine.RegisterForeignKeyRelationship(
                "Users", "UserProfiles", "Id", "UserId", "FK_UserProfiles_Users");
            _notificationService.ChangeCorrelationEngine.RegisterForeignKeyRelationship(
                "Users", "UserRoles", "Id", "UserId", "FK_UserRoles_Users");
            Console.WriteLine("✓ Foreign key relationships registered");

            // Take initial schema snapshot
            var initialSnapshot = await _notificationService.SchemaChangeDetection.TakeTableSnapshotAsync("Users", _notificationService.CDCProvider);
            Console.WriteLine($"✓ Initial schema snapshot taken with {initialSnapshot.Columns.Count} columns");

            // Create a change context
            var context = _notificationService.ChangeContextManager.CreateContext("Users");
            context.Metadata["ExampleRun"] = "AdvancedCDCFeaturesExample";
            context.Metadata["StartTime"] = DateTime.UtcNow;
            Console.WriteLine($"✓ Change context created with ID: {context.ChangeId}");

            Console.WriteLine("Advanced features are now active and monitoring for:");
            Console.WriteLine("  • Performance thresholds and patterns");
            Console.WriteLine("  • Schema changes and their impact");
            Console.WriteLine("  • Correlated changes across related tables");
            Console.WriteLine("  • Change context propagation");
        }

        /// <summary>
        /// Stops monitoring
        /// </summary>
        private async Task StopMonitoringAsync()
        {
            if (_isRunning)
            {
                Console.WriteLine("Stopping monitoring...");
                _notificationService.StopMonitoring();
                _isRunning = false;
                Console.WriteLine("✓ Monitoring stopped");
            }
        }

        /// <summary>
        /// Gets current analytics and metrics
        /// </summary>
        public void DisplayCurrentMetrics()
        {
            if (_notificationService == null) return;

            Console.WriteLine("\n=== Current Metrics ===");

            // Display change analytics
            var tableMetrics = _notificationService.ChangeAnalytics.GetTableMetrics("Users");
            Console.WriteLine($"Change Metrics for Users table:");
            Console.WriteLine($"  • Total changes: {tableMetrics.TotalChanges}");
            Console.WriteLine($"  • Inserts: {tableMetrics.Inserts}");
            Console.WriteLine($"  • Updates: {tableMetrics.Updates}");
            Console.WriteLine($"  • Deletes: {tableMetrics.Deletes}");
            Console.WriteLine($"  • Changes per minute: {tableMetrics.ChangesPerMinute:F2}");

            var perfMetrics = _notificationService.ChangeAnalytics.GetPerformanceMetrics("Users");
            Console.WriteLine($"Performance Metrics:");
            Console.WriteLine($"  • Average processing time: {perfMetrics.AverageProcessingTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  • Peak processing time: {perfMetrics.PeakProcessingTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  • Total events: {perfMetrics.TotalEvents}");

            // Display aggregated metrics
            var aggregatedMetrics = _notificationService.ChangeAnalytics.GetAggregatedMetrics();
            Console.WriteLine($"Aggregated Metrics:");
            Console.WriteLine($"  • Total tables: {aggregatedMetrics.TotalTables}");
            Console.WriteLine($"  • Total changes: {aggregatedMetrics.TotalChanges}");
            Console.WriteLine($"  • Total processing time: {aggregatedMetrics.TotalProcessingTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  • Tables with high activity: {aggregatedMetrics.TablesWithHighActivity}");

            // Display change patterns
            var changePattern = _notificationService.ChangeAnalytics.GetChangePattern("Users");
            if (changePattern.HasSignificantPatterns(out var patterns))
            {
                Console.WriteLine($"Change Patterns:");
                foreach (var pattern in patterns)
                {
                    Console.WriteLine($"  • {pattern.PatternType}: {pattern.Description}");
                }
            }

            // Display dependency graph
            var dependencyGraph = _notificationService.ChangeCorrelationEngine.GetDependencyGraph("Users");
            Console.WriteLine($"Dependency Graph for Users table:");
            Console.WriteLine($"  • Dependencies: {string.Join(", ", dependencyGraph.GetDependentTables())}");
            Console.WriteLine($"  • Reverse dependencies: {string.Join(", ", dependencyGraph.ReverseDependencies.Select(d => d.SourceTable))}");

            // Display schema change history
            var schemaHistory = _notificationService.SchemaChangeDetection.GetChangeHistory("Users");
            Console.WriteLine($"Schema Change History:");
            Console.WriteLine($"  • Total changes: {schemaHistory.Changes.Count}");
            if (schemaHistory.Changes.Any())
            {
                var recentChanges = schemaHistory.Changes.OrderByDescending(c => c.Timestamp).Take(3);
                foreach (var change in recentChanges)
                {
                    Console.WriteLine($"  • {change.Timestamp:HH:mm:ss} - {change.ChangeType}: {change.Description}");
                }
            }
        }

        /// <summary>
        /// Example User entity
        /// </summary>
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            public override string ToString()
            {
                return $"User(Id={Id}, Name='{Name}', Email='{Email}', Status='{Status}')";
            }
        }
    }
}
