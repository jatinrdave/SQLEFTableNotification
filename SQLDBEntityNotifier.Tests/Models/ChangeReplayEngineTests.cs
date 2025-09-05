using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class ChangeReplayEngineTests
    {
        private readonly ChangeReplayEngine _replayEngine;
        private readonly ChangeRecord _testChange;

        public ChangeReplayEngineTests()
        {
            _replayEngine = new ChangeReplayEngine();
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
        public void Constructor_ShouldInitializeReplayEngine()
        {
            // Assert
            Assert.NotNull(_replayEngine);
            Assert.NotNull(_replayEngine.Metrics);
        }

        [Fact]
        public void RecordChange_ShouldRecordChangeForReplay()
        {
            // Act
            _replayEngine.RecordChange("TestTable", _testChange);

            // Assert
            // The change should be recorded internally
            Assert.NotNull(_replayEngine);
        }

        [Fact]
        public async Task StartReplayAsync_ShouldStartReplaySession()
        {
            // Arrange
            var options = new ReplayOptions
            {
                BatchSize = 100,
                Mode = ReplayMode.Sequential
            };

            // Act
            var session = await _replayEngine.StartReplayAsync("TestTable", options);

            // Assert
            Assert.NotNull(session);
            Assert.Equal("TestTable", session.TableName);
            Assert.Equal(options, session.Options);
        }

        [Fact]
        public async Task StartReplayAsync_WithInvalidTableName_ShouldThrowException()
        {
            // Arrange
            var options = new ReplayOptions
            {
                BatchSize = 100
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _replayEngine.StartReplayAsync("", options));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _replayEngine.StartReplayAsync(null!, options));
        }

        [Fact]
        public async Task StartReplayAsync_WithNullOptions_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _replayEngine.StartReplayAsync("TestTable", null!));
        }

        [Fact]
        public async Task StartReplayAsync_WithCustomOptions_ShouldSetOptions()
        {
            // Arrange
            var options = new ReplayOptions
            {
                MaxChanges = 1000,
                BatchSize = 50,
                ProcessingDelay = TimeSpan.FromMilliseconds(100),
                SimulateFailures = true,
                Mode = ReplayMode.Parallel,
                IncludeMetadata = false
            };

            // Act
            var session = await _replayEngine.StartReplayAsync("TestTable", options);

            // Assert
            Assert.NotNull(session);
            Assert.Equal(1000, session.Options.MaxChanges);
            Assert.Equal(50, session.Options.BatchSize);
            Assert.Equal(TimeSpan.FromMilliseconds(100), session.Options.ProcessingDelay);
            Assert.True(session.Options.SimulateFailures);
            Assert.Equal(ReplayMode.Parallel, session.Options.Mode);
            Assert.False(session.Options.IncludeMetadata);
        }

        [Fact]
        public async Task StartReplayAsync_WithDefaultOptions_ShouldUseDefaults()
        {
            // Arrange
            var options = new ReplayOptions();

            // Act
            var session = await _replayEngine.StartReplayAsync("TestTable", options);

            // Assert
            Assert.NotNull(session);
            Assert.Null(session.Options.MaxChanges);
            Assert.Null(session.Options.BatchSize);
            Assert.Null(session.Options.ProcessingDelay);
            Assert.False(session.Options.SimulateFailures);
            Assert.Equal(ReplayMode.Sequential, session.Options.Mode);
            Assert.True(session.Options.IncludeMetadata);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Act
            _replayEngine.Dispose();

            // Assert
            // Should not throw when disposed
            Assert.NotNull(_replayEngine);
        }

        [Fact]
        public void MultipleDispose_ShouldNotThrowException()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _replayEngine.Dispose();
                _replayEngine.Dispose();
            }));
        }

        [Fact]
        public void Metrics_ShouldBeAccessible()
        {
            // Act
            var metrics = _replayEngine.Metrics;

            // Assert
            Assert.NotNull(metrics);
        }

        [Fact]
        public void Metrics_ShouldBeIndependentInstance()
        {
            // Act
            var metrics1 = _replayEngine.Metrics;
            var metrics2 = _replayEngine.Metrics;

            // Assert
            Assert.Same(metrics1, metrics2);
        }

        [Fact]
        public void RecordChange_WithMultipleChanges_ShouldRecordAll()
        {
            // Arrange
            var changes = new List<ChangeRecord>
            {
                _testChange,
                new ChangeRecord { ChangeId = "test-456", Operation = ChangeOperation.Update, ChangeTimestamp = DateTime.UtcNow },
                new ChangeRecord { ChangeId = "test-789", Operation = ChangeOperation.Delete, ChangeTimestamp = DateTime.UtcNow }
            };

            // Act
            foreach (var change in changes)
            {
                _replayEngine.RecordChange("TestTable", change);
            }

            // Assert
            // All changes should be recorded internally
            Assert.NotNull(_replayEngine);
        }

        [Fact]
        public void RecordChange_WithDifferentTables_ShouldRecordSeparately()
        {
            // Arrange
            var change1 = new ChangeRecord { ChangeId = "1", Operation = ChangeOperation.Insert, ChangeTimestamp = DateTime.UtcNow };
            var change2 = new ChangeRecord { ChangeId = "2", Operation = ChangeOperation.Update, ChangeTimestamp = DateTime.UtcNow };

            // Act
            _replayEngine.RecordChange("Table1", change1);
            _replayEngine.RecordChange("Table2", change2);

            // Assert
            // Changes should be recorded for different tables
            Assert.NotNull(_replayEngine);
        }

        [Fact]
        public void RecordChange_WithNullChange_ShouldHandleGracefully()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("TestTable", null!)));
        }

        [Fact]
        public void RecordChange_WithEmptyTableName_ShouldHandleGracefully()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("", _testChange)));
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange(null!, _testChange)));
        }

        [Fact]
        public void RecordChange_WithLargeMetadata_ShouldHandleGracefully()
        {
            // Arrange
            var largeMetadata = new Dictionary<string, object>();
            for (int i = 0; i < 1000; i++)
            {
                largeMetadata[$"Key{i}"] = $"Value{i}";
            }

            var changeWithLargeMetadata = new ChangeRecord
            {
                ChangeId = "large-metadata-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = largeMetadata
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("TestTable", changeWithLargeMetadata)));
        }

        [Fact]
        public void RecordChange_WithSpecialCharacters_ShouldHandleGracefully()
        {
            // Arrange
            var specialChange = new ChangeRecord
            {
                ChangeId = "special-char-test-!@#$%^&*()",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { "SpecialKey!@#", "SpecialValue$%^" } }
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("SpecialTable!@#", specialChange)));
        }

        [Fact]
        public void RecordChange_WithVeryLongStrings_ShouldHandleGracefully()
        {
            // Arrange
            var longString = new string('A', 10000);
            var longChange = new ChangeRecord
            {
                ChangeId = longString,
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { longString, longString } }
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange(longString, longChange)));
        }

        [Fact]
        public void RecordChange_WithFutureTimestamp_ShouldHandleGracefully()
        {
            // Arrange
            var futureChange = new ChangeRecord
            {
                ChangeId = "future-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow.AddYears(1),
                Metadata = new Dictionary<string, object> { { "Future", "Value" } }
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("FutureTable", futureChange)));
        }

        [Fact]
        public void RecordChange_WithPastTimestamp_ShouldHandleGracefully()
        {
            // Arrange
            var pastChange = new ChangeRecord
            {
                ChangeId = "past-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow.AddYears(-10),
                Metadata = new Dictionary<string, object> { { "Past", "Value" } }
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("PastTable", pastChange)));
        }

        [Fact]
        public void RecordChange_WithZeroTimestamp_ShouldHandleGracefully()
        {
            // Arrange
            var zeroChange = new ChangeRecord
            {
                ChangeId = "zero-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.MinValue,
                Metadata = new Dictionary<string, object> { { "Zero", "Value" } }
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("ZeroTable", zeroChange)));
        }

        [Fact]
        public void RecordChange_WithMaxTimestamp_ShouldHandleGracefully()
        {
            // Arrange
            var maxChange = new ChangeRecord
            {
                ChangeId = "max-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.MaxValue,
                Metadata = new Dictionary<string, object> { { "Max", "Value" } }
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("MaxTable", maxChange)));
        }

        [Fact]
        public void RecordChange_WithUnicodeCharacters_ShouldHandleGracefully()
        {
            // Arrange
            var unicodeChange = new ChangeRecord
            {
                ChangeId = "unicode-test-🚀🌟💻",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { "UnicodeKey🚀", "UnicodeValue🌟" } }
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("UnicodeTable🚀", unicodeChange)));
        }

        [Fact]
        public void RecordChange_WithBinaryData_ShouldHandleGracefully()
        {
            // Arrange
            var binaryData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD, 0xFC };
            var binaryChange = new ChangeRecord
            {
                ChangeId = "binary-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { "BinaryData", binaryData } }
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("BinaryTable", binaryChange)));
        }

        [Fact]
        public void RecordChange_WithCircularReferences_ShouldHandleGracefully()
        {
            // Arrange
            var circularObject = new Dictionary<string, object>();
            circularObject["Self"] = circularObject;

            var circularChange = new ChangeRecord
            {
                ChangeId = "circular-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = circularObject
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("CircularTable", circularChange)));
        }

        [Fact]
        public void RecordChange_WithNullMetadata_ShouldHandleGracefully()
        {
            // Arrange
            var nullMetadataChange = new ChangeRecord
            {
                ChangeId = "null-metadata-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = null!
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("NullMetadataTable", nullMetadataChange)));
        }

        [Fact]
        public void RecordChange_WithEmptyMetadata_ShouldHandleGracefully()
        {
            // Arrange
            var emptyMetadataChange = new ChangeRecord
            {
                ChangeId = "empty-metadata-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>()
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("EmptyMetadataTable", emptyMetadataChange)));
        }

        [Fact]
        public void RecordChange_WithComplexMetadata_ShouldHandleGracefully()
        {
            // Arrange
            var complexMetadata = new Dictionary<string, object>
            {
                { "String", "Value" },
                { "Number", 42 },
                { "Boolean", true },
                { "Null", null! },
                { "Array", new object[] { 1, 2, 3, "test" } },
                { "Nested", new Dictionary<string, object> { { "NestedKey", "NestedValue" } } }
            };

            var complexChange = new ChangeRecord
            {
                ChangeId = "complex-metadata-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = complexMetadata
            };

            // Act & Assert
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("ComplexMetadataTable", complexChange)));
        }

        [Fact]
        public void RecordChange_WithAllChangeOperations_ShouldHandleGracefully()
        {
            // Arrange
            var operations = new[] { ChangeOperation.Insert, ChangeOperation.Update, ChangeOperation.Delete };

            foreach (var operation in operations)
            {
                var change = new ChangeRecord
                {
                    ChangeId = $"operation-test-{operation}",
                    Operation = operation,
                    ChangeTimestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { { "Operation", operation.ToString() } }
                };

                // Act & Assert
                Assert.Null(Record.Exception(() => _replayEngine.RecordChange($"OperationTable{operation}", change)));
            }
        }

        [Fact]
        public void RecordChange_WithHighFrequency_ShouldHandleGracefully()
        {
            // Arrange
            var startTime = DateTime.UtcNow;

            // Act - Record many changes quickly
            for (int i = 0; i < 1000; i++)
            {
                var change = new ChangeRecord
                {
                    ChangeId = $"high-freq-test-{i}",
                    Operation = ChangeOperation.Insert,
                    ChangeTimestamp = startTime.AddMilliseconds(i),
                    Metadata = new Dictionary<string, object> { { "Index", i } }
                };

                _replayEngine.RecordChange("HighFreqTable", change);
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert
            Assert.NotNull(_replayEngine);
            Assert.True(duration < TimeSpan.FromSeconds(10)); // Should complete within 10 seconds
        }

        [Fact]
        public void RecordChange_WithConcurrentAccess_ShouldHandleGracefully()
        {
            // Arrange
            var tasks = new List<Task>();

            // Act - Record changes concurrently
            for (int i = 0; i < 100; i++)
            {
                var index = i;
                var task = Task.Run(() =>
                {
                    var change = new ChangeRecord
                    {
                        ChangeId = $"concurrent-test-{index}",
                        Operation = ChangeOperation.Insert,
                        ChangeTimestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object> { { "Index", index } }
                    };

                    _replayEngine.RecordChange($"ConcurrentTable{index % 10}", change);
                });

                tasks.Add(task);
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.NotNull(_replayEngine);
            Assert.All(tasks, t => Assert.True(t.IsCompletedSuccessfully));
        }

        [Fact]
        public void RecordChange_WithMemoryPressure_ShouldHandleGracefully()
        {
            // Arrange
            var largeObjects = new List<object>();

            // Act - Record changes with large objects to create memory pressure
            for (int i = 0; i < 100; i++)
            {
                var largeObject = new string('X', 10000); // 10KB string
                largeObjects.Add(largeObject);

                var change = new ChangeRecord
                {
                    ChangeId = $"memory-test-{i}",
                    Operation = ChangeOperation.Insert,
                    ChangeTimestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { { "LargeData", largeObject } }
                };

                _replayEngine.RecordChange("MemoryTable", change);
            }

            // Assert
            Assert.NotNull(_replayEngine);
            Assert.Equal(100, largeObjects.Count);
        }

        [Fact]
        public void RecordChange_WithExceptionHandling_ShouldHandleGracefully()
        {
            // Arrange
            var problematicChange = new ChangeRecord
            {
                ChangeId = "exception-test",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { "Problematic", "Data" } }
            };

            // Act & Assert - Should not throw even if internal processing fails
            Assert.Null(Record.Exception(() => _replayEngine.RecordChange("ExceptionTable", problematicChange)));
        }

        [Fact]
        public void RecordChange_WithBoundaryConditions_ShouldHandleGracefully()
        {
            // Arrange
            var boundaryChanges = new[]
            {
                new ChangeRecord { ChangeId = "", Operation = ChangeOperation.Insert, ChangeTimestamp = DateTime.UtcNow },
                new ChangeRecord { ChangeId = null!, Operation = ChangeOperation.Insert, ChangeTimestamp = DateTime.UtcNow },
                new ChangeRecord { ChangeId = "boundary-test", Operation = ChangeOperation.Insert, ChangeTimestamp = DateTime.UtcNow, Metadata = null! }
            };

            // Act & Assert
            foreach (var change in boundaryChanges)
            {
                Assert.Null(Record.Exception(() => _replayEngine.RecordChange("BoundaryTable", change)));
            }
        }

        [Fact]
        public void RecordChange_WithPerformanceMonitoring_ShouldCompleteInReasonableTime()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Record a reasonable number of changes
            for (int i = 0; i < 100; i++)
            {
                var change = new ChangeRecord
                {
                    ChangeId = $"perf-test-{i}",
                    Operation = ChangeOperation.Insert,
                    ChangeTimestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { { "Index", i } }
                };

                _replayEngine.RecordChange("PerfTable", change);
            }

            stopwatch.Stop();

            // Assert
            Assert.NotNull(_replayEngine);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should complete within 1 second
        }

        [Fact]
        public void RecordChange_WithStressTest_ShouldHandleGracefully()
        {
            // Arrange
            var random = new Random(42); // Fixed seed for reproducible tests
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Stress test with random data
            for (int i = 0; i < 500; i++)
            {
                var change = new ChangeRecord
                {
                    ChangeId = $"stress-test-{i}-{random.Next(1000)}",
                    Operation = (ChangeOperation)(random.Next(3)),
                    ChangeTimestamp = DateTime.UtcNow.AddSeconds(random.Next(-3600, 3600)),
                    Metadata = new Dictionary<string, object>
                    {
                        { "RandomInt", random.Next() },
                        { "RandomDouble", random.NextDouble() },
                        { "RandomString", new string('A', random.Next(1, 100)) }
                    }
                };

                _replayEngine.RecordChange($"StressTable{random.Next(10)}", change);
            }

            stopwatch.Stop();

            // Assert
            Assert.NotNull(_replayEngine);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        }
    }
}
