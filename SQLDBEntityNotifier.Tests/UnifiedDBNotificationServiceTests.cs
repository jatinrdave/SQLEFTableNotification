using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using Xunit;

namespace SQLDBEntityNotifier.Tests
{
    public class UnifiedDBNotificationServiceTests : IDisposable
    {
        private readonly Mock<ICDCProvider> _mockCDCProvider;
        private readonly DatabaseConfiguration _validConfiguration;
        private readonly string _tableName = "TestTable";

        public UnifiedDBNotificationServiceTests()
        {
            _mockCDCProvider = new Mock<ICDCProvider>();
            _validConfiguration = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                "TestDB"
            );

            // Setup default mock behavior
            _mockCDCProvider.Setup(p => p.DatabaseType).Returns(DatabaseType.SqlServer);
            _mockCDCProvider.Setup(p => p.Configuration).Returns(_validConfiguration);
            _mockCDCProvider.Setup(p => p.InitializeAsync()).ReturnsAsync(true);
            _mockCDCProvider.Setup(p => p.IsCDCEnabledAsync(_tableName)).ReturnsAsync(true);
            _mockCDCProvider.Setup(p => p.EnableCDCAsync(_tableName)).ReturnsAsync(true);
            _mockCDCProvider.Setup(p => p.GetCurrentChangePositionAsync()).ReturnsAsync("0");
        }

        [Fact]
        public void Constructor_WithCDCProvider_ShouldCreateInstance()
        {
            // Act
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);

