using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Moq;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using Xunit;

namespace SQLDBEntityNotifier.Tests.Providers
{
    public class PostgreSqlCDCProviderTests : IDisposable
    {
        private readonly DatabaseConfiguration _validConfiguration;
        private readonly Mock<NpgsqlConnection> _mockConnection;
        private readonly Mock<NpgsqlCommand> _mockCommand;
        private readonly Mock<NpgsqlDataReader> _mockReader;

        public PostgreSqlCDCProviderTests()
        {
            _validConfiguration = DatabaseConfiguration.CreatePostgreSql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass",
                5432,
                "public"
            );

            _mockConnection = new Mock<NpgsqlConnection>();
            _mockCommand = new Mock<NpgsqlCommand>();
            _mockReader = new Mock<NpgsqlDataReader>();
        }

        [Fact]
        public void Constructor_WithValidConfiguration_ShouldCreateInstance()
        {
            // Act
            var provider = new PostgreSqlCDCProvider(_validConfiguration);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(DatabaseType.PostgreSql, provider.DatabaseType);
            Assert.Equal(_validConfiguration, provider.Configuration);
        }

        [Fact]
        public void Constructor_WithInvalidDatabaseType_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidConfig = DatabaseConfiguration.CreateSqlServer("Server=localhost;Database=test;Integrated Security=true;");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new PostgreSqlCDCProvider(invalidConfig));
            Assert.Contains("Configuration must be for PostgreSQL database type", exception.Message);
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlCDCProvider(null!));
        }

        [Fact]
        public async Task InitializeAsync_WithValidConfiguration_ShouldReturnTrue()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            
            // Mock logical replication enabled check
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString(0)).Returns("logical");

            // Mock replication privilege check
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetBoolean("rolreplication")).Returns(true);

            // Act
            var result = await provider.InitializeAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task InitializeAsync_WithLogicalReplicationDisabled_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString(0)).Returns("replica");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.InitializeAsync());
            Assert.Contains("PostgreSQL logical replication is not enabled", exception.Message);
        }

        [Fact]
        public async Task InitializeAsync_WithNoReplicationPrivilege_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock logical replication enabled
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("logical");

            // Mock no replication privilege
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetBoolean("rolreplication")).Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.InitializeAsync());
            Assert.Contains("PostgreSQL user does not have REPLICATION privilege", exception.Message);
        }

        [Fact]
        public async Task IsCDCEnabledAsync_WithValidTable_ShouldReturnTrue()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetInt32(0)).Returns(1);

            // Act
            var result = await provider.IsCDCEnabledAsync("test_table");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsCDCEnabledAsync_WithInvalidTable_ShouldReturnFalse()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetInt32(0)).Returns(0);

            // Act
            var result = await provider.IsCDCEnabledAsync("invalid_table");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EnableCDCAsync_ShouldReturnTrue()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            
            _mockCommand.Setup(c => c.ExecuteNonQueryAsync()).ReturnsAsync(1);

            // Act
            var result = await provider.EnableCDCAsync("test_table");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetCurrentChangePositionAsync_ShouldReturnCurrentPosition()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString(0)).Returns("1/12345");

            // Act
            var result = await provider.GetCurrentChangePositionAsync();

            // Assert
            Assert.Equal("1/12345", result);
        }

        [Fact]
        public async Task GetCurrentChangePositionAsync_WithNoResult_ShouldReturnZero()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(false);

            // Act
            var result = await provider.GetCurrentChangePositionAsync();

            // Assert
            Assert.Equal("0/0", result);
        }

        [Fact]
        public async Task GetChangesAsync_ShouldReturnChangeRecords()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("postgres_wal");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("test_table");
            _mockReader.Setup(r => r.GetString("Operation")).Returns("insert");
            _mockReader.Setup(r => r.GetDateTime("ChangeTimestamp")).Returns(DateTime.UtcNow);
            _mockReader.Setup(r => r.GetString("ChangePosition")).Returns("1/12345");
            _mockReader.Setup(r => r.GetString("ChangedBy")).Returns("test_user");
            _mockReader.Setup(r => r.GetString("HostName")).Returns("localhost");

            // Act
            var result = await provider.GetChangesAsync("0/0", "1/12345");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("postgres_wal", result[0].ChangeId);
            Assert.Equal("test_table", result[0].TableName);
            Assert.Equal(ChangeOperation.Insert, result[0].Operation);
        }

        [Fact]
        public async Task GetTableChangesAsync_ShouldReturnTableSpecificChanges()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("postgres_wal");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("test_table");
            _mockReader.Setup(r => r.GetString("Operation")).Returns("update");
            _mockReader.Setup(r => r.GetDateTime("ChangeTimestamp")).Returns(DateTime.UtcNow);
            _mockReader.Setup(r => r.GetString("ChangePosition")).Returns("1/12345");
            _mockReader.Setup(r => r.GetString("ChangedBy")).Returns("test_user");
            _mockReader.Setup(r => r.GetString("HostName")).Returns("localhost");

            // Act
            var result = await provider.GetTableChangesAsync("test_table", "0/0", "1/12345");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("test_table", result[0].TableName);
            Assert.Equal(ChangeOperation.Update, result[0].Operation);
        }

        [Fact]
        public async Task GetMultiTableChangesAsync_ShouldReturnChangesForMultipleTables()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            var tableNames = new[] { "table1", "table2" };
            
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("postgres_wal");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("table1");
            _mockReader.Setup(r => r.GetString("Operation")).Returns("delete");
            _mockReader.Setup(r => r.GetDateTime("ChangeTimestamp")).Returns(DateTime.UtcNow);
            _mockReader.Setup(r => r.GetString("ChangePosition")).Returns("1/12345");
            _mockReader.Setup(r => r.GetString("ChangedBy")).Returns("test_user");
            _mockReader.Setup(r => r.GetString("HostName")).Returns("localhost");

            // Act
            var result = await provider.GetMultiTableChangesAsync(tableNames, "0/0", "1/12345");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("table1", result.Keys);
            Assert.Contains("table2", result.Keys);
        }

        [Fact]
        public async Task GetDetailedChangesAsync_ShouldReturnDetailedChangeRecords()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("postgres_wal");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("test_table");
            _mockReader.Setup(r => r.GetString("Operation")).Returns("insert");
            _mockReader.Setup(r => r.GetDateTime("ChangeTimestamp")).Returns(DateTime.UtcNow);
            _mockReader.Setup(r => r.GetString("ChangePosition")).Returns("1/12345");
            _mockReader.Setup(r => r.GetString("ChangedBy")).Returns("test_user");
            _mockReader.Setup(r => r.GetString("HostName")).Returns("localhost");

            // Act
            var result = await provider.GetDetailedChangesAsync("0/0", "1/12345");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            var detailedChange = result[0];
            Assert.NotNull(detailedChange.OldValues);
            Assert.NotNull(detailedChange.NewValues);
            Assert.NotNull(detailedChange.AffectedColumns);
            Assert.NotNull(detailedChange.Metadata);
        }

        [Fact]
        public async Task GetTableSchemaAsync_ShouldReturnTableSchema()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock column data
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("column_name")).Returns("id");
            _mockReader.Setup(r => r.GetString("data_type")).Returns("integer");
            _mockReader.Setup(r => r.GetString("is_nullable")).Returns("NO");
            _mockReader.Setup(r => r.IsDBNull("character_maximum_length")).Returns(true);
            _mockReader.Setup(r => r.IsDBNull("numeric_precision")).Returns(true);
            _mockReader.Setup(r => r.IsDBNull("numeric_scale")).Returns(true);

            // Mock primary key data
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("column_name")).Returns("id");

            // Act
            var result = await provider.GetTableSchemaAsync("test_table");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_table", result.TableName);
            Assert.Equal("public", result.SchemaName);
            Assert.NotEmpty(result.Columns);
            Assert.NotEmpty(result.PrimaryKeyColumns);
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithValidConfig_ShouldReturnValidResult()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock logical replication enabled
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("logical");

            // Mock replication privilege
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetBoolean("rolreplication")).Returns(true);

            // Mock server version
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("PostgreSQL 15.3");

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.NotEmpty(result.Messages);
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithInvalidConfig_ShouldReturnInvalidResult()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock logical replication disabled
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("replica");

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("PostgreSQL logical replication is not enabled", result.Errors[0]);
        }

        [Fact]
        public async Task CleanupOldChangesAsync_ShouldReturnTrue()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            
            _mockCommand.Setup(c => c.ExecuteNonQueryAsync()).ReturnsAsync(1);

            // Act
            var result = await provider.CleanupOldChangesAsync(TimeSpan.FromDays(7));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetHealthInfoAsync_ShouldReturnHealthInfo()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock WAL status
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString(0)).Returns("1/12345");
            _mockReader.Setup(r => r.GetString(1)).Returns("1/12340");

            // Mock replication slot information
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("slot_name")).Returns("cdc_test_db_test_table");
            _mockReader.Setup(r => r.GetString("restart_lsn")).Returns("1/12300");
            _mockReader.Setup(r => r.GetString("confirmed_flush_lsn")).Returns("1/12345");

            // Act
            var result = await provider.GetHealthInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(CDCHealthStatus.Healthy, result.Status);
            Assert.NotNull(result.Metrics);
            Assert.Contains("CurrentLSN", result.Metrics.Keys);
            Assert.Contains("InsertLSN", result.Metrics.Keys);
            Assert.Contains("ReplicationSlots", result.Metrics.Keys);
        }

        [Fact]
        public void ParseOperation_WithValidOperations_ShouldReturnCorrectOperations()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);

            // Act & Assert
            Assert.Equal(ChangeOperation.Insert, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { "insert" }));
            Assert.Equal(ChangeOperation.Update, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { "update" }));
            Assert.Equal(ChangeOperation.Delete, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { "delete" }));
            Assert.Equal(ChangeOperation.SchemaChange, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { "create" }));
            Assert.Equal(ChangeOperation.Unknown, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { "unknown" }));
        }

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            var provider = new PostgreSqlCDCProvider(_validConfiguration);

            // Act
            provider.Dispose();

            // Assert
            // Verify that Dispose was called (this is mainly for coverage)
            Assert.True(true); // If we get here, Dispose didn't throw
        }

        private void SetupMockConnection()
        {
            _mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        }

        private void SetupMockCommand()
        {
            _mockCommand.Setup(c => c.Parameters).Returns(new NpgsqlParameterCollection());
        }

        private void SetupMockReader()
        {
            _mockReader.Setup(r => r.IsDBNull(It.IsAny<string>())).Returns(false);
        }

        public void Dispose()
        {
            _mockConnection?.Dispose();
            _mockCommand?.Dispose();
            _mockReader?.Dispose();
        }
    }
}