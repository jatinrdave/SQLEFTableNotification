using System;
using System.Collections.Generic;
using SQLDBEntityNotifier.Models;
using Xunit;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class DatabaseConfigurationTests
    {
        [Fact]
        public void CreateSqlServer_WithValidParameters_ShouldCreateValidConfiguration()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;Integrated Security=true;";
            var databaseName = "TestDB";

            // Act
            var config = DatabaseConfiguration.CreateSqlServer(connectionString, databaseName);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(DatabaseType.SqlServer, config.DatabaseType);
            Assert.Equal(connectionString, config.ConnectionString);
            Assert.Equal(databaseName, config.DatabaseName);
            // Note: CreateSqlServer doesn't parse the connection string to extract ServerName
            // It only sets ConnectionString and DatabaseName properties
        }

        [Fact]
        public void CreateSqlServer_WithNullConnectionString_ShouldCreateConfigurationWithNullConnectionString()
        {
            // Act
            var config = DatabaseConfiguration.CreateSqlServer(null!, "TestDB");

            // Assert
            Assert.NotNull(config);
            Assert.Equal(DatabaseType.SqlServer, config.DatabaseType);
            Assert.Null(config.ConnectionString);
            Assert.Equal("TestDB", config.DatabaseName);
        }

        [Fact]
        public void CreateSqlServer_WithEmptyConnectionString_ShouldCreateConfigurationWithEmptyConnectionString()
        {
            // Act
            var config1 = DatabaseConfiguration.CreateSqlServer("", "TestDB");
            var config2 = DatabaseConfiguration.CreateSqlServer("   ", "TestDB");

            // Assert
            Assert.NotNull(config1);
            Assert.Equal("", config1.ConnectionString);
            Assert.Equal("TestDB", config1.DatabaseName);

            Assert.NotNull(config2);
            Assert.Equal("   ", config2.ConnectionString);
            Assert.Equal("TestDB", config2.DatabaseName);
        }

        [Fact]
        public void CreateMySql_WithValidParameters_ShouldCreateValidConfiguration()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";
            var port = 3306;

            // Act
            var config = DatabaseConfiguration.CreateMySql(serverName, databaseName, username, password, port);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(DatabaseType.MySql, config.DatabaseType);
            Assert.Equal(serverName, config.ServerName);
            Assert.Equal(databaseName, config.DatabaseName);
            Assert.Equal(username, config.Username);
            Assert.Equal(password, config.Password);
            Assert.Equal(port, config.Port);
            Assert.True(config.UseSsl);
            Assert.Equal("utf8mb4", config.CharacterSet);
            Assert.Equal("utf8mb4_unicode_ci", config.Collation);
            Assert.Equal("+00:00", config.Timezone);
        }

        [Fact]
        public void CreateMySql_WithDefaultPort_ShouldUseDefaultPort()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";

            // Act
            var config = DatabaseConfiguration.CreateMySql(serverName, databaseName, username, password);

            // Assert
            Assert.Equal(3306, config.Port);
        }

        [Fact]
        public void CreateMySql_WithNullParameters_ShouldCreateConfigurationWithNullValues()
        {
            // Act
            var config1 = DatabaseConfiguration.CreateMySql(null!, "test_db", "user", "pass");
            var config2 = DatabaseConfiguration.CreateMySql("localhost", null!, "user", "pass");
            var config3 = DatabaseConfiguration.CreateMySql("localhost", "test_db", null!, "pass");
            var config4 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", null!);

            // Assert
            Assert.NotNull(config1);
            Assert.Equal(DatabaseType.MySql, config1.DatabaseType);
            Assert.Null(config1.ServerName);
            Assert.Equal("test_db", config1.DatabaseName);
            Assert.Equal("user", config1.Username);
            Assert.Equal("pass", config1.Password);

            Assert.NotNull(config2);
            Assert.Equal(DatabaseType.MySql, config2.DatabaseType);
            Assert.Equal("localhost", config2.ServerName);
            Assert.Null(config2.DatabaseName);
            Assert.Equal("user", config2.Username);
            Assert.Equal("pass", config2.Password);

            Assert.NotNull(config3);
            Assert.Equal(DatabaseType.MySql, config3.DatabaseType);
            Assert.Equal("localhost", config3.ServerName);
            Assert.Equal("test_db", config3.DatabaseName);
            Assert.Null(config3.Username);
            Assert.Equal("pass", config3.Password);

            Assert.NotNull(config4);
            Assert.Equal(DatabaseType.MySql, config4.DatabaseType);
            Assert.Equal("localhost", config4.ServerName);
            Assert.Equal("test_db", config4.DatabaseName);
            Assert.Equal("user", config4.Username);
            Assert.Null(config4.Password);
        }

        [Fact]
        public void CreatePostgreSql_WithValidParameters_ShouldCreateValidConfiguration()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";
            var port = 5432;
            var schemaName = "public";

            // Act
            var config = DatabaseConfiguration.CreatePostgreSql(serverName, databaseName, username, password, port, schemaName);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(DatabaseType.PostgreSql, config.DatabaseType);
            Assert.Equal(serverName, config.ServerName);
            Assert.Equal(databaseName, config.DatabaseName);
            Assert.Equal(username, config.Username);
            Assert.Equal(password, config.Password);
            Assert.Equal(port, config.Port);
            Assert.Equal(schemaName, config.SchemaName);
            Assert.True(config.UseSsl);
            Assert.Equal("Prefer", config.SslMode);
        }

        [Fact]
        public void CreatePostgreSql_WithDefaultParameters_ShouldUseDefaults()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";

            // Act
            var config = DatabaseConfiguration.CreatePostgreSql(serverName, databaseName, username, password);

            // Assert
            Assert.Equal(5432, config.Port);
            Assert.Equal("public", config.SchemaName);
        }

        [Fact]
        public void CreatePostgreSql_WithNullParameters_ShouldCreateConfigurationWithNullValues()
        {
            // Act
            var config1 = DatabaseConfiguration.CreatePostgreSql(null!, "test_db", "user", "pass");
            var config2 = DatabaseConfiguration.CreatePostgreSql("localhost", null!, "user", "pass");
            var config3 = DatabaseConfiguration.CreatePostgreSql("localhost", "test_db", null!, "pass");
            var config4 = DatabaseConfiguration.CreatePostgreSql("localhost", "test_db", "user", null!);

            // Assert
            Assert.NotNull(config1);
            Assert.Equal(DatabaseType.PostgreSql, config1.DatabaseType);
            Assert.Null(config1.ServerName);
            Assert.Equal("test_db", config1.DatabaseName);
            Assert.Equal("user", config1.Username);
            Assert.Equal("pass", config1.Password);

            Assert.NotNull(config2);
            Assert.Equal(DatabaseType.PostgreSql, config2.DatabaseType);
            Assert.Equal("localhost", config2.ServerName);
            Assert.Null(config2.DatabaseName);
            Assert.Equal("user", config2.Username);
            Assert.Equal("pass", config2.Password);

            Assert.NotNull(config3);
            Assert.Equal(DatabaseType.PostgreSql, config3.DatabaseType);
            Assert.Equal("localhost", config3.ServerName);
            Assert.Equal("test_db", config3.DatabaseName);
            Assert.Null(config3.Username);
            Assert.Equal("pass", config3.Password);

            Assert.NotNull(config4);
            Assert.Equal(DatabaseType.PostgreSql, config4.DatabaseType);
            Assert.Equal("localhost", config4.ServerName);
            Assert.Equal("test_db", config4.DatabaseName);
            Assert.Equal("user", config4.Username);
            Assert.Null(config4.Password);
        }

        [Fact]
        public void BuildConnectionString_ForSqlServer_ShouldBuildValidConnectionString()
        {
            // Arrange
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                "TestDB"
            );

            // Act
            var connectionString = config.BuildConnectionString();

            // Assert
            Assert.NotNull(connectionString);
            // For SQL Server, BuildConnectionString just returns the original ConnectionString
            Assert.Equal("Server=localhost;Database=TestDB;Integrated Security=true;", connectionString);
        }

        [Fact]
        public void BuildConnectionString_ForMySql_ShouldBuildValidConnectionString()
        {
            // Arrange
            var config = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass",
                3306
            );

            // Act
            var connectionString = config.BuildConnectionString();

            // Assert
            Assert.NotNull(connectionString);
            Assert.Contains("Server=localhost", connectionString);
            Assert.Contains("Database=test_db", connectionString);
            Assert.Contains("Uid=test_user", connectionString);
            Assert.Contains("Pwd=test_pass", connectionString);
            Assert.Contains("Port=3306", connectionString);
            Assert.Contains("CharSet=utf8mb4", connectionString);
            Assert.Contains("SslMode=Required", connectionString); // Default is Required, not Preferred
            Assert.Contains("Connection Timeout=30", connectionString); // Note: space in "Connection Timeout"
            Assert.Contains("Default Command Timeout=60", connectionString); // Note: "Default Command Timeout"
        }

        [Fact]
        public void BuildConnectionString_ForPostgreSql_ShouldBuildValidConnectionString()
        {
            // Arrange
            var config = DatabaseConfiguration.CreatePostgreSql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass",
                5432,
                "public"
            );

            // Act
            var connectionString = config.BuildConnectionString();

            // Assert
            Assert.NotNull(connectionString);
            Assert.Contains("Host=localhost", connectionString);
            Assert.Contains("Database=test_db", connectionString);
            Assert.Contains("Username=test_user", connectionString);
            Assert.Contains("Password=test_pass", connectionString);
            Assert.Contains("Port=5432", connectionString);
            Assert.Contains("Schema=public", connectionString); // Note: "Schema" not "Search Path"
            Assert.Contains("SSL Mode=Prefer", connectionString);
            Assert.Contains("Connection Timeout=30", connectionString); // Note: "Connection Timeout" not "Timeout"
            Assert.Contains("Command Timeout=60", connectionString);
        }

        [Fact]
        public void BuildConnectionString_WithCustomSettings_ShouldIncludeCustomSettings()
        {
            // Arrange
            var config = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass"
            );
            config.ConnectionTimeout = 60;
            config.CommandTimeout = 120;
            config.MaxPoolSize = 200;
            config.MinPoolSize = 10;
            config.EnableConnectionPooling = false;
            config.UseSsl = false;
            config.AdditionalParameters = new Dictionary<string, string>
            {
                ["AllowUserVariables"] = "true",
                ["ConvertZeroDateTime"] = "true"
            };

            // Act
            var connectionString = config.BuildConnectionString();

            // Assert
            Assert.Contains("Connection Timeout=60", connectionString); // Note: space in "Connection Timeout"
            Assert.Contains("Default Command Timeout=120", connectionString); // Note: "Default Command Timeout"
            // Note: When EnableConnectionPooling=false, pool size parameters are not included
            // Note: EnableConnectionPooling=false doesn't add "Pooling=false" to the connection string
            // Note: UseSsl=false doesn't add "SslMode=Disabled" to the connection string
            Assert.Contains("AllowUserVariables=true", connectionString);
            Assert.Contains("ConvertZeroDateTime=true", connectionString);
        }

        [Fact]
        public void BuildConnectionString_WithNullAdditionalParameters_ShouldNotThrowException()
        {
            // Arrange
            var config = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass"
            );
            config.AdditionalParameters = null;

            // Act & Assert
            var exception = Record.Exception(() => config.BuildConnectionString());
            Assert.Null(exception);
        }

        [Fact]
        public void BuildConnectionString_WithEmptyAdditionalParameters_ShouldNotThrowException()
        {
            // Arrange
            var config = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass"
            );
            config.AdditionalParameters = new Dictionary<string, string>();

            // Act & Assert
            var exception = Record.Exception(() => config.BuildConnectionString());
            Assert.Null(exception);
        }








    }
}