using System;
using System.Collections.Generic;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Configuration for database change detection across different database types
    /// </summary>
    public class DatabaseConfiguration
    {
        /// <summary>
        /// Gets or sets the type of database
        /// </summary>
        public DatabaseType DatabaseType { get; set; }
        
        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the database name
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the schema name (for PostgreSQL)
        /// </summary>
        public string? SchemaName { get; set; }
        
        /// <summary>
        /// Gets or sets the server name or host
        /// </summary>
        public string ServerName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the port number
        /// </summary>
        public int? Port { get; set; }
        
        /// <summary>
        /// Gets or sets the username for authentication
        /// </summary>
        public string? Username { get; set; }
        
        /// <summary>
        /// Gets or sets the password for authentication
        /// </summary>
        public string? Password { get; set; }
        
        /// <summary>
        /// Gets or sets whether to use SSL/TLS connection
        /// </summary>
        public bool UseSsl { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the SSL mode (for PostgreSQL)
        /// </summary>
        public string? SslMode { get; set; }
        
        /// <summary>
        /// Gets or sets the connection timeout in seconds
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;
        
        /// <summary>
        /// Gets or sets the command timeout in seconds
        /// </summary>
        public int CommandTimeout { get; set; } = 60;
        
        /// <summary>
        /// Gets or sets the maximum pool size for connection pooling
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets the minimum pool size for connection pooling
        /// </summary>
        public int MinPoolSize { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets whether to enable connection pooling
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the character set (for MySQL)
        /// </summary>
        public string? CharacterSet { get; set; }
        
        /// <summary>
        /// Gets or sets the collation (for MySQL)
        /// </summary>
        public string? Collation { get; set; }
        
        /// <summary>
        /// Gets or sets the timezone (for MySQL)
        /// </summary>
        public string? Timezone { get; set; }
        
        /// <summary>
        /// Gets or sets the application name for connection identification
        /// </summary>
        public string? ApplicationName { get; set; }
        
        /// <summary>
        /// Gets or sets additional connection string parameters
        /// </summary>
        public Dictionary<string, string>? AdditionalParameters { get; set; }
        
        /// <summary>
        /// Creates a SQL Server configuration
        /// </summary>
        public static DatabaseConfiguration CreateSqlServer(string connectionString, string databaseName = "")
        {
            return new DatabaseConfiguration
            {
                DatabaseType = DatabaseType.SqlServer,
                ConnectionString = connectionString,
                DatabaseName = databaseName
            };
        }
        
        /// <summary>
        /// Creates a MySQL configuration
        /// </summary>
        public static DatabaseConfiguration CreateMySql(string serverName, string databaseName, string username, string password, int port = 3306)
        {
            return new DatabaseConfiguration
            {
                DatabaseType = DatabaseType.MySql,
                ServerName = serverName,
                DatabaseName = databaseName,
                Username = username,
                Password = password,
                Port = port,
                CharacterSet = "utf8mb4",
                Collation = "utf8mb4_unicode_ci",
                Timezone = "+00:00"
            };
        }
        
        /// <summary>
        /// Creates a PostgreSQL configuration
        /// </summary>
        public static DatabaseConfiguration CreatePostgreSql(string serverName, string databaseName, string username, string password, int port = 5432, string schemaName = "public")
        {
            return new DatabaseConfiguration
            {
                DatabaseType = DatabaseType.PostgreSql,
                ServerName = serverName,
                DatabaseName = databaseName,
                Username = username,
                Password = password,
                Port = port,
                SchemaName = schemaName,
                SslMode = "Prefer"
            };
        }
        
        /// <summary>
        /// Builds the connection string based on the configuration
        /// </summary>
        public string BuildConnectionString()
        {
            return DatabaseType switch
            {
                DatabaseType.SqlServer => ConnectionString,
                DatabaseType.MySql => BuildMySqlConnectionString(),
                DatabaseType.PostgreSql => BuildPostgreSqlConnectionString(),
                _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported")
            };
        }
        
        private string BuildMySqlConnectionString()
        {
            var parameters = new List<string>
            {
                $"Server={ServerName}",
                $"Database={DatabaseName}",
                $"Uid={Username}",
                $"Pwd={Password}",
                $"Port={Port}",
                $"CharSet={CharacterSet}",
                $"Convert Zero Datetime=True",
                $"Allow User Variables=True",
                $"Connection Timeout={ConnectionTimeout}",
                $"Default Command Timeout={CommandTimeout}"
            };
            
            if (UseSsl)
                parameters.Add("SslMode=Required");
                
            if (EnableConnectionPooling)
            {
                parameters.Add($"Maximum Pool Size={MaxPoolSize}");
                parameters.Add($"Minimum Pool Size={MinPoolSize}");
            }
            
            if (AdditionalParameters != null)
            {
                foreach (var param in AdditionalParameters)
                {
                    parameters.Add($"{param.Key}={param.Value}");
                }
            }
            
            return string.Join(";", parameters);
        }
        
        private string BuildPostgreSqlConnectionString()
        {
            var parameters = new List<string>
            {
                $"Host={ServerName}",
                $"Database={DatabaseName}",
                $"Username={Username}",
                $"Password={Password}",
                $"Port={Port}",
                $"Schema={SchemaName}",
                $"Connection Timeout={ConnectionTimeout}",
                $"Command Timeout={CommandTimeout}",
                $"Application Name={ApplicationName ?? "SQLDBEntityNotifier"}"
            };
            
            if (UseSsl)
                parameters.Add($"SSL Mode={SslMode}");
                
            if (EnableConnectionPooling)
            {
                parameters.Add($"Maximum Pool Size={MaxPoolSize}");
                parameters.Add($"Minimum Pool Size={MinPoolSize}");
            }
            
            if (AdditionalParameters != null)
            {
                foreach (var param in AdditionalParameters)
                {
                    parameters.Add($"{param.Key}={param.Value}");
                }
            }
            
            return string.Join(";", parameters);
        }
    }
}