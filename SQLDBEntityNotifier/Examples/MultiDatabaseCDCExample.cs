using System;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using System.Collections.Generic; // Added for Dictionary
using System.Linq; // Added for Select

namespace SQLDBEntityNotifier.Examples
{
    /// <summary>
    /// Examples demonstrating how to use the unified CDC functionality across different database types
    /// </summary>
    public class MultiDatabaseCDCExample
    {
        /// <summary>
        /// Example 1: SQL Server CDC with minimal configuration
        /// </summary>
        public static async Task Example1_SqlServerCDC()
        {
            Console.WriteLine("=== SQL Server CDC Example ===");
            
            // Minimal configuration - just connection string
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=YourDatabase;Integrated Security=true;"
            );
            
            // Create notification service
            using var notificationService = new UnifiedDBNotificationService<YourEntity>(
                config, 
                "YourTable", 
                TimeSpan.FromSeconds(15)
            );
            
            // Subscribe to events
            notificationService.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Change detected in {e.DatabaseType} table {e.TableName}");
                Console.WriteLine($"Operation: {e.Operation}");
                Console.WriteLine($"Change ID: {e.ChangeIdentifier}");
                Console.WriteLine($"Timestamp: {e.DatabaseChangeTimestamp}");
                Console.WriteLine($"Affected records: {e.Metadata?["ChangeCount"]}");
            };
            
            notificationService.OnError += (sender, e) =>
            {
                Console.WriteLine($"Error: {e.Message}");
            };
            
            notificationService.OnHealthCheck += (sender, healthInfo) =>
            {
                Console.WriteLine($"Health Status: {healthInfo.Status}");
                Console.WriteLine($"Changes Last Hour: {healthInfo.ChangesLastHour}");
            };
            
            // Start monitoring
            await notificationService.StartMonitoringAsync();
            
            Console.WriteLine("Monitoring started. Press any key to stop...");
            Console.ReadKey();
            
