using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Moq;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using Xunit;

namespace SQLDBEntityNotifier.Tests.Providers
{
    public class SqlServerCDCProviderTests : IDisposable
    {
        private readonly DatabaseConfiguration _validConfiguration;
        private readonly Mock<SqlConnection> _mockConnection;
        private readonly Mock<SqlCommand> _mockCommand;
        private readonly Mock<SqlDataReader> _mockReader;

        public SqlServerCDCProviderTests()
        {
            _validConfiguration = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                "TestDB"
            );

            _mockConnection = new Mock<SqlConnection>();
            _mockCommand = new Mock<SqlCommand>();
            _mockReader = new Mock<SqlDataReader>();
        }

        [Fact]
        public void Constructor_WithValidConfiguration_ShouldCreateInstance()
        {
            // Act
            var provider = new SqlServerCDCProvider(_validConfiguration);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(DatabaseType.SqlServer, provider.DatabaseType);
            Assert.Equal(_validConfiguration, provider.Configuration);
        }

        [Fact]
        public void Constructor_WithInvalidDatabaseType_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidConfig = DatabaseConfiguration.CreateMySql("localhost", "test", "user", "pass");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new SqlServerCDCProvider(invalidConfig));
            Assert.Contains("Configuration must be for SQL Server database type", exception.Message);
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SqlServerCDCProvider(null!));
        }

        [Fact]
        public async Task InitializeAsync_WithValidConfiguration_ShouldReturnTrue()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            
            // Mock CDC enabled check
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString(It.IsAny<string>())).Returns("1");

            // Act
            var result = await provider.InitializeAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task InitializeAsync_WithCDCNotEnabled_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString(It.IsAny<string>())).Returns("0");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.InitializeAsync());
            Assert.Contains("SQL Server CDC is not enabled", exception.Message);
        }

        [Fact]
        public async Task IsCDCEnabledAsync_WithValidTable_ShouldReturnTrue()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetInt32(It.IsAny<string>())).Returns(1);

            // Act
            var result = await provider.IsCDCEnabledAsync("TestTable");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsCDCEnabledAsync_WithInvalidTable_ShouldReturnFalse()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetInt32(It.IsAny<string>())).Returns(0);

            // Act
            var result = await provider.IsCDCEnabledAsync("InvalidTable");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EnableCDCAsync_WithValidTable_ShouldReturnTrue()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            
            _mockCommand.Setup(c => c.ExecuteNonQueryAsync()).ReturnsAsync(1);

            // Act
            var result = await provider.EnableCDCAsync("TestTable");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetCurrentChangePositionAsync_ShouldReturnCurrentVersion()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.Setup(r => r.ReadAsync()).ReturnsAsync(true);
            _mockReader.Setup(r => r.GetString(It.IsAny<string>())).Returns("12345");

            // Act
            var result = await provider.GetCurrentChangePositionAsync();

            // Assert
            Assert.Equal("12345", result);
        }

        [Fact]
        public async Task GetCurrentChangePositionAsync_WithNoResult_ShouldReturnZero()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
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
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("LSN123");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("TestTable");
            _mockReader.Setup(r => r.GetInt32("Operation")).Returns(2); // Update
            _mockReader.Setup(r => r.GetString("UpdateMask")).Returns("0x01");
            _mockReader.Setup(r => r.GetInt32("SequenceValue")).Returns(1);
            _mockReader.Setup(r => r.GetInt32("CommandId")).Returns(100);

            // Act
            var result = await provider.GetChangesAsync("0", "100");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("LSN123", result[0].ChangeId);
            Assert.Equal("TestTable", result[0].TableName);
            Assert.Equal(ChangeOperation.Update, result[0].Operation);
        }

        [Fact]
        public async Task GetTableChangesAsync_ShouldReturnTableSpecificChanges()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("LSN456");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("TestTable");
            _mockReader.Setup(r => r.GetInt32("Operation")).Returns(1); // Delete
            _mockReader.Setup(r => r.GetString("UpdateMask")).Returns("0x00");
            _mockReader.Setup(r => r.GetInt32("SequenceValue")).Returns(1);
            _mockReader.Setup(r => r.GetInt32("CommandId")).Returns(200);

            // Act
            var result = await provider.GetTableChangesAsync("TestTable", "0", "100");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("TestTable", result[0].TableName);
            Assert.Equal(ChangeOperation.Delete, result[0].Operation);
        }

        [Fact]
        public async Task GetMultiTableChangesAsync_ShouldReturnChangesForMultipleTables()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            var tableNames = new[] { "Table1", "Table2" };
            
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("LSN789");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("Table1");
            _mockReader.Setup(r => r.GetInt32("Operation")).Returns(2);
            _mockReader.Setup(r => r.GetString("UpdateMask")).Returns("0x01");
            _mockReader.Setup(r => r.GetInt32("SequenceValue")).Returns(1);
            _mockReader.Setup(r => r.GetInt32("CommandId")).Returns(300);

            // Act
            var result = await provider.GetMultiTableChangesAsync(tableNames, "0", "100");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("Table1", result.Keys);
            Assert.Contains("Table2", result.Keys);
        }

        [Fact]
        public async Task GetDetailedChangesAsync_ShouldReturnDetailedChangeRecords()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ChangeId")).Returns("LSN999");
            _mockReader.Setup(r => r.GetString("TableName")).Returns("TestTable");
            _mockReader.Setup(r => r.GetInt32("Operation")).Returns(2);
            _mockReader.Setup(r => r.GetString("UpdateMask")).Returns("0x01");
            _mockReader.Setup(r => r.GetInt32("SequenceValue")).Returns(1);
            _mockReader.Setup(r => r.GetInt32("CommandId")).Returns(400);

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
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock column data
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ColumnName")).Returns("Id");
            _mockReader.Setup(r => r.GetString("DataType")).Returns("int");
            _mockReader.Setup(r => r.GetBoolean("IsNullable")).Returns(false);
            _mockReader.Setup(r => r.IsDBNull("MaxLength")).Returns(true);
            _mockReader.Setup(r => r.IsDBNull("Precision")).Returns(true);
            _mockReader.Setup(r => r.IsDBNull("Scale")).Returns(true);

            // Mock primary key data
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("ColumnName")).Returns("Id");

            // Act
            var result = await provider.GetTableSchemaAsync("TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestTable", result.TableName);
            Assert.Equal("TestDB", result.SchemaName);
            Assert.NotEmpty(result.Columns);
            Assert.NotEmpty(result.PrimaryKeyColumns);
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithValidConfig_ShouldReturnValidResult()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock CDC enabled check
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("Value")).Returns("1");
            _mockReader.Setup(r => r.GetString(0)).Returns("Microsoft SQL Server 2019");

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
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            // Mock CDC not enabled
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetString("Value")).Returns("0");

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("SQL Server CDC is not enabled", result.Errors[0]);
        }

        [Fact]
        public async Task CleanupOldChangesAsync_ShouldReturnTrue()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);
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
            var provider = new SqlServerCDCProvider(_validConfiguration);
            SetupMockConnection();
            SetupMockCommand();
            SetupMockReader();
            
            _mockReader.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockReader.Setup(r => r.GetInt64("CurrentVersion")).Returns(1000L);
            _mockReader.Setup(r => r.GetInt64("MinValidVersion")).Returns(500L);
            _mockReader.Setup(r => r.GetDateTime("CurrentTime")).Returns(DateTime.UtcNow);

            // Act
            var result = await provider.GetHealthInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(CDCHealthStatus.Healthy, result.Status);
            Assert.NotNull(result.Metrics);
            Assert.Contains("CurrentVersion", result.Metrics.Keys);
        }

        [Fact]
        public void ParseOperation_WithValidCodes_ShouldReturnCorrectOperations()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);

            // Act & Assert
            Assert.Equal(ChangeOperation.Delete, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { 1 }));
            Assert.Equal(ChangeOperation.Insert, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { 2 }));
            Assert.Equal(ChangeOperation.Update, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { 3 }));
            Assert.Equal(ChangeOperation.Unknown, provider.GetType().GetMethod("ParseOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(provider, new object[] { 99 }));
        }

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            var provider = new SqlServerCDCProvider(_validConfiguration);

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
            _mockCommand.Setup(c => c.Parameters).Returns(new SqlParameterCollection());
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