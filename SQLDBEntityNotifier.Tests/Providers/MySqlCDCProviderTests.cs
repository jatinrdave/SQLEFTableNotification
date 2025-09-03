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
        private readonly Mock<MySqlConnection> _mockConnection;
        private readonly Mock<MySqlCommand> _mockCommand;
        private readonly Mock<MySqlDataReader> _mockReader;

        public MySqlCDCProviderTests()
        {
            _validConfiguration = DatabaseConfiguration.CreateMySql(
                "localhost",
                "test_db",
                "test_user",
                "test_pass",
                3306
            );

            _mockConnection = new Mock<MySqlConnection>();
            _mockCommand = new Mock<MySqlCommand>();
            _mockReader = new Mock<MySqlDataReader>();
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
        public async Task InitializeAsync_WithValidConfiguration_ShouldReturnTrue()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            
            // Mock binary log enabled check
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString("Value")).Returns("ON");

            // Mock replication privilege check
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("GRANT REPLICATION SLAVE ON *.* TO 'test_user'@'%'");

            // Act
            var result = await provider.InitializeAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task InitializeAsync_WithBinaryLogDisabled_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString("Value")).Returns("OFF");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.InitializeAsync());
            Assert.Contains("MySQL binary logging is not enabled", exception.Message);
        }

        [Fact]
        public async Task InitializeAsync_WithNoReplicationPrivilege_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock binary log enabled
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString("Value")).Returns("ON");

            // Mock no replication privilege
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("GRANT SELECT ON *.* TO 'test_user'@'%'");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.InitializeAsync());
            Assert.Contains("MySQL user does not have REPLICATION SLAVE privilege", exception.Message);
        }

        [Fact]
        public async Task IsCDCEnabledAsync_WithValidTable_ShouldReturnTrue()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
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
            var provider = new MySqlCDCProvider(_validConfiguration);
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
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetInt32(0)).Returns(1);

            // Act
            var result = await provider.EnableCDCAsync("test_table");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetCurrentChangePositionAsync_ShouldReturnCurrentPosition()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString("Position")).Returns("12345");

            // Act
            var result = await provider.GetCurrentChangePositionAsync();

            // Assert
            Assert.Equal("12345", result);
        }

        [Fact]
        public async Task GetCurrentChangePositionAsync_WithNoResult_ShouldReturnZero()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(false);

            // Act
            var result = await provider.GetCurrentChangePositionAsync();

            // Assert
            Assert.Equal("0", result);
        }

        [Fact]
        public async Task GetChangesAsync_ShouldReturnChangeRecords()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("mysql_binlog");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("test_table");
            _mockReader.Setup(r => r.GetString("Operation")).Returns("insert");
            _mockReader.Setup(r => r.GetDateTime("ChangeTimestamp")).Returns(DateTime.UtcNow);
            _mockReader.Setup(r => r.GetString("ChangePosition")).Returns("12345");
            _mockReader.Setup(r => r.GetString("ChangedBy")).Returns("test_user");
            _mockReader.Setup(r => r.GetString("HostName")).Returns("localhost");

            // Act
            var result = await provider.GetChangesAsync("0", "100");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("mysql_binlog", result[0].ChangeId);
            Assert.Equal("test_table", result[0].TableName);
            Assert.Equal(ChangeOperation.Insert, result[0].Operation);
        }

        [Fact]
        public async Task GetTableChangesAsync_ShouldReturnTableSpecificChanges()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("mysql_binlog");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("test_table");
            _mockReader.Setup(r => r.GetString("Operation")).Returns("update");
            _mockReader.Setup(r => r.GetDateTime("ChangeTimestamp")).Returns(DateTime.UtcNow);
            _mockReader.Setup(r => r.GetString("ChangePosition")).Returns("12345");
            _mockReader.Setup(r => r.GetString("ChangedBy")).Returns("test_user");
            _mockReader.Setup(r => r.GetString("HostName")).Returns("localhost");

            // Act
            var result = await provider.GetTableChangesAsync("test_table", "0", "100");

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
            var provider = new MySqlCDCProvider(_validConfiguration);
            var tableNames = new[] { "table1", "table2" };
            
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("mysql_binlog");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("table1");
            _mockReader.Setup(r => r.GetString("Operation")).Returns("delete");
            _mockReader.Setup(r => r.GetDateTime("ChangeTimestamp")).Returns(DateTime.UtcNow);
            _mockReader.Setup(r => r.GetString("ChangePosition")).Returns("12345");
            _mockReader.Setup(r => r.GetString("ChangedBy")).Returns("test_user");
            _mockReader.Setup(r => r.GetString("HostName")).Returns("localhost");

            // Act
            var result = await provider.GetMultiTableChangesAsync(tableNames, "0", "100");

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
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("mysql_binlog");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("test_table");
            _mockReader.Setup(r => r.GetString("Operation")).Returns("insert");
            _mockReader.Setup(r => r.GetDateTime("ChangeTimestamp")).Returns(DateTime.UtcNow);
            _mockReader.Setup(r => r.GetString("ChangePosition")).Returns("12345");
            _mockReader.Setup(r => r.GetString("ChangedBy")).Returns("test_user");
            _mockReader.Setup(r => r.GetString("HostName")).Returns("localhost");

            // Act
            var result = await provider.GetDetailedChangesAsync("0", "100");

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
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock column data
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("COLUMN_NAME")).Returns("id");
            _mockReader.Setup(r => r.GetString("DATA_TYPE")).Returns("int");
            _mockReader.Setup(r => r.GetString("IS_NULLABLE")).Returns("NO");
            _mockReader.Setup(r => r.IsDBNull("CHARACTER_MAXIMUM_LENGTH")).Returns(true);
            _mockReader.Setup(r => r.IsDBNull("NUMERIC_PRECISION")).Returns(true);
            _mockReader.Setup(r => r.IsDBNull("NUMERIC_SCALE")).Returns(true);

            // Mock primary key data
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("COLUMN_NAME")).Returns("id");

            // Act
            var result = await provider.GetTableSchemaAsync("test_table");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_table", result.TableName);
            Assert.Equal("test_db", result.SchemaName);
            Assert.NotEmpty(result.Columns);
            Assert.NotEmpty(result.PrimaryKeyColumns);
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithValidConfig_ShouldReturnValidResult()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock binary log enabled
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString("Value")).Returns("ON");

            // Mock replication privilege
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("GRANT REPLICATION SLAVE ON *.* TO 'test_user'@'%'");

            // Mock server version
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("8.0.33");

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
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock binary log disabled
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString("Value")).Returns("OFF");

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("MySQL binary logging is not enabled", result.Errors[0]);
        }

        [Fact]
        public async Task CleanupOldChangesAsync_ShouldReturnTrue()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);

            // Act
            var result = await provider.CleanupOldChangesAsync(TimeSpan.FromDays(7));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetHealthInfoAsync_ShouldReturnHealthInfo()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock master status
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("Position")).Returns("12345");
            _mockReader.Setup(r => r.GetString("File")).Returns("mysql-bin.000001");

            // Mock server uptime
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            _mockReader.Setup(r => r.GetString(0)).Returns("3600");

            // Act
            var result = await provider.GetHealthInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(CDCHealthStatus.Healthy, result.Status);
            Assert.NotNull(result.Metrics);
            Assert.Contains("CurrentPosition", result.Metrics.Keys);
            Assert.Contains("CurrentFile", result.Metrics.Keys);
        }

        [Fact]
        public void ParseOperation_WithValidOperations_ShouldReturnCorrectOperations()
        {
            // Arrange
            var provider = new MySqlCDCProvider(_validConfiguration);

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
            var provider = new MySqlCDCProvider(_validConfiguration);

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
            _mockCommand.Setup(c => c.Parameters).Returns(new MySqlParameterCollection());
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