            notificationService.StopMonitoring();
        }
        
        /// <summary>
        /// Example 2: MySQL CDC with minimal configuration
        /// </summary>
        public static async Task Example2_MySqlCDC()
        {
            Console.WriteLine("=== MySQL CDC Example ===");
            
            // Minimal configuration - just server details
            var config = DatabaseConfiguration.CreateMySql(
                serverName: "localhost",
                databaseName: "your_database",
                username: "your_username",
                password: "your_password"
            );
            
            using var notificationService = new UnifiedDBNotificationService<YourEntity>(
                config, 
                "your_table", 
                TimeSpan.FromSeconds(10)
            );
            
            // Subscribe to events
            notificationService.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"MySQL change detected in table {e.TableName}");
                Console.WriteLine($"Operation: {e.Operation}");
                Console.WriteLine($"Binary Log Position: {e.ChangeIdentifier}");
            };
            
            notificationService.OnError += (sender, e) =>
            {
                Console.WriteLine($"MySQL Error: {e.Message}");
            };
            
            // Start monitoring
            await notificationService.StartMonitoringAsync();
            
            Console.WriteLine("MySQL monitoring started. Press any key to stop...");
            Console.ReadKey();
            
            notificationService.StopMonitoring();
        }
        
        /// <summary>
        /// Example 3: PostgreSQL CDC with minimal configuration
        /// </summary>
        public static async Task Example3_PostgreSqlCDC()
        {
            Console.WriteLine("=== PostgreSQL CDC Example ===");
            
            // Minimal configuration - just server details
            var config = DatabaseConfiguration.CreatePostgreSql(
                serverName: "localhost",
                databaseName: "your_database",
                username: "your_username",
                password: "your_password",
                schemaName: "public"
            );
            
            using var notificationService = new UnifiedDBNotificationService<YourEntity>(
                config, 
                "your_table", 
                TimeSpan.FromSeconds(20)
            );
            
            // Subscribe to events
            notificationService.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"PostgreSQL change detected in table {e.TableName}");
                Console.WriteLine($"Operation: {e.Operation}");
                Console.WriteLine($"WAL Position: {e.ChangeIdentifier}");
            };
            
            notificationService.OnError += (sender, e) =>
            {
                Console.WriteLine($"PostgreSQL Error: {e.Message}");
            };
            
            // Start monitoring
            await notificationService.StartMonitoringAsync();
            
            Console.WriteLine("PostgreSQL monitoring started. Press any key to stop...");
            Console.ReadKey();
            
            notificationService.StopMonitoring();
        }
        
        /// <summary>
        /// Example 4: Multi-table monitoring with SQL Server
        /// </summary>
        public static async Task Example4_MultiTableMonitoring()
        {
            Console.WriteLine("=== Multi-Table Monitoring Example ===");
            
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=YourDatabase;Integrated Security=true;"
            );
            
            using var notificationService = new UnifiedDBNotificationService<YourEntity>(
                config, 
                "Users", // Primary table
                TimeSpan.FromSeconds(30)
            );
            
            // Subscribe to events
            notificationService.OnChanged += async (sender, e) =>
            {
                Console.WriteLine($"Change detected in primary table: {e.TableName}");
                
                // Get changes for multiple tables
                var multiTableChanges = await notificationService.GetMultiTableChangesAsync(
                    new[] { "Users", "Orders", "Products" }
                );
                
                foreach (var tableChange in multiTableChanges)
                {
                    Console.WriteLine($"Table {tableChange.Key}: {tableChange.Value.Count} changes");
                }
            };
            
            await notificationService.StartMonitoringAsync();
            
            Console.WriteLine("Multi-table monitoring started. Press any key to stop...");
            Console.ReadKey();
            
            notificationService.StopMonitoring();
        }
        
        /// <summary>
        /// Example 5: Advanced configuration with custom settings
        /// </summary>
        public static async Task Example5_AdvancedConfiguration()
        {
            Console.WriteLine("=== Advanced Configuration Example ===");
            
            // Advanced configuration with custom settings
            var config = new DatabaseConfiguration
            {
                DatabaseType = DatabaseType.SqlServer,
                ServerName = "localhost",
                DatabaseName = "YourDatabase",
                Username = "sa",
                Password = "your_password",
                ConnectionTimeout = 60,
                CommandTimeout = 120,
                MaxPoolSize = 200,
                MinPoolSize = 10,
                EnableConnectionPooling = true,
                ApplicationName = "MyCustomApp",
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["TrustServerCertificate"] = "true",
                    ["MultipleActiveResultSets"] = "true"
                }
            };
            
            using var notificationService = new UnifiedDBNotificationService<YourEntity>(
                config, 
                "YourTable", 
                TimeSpan.FromSeconds(5)
            );
            
            // Subscribe to events
            notificationService.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Advanced monitoring: Change in {e.TableName}");
                Console.WriteLine($"Application: {e.ApplicationName}");
                Console.WriteLine($"Host: {e.HostName}");
                Console.WriteLine($"Metadata: {string.Join(", ", e.Metadata.Select(kv => $"{kv.Key}={kv.Value}"))}");
            };
            
            // Validate configuration before starting
            var validation = await notificationService.ValidateConfigurationAsync();
            if (validation.IsValid)
            {
                Console.WriteLine("Configuration validation passed");
                await notificationService.StartMonitoringAsync();
            }
            else
            {
                Console.WriteLine("Configuration validation failed:");
                foreach (var error in validation.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            
            Console.WriteLine("Advanced monitoring started. Press any key to stop...");
            Console.ReadKey();
            
            notificationService.StopMonitoring();
        }
        
        /// <summary>
        /// Example 6: Health monitoring and maintenance
        /// </summary>
        public static async Task Example6_HealthMonitoring()
        {
            Console.WriteLine("=== Health Monitoring Example ===");
            
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=YourDatabase;Integrated Security=true;"
            );
            
            using var notificationService = new UnifiedDBNotificationService<YourEntity>(
                config, 
                "YourTable", 
                TimeSpan.FromSeconds(15)
            );
            
            // Subscribe to health check events
            notificationService.OnHealthCheck += (sender, healthInfo) =>
            {
                Console.WriteLine($"Health Check - Status: {healthInfo.Status}");
                Console.WriteLine($"  Last Detection: {healthInfo.LastSuccessfulDetection}");
                Console.WriteLine($"  Changes/Hour: {healthInfo.ChangesLastHour}");
                Console.WriteLine($"  Errors/Hour: {healthInfo.ErrorsLastHour}");
                Console.WriteLine($"  Response Time: {healthInfo.AverageResponseTime}");
                Console.WriteLine($"  CDC Lag: {healthInfo.CDCLag}");
                
                // Display custom metrics
                foreach (var metric in healthInfo.Metrics)
                {
                    Console.WriteLine($"  {metric.Key}: {metric.Value}");
                }
            };
            
            // Subscribe to change events
            notificationService.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Change detected: {e.Operation} on {e.TableName}");
            };
            
            await notificationService.StartMonitoringAsync();
            
            // Perform manual health check
            var healthInfo = await notificationService.GetHealthInfoAsync();
            Console.WriteLine($"Initial Health Status: {healthInfo.Status}");
            
            // Clean up old changes (retention policy)
            var cleanupResult = await notificationService.CleanupOldChangesAsync(TimeSpan.FromDays(7));
            Console.WriteLine($"Cleanup result: {cleanupResult}");
            
            Console.WriteLine("Health monitoring started. Press any key to stop...");
            Console.ReadKey();
            
            notificationService.StopMonitoring();
        }
        
        /// <summary>
        /// Example 7: Factory pattern usage
        /// </summary>
        public static async Task Example7_FactoryPattern()
        {
            Console.WriteLine("=== Factory Pattern Example ===");
            
            // Using factory pattern for different database types
            var sqlServerProvider = CDCProviderFactory.CreateSqlServerProvider(
                "Server=localhost;Database=YourDatabase;Integrated Security=true;"
            );
            
            var mySqlProvider = CDCProviderFactory.CreateMySqlProvider(
                "localhost", "your_database", "username", "password"
            );
            
            var postgreSqlProvider = CDCProviderFactory.CreatePostgreSqlProvider(
                "localhost", "your_database", "username", "password"
            );
            
            // Create notification services using providers
            using var sqlServerService = new UnifiedDBNotificationService<YourEntity>(
                sqlServerProvider, "YourTable", TimeSpan.FromSeconds(20)
            );
            
            using var mySqlService = new UnifiedDBNotificationService<YourEntity>(
                mySqlProvider, "your_table", TimeSpan.FromSeconds(15)
            );
            
            using var postgreSqlService = new UnifiedDBNotificationService<YourEntity>(
                postgreSqlProvider, "your_table", TimeSpan.FromSeconds(25)
            );
            
            // Subscribe to events for all services
            sqlServerService.OnChanged += (sender, e) => Console.WriteLine($"SQL Server: {e.Operation} on {e.TableName}");
            mySqlService.OnChanged += (sender, e) => Console.WriteLine($"MySQL: {e.Operation} on {e.TableName}");
            postgreSqlService.OnChanged += (sender, e) => Console.WriteLine($"PostgreSQL: {e.Operation} on {e.TableName}");
            
            // Start all services
            await Task.WhenAll(
                sqlServerService.StartMonitoringAsync(),
                mySqlService.StartMonitoringAsync(),
                postgreSqlService.StartMonitoringAsync()
            );
            
            Console.WriteLine("All services started. Press any key to stop...");
            Console.ReadKey();
            
            sqlServerService.StopMonitoring();
            mySqlService.StopMonitoring();
            postgreSqlService.StopMonitoring();
        }
        
        /// <summary>
        /// Example 8: Error handling and recovery
        /// </summary>
        public static async Task Example8_ErrorHandling()
        {
            Console.WriteLine("=== Error Handling Example ===");
            
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=invalid_server;Database=InvalidDB;Integrated Security=true;"
            );
            
            using var notificationService = new UnifiedDBNotificationService<YourEntity>(
                config, 
                "YourTable", 
                TimeSpan.FromSeconds(10)
            );
            
            // Subscribe to error events
            notificationService.OnError += (sender, e) =>
            {
                Console.WriteLine($"Error occurred: {e.Message}");
                Console.WriteLine($"Exception: {e.Exception?.GetType().Name}");
                
                // Implement retry logic or fallback
                if (e.Message.Contains("connection"))
                {
                    Console.WriteLine("Attempting to reconnect...");
                    // Implement reconnection logic
                }
            };
            
            try
            {
                await notificationService.StartMonitoringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start monitoring: {ex.Message}");
                
                // Try with valid configuration
                var validConfig = DatabaseConfiguration.CreateSqlServer(
                    "Server=localhost;Database=YourDatabase;Integrated Security=true;"
                );
                
                notificationService = new UnifiedDBNotificationService<YourEntity>(validConfig, "YourTable");
                
                try
                {
                    await notificationService.StartMonitoringAsync();
                    Console.WriteLine("Successfully started with valid configuration");
                }
                catch (Exception retryEx)
                {
                    Console.WriteLine($"Retry failed: {retryEx.Message}");
                }
            }
            
            Console.WriteLine("Error handling example completed. Press any key to continue...");
            Console.ReadKey();
        }
    }
    
    /// <summary>
    /// Sample entity class for examples
    /// </summary>
    public class YourEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}