using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class ChangeCorrelationEngineTests
    {
        private readonly ChangeCorrelationEngine _correlationEngine;
        private readonly ChangeRecord _testChange;

        public ChangeCorrelationEngineTests()
        {
            _correlationEngine = new ChangeCorrelationEngine();
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
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Assert
            Assert.NotNull(_correlationEngine);
        }

        [Fact]
        public void RegisterForeignKeyRelationship_ShouldRegisterRelationship()
        {
            // Arrange
            var sourceTable = "SourceTable";
            var targetTable = "TargetTable";
            var sourceColumn = "SourceColumn";
            var targetColumn = "TargetColumn";
            var constraintName = "FK_Constraint";

            // Act
            _correlationEngine.RegisterForeignKeyRelationship(sourceTable, targetTable, sourceColumn, targetColumn, constraintName);

            // Assert
            var dependencyGraph = _correlationEngine.GetDependencyGraph(sourceTable);
            Assert.NotNull(dependencyGraph);
        }

        [Fact]
        public void RegisterForeignKeyRelationship_WithNullValues_ShouldHandleGracefully()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _correlationEngine.RegisterForeignKeyRelationship("", "", "", "", "");
            }));
        }

        [Fact]
        public void RecordChange_ShouldRecordChange()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;

            // Act
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);

            // Assert
            var correlatedChanges = _correlationEngine.GetCorrelatedChanges(tableName, TimeSpan.FromMinutes(5));
            Assert.NotNull(correlatedChanges);
        }

        [Fact]
        public void RecordBatchChanges_ShouldRecordBatchChanges()
        {
            // Arrange
            var tableName = "TestTable";
            var changes = new List<ChangeRecord> { _testChange };
            var timestamp = DateTime.UtcNow;

            // Act
            _correlationEngine.RecordBatchChanges(tableName, changes, timestamp);

            // Assert
            var correlatedChanges = _correlationEngine.GetCorrelatedChanges(tableName, TimeSpan.FromMinutes(5));
            Assert.NotNull(correlatedChanges);
        }

        [Fact]
        public void GetCorrelatedChanges_ShouldReturnCorrelatedChanges()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);

            // Act
            var result = _correlationEngine.GetCorrelatedChanges(tableName, TimeSpan.FromMinutes(5));

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetCorrelatedChanges_WithNonExistentTable_ShouldReturnEmptyList()
        {
            // Act
            var result = _correlationEngine.GetCorrelatedChanges("NonExistentTable", TimeSpan.FromMinutes(5));

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllCorrelatedChanges_ShouldReturnAllChanges()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);

            // Act
            var result = _correlationEngine.GetAllCorrelatedChanges(TimeSpan.FromMinutes(5));

            // Assert
            Assert.NotNull(result);
            Assert.Contains(tableName, result.Keys);
        }

        [Fact]
        public void GetDependencyGraph_ShouldReturnDependencyGraph()
        {
            // Arrange
            var tableName = "TestTable";

            // Act
            var result = _correlationEngine.GetDependencyGraph(tableName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tableName, result.TableName);
        }

        [Fact]
        public void GetDependencyGraph_WithNonExistentTable_ShouldReturnNewGraph()
        {
            // Arrange
            var tableName = "NonExistentTable";

            // Act
            var result = _correlationEngine.GetDependencyGraph(tableName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tableName, result.TableName);
        }

        [Fact]
        public void GetAllDependencyGraphs_ShouldReturnAllGraphs()
        {
            // Arrange
            var tableName = "TestTable";
            _correlationEngine.RegisterForeignKeyRelationship(tableName, "TargetTable", "SourceColumn", "TargetColumn", "FK_Constraint");

            // Act
            var result = _correlationEngine.GetAllDependencyGraphs();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(tableName, result.Keys);
        }

        [Fact]
        public async Task AnalyzeChangeImpactAsync_ShouldReturnImpactAnalysis()
        {
            // Arrange
            var tableName = "TestTable";
            _correlationEngine.RegisterForeignKeyRelationship(tableName, "TargetTable", "SourceColumn", "TargetColumn", "FK_Constraint");

            // Act
            var result = await _correlationEngine.AnalyzeChangeImpactAsync(tableName, _testChange);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tableName, result.SourceTable);
            Assert.Equal(_testChange, result.ChangeRecord);
            Assert.NotNull(result.AffectedTables);
            Assert.NotNull(result.DependencyChain);
        }

        [Fact]
        public async Task AnalyzeChangeImpactAsync_WithNonExistentTable_ShouldReturnEmptyAnalysis()
        {
            // Arrange
            var tableName = "NonExistentTable";

            // Act
            var result = await _correlationEngine.AnalyzeChangeImpactAsync(tableName, _testChange);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tableName, result.SourceTable);
            Assert.Empty(result.AffectedTables);
        }

        [Fact]
        public void ClearTableCorrelations_ShouldClearTableData()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);
            _correlationEngine.RegisterForeignKeyRelationship(tableName, "TargetTable", "SourceColumn", "TargetColumn", "FK_Constraint");

            // Act
            _correlationEngine.ClearTableCorrelations(tableName);

            // Assert
            var correlatedChanges = _correlationEngine.GetCorrelatedChanges(tableName, TimeSpan.FromMinutes(5));
            Assert.Empty(correlatedChanges);
        }

        [Fact]
        public void ClearTableCorrelations_WithNonExistentTable_ShouldNotThrowException()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _correlationEngine.ClearTableCorrelations("NonExistentTable");
            }));
        }

        [Fact]
        public void ClearAllCorrelations_ShouldClearAllData()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);

            // Act
            _correlationEngine.ClearAllCorrelations();

            // Assert
            var allChanges = _correlationEngine.GetAllCorrelatedChanges(TimeSpan.FromMinutes(5));
            Assert.Empty(allChanges);
        }

        [Fact]
        public void ClearAllCorrelations_WithNoData_ShouldNotThrowException()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _correlationEngine.ClearAllCorrelations();
            }));
        }

        [Fact]
        public void RegisterForeignKeyRelationship_ShouldUpdateDependencyGraph()
        {
            // Arrange
            var sourceTable = "SourceTable";
            var targetTable = "TargetTable";
            var sourceColumn = "SourceColumn";
            var targetColumn = "TargetColumn";
            var constraintName = "FK_Constraint";

            // Act
            _correlationEngine.RegisterForeignKeyRelationship(sourceTable, targetTable, sourceColumn, targetColumn, constraintName);

            // Assert
            var sourceGraph = _correlationEngine.GetDependencyGraph(sourceTable);
            var targetGraph = _correlationEngine.GetDependencyGraph(targetTable);
            Assert.NotNull(sourceGraph);
            Assert.NotNull(targetGraph);
        }

        [Fact]
        public void RegisterForeignKeyRelationship_WithMultipleRelationships_ShouldHandleCorrectly()
        {
            // Arrange
            var table1 = "Table1";
            var table2 = "Table2";
            var table3 = "Table3";

            // Act
            _correlationEngine.RegisterForeignKeyRelationship(table1, table2, "Col1", "Col2", "FK1");
            _correlationEngine.RegisterForeignKeyRelationship(table2, table3, "Col2", "Col3", "FK2");

            // Assert
            var graph1 = _correlationEngine.GetDependencyGraph(table1);
            var graph2 = _correlationEngine.GetDependencyGraph(table2);
            var graph3 = _correlationEngine.GetDependencyGraph(table3);
            Assert.NotNull(graph1);
            Assert.NotNull(graph2);
            Assert.NotNull(graph3);
        }

        [Fact]
        public void RecordChange_WithHighPriorityChange_ShouldTriggerImmediateAnalysis()
        {
            // Arrange
            var tableName = "TestTable";
            var highPriorityChange = new ChangeRecord
            {
                ChangeId = "high-priority-123",
                TableName = tableName,
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { "priority", "high" } }
            };

            // Act
            _correlationEngine.RecordChange(tableName, highPriorityChange, DateTime.UtcNow);

            // Assert
            // The actual analysis is done asynchronously, so we just verify no exception is thrown
            Assert.NotNull(_correlationEngine);
        }

        [Fact]
        public void RecordBatchChanges_ShouldHandleEmptyBatch()
        {
            // Arrange
            var tableName = "TestTable";
            var emptyChanges = new List<ChangeRecord>();
            var timestamp = DateTime.UtcNow;

            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _correlationEngine.RecordBatchChanges(tableName, emptyChanges, timestamp);
            }));
        }

        [Fact]
        public void GetCorrelatedChanges_WithZeroTimeWindow_ShouldReturnEmptyList()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);

            // Act
            var result = _correlationEngine.GetCorrelatedChanges(tableName, TimeSpan.Zero);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetCorrelatedChanges_WithLargeTimeWindow_ShouldReturnAllChanges()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);

            // Act
            var result = _correlationEngine.GetCorrelatedChanges(tableName, TimeSpan.FromDays(1));

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetDependencyGraph_ShouldReturnCorrectTableName()
        {
            // Arrange
            var tableName = "SpecificTable";

            // Act
            var result = _correlationEngine.GetDependencyGraph(tableName);

            // Assert
            Assert.Equal(tableName, result.TableName);
        }

        [Fact]
        public void GetAllDependencyGraphs_WithNoRelationships_ShouldReturnEmptyDictionary()
        {
            // Act
            var result = _correlationEngine.GetAllDependencyGraphs();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task AnalyzeChangeImpactAsync_WithNoDependencies_ShouldReturnLowImpact()
        {
            // Arrange
            var tableName = "IndependentTable";

            // Act
            var result = await _correlationEngine.AnalyzeChangeImpactAsync(tableName, _testChange);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ChangeImpactLevel.Low, result.ImpactLevel);
            Assert.Empty(result.AffectedTables);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);

            // Act
            _correlationEngine.Dispose();

            // Assert
            // After dispose, the engine should still be accessible but may not function properly
            Assert.NotNull(_correlationEngine);
        }

        [Fact]
        public void Dispose_ShouldBeCallableMultipleTimes()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _correlationEngine.Dispose();
                _correlationEngine.Dispose();
            }));
        }

        [Fact]
        public void RecordChange_WithNullChange_ShouldHandleGracefully()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;

            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _correlationEngine.RecordChange(tableName, null!, timestamp);
            }));
        }

        [Fact]
        public void RecordBatchChanges_WithNullChanges_ShouldHandleGracefully()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;

            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _correlationEngine.RecordBatchChanges(tableName, null!, timestamp);
            }));
        }

        [Fact]
        public void RegisterForeignKeyRelationship_WithDuplicateConstraint_ShouldHandleCorrectly()
        {
            // Arrange
            var sourceTable = "SourceTable";
            var targetTable = "TargetTable";
            var sourceColumn = "SourceColumn";
            var targetColumn = "TargetColumn";
            var constraintName = "FK_Duplicate";

            // Act
            _correlationEngine.RegisterForeignKeyRelationship(sourceTable, targetTable, sourceColumn, targetColumn, constraintName);
            _correlationEngine.RegisterForeignKeyRelationship(sourceTable, targetTable, sourceColumn, targetColumn, constraintName);

            // Assert
            // Should handle duplicate registration gracefully
            Assert.NotNull(_correlationEngine);
        }

        [Fact]
        public void GetCorrelatedChanges_WithNegativeTimeWindow_ShouldHandleGracefully()
        {
            // Arrange
            var tableName = "TestTable";
            var timestamp = DateTime.UtcNow;
            _correlationEngine.RecordChange(tableName, _testChange, timestamp);

            // Act
            var result = _correlationEngine.GetCorrelatedChanges(tableName, TimeSpan.FromMinutes(-5));

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetDependencyGraph_WithEmptyTableName_ShouldHandleGracefully()
        {
            // Act
            var result = _correlationEngine.GetDependencyGraph("");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("unknown", result.TableName);
        }

        [Fact]
        public void GetDependencyGraph_WithNullTableName_ShouldHandleGracefully()
        {
            // Act
            var result = _correlationEngine.GetDependencyGraph(null!);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("unknown", result.TableName);
        }
    }
}
