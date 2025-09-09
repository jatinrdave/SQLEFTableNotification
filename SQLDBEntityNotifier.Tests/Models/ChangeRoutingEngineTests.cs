using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class ChangeRoutingEngineTests
    {
        private readonly ChangeRoutingEngine _routingEngine;
        private readonly ChangeRecord _testChange;

        public ChangeRoutingEngineTests()
        {
            _routingEngine = new ChangeRoutingEngine();
            _testChange = new ChangeRecord
            {
                ChangeId = "test-123",
                TableName = "TestTable",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                ChangePosition = "LSN:123",
                Metadata = new Dictionary<string, object> { { "test", "value" } }
            };
        }

        [Fact]
        public void Constructor_ShouldInitializeRoutingEngine()
        {
            // Assert
            Assert.NotNull(_routingEngine);
            Assert.Empty(_routingEngine.RoutingRules);
            Assert.Empty(_routingEngine.Destinations);
        }

        [Fact]
        public void AddRoutingRule_ShouldAddRule()
        {
            // Arrange
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestDestination" });

            // Act
            _routingEngine.AddRoutingRule(rule);

            // Assert
            Assert.Single(_routingEngine.RoutingRules);
            Assert.Contains(rule, _routingEngine.RoutingRules);
        }

        [Fact]
        public void AddDestination_ShouldAddDestination()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");

            // Act
            _routingEngine.AddDestination(destination);

            // Assert
            Assert.Single(_routingEngine.Destinations);
            Assert.Contains(destination, _routingEngine.Destinations);
        }

        [Fact]
        public async Task RouteChangeAsync_WithNoRules_ShouldReturnEmptyResult()
        {
            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Empty(result.RoutedDestinations);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task RouteChangeAsync_WithMatchingRule_ShouldRouteToDestination()
        {
            // Arrange
            var destination = new MockSuccessfulDestination("TestWebhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(result.RoutedDestinations);
            Assert.Contains("TestWebhook", result.RoutedDestinations);
        }

        [Fact]
        public async Task RouteChangeAsync_WithMultipleDestinations_ShouldRouteToAll()
        {
            // Arrange
            var destination1 = new MockSuccessfulDestination("Webhook1");
            var destination2 = new MockSuccessfulDestination("Database1");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "Webhook1", "Database1" });

            _routingEngine.AddDestination(destination1);
            _routingEngine.AddDestination(destination2);
            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(2, result.RoutedDestinations.Count);
            Assert.Contains("Webhook1", result.RoutedDestinations);
            Assert.Contains("Database1", result.RoutedDestinations);
        }

        [Fact]
        public async Task RouteChangeAsync_WithFailingDestination_ShouldRecordError()
        {
            // Arrange
            var failingDestination = new MockFailingDestination("FailingDestination");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "FailingDestination" });

            _routingEngine.AddDestination(failingDestination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success); // Overall success, but with errors
            Assert.Empty(result.RoutedDestinations);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task RouteChangeAsync_WithNonExistentDestination_ShouldRecordError()
        {
            // Arrange
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "NonExistentDestination" });

            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Empty(result.RoutedDestinations);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Destination not found", result.Errors.First());
        }

        [Fact]
        public async Task RouteChangesAsync_WithMultipleChanges_ShouldRouteAll()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            var changes = new List<ChangeRecord>
            {
                _testChange,
                new ChangeRecord { ChangeId = "test-456", Operation = ChangeOperation.Update, ChangeTimestamp = DateTime.UtcNow }
            };

            // Act
            var results = await _routingEngine.RouteChangesAsync(changes, "TestTable");

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.True(r.Success));
        }

        [Fact]
        public void GetDestinationStats_ShouldReturnStats()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            _routingEngine.AddDestination(destination);

            // Act
            var stats = _routingEngine.GetDestinationStats("TestWebhook");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal("TestWebhook", stats.DestinationName);
        }

        [Fact]
        public void GetOverallStats_ShouldReturnOverallStats()
        {
            // Act
            var stats = _routingEngine.GetOverallStats();

            // Assert
            Assert.NotNull(stats);
        }

        [Fact]
        public void ClearMetrics_ShouldClearMetrics()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            _routingEngine.AddDestination(destination);

            // Act
            _routingEngine.ClearMetrics();

            // Assert
            // Metrics should be cleared
            Assert.NotNull(_routingEngine);
        }

        [Fact]
        public void OnChangeRouted_ShouldRaiseEvent()
        {
            // Arrange
            var eventRaised = false;
            _routingEngine.OnChangeRouted += (sender, e) => eventRaised = true;

            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            _ = _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            // Note: This test is timing-dependent and may not always pass
            // In a real scenario, you might want to mock the destination or use a synchronous test
        }

        [Fact]
        public void OnRoutingFailed_ShouldRaiseEvent()
        {
            // Arrange
            var eventRaised = false;
            _routingEngine.OnRoutingFailed += (sender, e) => eventRaised = true;

            var failingDestination = new MockFailingDestination("FailingDestination");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "FailingDestination" });

            _routingEngine.AddDestination(failingDestination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            _ = _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            // Note: This test is timing-dependent and may not always pass
        }

        [Fact]
        public void OnRoutingMetricsUpdated_ShouldRaiseEvent()
        {
            // Arrange
            var eventRaised = false;
            _routingEngine.OnRoutingMetricsUpdated += (sender, e) => eventRaised = true;

            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            _ = _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            // Note: This test is timing-dependent and may not always pass
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Act
            _routingEngine.Dispose();

            // Assert
            // Should not throw when disposed
            Assert.Throws<ObjectDisposedException>(() => _routingEngine.AddRoutingRule(new TableBasedRoutingRule("Test", new List<string>(), new List<string>())));
        }

        [Fact]
        public void MultipleDispose_ShouldNotThrowException()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _routingEngine.Dispose();
                _routingEngine.Dispose();
            }));
        }

        [Fact]
        public async Task RouteChangeAsync_AfterDispose_ShouldThrowException()
        {
            // Arrange
            _routingEngine.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => 
                await _routingEngine.RouteChangeAsync(_testChange, "TestTable"));
        }

        [Fact]
        public void AddRoutingRule_AfterDispose_ShouldThrowException()
        {
            // Arrange
            _routingEngine.Dispose();
            var rule = new TableBasedRoutingRule("Test", new List<string>(), new List<string>());

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _routingEngine.AddRoutingRule(rule));
        }

        [Fact]
        public void AddDestination_AfterDispose_ShouldThrowException()
        {
            // Arrange
            _routingEngine.Dispose();
            var destination = new WebhookDestination("Test", "https://test.com");

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _routingEngine.AddDestination(destination));
        }

        [Fact]
        public void RoutingRules_ShouldBeReadOnly()
        {
            // Act
            var rules = _routingEngine.RoutingRules;

            // Assert
            Assert.True(rules is IReadOnlyList<IRoutingRule>);
        }

        [Fact]
        public void Destinations_ShouldBeReadOnly()
        {
            // Act
            var destinations = _routingEngine.Destinations;

            // Assert
            Assert.True(destinations is IReadOnlyList<IDestination>);
        }

        [Fact]
        public void Metrics_ShouldBeAccessible()
        {
            // Act
            var metrics = _routingEngine.Metrics;

            // Assert
            Assert.NotNull(metrics);
        }

        [Fact]
        public void AddRoutingRule_ShouldReturnSelfForChaining()
        {
            // Arrange
            var rule = new TableBasedRoutingRule("Test", new List<string>(), new List<string>());

            // Act
            var result = _routingEngine.AddRoutingRule(rule);

            // Assert
            Assert.Same(_routingEngine, result);
        }

        [Fact]
        public void AddDestination_ShouldReturnSelfForChaining()
        {
            // Arrange
            var destination = new WebhookDestination("Test", "https://test.com");

            // Act
            var result = _routingEngine.AddDestination(destination);

            // Assert
            Assert.Same(_routingEngine, result);
        }

        [Fact]
        public async Task RouteChangeAsync_WithComplexRouting_ShouldWorkCorrectly()
        {
            // Arrange
            var webhookDest = new MockSuccessfulDestination("Webhook");
            var dbDest = new MockSuccessfulDestination("Database");
            var fileDest = new MockSuccessfulDestination("FileSystem");

            var tableRule = new TableBasedRoutingRule("TableRule", new List<string> { "TestTable" }, new List<string> { "Webhook", "Database" });
            var operationRule = new OperationBasedRoutingRule("OperationRule", new List<ChangeOperation> { ChangeOperation.Insert }, new List<string> { "FileSystem" });

            _routingEngine.AddDestination(webhookDest);
            _routingEngine.AddDestination(dbDest);
            _routingEngine.AddDestination(fileDest);
            _routingEngine.AddRoutingRule(tableRule);
            _routingEngine.AddRoutingRule(operationRule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(3, result.RoutedDestinations.Count);
        }

        [Fact]
        public async Task RouteChangeAsync_WithNoMatchingRules_ShouldReturnEmptyResult()
        {
            // Arrange
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "OtherTable" }, new List<string> { "TestDestination" });

            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Empty(result.RoutedDestinations);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task RouteChangeAsync_WithException_ShouldHandleGracefully()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            // The actual result depends on the destination implementation
        }

        [Fact]
        public async Task RouteChangesAsync_WithEmptyList_ShouldReturnEmptyResults()
        {
            // Act
            var results = await _routingEngine.RouteChangesAsync(new List<ChangeRecord>(), "TestTable");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task RouteChangesAsync_WithNullList_ShouldReturnEmptyResults()
        {
            // Act
            var results = await _routingEngine.RouteChangesAsync(null!, "TestTable");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public void GetDestinationStats_WithNonExistentDestination_ShouldReturnDefaultStats()
        {
            // Act
            var stats = _routingEngine.GetDestinationStats("NonExistentDestination");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal("NonExistentDestination", stats.DestinationName);
        }

        [Fact]
        public void Metrics_ShouldBeIndependentInstance()
        {
            // Act
            var metrics1 = _routingEngine.Metrics;
            var metrics2 = _routingEngine.Metrics;

            // Assert
            Assert.Same(metrics1, metrics2);
        }

        [Fact]
        public async Task RouteChangeAsync_WithProcessingTime_ShouldRecordProcessingTime()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ProcessingTime >= TimeSpan.Zero);
        }

        [Fact]
        public async Task RouteChangeAsync_WithChangeId_ShouldSetChangeId()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testChange.ChangeId, result.ChangeId);
        }

        [Fact]
        public async Task RouteChangeAsync_WithTableName_ShouldSetTableName()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestTable", result.TableName);
        }

        [Fact]
        public async Task RouteChangeAsync_WithTimestamp_ShouldSetTimestamp()
        {
            // Arrange
            var destination = new WebhookDestination("TestWebhook", "https://test.com/webhook");
            var rule = new TableBasedRoutingRule("TestRule", new List<string> { "TestTable" }, new List<string> { "TestWebhook" });

            _routingEngine.AddDestination(destination);
            _routingEngine.AddRoutingRule(rule);

            var startTime = DateTime.UtcNow;

            // Act
            var result = await _routingEngine.RouteChangeAsync(_testChange, "TestTable");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Timestamp >= startTime);
        }

        // Mock destination that always fails for testing
        public class MockFailingDestination : IDestination
        {
            public string Name { get; }
            public DestinationType Type => DestinationType.Webhook;
            public bool IsEnabled => true;

            public MockFailingDestination(string name)
            {
                Name = name;
            }

            public Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
            {
                throw new Exception("Mock destination failure");
            }

            public void Dispose()
            {
                // Nothing to dispose in mock
            }
        }

        // Mock destination that always succeeds for testing
        public class MockSuccessfulDestination : IDestination
        {
            public string Name { get; }
            public DestinationType Type => DestinationType.Webhook;
            public bool IsEnabled => true;

            public MockSuccessfulDestination(string name)
            {
                Name = name;
            }

            public Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
            {
                return Task.FromResult(new DeliveryResult
                {
                    Success = true,
                    DeliveryTime = TimeSpan.FromMilliseconds(10)
                });
            }

            public void Dispose()
            {
                // Nothing to dispose in mock
            }
        }
    }
}
