using System;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;

namespace SQLDBEntityNotifier.Compatibility
{
    /// <summary>
    /// Compatibility layer for existing SqlDBNotificationService to work with new CDC infrastructure
    /// </summary>
    internal static class SqlDBNotificationServiceCompatibility
    {
        /// <summary>
        /// Creates a backward-compatible CDC provider for existing SQL Server connections
        /// </summary>
        public static ICDCProvider CreateCompatibleCDCProvider(string connectionString, string databaseName = "")
        {
            // Automatically detect if this is a SQL Server connection string
            if (IsSqlServerConnectionString(connectionString))
            {
                var config = DatabaseConfiguration.CreateSqlServer(connectionString, databaseName);
                return new SqlServerCDCProvider(config);
            }
            
            // For non-SQL Server connections, throw an exception to maintain existing behavior
            throw new NotSupportedException("This connection string is not supported by the existing SqlDBNotificationService. " +
                "Use UnifiedDBNotificationService for multi-database support.");
        }

        /// <summary>
        /// Determines if a connection string is for SQL Server
        /// </summary>
        private static bool IsSqlServerConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            var lowerConnectionString = connectionString.ToLowerInvariant();
            
            // SQL Server connection string patterns
            return lowerConnectionString.Contains("server=") ||
                   lowerConnectionString.Contains("data source=") ||
                   lowerConnectionString.Contains("initial catalog=") ||
                   lowerConnectionString.Contains("database=") ||
                   lowerConnectionString.Contains("integrated security=") ||
                   lowerConnectionString.Contains("user id=") ||
                   lowerConnectionString.Contains("password=") ||
                   lowerConnectionString.Contains("trusted_connection=");
        }

        /// <summary>
        /// Creates a backward-compatible database configuration
        /// </summary>
        public static DatabaseConfiguration CreateCompatibleDatabaseConfiguration(string connectionString, string databaseName = "")
        {
            return DatabaseConfiguration.CreateSqlServer(connectionString, databaseName);
        }
    }
}