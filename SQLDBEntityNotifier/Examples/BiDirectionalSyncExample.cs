using System;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Models;
using System.Collections.Generic; // Added for IEnumerable

namespace SQLDBEntityNotifier.Examples
{
    /// <summary>
    /// Example demonstrating how to use change context filtering to prevent infinite loops
    /// in bi-directional synchronization scenarios.
    /// </summary>
    public class BiDirectionalSyncExample
    {
        /// <summary>
        /// Example 1: Exclude changes from replication/sync processes to prevent loops
        /// </summary>
        public static async Task Example1_PreventReplicationLoops()
        {
            // Configure the service to exclude changes from replication processes
            var filterOptions = ChangeFilterOptions.Exclude(
                ChangeContext.Replication,      // Exclude changes from sync processes
                ChangeContext.DataMigration,    // Exclude changes from ETL processes
                ChangeContext.Maintenance       // Exclude changes from maintenance scripts
            );

            // Create the notification service with filtering
            var notificationService = new SqlDBNotificationService<YourEntity>(
                changeTableService: new ChangeTableService<YourEntity>(dbContext),
                tableName: "YourTable",
                connectionString: "your_connection_string",
                version: -1L,
                period: null,
                changeTrackingQueryFunc: null,
                filterOptions: filterOptions
            );

            // Subscribe to change events
            notificationService.OnChanged += (sender, e) =>
            {
                Console.WriteLine($"Change detected from version {e.ChangeVersion}");
                Console.WriteLine($"Change context: {e.ChangeContext?.Context}");
                
                // Process only legitimate application changes
                foreach (var entity in e.Entities ?? Array.Empty<YourEntity>())
                {
                    // Process the change without triggering another sync
                    ProcessChange(entity);
                }
            };

            await notificationService.StartNotify();
        }

        /// <summary>
        /// Example 2: Allow only specific change contexts
        /// </summary>
        public static async Task Example2_AllowOnlySpecificContexts()
        {
            // Only allow changes from user interface and web services
            var filterOptions = ChangeFilterOptions.AllowOnly(
                ChangeContext.UserInterface,    // User actions in the UI
                ChangeContext.WebService        // API calls
            );

            var notificationService = new SqlDBNotificationService<YourEntity>(
                changeTableService: new ChangeTableService<YourEntity>(dbContext),
                tableName: "YourTable",
                connectionString: "your_connection_string",
                version: -1L,
                period: null,
                changeTrackingQueryFunc: null,
                filterOptions: filterOptions
            );

            // This will only trigger for UI and API changes, not for sync processes
            await notificationService.StartNotify();
        }

        /// <summary>
        /// Example 3: Advanced filtering with extended context information
        /// </summary>
        public static async Task Example3_AdvancedFiltering()
        {
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
                version: -1L,
                period: null,
                changeTrackingQueryFunc: null,
                filterOptions: filterOptions
            );

            notificationService.OnChanged += (sender, e) =>
            {
                var context = e.ChangeContext;
                Console.WriteLine($"Change from {context?.ApplicationName} on {context?.HostName}");
                Console.WriteLine($"Context: {context?.Context}");
                
                // Process the change
                ProcessChange(e.Entities ?? Array.Empty<YourEntity>());
            };

            await notificationService.StartNotify();
        }

        /// <summary>
        /// Example 4: Custom query function for complex scenarios
        /// </summary>
        public static async Task Example4_CustomQueryFunction()
        {
            // Create a custom query that includes additional filtering logic
            Func<long, string> customQuery = (fromVersion) =>
            {
                return $@"
                    SELECT ct.*, ct.SYS_CHANGE_CONTEXT as ChangeContext
                    FROM CHANGETABLE(CHANGES YourTable, {fromVersion}) ct 
                    WHERE ct.SYS_CHANGE_VERSION <= {{0}}
                    AND ct.SYS_CHANGE_CONTEXT NOT IN (6, 7)  -- Exclude Replication and Maintenance
                    AND ct.SYS_CHANGE_CONTEXT IS NOT NULL    -- Only changes with context
                    ORDER BY ct.SYS_CHANGE_VERSION";
            };

            var notificationService = new SqlDBNotificationService<YourEntity>(
                changeTableService: new ChangeTableService<YourEntity>(dbContext),
                tableName: "YourTable",
                connectionString: "your_connection_string",
                version: -1L,
                period: null,
                changeTrackingQueryFunc: customQuery,
                filterOptions: null
            );

            await notificationService.StartNotify();
        }

        /// <summary>
        /// Example 5: Multi-system integration with context mapping
        /// </summary>
        public static async Task Example5_MultiSystemIntegration()
        {
            // Define context mappings for different systems
            var system1Contexts = new[] { ChangeContext.Application, ChangeContext.UserInterface };
            var system2Contexts = new[] { ChangeContext.WebService, ChangeContext.ScheduledJob };

            // System 1: Only listen to its own changes
            var system1Filter = ChangeFilterOptions.AllowOnly(system1Contexts);
            
            var system1Service = new SqlDBNotificationService<YourEntity>(
                changeTableService: new ChangeTableService<YourEntity>(dbContext),
                tableName: "YourTable",
                connectionString: "system1_connection_string",
                version: -1L,
                period: null,
                changeTrackingQueryFunc: null,
                filterOptions: system1Filter
            );

            // System 2: Only listen to its own changes
            var system2Filter = ChangeFilterOptions.AllowOnly(system2Contexts);
            
            var system2Service = new SqlDBNotificationService<YourEntity>(
                changeTableService: new ChangeTableService<YourEntity>(dbContext),
                tableName: "YourTable",
                connectionString: "system2_connection_string",
                version: -1L,
                period: null,
                changeTrackingQueryFunc: null,
                filterOptions: system2Filter
            );

            // Each system only processes its own changes, preventing loops
            await Task.WhenAll(
                system1Service.StartNotify(),
                system2Service.StartNotify()
            );
        }

        // Helper methods and placeholder classes
        private static void ProcessChange(YourEntity entity) { }
        private static void ProcessChange(IEnumerable<YourEntity> entities) { }
        
        // Placeholder classes - replace with your actual entities and services
        public class YourEntity { }
        private static Microsoft.EntityFrameworkCore.DbContext dbContext = null!;
    }
}
