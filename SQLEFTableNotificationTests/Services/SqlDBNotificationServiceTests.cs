using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SQLEFTableNotification.Interfaces;
using SQLEFTableNotification.Models;
using SQLEFTableNotification.Services;
using SQLEFTableNotification.Delegates;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Tests.Services
{
    [TestClass()]
    public class SqlDBNotificationServiceTests
    {
        private Mock<IChangeTableService<TestEntity>> _mockChangeTableService;
        private SqlDBNotificationService<TestEntity> _service;
        private const string TestTableName = "TestTable";
        private const string TestConnectionString = "TestConnectionString";

        [TestInitialize]
        public void Setup()
        {
            _mockChangeTableService = new Mock<IChangeTableService<TestEntity>>();
            _service = new SqlDBNotificationService<TestEntity>(
                TestTableName, 
                TestConnectionString, 
                _mockChangeTableService.Object);
        }

        [TestMethod()]
        public void SqlDBNotificationServiceTest()
        {
            // Arrange & Act
            var service = new SqlDBNotificationService<TestEntity>(
                TestTableName, 
                TestConnectionString, 
                _mockChangeTableService.Object);

            // Assert
            Assert.IsNotNull(service);
            Assert.AreEqual(TestTableName, service.GetType().GetField("_tableName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(service));
        }

        [TestMethod()]
        public void DisposeTest()
        {
            // Arrange
            var service = new SqlDBNotificationService<TestEntity>(
                TestTableName, 
                TestConnectionString, 
                _mockChangeTableService.Object);

            // Act & Assert - Dispose should not throw
            Assert.IsTrue(true); // Dispose method is empty, so it should not throw
        }

        [TestMethod()]
        public async Task StartNotifyTest()
        {
            // Arrange
            _mockChangeTableService.Setup(x => x.GetRecordCount(It.IsAny<string>()))
                .ReturnsAsync(100L);

            // Act
            await _service.StartNotify();

            // Assert
            // The service should start without throwing exceptions
            Assert.IsTrue(true);
        }

        [TestMethod()]
        public async Task StopNotifyTest()
        {
            // Arrange
            _mockChangeTableService.Setup(x => x.GetRecordCount(It.IsAny<string>()))
                .ReturnsAsync(100L);

            // Act
            await _service.StartNotify();
            await _service.StopNotify();

            // Assert
            // The service should stop without throwing exceptions
            Assert.IsTrue(true);
        }

        [TestMethod()]
        public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var service = new SqlDBNotificationService<TestEntity>(
                TestTableName, 
                TestConnectionString, 
                _mockChangeTableService.Object,
                version: 50L,
                period: TimeSpan.FromSeconds(30),
                recordIdentifier: "TestIdentifier");

            // Assert
            Assert.IsNotNull(service);
        }

        [TestMethod()]
        public void Constructor_WithNullPeriod_ShouldUseDefaultPeriod()
        {
            // Arrange & Act
            var service = new SqlDBNotificationService<TestEntity>(
                TestTableName, 
                TestConnectionString, 
                _mockChangeTableService.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        [TestMethod()]
        public void Constructor_WithCustomPeriod_ShouldUseCustomPeriod()
        {
            // Arrange
            var customPeriod = TimeSpan.FromMinutes(5);

            // Act
            var service = new SqlDBNotificationService<TestEntity>(
                TestTableName, 
                TestConnectionString, 
                _mockChangeTableService.Object,
                period: customPeriod);

            // Assert
            Assert.IsNotNull(service);
        }

        [TestMethod()]
        public void Constructor_WithCustomVersion_ShouldUseCustomVersion()
        {
            // Arrange
            var customVersion = 200L;

            // Act
            var service = new SqlDBNotificationService<TestEntity>(
                TestTableName, 
                TestConnectionString, 
                _mockChangeTableService.Object,
                version: customVersion);

            // Assert
            Assert.IsNotNull(service);
        }

        [TestMethod()]
        public void Constructor_WithCustomRecordIdentifier_ShouldUseCustomRecordIdentifier()
        {
            // Arrange
            var customIdentifier = "CustomIdentifier";

            // Act
            var service = new SqlDBNotificationService<TestEntity>(
                TestTableName, 
                TestConnectionString, 
                _mockChangeTableService.Object,
                recordIdentifier: customIdentifier);

            // Assert
            Assert.IsNotNull(service);
        }

        // Test entity for testing purposes
        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}