using System;
using System.Collections.Generic;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using Xunit;

namespace SQLDBEntityNotifier.Tests.Providers
{
    public class CDCProviderFactoryTests
    {
        [Fact]
        public void CreateProvider_WithSqlServerConfiguration_ShouldReturnSqlServerProvider()
        {
            // Arrange
            var config = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                "TestDB"
            );

            // Act
            var provider = CDCProviderFactory.CreateProvider(config);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<SqlServerCDCProvider>(provider);
            Assert.Equal(DatabaseType.SqlServer, provider.DatabaseType);
            Assert.Equal(config, provider.Configuration);
        }

        [Fact]
        public void CreateProvider_WithMySqlConfiguration_ShouldReturnMySqlProvider()
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
            var provider = CDCProviderFactory.CreateProvider(config);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<MySqlCDCProvider>(provider);
            Assert.Equal(DatabaseType.MySql, provider.DatabaseType);
            Assert.Equal(config, provider.Configuration);
        }

        [Fact]
        public void CreateProvider_WithPostgreSqlConfiguration_ShouldReturnPostgreSqlProvider()
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
            var provider = CDCProviderFactory.CreateProvider(config);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<PostgreSqlCDCProvider>(provider);
            Assert.Equal(DatabaseType.PostgreSql, provider.DatabaseType);
            Assert.Equal(config, provider.Configuration);
        }

        [Fact]
        public void CreateProvider_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreateProvider(null!));
        }

        [Fact]
        public void CreateProvider_WithUnsupportedDatabaseType_ShouldThrowNotSupportedException()
        {
            // Arrange
            var config = new DatabaseConfiguration
            {
                DatabaseType = (DatabaseType)999, // Invalid database type
                ServerName = "localhost",
                DatabaseName = "test"
            };

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => CDCProviderFactory.CreateProvider(config));
            Assert.Contains("Database type 999 is not supported", exception.Message);
        }

        [Fact]
        public void CreateSqlServerProvider_WithValidParameters_ShouldReturnSqlServerProvider()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;Integrated Security=true;";
            var databaseName = "TestDB";

            // Act
            var provider = CDCProviderFactory.CreateSqlServerProvider(connectionString, databaseName);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<SqlServerCDCProvider>(provider);
            Assert.Equal(DatabaseType.SqlServer, provider.DatabaseType);
            Assert.Equal(connectionString, provider.Configuration.ConnectionString);
            Assert.Equal(databaseName, provider.Configuration.DatabaseName);
        }

        [Fact]
        public void CreateSqlServerProvider_WithNullConnectionString_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreateSqlServerProvider(null!, "TestDB"));
        }

        [Fact]
        public void CreateMySqlProvider_WithValidParameters_ShouldReturnMySqlProvider()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";
            var port = 3306;

            // Act
            var provider = CDCProviderFactory.CreateMySqlProvider(serverName, databaseName, username, password, port);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<MySqlCDCProvider>(provider);
            Assert.Equal(DatabaseType.MySql, provider.DatabaseType);
            Assert.Equal(serverName, provider.Configuration.ServerName);
            Assert.Equal(databaseName, provider.Configuration.DatabaseName);
            Assert.Equal(username, provider.Configuration.Username);
            Assert.Equal(password, provider.Configuration.Password);
            Assert.Equal(port, provider.Configuration.Port);
        }

        [Fact]
        public void CreateMySqlProvider_WithDefaultPort_ShouldUseDefaultPort()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";

            // Act
            var provider = CDCProviderFactory.CreateMySqlProvider(serverName, databaseName, username, password);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(3306, provider.Configuration.Port);
        }

        [Fact]
        public void CreateMySqlProvider_WithNullParameters_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreateMySqlProvider(null!, "test_db", "user", "pass"));
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreateMySqlProvider("localhost", null!, "user", "pass"));
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreateMySqlProvider("localhost", "test_db", null!, "pass"));
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreateMySqlProvider("localhost", "test_db", "user", null!));
        }

        [Fact]
        public void CreatePostgreSqlProvider_WithValidParameters_ShouldReturnPostgreSqlProvider()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";
            var port = 5432;
            var schemaName = "public";

            // Act
            var provider = CDCProviderFactory.CreatePostgreSqlProvider(serverName, databaseName, username, password, port, schemaName);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<PostgreSqlCDCProvider>(provider);
            Assert.Equal(DatabaseType.PostgreSql, provider.DatabaseType);
            Assert.Equal(serverName, provider.Configuration.ServerName);
            Assert.Equal(databaseName, provider.Configuration.DatabaseName);
            Assert.Equal(username, provider.Configuration.Username);
            Assert.Equal(password, provider.Configuration.Password);
            Assert.Equal(port, provider.Configuration.Port);
            Assert.Equal(schemaName, provider.Configuration.SchemaName);
        }

        [Fact]
        public void CreatePostgreSqlProvider_WithDefaultParameters_ShouldUseDefaults()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";

            // Act
            var provider = CDCProviderFactory.CreatePostgreSqlProvider(serverName, databaseName, username, password);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(5432, provider.Configuration.Port);
            Assert.Equal("public", provider.Configuration.SchemaName);
        }

        [Fact]
        public void CreatePostgreSqlProvider_WithNullParameters_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreatePostgreSqlProvider(null!, "test_db", "user", "pass"));
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreatePostgreSqlProvider("localhost", null!, "user", "pass"));
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreatePostgreSqlProvider("localhost", "test_db", null!, "pass"));
            Assert.Throws<ArgumentNullException>(() => CDCProviderFactory.CreatePostgreSqlProvider("localhost", "test_db", "user", null!));
        }

        [Fact]
        public void CreateProviderFromConnectionString_WithSqlServerConnection_ShouldReturnSqlServerProvider()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;Integrated Security=true;";

            // Act
            var provider = CDCProviderFactory.CreateProviderFromConnectionString(connectionString);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<SqlServerCDCProvider>(provider);
            Assert.Equal(DatabaseType.SqlServer, provider.DatabaseType);
        }

        [Fact]
        public void CreateProviderFromConnectionString_WithMySqlConnection_ShouldReturnMySqlProvider()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=test_db;Uid=test_user;Pwd=test_pass;";

            // Act
            var provider = CDCProviderFactory.CreateProviderFromConnectionString(connectionString);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<MySqlCDCProvider>(provider);
            Assert.Equal(DatabaseType.MySql, provider.DatabaseType);
        }

        [Fact]
        public void CreateProviderFromConnectionString_WithPostgreSqlConnection_ShouldReturnPostgreSqlProvider()
        {
            // Arrange
            var connectionString = "Host=localhost;Database=test_db;Username=test_user;Password=test_pass;";

            // Act
            var provider = CDCProviderFactory.CreateProviderFromConnectionString(connectionString);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<PostgreSqlCDCProvider>(provider);
            Assert.Equal(DatabaseType.PostgreSql, provider.DatabaseType);
        }

        [Fact]
        public void CreateProviderFromConnectionString_WithUnrecognizedConnection_ShouldThrowNotSupportedException()
        {
            // Arrange
            var connectionString = "UnknownFormat=localhost;";

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => CDCProviderFactory.CreateProviderFromConnectionString(connectionString));
            Assert.Contains("Unable to determine database type from connection string", exception.Message);
        }

        [Fact]
        public void CreateProviderFromConnectionString_WithNullConnectionString_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => CDCProviderFactory.CreateProviderFromConnectionString(null!));
            Assert.Contains("Connection string cannot be null or empty", exception.Message);
        }

        [Fact]
        public void CreateProviderFromConnectionString_WithEmptyConnectionString_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => CDCProviderFactory.CreateProviderFromConnectionString(""));
            Assert.Contains("Connection string cannot be null or empty", exception.Message);
        }

        [Fact]
        public void CreateProviderFromConnectionString_WithWhitespaceConnectionString_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => CDCProviderFactory.CreateProviderFromConnectionString("   "));
            Assert.Contains("Connection string cannot be null or empty", exception.Message);
        }

        [Theory]
        [InlineData("Server=localhost;Database=TestDB;Integrated Security=true;", DatabaseType.SqlServer)]
        [InlineData("Data Source=localhost;Initial Catalog=TestDB;Integrated Security=true;", DatabaseType.SqlServer)]
        [InlineData("Server=localhost;Database=test_db;Uid=user;Pwd=pass;", DatabaseType.MySql)]
        [InlineData("Host=localhost;Database=test_db;Username=user;Password=pass;", DatabaseType.PostgreSql)]
        public void CreateProviderFromConnectionString_WithVariousFormats_ShouldDetectCorrectDatabaseType(string connectionString, DatabaseType expectedType)
        {
            // Act
            var provider = CDCProviderFactory.CreateProviderFromConnectionString(connectionString);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(expectedType, provider.DatabaseType);
        }

        [Fact]
        public void FactoryMethods_ShouldCreateConsistentConfigurations()
        {
            // Arrange
            var serverName = "localhost";
            var databaseName = "test_db";
            var username = "test_user";
            var password = "test_pass";

            // Act
            var mySqlProvider = CDCProviderFactory.CreateMySqlProvider(serverName, databaseName, username, password);
            var postgreSqlProvider = CDCProviderFactory.CreatePostgreSqlProvider(serverName, databaseName, username, password);

            // Assert
            Assert.Equal(serverName, mySqlProvider.Configuration.ServerName);
            Assert.Equal(databaseName, mySqlProvider.Configuration.DatabaseName);
            Assert.Equal(username, mySqlProvider.Configuration.Username);
            Assert.Equal(password, mySqlProvider.Configuration.Password);

            Assert.Equal(serverName, postgreSqlProvider.Configuration.ServerName);
            Assert.Equal(databaseName, postgreSqlProvider.Configuration.DatabaseName);
            Assert.Equal(username, postgreSqlProvider.Configuration.Username);
            Assert.Equal(password, postgreSqlProvider.Configuration.Password);
        }
    }
}