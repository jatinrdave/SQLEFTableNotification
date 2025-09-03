using System;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;

namespace SQLDBEntityNotifier.Providers
{
    /// <summary>
    /// Factory class for creating CDC providers based on database type
    /// </summary>
    public static class CDCProviderFactory
    {
        /// <summary>
        /// Creates a CDC provider based on the database configuration
        /// </summary>
        /// <param name="configuration">The database configuration</param>
        /// <returns>A CDC provider instance</returns>
        public static ICDCProvider CreateProvider(DatabaseConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return configuration.DatabaseType switch
            {
                DatabaseType.SqlServer => new SqlServerCDCProvider(configuration),
                DatabaseType.MySql => new MySqlCDCProvider(configuration),
                DatabaseType.PostgreSql => new PostgreSqlCDCProvider(configuration),
                _ => throw new NotSupportedException($"Database type {configuration.DatabaseType} is not supported")
            };
        }

        /// <summary>
        /// Creates a CDC provider for SQL Server
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <param name="databaseName">The database name</param>
        /// <returns>A SQL Server CDC provider</returns>
        public static ICDCProvider CreateSqlServerProvider(string connectionString, string databaseName = "")
        {
            var config = DatabaseConfiguration.CreateSqlServer(connectionString, databaseName);
            return new SqlServerCDCProvider(config);
        }

        /// <summary>
        /// Creates a CDC provider for MySQL
        /// </summary>
        /// <param name="serverName">The server name</param>
        /// <param name="databaseName">The database name</param>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <param name="port">The port number (default: 3306)</param>
        /// <returns>A MySQL CDC provider</returns>
        public static ICDCProvider CreateMySqlProvider(string serverName, string databaseName, string username, string password, int port = 3306)
        {
            var config = DatabaseConfiguration.CreateMySql(serverName, databaseName, username, password, port);
            return new MySqlCDCProvider(config);
        }

        /// <summary>
        /// Creates a CDC provider for PostgreSQL
        /// </summary>
        /// <param name="serverName">The server name</param>
        /// <param name="databaseName">The database name</param>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <param name="port">The port number (default: 5432)</param>
        /// <param name="schemaName">The schema name (default: public)</param>
        /// <returns>A PostgreSQL CDC provider</returns>
        public static ICDCProvider CreatePostgreSqlProvider(string serverName, string databaseName, string username, string password, int port = 5432, string schemaName = "public")
        {
            var config = DatabaseConfiguration.CreatePostgreSql(serverName, databaseName, username, password, port, schemaName);
            return new PostgreSqlCDCProvider(config);
        }

        /// <summary>
        /// Creates a CDC provider from a connection string with automatic database type detection
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <returns>A CDC provider instance</returns>
        public static ICDCProvider CreateProviderFromConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            // Try to detect database type from connection string
            if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) || 
                connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                // SQL Server connection string
                return CreateSqlServerProvider(connectionString);
            }
            else if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            {
                // PostgreSQL connection string
                var config = new DatabaseConfiguration
                {
                    DatabaseType = DatabaseType.PostgreSql,
                    ConnectionString = connectionString
                };
                return new PostgreSqlCDCProvider(config);
            }
            else if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) && 
                     connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
            {
                // MySQL connection string
                var config = new DatabaseConfiguration
                {
                    DatabaseType = DatabaseType.MySql,
                    ConnectionString = connectionString
                };
                return new MySqlCDCProvider(config);
            }
            else
            {
                throw new NotSupportedException("Unable to determine database type from connection string. Please use a specific factory method.");
            }
        }
    }
}