            // Assert
            Assert.NotNull(service);
            Assert.Equal(_tableName, service.TableName);
            Assert.NotNull(service.OnChanged);
            Assert.NotNull(service.OnError);
            Assert.NotNull(service.OnHealthCheck);
        }

        [Fact]
        public void Constructor_WithConfiguration_ShouldCreateInstance()
        {
            // Act
            var service = new UnifiedDBNotificationService<TestEntity>(_validConfiguration, _tableName);

            // Assert
            Assert.NotNull(service);
            Assert.Equal(_tableName, service.TableName);
            Assert.NotNull(service.OnChanged);
            Assert.NotNull(service.OnError);
            Assert.NotNull(service.OnHealthCheck);
        }

        [Fact]
        public void Constructor_WithNullCDCProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UnifiedDBNotificationService<TestEntity>(null!, _tableName));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UnifiedDBNotificationService<TestEntity>((DatabaseConfiguration)null!, _tableName));
        }

        [Fact]
        public void Constructor_WithNullTableName_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, null!));
            Assert.Throws<ArgumentException>(() => new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, ""));
            Assert.Throws<ArgumentException>(() => new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, "   "));
        }

        [Fact]
        public void Constructor_WithCustomPollingInterval_ShouldUseCustomInterval()
        {
            // Arrange
            var customInterval = TimeSpan.FromSeconds(30);

            // Act
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName, customInterval);

            // Assert
            Assert.Equal(customInterval, service.PollingInterval);
        }

        [Fact]
        public void Constructor_WithDefaultPollingInterval_ShouldUseDefaultInterval()
        {
            // Act
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(60), service.PollingInterval);
        }

        [Fact]
        public async Task StartMonitoringAsync_WithValidSetup_ShouldStartSuccessfully()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);

            // Act
            await service.StartMonitoringAsync();

            // Assert
            Assert.True(service.IsMonitoring);
            _mockCDCProvider.Verify(p => p.InitializeAsync(), Times.Once);
            _mockCDCProvider.Verify(p => p.IsCDCEnabledAsync(_tableName), Times.Once);
            _mockCDCProvider.Verify(p => p.EnableCDCAsync(_tableName), Times.Once);
        }

        [Fact]
        public async Task StartMonitoringAsync_WithCDCNotEnabled_ShouldEnableCDC()
        {
            // Arrange
            _mockCDCProvider.Setup(p => p.IsCDCEnabledAsync(_tableName)).ReturnsAsync(false);
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);

            // Act
            await service.StartMonitoringAsync();

            // Assert
            _mockCDCProvider.Verify(p => p.EnableCDCAsync(_tableName), Times.Once);
        }

        [Fact]
        public async Task StartMonitoringAsync_WithInitializationFailure_ShouldThrowException()
        {
            // Arrange
            _mockCDCProvider.Setup(p => p.InitializeAsync()).ReturnsAsync(false);
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.StartMonitoringAsync());
            Assert.Contains("Failed to initialize CDC provider", exception.Message);
        }

        [Fact]
        public async Task StartMonitoringAsync_WhenAlreadyMonitoring_ShouldNotStartAgain()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            await service.StartMonitoringAsync();

            // Act
            await service.StartMonitoringAsync();

            // Assert
            _mockCDCProvider.Verify(p => p.InitializeAsync(), Times.Once);
        }

        [Fact]
        public void StopMonitoring_WhenMonitoring_ShouldStopSuccessfully()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            service.StartMonitoringAsync().Wait();

            // Act
            service.StopMonitoring();

            // Assert
            Assert.False(service.IsMonitoring);
        }

        [Fact]
        public void StopMonitoring_WhenNotMonitoring_ShouldNotThrowException()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);

            // Act & Assert
            var exception = Record.Exception(() => service.StopMonitoring());
            Assert.Null(exception);
        }

        [Fact]
        public async Task PollForChangesAsync_WithChanges_ShouldRaiseOnChangedEvent()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            var changes = new List<ChangeRecord>
            {
                new ChangeRecord
                {
                    ChangeId = "1",
                    TableName = _tableName,
                    Operation = ChangeOperation.Insert,
                    ChangeTimestamp = DateTime.UtcNow,
                    ChangePosition = "1"
                }
            };

            _mockCDCProvider.Setup(p => p.GetTableChangesAsync(_tableName, It.IsAny<string>(), null))
                .ReturnsAsync(changes);

            bool eventRaised = false;
            service.OnChanged += (sender, e) =>
            {
                eventRaised = true;
                Assert.NotNull(e.Entities);
                Assert.NotEmpty(e.Entities);
            };

            await service.StartMonitoringAsync();

            // Act
            await service.PollForChangesAsync();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public async Task PollForChangesAsync_WithNoChanges_ShouldNotRaiseOnChangedEvent()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            _mockCDCProvider.Setup(p => p.GetTableChangesAsync(_tableName, It.IsAny<string>(), null))
                .ReturnsAsync(new List<ChangeRecord>());

            bool eventRaised = false;
            service.OnChanged += (sender, e) => eventRaised = true;

            await service.StartMonitoringAsync();

            // Act
            await service.PollForChangesAsync();

            // Assert
            Assert.False(eventRaised);
        }

        [Fact]
        public async Task PollForChangesAsync_WithError_ShouldRaiseOnErrorEvent()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            _mockCDCProvider.Setup(p => p.GetTableChangesAsync(_tableName, It.IsAny<string>(), null))
                .ThrowsAsync(new Exception("Test error"));

            bool errorEventRaised = false;
            service.OnError += (sender, e) =>
            {
                errorEventRaised = true;
                Assert.Contains("Test error", e.Message);
            };

            await service.StartMonitoringAsync();

            // Act
            await service.PollForChangesAsync();

            // Assert
            Assert.True(errorEventRaised);
        }

        [Fact]
        public async Task GetMultiTableChangesAsync_ShouldReturnChangesForMultipleTables()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            var tableNames = new[] { "Table1", "Table2" };
            var changes = new Dictionary<string, List<ChangeRecord>>
            {
                ["Table1"] = new List<ChangeRecord>
                {
                    new ChangeRecord { ChangeId = "1", TableName = "Table1", Operation = ChangeOperation.Insert }
                },
                ["Table2"] = new List<ChangeRecord>
                {
                    new ChangeRecord { ChangeId = "2", TableName = "Table2", Operation = ChangeOperation.Update }
                }
            };

            _mockCDCProvider.Setup(p => p.GetMultiTableChangesAsync(tableNames, It.IsAny<string>(), null))
                .ReturnsAsync(changes);

            // Act
            var result = await service.GetMultiTableChangesAsync(tableNames);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("Table1", result.Keys);
            Assert.Contains("Table2", result.Keys);
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldReturnValidationResult()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            var validationResult = new CDCValidationResult
            {
                IsValid = true,
                Messages = new List<string> { "Configuration is valid" }
            };

            _mockCDCProvider.Setup(p => p.ValidateConfigurationAsync()).ReturnsAsync(validationResult);

            // Act
            var result = await service.ValidateConfigurationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Contains("Configuration is valid", result.Messages);
        }

        [Fact]
        public async Task GetHealthInfoAsync_ShouldReturnHealthInfo()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            var healthInfo = new CDCHealthInfo
            {
                Status = CDCHealthStatus.Healthy,
                Metrics = new Dictionary<string, object>
                {
                    ["CurrentPosition"] = "100",
                    ["LastCheck"] = DateTime.UtcNow
                }
            };

            _mockCDCProvider.Setup(p => p.GetHealthInfoAsync()).ReturnsAsync(healthInfo);

            // Act
            var result = await service.GetHealthInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(CDCHealthStatus.Healthy, result.Status);
            Assert.Contains("CurrentPosition", result.Metrics.Keys);
        }

        [Fact]
        public async Task CleanupOldChangesAsync_ShouldReturnTrue()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            var retentionPeriod = TimeSpan.FromDays(7);

            _mockCDCProvider.Setup(p => p.CleanupOldChangesAsync(retentionPeriod)).ReturnsAsync(true);

            // Act
            var result = await service.CleanupOldChangesAsync(retentionPeriod);

            // Assert
            Assert.True(result);
            _mockCDCProvider.Verify(p => p.CleanupOldChangesAsync(retentionPeriod), Times.Once);
        }

        [Fact]
        public async Task GetTableSchemaAsync_ShouldReturnTableSchema()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            var tableSchema = new TableSchema
            {
                TableName = _tableName,
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Id", DataType = "int", IsNullable = false }
                },
                PrimaryKeyColumns = new List<string> { "Id" }
            };

            _mockCDCProvider.Setup(p => p.GetTableSchemaAsync(_tableName)).ReturnsAsync(tableSchema);

            // Act
            var result = await service.GetTableSchemaAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_tableName, result.TableName);
            Assert.Equal("dbo", result.SchemaName);
            Assert.NotEmpty(result.Columns);
            Assert.NotEmpty(result.PrimaryKeyColumns);
        }

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);

            // Act
            service.Dispose();

            // Assert
            // Verify that Dispose was called (this is mainly for coverage)
            Assert.True(true); // If we get here, Dispose didn't throw
        }

        [Fact]
        public void Dispose_WhenMonitoring_ShouldStopMonitoring()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            service.StartMonitoringAsync().Wait();

            // Act
            service.Dispose();

            // Assert
            Assert.False(service.IsMonitoring);
        }

        [Fact]
        public async Task StartMonitoringAsync_WithHealthCheckInterval_ShouldStartHealthMonitoring()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            var healthCheckInterval = TimeSpan.FromMinutes(5);

            // Act
            await service.StartMonitoringAsync(healthCheckInterval);

            // Assert
            Assert.True(service.IsMonitoring);
            Assert.Equal(healthCheckInterval, service.HealthCheckInterval);
        }

        [Fact]
        public async Task HealthCheck_ShouldRaiseOnHealthCheckEvent()
        {
            // Arrange
            var service = new UnifiedDBNotificationService<TestEntity>(_mockCDCProvider.Object, _tableName);
            var healthInfo = new CDCHealthInfo
            {
                Status = CDCHealthStatus.Healthy,
                Metrics = new Dictionary<string, object> { ["Status"] = "OK" }
            };

            _mockCDCProvider.Setup(p => p.GetHealthInfoAsync()).ReturnsAsync(healthInfo);

            bool healthEventRaised = false;
            service.OnHealthCheck += (sender, e) =>
            {
                healthEventRaised = true;
                Assert.Equal(CDCHealthStatus.Healthy, e.Status);
            };

            await service.StartMonitoringAsync();

            // Act
            await service.PerformHealthCheckAsync();

            // Assert
            Assert.True(healthEventRaised);
        }

        public void Dispose()
        {
            _mockCDCProvider?.Dispose();
        }
    }

    /// <summary>
    /// Test entity for testing
    /// </summary>
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}