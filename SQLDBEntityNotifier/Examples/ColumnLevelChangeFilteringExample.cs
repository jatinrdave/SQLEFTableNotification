using System;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using System.Collections.Generic; // Added missing import for List

namespace SQLDBEntityNotifier.Examples
{
    /// <summary>
    /// Examples demonstrating column-level change filtering in UnifiedDBNotificationService
    /// </summary>
    public class ColumnLevelChangeFilteringExample
    {
        /// <summary>
        /// Example 1: Monitor only specific columns for changes
        /// </summary>
        public static async Task Example1_MonitorSpecificColumns()
        {
            Console.WriteLine("=== Example 1: Monitor Only Specific Columns ===");

            // Create column filter options to monitor only specific columns
            var columnFilter = ColumnChangeFilterOptions.MonitorOnly("Name", "Email", "Status");

            // Create database configuration
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=UserDB;Integrated Security=true;"
            );

            // Create notification service with column filtering
            using var service = new UnifiedDBNotificationService<User>(
                config,
                "Users",
                pollingInterval: TimeSpan.FromSeconds(30),
                columnFilterOptions: columnFilter
            );

            // Subscribe to change events
            service.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Change detected in columns: {string.Join(", ", e.AffectedColumns ?? new List<string>())}");
                Console.WriteLine($"Operation: {e.Operation}");
                Console.WriteLine($"Entity count: {e.Entities.Count}");
            };

