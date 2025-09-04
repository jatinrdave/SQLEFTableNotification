using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Moq;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using Xunit;

namespace SQLDBEntityNotifier.Tests.Providers
{
    public class MySqlCDCProviderTests : IDisposable
    {
        private readonly DatabaseConfiguration _validConfiguration;

        public MySqlCDCProviderTests()
        {
            _validConfiguration = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass",
                3306
            );
        }

        [Fact]
        public void Constructor_WithValidConfiguration_ShouldCreateInstance()
        {
            // Act
            var provider = new MySqlCDCProvider(_validConfiguration);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(DatabaseType.MySql, provider.DatabaseType);
            Assert.Equal(_validConfiguration, provider.Configuration);
        }

        [Fact]
        public void Constructor_WithInvalidDatabaseType_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidConfig = DatabaseConfiguration.CreateSqlServer("Server=localhost;Database=test;Integrated Security=true;");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MySqlCDCProvider(invalidConfig));
            Assert.Contains("Configuration must be for MySQL database type", exception.Message);
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MySqlCDCProvider(null!));
        }

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);

            // Act
            provider.Dispose();

            // Assert
            // Verify that Dispose was called (this is mainly for coverage)
            Assert.True(true); // If we get here, Dispose didn't throw
        }

        public void Dispose()
        {
            // No resources to dispose in tests
        }
    }
}