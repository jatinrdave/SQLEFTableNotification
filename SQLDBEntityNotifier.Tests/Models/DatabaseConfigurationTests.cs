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
            Assert.Equal("localhost", config.ServerName);
            Assert.True(config.UseSsl);
            Assert.Equal(30, config.ConnectionTimeout);
            Assert.Equal(60, config.CommandTimeout);
            Assert.Equal(100, config.MaxPoolSize);
            Assert.Equal(0, config.MinPoolSize);
            Assert.True(config.EnableConnectionPooling);
        }

        [Fact]
        public void CreateSqlServer_WithNullConnectionString_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreateSqlServer(null!, "TestDB"));
        }

        [Fact]
        public void CreateSqlServer_WithEmptyConnectionString_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => DatabaseConfiguration.CreateSqlServer("", "TestDB"));
            Assert.Throws<ArgumentException>(() => DatabaseConfiguration.CreateSqlServer("   ", "TestDB"));
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
        public void CreateMySql_WithNullParameters_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreateMySql(null!, "test_db", "user", "pass"));
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreateMySql("localhost", null!, "user", "pass"));
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreateMySql("localhost", "test_db", null!, "pass"));
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", null!));
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
        public void CreatePostgreSql_WithNullParameters_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreatePostgreSql(null!, "test_db", "user", "pass"));
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreatePostgreSql("localhost", null!, "user", "pass"));
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreatePostgreSql("localhost", "test_db", null!, "pass"));
            Assert.Throws<ArgumentNullException>(() => DatabaseConfiguration.CreatePostgreSql("localhost", "test_db", "user", null!));
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
            Assert.Contains("Server=localhost", connectionString);
            Assert.Contains("Database=TestDB", connectionString);
            Assert.Contains("Integrated Security=true", connectionString);
            Assert.Contains("Connection Timeout=30", connectionString);
            Assert.Contains("Command Timeout=60", connectionString);
            Assert.Contains("Max Pool Size=100", connectionString);
            Assert.Contains("Min Pool Size=0", connectionString);
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
            Assert.Contains("SslMode=Preferred", connectionString);
            Assert.Contains("ConnectionTimeout=30", connectionString);
            Assert.Contains("CommandTimeout=60", connectionString);
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
            Assert.Contains("Search Path=public", connectionString);
            Assert.Contains("SSL Mode=Prefer", connectionString);
            Assert.Contains("Timeout=30", connectionString);
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
            Assert.Contains("ConnectionTimeout=60", connectionString);
            Assert.Contains("CommandTimeout=120", connectionString);
            Assert.Contains("Max Pool Size=200", connectionString);
            Assert.Contains("Min Pool Size=10", connectionString);
            Assert.Contains("Pooling=false", connectionString);
            Assert.Contains("SslMode=Disabled", connectionString);
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

        [Fact]
        public void Clone_ShouldCreateDeepCopy()
        {
            // Arrange
            var original = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass"
            );
            original.AdditionalParameters = new Dictionary<string, string>
            {
                ["TestParam"] = "TestValue"
            };

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.Equal(original.DatabaseType, cloned.DatabaseType);
            Assert.Equal(original.ServerName, cloned.ServerName);
            Assert.Equal(original.DatabaseName, cloned.DatabaseName);
            Assert.Equal(original.Username, cloned.Username);
            Assert.Equal(original.Password, cloned.Password);
            Assert.Equal(original.Port, cloned.Port);
            Assert.Equal(original.ConnectionTimeout, cloned.ConnectionTimeout);
            Assert.Equal(original.CommandTimeout, cloned.CommandTimeout);
            Assert.Equal(original.MaxPoolSize, cloned.MaxPoolSize);
            Assert.Equal(original.MinPoolSize, cloned.MinPoolSize);
            Assert.Equal(original.EnableConnectionPooling, cloned.EnableConnectionPooling);
            Assert.Equal(original.UseSsl, cloned.UseSsl);
            Assert.Equal(original.SslMode, cloned.SslMode);
            Assert.Equal(original.CharacterSet, cloned.CharacterSet);
            Assert.Equal(original.Collation, cloned.Collation);
            Assert.Equal(original.Timezone, cloned.Timezone);
            Assert.Equal(original.ApplicationName, cloned.ApplicationName);
            
            // AdditionalParameters should be a new dictionary
            Assert.NotNull(cloned.AdditionalParameters);
            Assert.NotSame(original.AdditionalParameters, cloned.AdditionalParameters);
            Assert.Equal(original.AdditionalParameters.Count, cloned.AdditionalParameters.Count);
            Assert.Equal(original.AdditionalParameters["TestParam"], cloned.AdditionalParameters["TestParam"]);
        }

        [Fact]
        public void Clone_WithNullAdditionalParameters_ShouldHandleCorrectly()
        {
            // Arrange
            var original = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass"
            );
            original.AdditionalParameters = null;

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.NotNull(cloned);
            Assert.Null(cloned.AdditionalParameters);
        }

        [Fact]
        public void ToString_ShouldReturnReadableRepresentation()
        {
            // Arrange
            var config = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass"
            );

            // Act
            var result = config.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("MySql", result);
            Assert.Contains("localhost", result);
            Assert.Contains("test_db", result);
            Assert.Contains("test_user", result);
        }

        [Fact]
        public void Equals_WithSameConfiguration_ShouldReturnTrue()
        {
            // Arrange
            var config1 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "pass");
            var config2 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "pass");

            // Act & Assert
            Assert.True(config1.Equals(config2));
            Assert.True(config1.Equals((object)config2));
        }

        [Fact]
        public void Equals_WithDifferentConfiguration_ShouldReturnFalse()
        {
            // Arrange
            var config1 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "pass");
            var config2 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "different_pass");

            // Act & Assert
            Assert.False(config1.Equals(config2));
            Assert.False(config1.Equals((object)config2));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var config = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "pass");

            // Act & Assert
            Assert.False(config.Equals(null));
            Assert.False(config.Equals((object)null));
        }

        [Fact]
        public void GetHashCode_WithSameConfiguration_ShouldReturnSameHashCode()
        {
            // Arrange
            var config1 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "pass");
            var config2 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "pass");

            // Act & Assert
            Assert.Equal(config1.GetHashCode(), config2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_WithDifferentConfiguration_ShouldReturnDifferentHashCode()
        {
            // Arrange
            var config1 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "pass");
            var config2 = DatabaseConfiguration.CreateMySql("localhost", "test_db", "user", "different_pass");

            // Act & Assert
            Assert.NotEqual(config1.GetHashCode(), config2.GetHashCode());
        }
    }
}