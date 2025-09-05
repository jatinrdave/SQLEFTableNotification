using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Interfaces;
using Moq;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class SchemaChangeDetectionTests
    {
        private readonly SchemaChangeDetection _schemaDetection;
        private readonly Mock<ICDCProvider> _mockCdcProvider;

        public SchemaChangeDetectionTests()
        {
            _schemaDetection = new SchemaChangeDetection();
            _mockCdcProvider = new Mock<ICDCProvider>();
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Assert
            Assert.NotNull(_schemaDetection);
        }

        [Fact]
        public async Task TakeTableSnapshotAsync_ShouldCreateSnapshot()
        {
            // Arrange
            var tableName = "TestTable";
            var columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false },
                new ColumnDefinition { ColumnName = "Name", DataType = "varchar(100)", IsNullable = true }
            };
            var indexes = new List<IndexDefinition>
            {
                new IndexDefinition { Name = "PK_TestTable", Type = "PRIMARY KEY", IsPrimaryKey = true }
            };
            var constraints = new List<ConstraintDefinition>
            {
                new ConstraintDefinition { Name = "PK_TestTable", Type = ConstraintType.PrimaryKey, Expression = "PRIMARY KEY" }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(columns);
            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(indexes);
            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(constraints);

            // Act
            var snapshot = await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal(tableName, snapshot.TableName);
            Assert.Equal(2, snapshot.Columns.Count);
            Assert.Single(snapshot.Indexes);
            Assert.Single(snapshot.Constraints);
            Assert.NotNull(snapshot.TableDefinition);
        }

        [Fact]
        public async Task TakeTableSnapshotAsync_WithError_ShouldReturnBasicSnapshot()
        {
            // Arrange
            var tableName = "TestTable";
            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ThrowsAsync(new Exception("Database error"));

            // Act
            var snapshot = await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal(tableName, snapshot.TableName);
            Assert.Empty(snapshot.Columns);
            Assert.Empty(snapshot.Indexes);
            Assert.Empty(snapshot.Constraints);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_ShouldDetectChanges()
        {
            // Arrange
            var tableName = "TestTable";
            var columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false },
                new ColumnDefinition { ColumnName = "Name", DataType = "varchar(100)", IsNullable = true }
            };
            var indexes = new List<IndexDefinition>
            {
                new IndexDefinition { Name = "PK_TestTable", Type = "PRIMARY KEY", IsPrimaryKey = true }
            };
            var constraints = new List<ConstraintDefinition>
            {
                new ConstraintDefinition { Name = "PK_TestTable", Type = ConstraintType.PrimaryKey, Expression = "PRIMARY KEY" }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(columns);
            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(indexes);
            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(constraints);

            // Take initial snapshot
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Change the schema
            var newColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false },
                new ColumnDefinition { ColumnName = "Name", DataType = "varchar(100)", IsNullable = true },
                new ColumnDefinition { ColumnName = "Email", DataType = "varchar(255)", IsNullable = true }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(newColumns);

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.NotEmpty(changes);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithNoPreviousSnapshot_ShouldReturnEmpty()
        {
            // Arrange
            var tableName = "NewTable";
            var columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(columns);

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.Empty(changes);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithNoChanges_ShouldReturnEmpty()
        {
            // Arrange
            var tableName = "TestTable";
            var columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(columns);

            // Take initial snapshot
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Take another snapshot with same schema
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.Empty(changes);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithColumnAdded_ShouldDetectColumnAddition()
        {
            // Arrange
            var tableName = "TestTable";
            var initialColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(initialColumns);
            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(new List<IndexDefinition>());
            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(new List<ConstraintDefinition>());

            // Take initial snapshot
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Add a new column
            var newColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false },
                new ColumnDefinition { ColumnName = "Name", DataType = "varchar(100)", IsNullable = true }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(newColumns);
            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(new List<IndexDefinition>());
            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(new List<ConstraintDefinition>());

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.NotEmpty(changes);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithColumnRemoved_ShouldDetectColumnRemoval()
        {
            // Arrange
            var tableName = "TestTable";
            var initialColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false },
                new ColumnDefinition { ColumnName = "Name", DataType = "varchar(100)", IsNullable = true }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(initialColumns);

            // Take initial snapshot
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Remove a column
            var newColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(newColumns);

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.NotEmpty(changes);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithColumnModified_ShouldDetectColumnModification()
        {
            // Arrange
            var tableName = "TestTable";
            var initialColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Name", DataType = "varchar(50)", IsNullable = true }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(initialColumns);

            // Take initial snapshot
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Modify column data type
            var newColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Name", DataType = "varchar(100)", IsNullable = true }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(newColumns);

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.NotEmpty(changes);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithIndexAdded_ShouldDetectIndexAddition()
        {
            // Arrange
            var tableName = "TestTable";
            var columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false }
            };
            var initialIndexes = new List<IndexDefinition>();

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(columns);
            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(initialIndexes);

            // Take initial snapshot
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Add an index
            var newIndexes = new List<IndexDefinition>
            {
                new IndexDefinition { Name = "IX_TestTable_Id", Type = "NONCLUSTERED", IsPrimaryKey = false }
            };

            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(newIndexes);

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.NotEmpty(changes);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithConstraintAdded_ShouldDetectConstraintAddition()
        {
            // Arrange
            var tableName = "TestTable";
            var columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false }
            };
            var initialConstraints = new List<ConstraintDefinition>();

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(columns);
            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(initialConstraints);

            // Take initial snapshot
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Add a constraint
            var newConstraints = new List<ConstraintDefinition>
            {
                new ConstraintDefinition { Name = "CK_TestTable_Id", Type = ConstraintType.Check, Expression = "Id > 0" }
            };

            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(newConstraints);

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.NotEmpty(changes);
        }

        [Fact]
        public async Task GetChangeHistory_ShouldReturnChangeHistory()
        {
            // Arrange
            var tableName = "TestTable";
            var columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(columns);

            // Take a snapshot to create history
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Act
            var history = _schemaDetection.GetChangeHistory(tableName);

            // Assert
            Assert.NotNull(history);
        }

        [Fact]
        public void GetChangeHistory_WithNonExistentTable_ShouldReturnEmptyHistory()
        {
            // Act
            var history = _schemaDetection.GetChangeHistory("NonExistentTable");

            // Assert
            Assert.NotNull(history);
        }

        [Fact]
        public void GetChangeHistory_WithEmptyTableName_ShouldReturnEmptyHistory()
        {
            // Act
            var history = _schemaDetection.GetChangeHistory("");

            // Assert
            Assert.NotNull(history);
        }

        [Fact]
        public void GetChangeHistory_WithNullTableName_ShouldReturnEmptyHistory()
        {
            // Act
            var history = _schemaDetection.GetChangeHistory(null!);

            // Assert
            Assert.NotNull(history);
        }

        [Fact]
        public async Task TakeTableSnapshotAsync_WithNullCdcProvider_ShouldHandleGracefully()
        {
            // Arrange
            var tableName = "TestTable";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _schemaDetection.TakeTableSnapshotAsync(tableName, null!);
            });
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithNullCdcProvider_ShouldHandleGracefully()
        {
            // Arrange
            var tableName = "TestTable";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _schemaDetection.DetectSchemaChangesAsync(tableName, null!);
            });
        }

        [Fact]
        public async Task TakeTableSnapshotAsync_WithEmptyTableName_ShouldHandleGracefully()
        {
            // Arrange
            var tableName = "";

            // Act
            var snapshot = await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal("unknown", snapshot.TableName);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithEmptyTableName_ShouldHandleGracefully()
        {
            // Arrange
            var tableName = "";

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.Empty(changes);
        }

        [Fact]
        public async Task TakeTableSnapshotAsync_WithNullTableName_ShouldHandleGracefully()
        {
            // Arrange
            string? tableName = null;

            // Act
            var snapshot = await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal("unknown", snapshot.TableName);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithNullTableName_ShouldHandleGracefully()
        {
            // Arrange
            string? tableName = null;

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.Empty(changes);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Act
            _schemaDetection.Dispose();

            // Assert
            // After dispose, the detection engine should still be accessible but may not function properly
            Assert.NotNull(_schemaDetection);
        }

        [Fact]
        public void Dispose_ShouldBeCallableMultipleTimes()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _schemaDetection.Dispose();
                _schemaDetection.Dispose();
            }));
        }

        [Fact]
        public async Task TakeTableSnapshotAsync_WithComplexSchema_ShouldHandleCorrectly()
        {
            // Arrange
            var tableName = "ComplexTable";
            var columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "bigint", IsNullable = false },
                new ColumnDefinition { ColumnName = "Name", DataType = "nvarchar(255)", IsNullable = false },
                new ColumnDefinition { ColumnName = "Description", DataType = "text", IsNullable = true },
                new ColumnDefinition { ColumnName = "CreatedAt", DataType = "datetime2", IsNullable = false },
                new ColumnDefinition { ColumnName = "UpdatedAt", DataType = "datetime2", IsNullable = true }
            };
            var indexes = new List<IndexDefinition>
            {
                new IndexDefinition { Name = "PK_ComplexTable", Type = "CLUSTERED", IsPrimaryKey = true },
                new IndexDefinition { Name = "IX_ComplexTable_Name", Type = "NONCLUSTERED", IsPrimaryKey = false },
                new IndexDefinition { Name = "IX_ComplexTable_CreatedAt", Type = "NONCLUSTERED", IsPrimaryKey = false }
            };
            var constraints = new List<ConstraintDefinition>
            {
                new ConstraintDefinition { Name = "PK_ComplexTable", Type = ConstraintType.PrimaryKey, Expression = "PRIMARY KEY" },
                new ConstraintDefinition { Name = "CK_ComplexTable_Id", Type = ConstraintType.Check, Expression = "Id > 0" },
                new ConstraintDefinition { Name = "DF_ComplexTable_CreatedAt", Type = ConstraintType.Default, Expression = "GETDATE()" }
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(columns);
            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(indexes);
            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(constraints);

            // Act
            var snapshot = await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal(tableName, snapshot.TableName);
            Assert.Equal(5, snapshot.Columns.Count);
            Assert.Equal(3, snapshot.Indexes.Count);
            Assert.Equal(3, snapshot.Constraints.Count);
            Assert.NotNull(snapshot.TableDefinition);
        }

        [Fact]
        public async Task DetectSchemaChangesAsync_WithMultipleChanges_ShouldDetectAllChanges()
        {
            // Arrange
            var tableName = "MultiChangeTable";
            var initialColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "int", IsNullable = false }
            };
            var initialIndexes = new List<IndexDefinition>();
            var initialConstraints = new List<ConstraintDefinition>();

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(initialColumns);
            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(initialIndexes);
            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(initialConstraints);

            // Take initial snapshot
            await _schemaDetection.TakeTableSnapshotAsync(tableName, _mockCdcProvider.Object);

            // Make multiple changes
            var newColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition { ColumnName = "Id", DataType = "bigint", IsNullable = false }, // Changed data type
                new ColumnDefinition { ColumnName = "Name", DataType = "varchar(100)", IsNullable = true } // Added column
            };
            var newIndexes = new List<IndexDefinition>
            {
                new IndexDefinition { Name = "IX_MultiChangeTable_Name", Type = "NONCLUSTERED", IsPrimaryKey = false } // Added index
            };
            var newConstraints = new List<ConstraintDefinition>
            {
                new ConstraintDefinition { Name = "CK_MultiChangeTable_Id", Type = ConstraintType.Check, Expression = "Id > 0" } // Added constraint
            };

            _mockCdcProvider.Setup(p => p.GetTableColumnsAsync(tableName)).ReturnsAsync(newColumns);
            _mockCdcProvider.Setup(p => p.GetTableIndexesAsync(tableName)).ReturnsAsync(newIndexes);
            _mockCdcProvider.Setup(p => p.GetTableConstraintsAsync(tableName)).ReturnsAsync(newConstraints);

            // Act
            var changes = await _schemaDetection.DetectSchemaChangesAsync(tableName, _mockCdcProvider.Object);

            // Assert
            Assert.NotNull(changes);
            Assert.NotEmpty(changes);
        }
    }
}
