using System;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using SQLDBEntityNotifier.Interfaces;

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
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString cannot be null or whitespace.", nameof(connectionString));

            if (!IsSqlServerConnectionString(connectionString))
                throw new NotSupportedException("This connection string is not supported by the existing SqlDBNotificationService. Use UnifiedDBNotificationService for multi-database support.");

            var config = DatabaseConfiguration.CreateSqlServer(connectionString, databaseName);
            return new SqlServerCDCProvider(config);
        }

        /// <summary>
        /// Determines if a connection string is for SQL Server
        /// </summary>
        public static bool IsSqlServerConnectionString(string connectionString)
        {
        public static bool IsSqlServerConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;
            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                return !string.IsNullOrWhiteSpace(builder.DataSource);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
                    !lowerConnectionString.Contains("username=")) ||
                   (lowerConnectionString.Contains("password=") && 
                    !lowerConnectionString.Contains("host=") && 
                    !lowerConnectionString.Contains("username="));
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