            // Start monitoring
            await service.StartMonitoringAsync();
            Console.WriteLine("Monitoring started - only changes to Name, Email, or Status columns will trigger notifications");
        }

        /// <summary>
        /// Example 2: Exclude specific columns from monitoring
        /// </summary>
        public static async Task Example2_ExcludeSpecificColumns()
        {
            Console.WriteLine("=== Example 2: Exclude Specific Columns ===");

            // Create column filter options to exclude specific columns
            var columnFilter = ColumnChangeFilterOptions.ExcludeColumns("LastLoginTime", "AuditTimestamp", "InternalFlags");

            // Create database configuration
            var config = DatabaseConfiguration.CreateMySql(
                "localhost",
                "user_db",
                "app_user",
                "password123"
            );

            // Create notification service with column filtering
            using var service = new UnifiedDBNotificationService<User>(
                config,
                "users",
                pollingInterval: TimeSpan.FromSeconds(30),
                columnFilterOptions: columnFilter
            );

            // Subscribe to change events
            service.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Change detected - excluded columns: LastLoginTime, AuditTimestamp, InternalFlags");
                Console.WriteLine($"Operation: {e.Operation}");
                Console.WriteLine($"Affected columns: {string.Join(", ", e.AffectedColumns ?? new List<string>())}");
            };

            // Start monitoring
            await service.StartMonitoringAsync();
            Console.WriteLine("Monitoring started - changes to LastLoginTime, AuditTimestamp, or InternalFlags will be ignored");
        }

        /// <summary>
        /// Example 3: Monitor all columns except specific ones
        /// </summary>
        public static async Task Example3_MonitorAllExceptSpecific()
        {
            Console.WriteLine("=== Example 3: Monitor All Columns Except Specific Ones ===");

            // Create column filter options to monitor all columns except specific ones
            var columnFilter = ColumnChangeFilterOptions.MonitorAllExcept("CreatedAt", "UpdatedAt", "Version");

            // Create database configuration
            var config = DatabaseConfiguration.CreatePostgreSql(
                "localhost",
                "user_db",
                "app_user",
                "password123"
            );

            // Create notification service with column filtering
            using var service = new UnifiedDBNotificationService<User>(
                config,
                "users",
                pollingInterval: TimeSpan.FromSeconds(30),
                columnFilterOptions: columnFilter
            );

            // Subscribe to change events
            service.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Change detected - excluding timestamp and version columns");
                Console.WriteLine($"Operation: {e.Operation}");
                Console.WriteLine($"Affected columns: {string.Join(", ", e.AffectedColumns ?? new List<string>())}");
            };

            // Start monitoring
            await service.StartMonitoringAsync();
            Console.WriteLine("Monitoring started - all columns monitored except CreatedAt, UpdatedAt, and Version");
        }

        /// <summary>
        /// Example 4: Advanced column filtering with custom settings
        /// </summary>
        public static async Task Example4_AdvancedColumnFiltering()
        {
            Console.WriteLine("=== Example 4: Advanced Column Filtering ===");

            // Create advanced column filter options
            var columnFilter = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("Name", "Email", "Phone", "Address")
                .AddExcludedColumns("PasswordHash", "SecurityToken", "InternalId")
                .AddColumnMapping("user_name", "Name")           // Map database column to entity property
                .AddColumnMapping("email_address", "Email")
                .AddColumnMapping("phone_number", "Phone")
                .AddColumnMapping("user_address", "Address");

            // Configure additional options
            columnFilter.IncludeColumnLevelChanges = true;        // Include which columns actually changed
            columnFilter.IncludeColumnValues = true;             // Include old/new values for changed columns
            columnFilter.MinimumColumnChanges = 1;               // Trigger on any column change
            columnFilter.CaseSensitiveColumnNames = false;       // Case-insensitive column matching
            columnFilter.NormalizeColumnNames = true;            // Trim whitespace from column names
            columnFilter.IncludeComputedColumns = false;         // Exclude computed columns
            columnFilter.IncludeIdentityColumns = false;         // Exclude identity columns
            columnFilter.IncludeTimestampColumns = false;        // Exclude timestamp columns

            // Create database configuration
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=UserDB;Integrated Security=true;"
            );

            // Create notification service with advanced column filtering
            using var service = new UnifiedDBNotificationService<User>(
                config,
                "Users",
                pollingInterval: TimeSpan.FromSeconds(30),
                columnFilterOptions: columnFilter
            );

            // Subscribe to change events
            service.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Advanced change detection:");
                Console.WriteLine($"  Operation: {e.Operation}");
                Console.WriteLine($"  Affected columns: {string.Join(", ", e.AffectedColumns ?? new List<string>())}");
                Console.WriteLine($"  Has old values: {e.OldValues != null}");
                Console.WriteLine($"  Has new values: {e.NewValues != null}");
                Console.WriteLine($"  Is batch operation: {e.IsBatchOperation}");
            };

            // Start monitoring
            await service.StartMonitoringAsync();
            Console.WriteLine("Advanced monitoring started with custom column filtering and mapping");
        }

        /// <summary>
        /// Example 5: Column filtering with different database types
        /// </summary>
        public static async Task Example5_MultiDatabaseColumnFiltering()
        {
            Console.WriteLine("=== Example 5: Multi-Database Column Filtering ===");

            // SQL Server example
            await MonitorWithColumnFiltering(
                DatabaseConfiguration.CreateSqlServer("Server=localhost;Database=UserDB;Integrated Security=true;"),
                "Users",
                ColumnChangeFilterOptions.MonitorOnly("Name", "Email")
            );

            // MySQL example
            await MonitorWithColumnFiltering(
                DatabaseConfiguration.CreateMySql("localhost", "user_db", "app_user", "password123"),
                "users",
                ColumnChangeFilterOptions.MonitorOnly("name", "email")
            );

            // PostgreSQL example
            await MonitorWithColumnFiltering(
                DatabaseConfiguration.CreatePostgreSql("localhost", "user_db", "app_user", "password123"),
                "users",
                ColumnChangeFilterOptions.MonitorOnly("name", "email")
            );
        }

        /// <summary>
        /// Helper method to monitor with column filtering
        /// </summary>
        private static async Task MonitorWithColumnFiltering(
            DatabaseConfiguration config,
            string tableName,
            ColumnChangeFilterOptions columnFilter)
        {
            Console.WriteLine($"\nMonitoring {tableName} in {config.DatabaseType} with column filtering...");

            using var service = new UnifiedDBNotificationService<User>(
                config,
                tableName,
                pollingInterval: TimeSpan.FromSeconds(30),
                columnFilterOptions: columnFilter
            );

            service.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"  {config.DatabaseType}: Change detected in {e.AffectedColumns?.Count ?? 0} columns");
            };

            service.OnError += (sender, e) =>
            {
                Console.WriteLine($"  {config.DatabaseType}: Error: {e.Message}");
            };

            try
            {
                await service.StartMonitoringAsync();
                Console.WriteLine($"  {config.DatabaseType}: Monitoring started successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {config.DatabaseType}: Failed to start monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 6: Dynamic column filtering
        /// </summary>
        public static async Task Example6_DynamicColumnFiltering()
        {
            Console.WriteLine("=== Example 6: Dynamic Column Filtering ===");

            // Create initial column filter
            var columnFilter = ColumnChangeFilterOptions.MonitorOnly("Name", "Email");

            // Create database configuration
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=UserDB;Integrated Security=true;"
            );

            // Create notification service
            using var service = new UnifiedDBNotificationService<User>(
                config,
                "Users",
                pollingInterval: TimeSpan.FromSeconds(30),
                columnFilterOptions: columnFilter
            );

            // Subscribe to change events
            service.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Change detected with current column filter:");
                Console.WriteLine($"  Monitored columns: {string.Join(", ", columnFilter.MonitoredColumns ?? new List<string>())}");
                Console.WriteLine($"  Excluded columns: {string.Join(", ", columnFilter.ExcludedColumns ?? new List<string>())}");
                Console.WriteLine($"  Affected columns: {string.Join(", ", e.AffectedColumns ?? new List<string>())}");
            };

            // Start monitoring
            await service.StartMonitoringAsync();
            Console.WriteLine("Initial monitoring started - only Name and Email columns monitored");

            // Simulate dynamic column filter changes
            await Task.Delay(5000); // Wait 5 seconds

            // Add more columns to monitor
            columnFilter.AddMonitoredColumns("Phone", "Address");
            Console.WriteLine("Column filter updated - now monitoring Name, Email, Phone, and Address");

            await Task.Delay(5000); // Wait 5 seconds

            // Exclude some columns
            columnFilter.AddExcludedColumns("InternalFlags", "AuditData");
            Console.WriteLine("Column filter updated - excluded InternalFlags and AuditData");

            await Task.Delay(5000); // Wait 5 seconds

            // Remove some monitored columns
            columnFilter.MonitoredColumns?.Remove("Phone");
            Console.WriteLine("Column filter updated - Phone column removed from monitoring");
        }

        /// <summary>
        /// Example 7: Column filtering with entity property mapping
        /// </summary>
        public static async Task Example7_EntityPropertyMapping()
        {
            Console.WriteLine("=== Example 7: Entity Property Mapping ===");

            // Create column filter with property mapping
            var columnFilter = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("user_name", "email_address", "phone_number")
                .AddColumnMapping("user_name", "Name")           // Database column -> Entity property
                .AddColumnMapping("email_address", "Email")
                .AddColumnMapping("phone_number", "Phone")
                .AddColumnMapping("created_date", "CreatedAt")
                .AddColumnMapping("modified_date", "UpdatedAt");

            // Create database configuration
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=UserDB;Integrated Security=true;"
            );

            // Create notification service
            using var service = new UnifiedDBNotificationService<User>(
                config,
                "Users",
                pollingInterval: TimeSpan.FromSeconds(30),
                columnFilterOptions: columnFilter
            );

            // Subscribe to change events
            service.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Change detected with property mapping:");
                Console.WriteLine($"  Database columns: {string.Join(", ", columnFilter.MonitoredColumns ?? new List<string>())}");
                Console.WriteLine($"  Mapped properties: Name, Email, Phone");
                Console.WriteLine($"  Affected columns: {string.Join(", ", e.AffectedColumns ?? new List<string>())}");
            };

            // Start monitoring
            await service.StartMonitoringAsync();
            Console.WriteLine("Monitoring started with entity property mapping");
        }

        /// <summary>
        /// Example 8: Performance optimization with column filtering
        /// </summary>
        public static async Task Example8_PerformanceOptimization()
        {
            Console.WriteLine("=== Example 8: Performance Optimization ===");

            // Create optimized column filter for high-performance scenarios
            var columnFilter = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("Status", "Priority", "AssignedTo")
                .AddExcludedColumns("Description", "Comments", "History", "Metadata")
                .AddExcludedColumns("CreatedAt", "UpdatedAt", "Version", "AuditTrail");

            // Configure for performance
            columnFilter.IncludeColumnLevelChanges = true;        // Only include essential column info
            columnFilter.IncludeColumnValues = false;             // Don't include old/new values for performance
            columnFilter.MinimumColumnChanges = 1;                // Trigger on any relevant change
            columnFilter.CaseSensitiveColumnNames = true;         // Faster string comparison
            columnFilter.NormalizeColumnNames = false;            // Skip normalization for performance

            // Create database configuration
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=TaskDB;Integrated Security=true;"
            );

            // Create notification service with performance optimization
            using var service = new UnifiedDBNotificationService<User>(
                config,
                "Tasks",
                pollingInterval: TimeSpan.FromSeconds(15),        // Faster polling for critical changes
                columnFilterOptions: columnFilter
            );

            // Subscribe to change events
            service.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"High-performance change detection:");
                Console.WriteLine($"  Operation: {e.Operation}");
                Console.WriteLine($"  Affected columns: {string.Join(", ", e.AffectedColumns ?? new List<string>())}");
                Console.WriteLine($"  Change detected at: {e.ChangeDetectedAt:HH:mm:ss.fff}");
            };

            // Start monitoring
            await service.StartMonitoringAsync();
            Console.WriteLine("High-performance monitoring started - optimized for critical column changes only");
        }
    }

    /// <summary>
    /// Sample user entity for examples
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Version { get; set; }
    }
}