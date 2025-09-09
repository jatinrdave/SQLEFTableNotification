using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLDBEntityNotifier;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using System.Linq; // Added for .Any() and .OrderBy()

namespace SQLDBEntityNotifier.Examples
{
    /// <summary>
    /// Example demonstrating the Phase 3 and Phase 4 advanced CDC features:
    /// - Advanced Filtering & Routing
    /// - Change Replay & Recovery
    /// </summary>
    public class Phase3And4FeaturesExample
    {
        private UnifiedDBNotificationService<User> _notificationService;
        private bool _isRunning = false;

        /// <summary>
        /// Demonstrates how to set up and use Phase 3 and Phase 4 features
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== Phase 3 & 4 Advanced CDC Features Example ===");
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("Phase 3: Advanced Filtering & Routing");
            Console.WriteLine("  • Complex filtering rules");
            Console.WriteLine("  • Change routing to multiple destinations");
            Console.WriteLine("  • Routing metrics and monitoring");
            Console.WriteLine();
            Console.WriteLine("Phase 4: Change Replay & Recovery");
            Console.WriteLine("  • Historical change replay");
            Console.WriteLine("  • Automated recovery procedures");
            Console.WriteLine("  • Recovery recommendations");
            Console.WriteLine();

            try
            {
                // Setup the notification service with all advanced features
                await SetupNotificationServiceAsync();

                // Wire up all the advanced feature events
                WireUpAdvancedFeatureEvents();

                // Start monitoring
                await StartMonitoringAsync();

                // Demonstrate Phase 3: Advanced Filtering & Routing
                await DemonstrateAdvancedFilteringAndRoutingAsync();

                // Demonstrate Phase 4: Change Replay & Recovery
                await DemonstrateChangeReplayAndRecoveryAsync();

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
        /// Sets up the notification service with all advanced features
        /// </summary>
        private async Task SetupNotificationServiceAsync()
        {
            Console.WriteLine("Setting up notification service with all advanced features...");

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

            // Create the notification service with all advanced features
            _notificationService = new UnifiedDBNotificationService<User>(
                config,
                "Users",
                TimeSpan.FromSeconds(30),  // Poll every 30 seconds
                TimeSpan.FromMinutes(2),   // Health check every 2 minutes
                columnFilters);

            Console.WriteLine("✓ Notification service created with all advanced features");
        }

        /// <summary>
        /// Wires up all the advanced feature events including Phase 3 & 4
        /// </summary>
        private void WireUpAdvancedFeatureEvents()
        {
            Console.WriteLine("Wiring up all advanced feature events...");

            // Basic change events
            _notificationService.OnChanged += (sender, e) =>
            {
                var entities = e.Entities?.ToList() ?? new List<User>();
                Console.WriteLine($"📊 Change detected: {entities.Count} records changed");
            };

            _notificationService.OnError += (sender, e) =>
            {
                Console.WriteLine($"❌ Error: {e.Message}");
            };

            // Phase 2: Advanced CDC Feature Events
            _notificationService.OnPerformanceThresholdExceeded += (sender, e) =>
            {
                Console.WriteLine($"🚨 Performance threshold exceeded for table {e.TableName}");
            };

            _notificationService.OnSchemaChangeDetected += (sender, e) =>
            {
                Console.WriteLine($"🏗️ Schema change detected for table {e.TableName}");
            };

            _notificationService.OnChangeCorrelationDetected += (sender, e) =>
            {
                Console.WriteLine($"🔗 Change correlation detected for table {e.TableName}");
            };

            // Phase 3: Advanced Filtering & Routing Events
            _notificationService.OnChangeRouted += (sender, e) =>
            {
                Console.WriteLine($"📤 Change routed to {e.RoutedDestinations.Count} destinations:");
                foreach (var destination in e.RoutedDestinations)
                {
                    Console.WriteLine($"   - {destination}");
                }
            };

            _notificationService.OnRoutingFailed += (sender, e) =>
            {
                Console.WriteLine($"❌ Routing failed for table {e.TableName}:");
                foreach (var error in e.Errors)
                {
                    Console.WriteLine($"   - {error}");
                }
            };

            _notificationService.OnRoutingMetricsUpdated += (sender, e) =>
            {
                var stats = e.Metrics.GetOverallStats();
                Console.WriteLine($"📈 Routing metrics updated: {stats.TotalChangesRouted} changes routed, {stats.SuccessRate:P0} success rate");
            };

            // Phase 4: Change Replay & Recovery Events
            _notificationService.OnReplayStarted += (sender, e) =>
            {
                Console.WriteLine($"▶️ Replay started for table {e.TableName} (Session: {e.SessionId})");
            };

            _notificationService.OnReplayCompleted += (sender, e) =>
            {
                Console.WriteLine($"✅ Replay completed for table {e.TableName}: {e.ProcessedChanges} changes processed in {e.Duration.TotalMilliseconds:F0}ms");
            };

            _notificationService.OnReplayFailed += (sender, e) =>
            {
                Console.WriteLine($"❌ Replay failed for table {e.TableName}: {e.Error}");
            };

            _notificationService.OnRecoveryPerformed += (sender, e) =>
            {
                var result = e.Result;
                Console.WriteLine($"🔧 Recovery performed for table {e.TableName}: {result.RecoveredChanges}/{result.TotalChanges} changes recovered in {result.ProcessingTime.TotalMilliseconds:F0}ms");
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
        /// Demonstrates Phase 3: Advanced Filtering & Routing
        /// </summary>
        private async Task DemonstrateAdvancedFilteringAndRoutingAsync()
        {
            Console.WriteLine("\n=== Phase 3: Advanced Filtering & Routing ===");

            // 1. Set up advanced filters
            Console.WriteLine("Setting up advanced filters...");
            
            var advancedFilters = _notificationService.AdvancedFilters;
            
            // Add column-based filters
            advancedFilters.AddColumnFilter("Status", FilterOperator.Equals, "Active");
            advancedFilters.AddColumnFilter("Email", FilterOperator.Contains, "@company.com");
            
            // Add time-based filters (business hours only)
            var businessHoursFilter = new TimeBasedRoutingRule(
                "BusinessHours",
                TimeSpan.FromHours(9),  // 9 AM
                TimeSpan.FromHours(17), // 5 PM
                new List<string> { "PrimaryQueue" },
                false  // Exclude weekends
            );
            
            // Add operation-based filters
            var criticalOperationsFilter = new OperationBasedRoutingRule(
                "CriticalOperations",
                new List<ChangeOperation> { ChangeOperation.Delete, ChangeOperation.SchemaChange },
                new List<string> { "AlertQueue", "AuditLog" },
                10  // High priority
            );

            Console.WriteLine("✓ Advanced filters configured");

            // 2. Set up routing destinations
            Console.WriteLine("Setting up routing destinations...");
            
            var routingEngine = _notificationService.ChangeRoutingEngine;
            
            // Webhook destination for real-time notifications
            var webhookDestination = new WebhookDestination(
                "WebhookAPI",
                "https://api.company.com/webhooks/db-changes",
                new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer your-api-key",
                    ["X-Source"] = "SQLDBEntityNotifier"
                }
            );
            
            // File system destination for audit logging
            var fileDestination = new FileSystemDestination(
                "AuditLog",
                @"C:\Logs\DatabaseChanges",
                ".json",
                true  // Append to file
            );
            
            // Console destination for debugging
            var consoleDestination = new ConsoleDestination(
                "DebugConsole",
                true,   // Include timestamp
                true    // Include metadata
            );
            
            // Database destination for change tracking
            var dbDestination = new DatabaseDestination(
                "ChangeTrackingDB",
                "Server=localhost;Database=ChangeTracking;Integrated Security=true;",
                "DatabaseChanges",
                "dbo"
            );

            // Add destinations to routing engine
            routingEngine.AddDestination(webhookDestination);
            routingEngine.AddDestination(fileDestination);
            routingEngine.AddDestination(consoleDestination);
            routingEngine.AddDestination(dbDestination);

            Console.WriteLine("✓ Routing destinations configured");

            // 3. Set up routing rules
            Console.WriteLine("Setting up routing rules...");
            
            // Route all changes to audit log
            var auditRule = new TableBasedRoutingRule(
                "AuditAllChanges",
                new List<string> { "Users", "UserProfiles", "UserRoles" },
                new List<string> { "AuditLog" },
                true,   // Exact match
                5       // Medium priority
            );
            
            // Route critical operations to multiple destinations
            var criticalRule = new CompositeRoutingRule(
                "CriticalOperations",
                new List<IRoutingRule>
                {
                    criticalOperationsFilter,
                    businessHoursFilter
                },
                CompositeRoutingRule.CompositeLogic.All,
                new List<string> { "WebhookAPI", "AlertQueue", "AuditLog" },
                15      // High priority
            );
            
            // Route high-frequency changes to message queue
            var frequencyRule = new FrequencyBasedRoutingRule(
                "HighFrequencyChanges",
                100,    // Max 100 changes per minute
                new List<string> { "MessageQueue" },
                8       // Medium priority
            );

            // Add routing rules
            routingEngine.AddRoutingRule(auditRule);
            routingEngine.AddRoutingRule(criticalRule);
            routingEngine.AddRoutingRule(frequencyRule);

            Console.WriteLine("✓ Routing rules configured");

            // 4. Display routing configuration
            Console.WriteLine("\nRouting Configuration Summary:");
            Console.WriteLine($"  • Total destinations: {routingEngine.Destinations.Count}");
            Console.WriteLine($"  • Total routing rules: {routingEngine.RoutingRules.Count}");
            
            foreach (var destination in routingEngine.Destinations)
            {
                Console.WriteLine($"  • Destination: {destination.Name} ({destination.Type}) - Enabled: {destination.IsEnabled}");
            }
            
            foreach (var rule in routingEngine.RoutingRules)
            {
                Console.WriteLine($"  • Rule: {rule.Name} (Priority: {rule.Priority})");
            }
        }

        /// <summary>
        /// Demonstrates Phase 4: Change Replay & Recovery
        /// </summary>
        private async Task DemonstrateChangeReplayAndRecoveryAsync()
        {
            Console.WriteLine("\n=== Phase 4: Change Replay & Recovery ===");

            var replayEngine = _notificationService.ChangeReplayEngine;

            // 1. Display replay statistics
            Console.WriteLine("Current replay statistics:");
            var stats = replayEngine.GetStatistics();
            Console.WriteLine($"  • Total tables: {stats.TotalTables}");
            Console.WriteLine($"  • Active sessions: {stats.ActiveSessions}");
            Console.WriteLine($"  • Total changes: {stats.TotalChanges}");
            Console.WriteLine($"  • Average changes per table: {stats.AverageChangesPerTable:F1}");

            // 2. Get recovery recommendations
            Console.WriteLine("\nRecovery recommendations for Users table:");
            var recommendations = replayEngine.GetRecoveryRecommendations("Users");
            
            if (recommendations.Any())
            {
                foreach (var recommendation in recommendations.OrderBy(r => r.Priority))
                {
                    Console.WriteLine($"  • {recommendation.Priority}: {recommendation.Description}");
                }
            }
            else
            {
                Console.WriteLine("  • No recovery recommendations at this time");
            }

            // 3. Demonstrate change replay
            Console.WriteLine("\nDemonstrating change replay...");
            
            var replayOptions = new ReplayOptions
            {
                MaxChanges = 100,
                BatchSize = 20,
                ProcessingDelay = TimeSpan.FromMilliseconds(50),
                SimulateFailures = false,
                Mode = ReplayMode.Batched,
                IncludeMetadata = true
            };

            try
            {
                var replaySession = await replayEngine.StartReplayAsync("Users", replayOptions);
                Console.WriteLine($"✓ Replay session started: {replaySession.SessionId}");
                
                // Wait a bit for replay to progress
                await Task.Delay(1000);
                
                // Check replay status
                var status = replayEngine.GetReplayStatus("Users");
                Console.WriteLine($"  • Replay status: {status}");
                
                if (replaySession.Status == ReplayStatus.Running)
                {
                    Console.WriteLine($"  • Processed changes: {replaySession.ProcessedChanges}");
                    Console.WriteLine($"  • Failed changes: {replaySession.FailedChanges}");
                }
                
                // Stop replay
                await replayEngine.StopReplayAsync("Users");
                Console.WriteLine("✓ Replay session stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Replay demonstration failed: {ex.Message}");
            }

            // 4. Demonstrate recovery
            Console.WriteLine("\nDemonstrating recovery...");
            
            var recoveryOptions = new RecoveryOptions
            {
                FromTime = DateTime.UtcNow.AddHours(-1),
                ToTime = DateTime.UtcNow,
                IncludeOperations = new List<ChangeOperation> { ChangeOperation.Insert, ChangeOperation.Update },
                MinimumPriority = ChangePriority.Medium,
                ValidateBeforeRecovery = true,
                CreateBackup = false,
                SimulatedProcessingTime = TimeSpan.FromMilliseconds(200)
            };

            try
            {
                var recoveryResult = await replayEngine.PerformRecoveryAsync("Users", recoveryOptions);
                
                if (recoveryResult.Success)
                {
                    Console.WriteLine($"✓ Recovery completed successfully:");
                    Console.WriteLine($"  • Total changes: {recoveryResult.TotalChanges}");
                    Console.WriteLine($"  • Recovered changes: {recoveryResult.RecoveredChanges}");
                    Console.WriteLine($"  • Processing time: {recoveryResult.ProcessingTime.TotalMilliseconds:F0}ms");
                    Console.WriteLine($"  • Session ID: {recoveryResult.RecoverySessionId}");
                }
                else
                {
                    Console.WriteLine($"❌ Recovery failed: {recoveryResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Recovery demonstration failed: {ex.Message}");
            }

            // 5. Display available changes for replay
            Console.WriteLine("\nAvailable changes for replay:");
            var availableChanges = replayEngine.GetAvailableChanges("Users", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow);
            Console.WriteLine($"  • Changes in last 2 hours: {availableChanges.Count}");
            
            if (availableChanges.Any())
            {
                var recentChanges = availableChanges.Take(5);
                foreach (var change in recentChanges)
                {
                    Console.WriteLine($"    - {change.Operation} at {change.ChangeTimestamp:HH:mm:ss} (ID: {change.ChangeId})");
                }
            }
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
        /// Displays current metrics for all features
        /// </summary>
        public void DisplayCurrentMetrics()
        {
            if (_notificationService == null) return;

            Console.WriteLine("\n=== Current Metrics for All Features ===");

            // Phase 2: Change Analytics
            var tableMetrics = _notificationService.ChangeAnalytics.GetTableMetrics("Users");
            Console.WriteLine($"Change Analytics:");
            Console.WriteLine($"  • Total changes: {tableMetrics.TotalChanges}");
            Console.WriteLine($"  • Changes per minute: {tableMetrics.ChangesPerMinute:F2}");

            // Phase 3: Routing Metrics
            var routingStats = _notificationService.ChangeRoutingEngine.GetOverallStats();
            Console.WriteLine($"Routing Metrics:");
            Console.WriteLine($"  • Total changes routed: {routingStats.TotalChangesRouted}");
            Console.WriteLine($"  • Success rate: {routingStats.SuccessRate:P0}");
            Console.WriteLine($"  • Average processing time: {routingStats.AverageProcessingTime.TotalMilliseconds:F2}ms");

            // Phase 4: Replay Metrics
            var replayMetrics = _notificationService.ChangeReplayEngine.Metrics;
            Console.WriteLine($"Replay & Recovery Metrics:");
            Console.WriteLine($"  • Total replays: {replayMetrics.TotalReplays}");
            Console.WriteLine($"  • Total recoveries: {replayMetrics.TotalRecoveries}");
            Console.WriteLine($"  • Average replay time: {replayMetrics.AverageReplayTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  • Average recovery time: {replayMetrics.AverageRecoveryTime.TotalMilliseconds:F2}ms");

            // Overall statistics
            var replayStats = _notificationService.ChangeReplayEngine.GetStatistics();
            Console.WriteLine($"Overall Statistics:");
            Console.WriteLine($"  • Tables with history: {replayStats.TotalTables}");
            Console.WriteLine($"  • Total historical changes: {replayStats.TotalChanges}");
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
