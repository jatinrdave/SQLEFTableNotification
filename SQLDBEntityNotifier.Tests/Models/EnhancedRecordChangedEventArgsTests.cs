using System;
using System.Collections.Generic;
using SQLDBEntityNotifier.Models;
using Xunit;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class EnhancedRecordChangedEventArgsTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange
            var entities = new List<TestEntity>
            {
                new TestEntity { Id = 1, Name = "Test1" },
                new TestEntity { Id = 2, Name = "Test2" }
            };
            var changeVersion = 123L;
            var changeDetectedAt = DateTime.UtcNow;

            // Act
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Entities = entities,
                ChangeVersion = changeVersion,
                ChangeDetectedAt = changeDetectedAt
            };

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal(entities, eventArgs.Entities);
            Assert.Equal(changeVersion, eventArgs.ChangeVersion);
            Assert.Equal(changeDetectedAt, eventArgs.ChangeDetectedAt);
        }

        [Fact]
        public void Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>();

            // Act & Assert
            var operation = ChangeOperation.Insert;
            eventArgs.Operation = operation;
            Assert.Equal(operation, eventArgs.Operation);

            var databaseType = DatabaseType.MySql;
            eventArgs.DatabaseType = databaseType;
            Assert.Equal(databaseType, eventArgs.DatabaseType);

            var changeIdentifier = "change_123";
            eventArgs.ChangeIdentifier = changeIdentifier;
            Assert.Equal(changeIdentifier, eventArgs.ChangeIdentifier);

            var databaseChangeTimestamp = DateTime.UtcNow;
            eventArgs.DatabaseChangeTimestamp = databaseChangeTimestamp;
            Assert.Equal(databaseChangeTimestamp, eventArgs.DatabaseChangeTimestamp);

            var changedBy = "test_user";
            eventArgs.ChangedBy = changedBy;
            Assert.Equal(changedBy, eventArgs.ChangedBy);

            var applicationName = "TestApp";
            eventArgs.ApplicationName = applicationName;
            Assert.Equal(applicationName, eventArgs.ApplicationName);

            var hostName = "localhost";
            eventArgs.HostName = hostName;
            Assert.Equal(hostName, eventArgs.HostName);

            var metadata = new Dictionary<string, object> { ["key"] = "value" };
            eventArgs.Metadata = metadata;
            Assert.Equal(metadata, eventArgs.Metadata);

            var oldValues = new TestEntity { Id = 1, Name = "OldName" };
            eventArgs.OldValues = oldValues;
            Assert.Equal(oldValues, eventArgs.OldValues);

            var newValues = new TestEntity { Id = 1, Name = "NewName" };
            eventArgs.NewValues = newValues;
            Assert.Equal(newValues, eventArgs.NewValues);

            var affectedColumns = new List<string> { "Name", "UpdatedAt" };
            eventArgs.AffectedColumns = affectedColumns;
            Assert.Equal(affectedColumns, eventArgs.AffectedColumns);

            var transactionId = "txn_456";
            eventArgs.TransactionId = transactionId;
            Assert.Equal(transactionId, eventArgs.TransactionId);

            var isBatchOperation = true;
            eventArgs.IsBatchOperation = isBatchOperation;
            Assert.Equal(isBatchOperation, eventArgs.IsBatchOperation);

            var batchSequence = 5;
            eventArgs.BatchSequence = batchSequence;
            Assert.Equal(batchSequence, eventArgs.BatchSequence);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Act
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>();

            // Assert
            Assert.Equal(ChangeOperation.Unknown, eventArgs.Operation);
            Assert.Equal(DatabaseType.SqlServer, eventArgs.DatabaseType);
            Assert.Null(eventArgs.ChangeIdentifier);
            Assert.Null(eventArgs.DatabaseChangeTimestamp);
            Assert.Null(eventArgs.ChangedBy);
            Assert.Null(eventArgs.ApplicationName);
            Assert.Null(eventArgs.HostName);
            Assert.Null(eventArgs.Metadata);
            Assert.Null(eventArgs.OldValues);
            Assert.Null(eventArgs.NewValues);
            Assert.Null(eventArgs.AffectedColumns);
            Assert.Null(eventArgs.TransactionId);
            Assert.False(eventArgs.IsBatchOperation);
            Assert.Null(eventArgs.BatchSequence);
        }

        [Fact]
        public void Clone_ShouldCreateDeepCopy()
        {
            // Arrange
            var original = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Entities = new List<TestEntity> { new TestEntity { Id = 1, Name = "Test" } },
                ChangeVersion = 123L,
                ChangeDetectedAt = DateTime.UtcNow,
                Operation = ChangeOperation.Update,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123",
                DatabaseChangeTimestamp = DateTime.UtcNow.AddMinutes(-5),
                ChangedBy = "test_user",
                ApplicationName = "TestApp",
                HostName = "localhost",
                Metadata = new Dictionary<string, object> { ["key"] = "value" },
                OldValues = new TestEntity { Id = 1, Name = "OldName" },
                NewValues = new TestEntity { Id = 1, Name = "NewName" },
                AffectedColumns = new List<string> { "Name" },
                TransactionId = "txn_456",
                IsBatchOperation = true,
                BatchSequence = 1
            };

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.Equal(original.ChangeVersion, cloned.ChangeVersion);
            Assert.Equal(original.ChangeDetectedAt, cloned.ChangeDetectedAt);
            Assert.Equal(original.Operation, cloned.Operation);
            Assert.Equal(original.DatabaseType, cloned.DatabaseType);
            Assert.Equal(original.ChangeIdentifier, cloned.ChangeIdentifier);
            Assert.Equal(original.DatabaseChangeTimestamp, cloned.DatabaseChangeTimestamp);
            Assert.Equal(original.ChangedBy, cloned.ChangedBy);
            Assert.Equal(original.ApplicationName, cloned.ApplicationName);
            Assert.Equal(original.HostName, cloned.HostName);
            Assert.Equal(original.TransactionId, cloned.TransactionId);
            Assert.Equal(original.IsBatchOperation, cloned.IsBatchOperation);
            Assert.Equal(original.BatchSequence, cloned.BatchSequence);

            // Collections should be new instances
            Assert.NotSame(original.Entities, cloned.Entities);
            Assert.NotSame(original.Metadata, cloned.Metadata);
            Assert.NotSame(original.AffectedColumns, cloned.AffectedColumns);
            Assert.NotSame(original.OldValues, cloned.OldValues);
            Assert.NotSame(original.NewValues, cloned.NewValues);

            // But content should be the same
            Assert.Equal(original.Entities.Count, cloned.Entities.Count);
            Assert.Equal(original.Metadata.Count, cloned.Metadata.Count);
            Assert.Equal(original.AffectedColumns.Count, cloned.AffectedColumns.Count);
        }

        [Fact]
        public void Clone_WithNullCollections_ShouldHandleCorrectly()
        {
            // Arrange
            var original = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Entities = null,
                Metadata = null,
                AffectedColumns = null,
                OldValues = null,
                NewValues = null
            };

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.NotNull(cloned);
            Assert.Null(cloned.Entities);
            Assert.Null(cloned.Metadata);
            Assert.Null(cloned.AffectedColumns);
            Assert.Null(cloned.OldValues);
            Assert.Null(cloned.NewValues);
        }

        [Fact]
        public void ToString_ShouldReturnReadableRepresentation()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Insert,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123",
                ChangedBy = "test_user",
                ApplicationName = "TestApp"
            };

            // Act
            var result = eventArgs.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Insert", result);
            Assert.Contains("MySql", result);
            Assert.Contains("change_123", result);
            Assert.Contains("test_user", result);
            Assert.Contains("TestApp", result);
        }

        [Fact]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var eventArgs1 = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Update,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123"
            };

            var eventArgs2 = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Update,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123"
            };

            // Act & Assert
            Assert.True(eventArgs1.Equals(eventArgs2));
            Assert.True(eventArgs1.Equals((object)eventArgs2));
        }

        [Fact]
        public void Equals_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var eventArgs1 = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Update,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123"
            };

            var eventArgs2 = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Insert,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123"
            };

            // Act & Assert
            Assert.False(eventArgs1.Equals(eventArgs2));
            Assert.False(eventArgs1.Equals((object)eventArgs2));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>();

            // Act & Assert
            Assert.False(eventArgs.Equals(null));
            Assert.False(eventArgs.Equals((object)null));
        }

        [Fact]
        public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
        {
            // Arrange
            var eventArgs1 = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Update,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123"
            };

            var eventArgs2 = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Update,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123"
            };

            // Act & Assert
            Assert.Equal(eventArgs1.GetHashCode(), eventArgs2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
        {
            // Arrange
            var eventArgs1 = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Update,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123"
            };

            var eventArgs2 = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Insert,
                DatabaseType = DatabaseType.MySql,
                ChangeIdentifier = "change_123"
            };

            // Act & Assert
            Assert.NotEqual(eventArgs1.GetHashCode(), eventArgs2.GetHashCode());
        }

        [Fact]
        public void IsSignificantChange_WithUpdateOperation_ShouldReturnTrue()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Update,
                AffectedColumns = new List<string> { "Name", "UpdatedAt" }
            };

            // Act
            var isSignificant = eventArgs.IsSignificantChange;

            // Assert
            Assert.True(isSignificant);
        }

        [Fact]
        public void IsSignificantChange_WithInsertOperation_ShouldReturnTrue()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Insert
            };

            // Act
            var isSignificant = eventArgs.IsSignificantChange;

            // Assert
            Assert.True(isSignificant);
        }

        [Fact]
        public void IsSignificantChange_WithDeleteOperation_ShouldReturnTrue()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Delete
            };

            // Act
            var isSignificant = eventArgs.IsSignificantChange;

            // Assert
            Assert.True(isSignificant);
        }

        [Fact]
        public void IsSignificantChange_WithSchemaChangeOperation_ShouldReturnTrue()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.SchemaChange
            };

            // Act
            var isSignificant = eventArgs.IsSignificantChange;

            // Assert
            Assert.True(isSignificant);
        }

        [Fact]
        public void IsSignificantChange_WithUnknownOperation_ShouldReturnFalse()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Operation = ChangeOperation.Unknown
            };

            // Act
            var isSignificant = eventArgs.IsSignificantChange;

            // Assert
            Assert.False(isSignificant);
        }

        [Fact]
        public void HasMetadata_WithMetadata_ShouldReturnTrue()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                Metadata = new Dictionary<string, object> { ["key"] = "value" }
            };

            // Act
            var hasMetadata = eventArgs.HasMetadata;

            // Assert
            Assert.True(hasMetadata);
        }

        [Fact]
        public void HasMetadata_WithoutMetadata_ShouldReturnFalse()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>();

            // Act
            var hasMetadata = eventArgs.HasMetadata;

            // Assert
            Assert.False(hasMetadata);
        }

        [Fact]
        public void HasAffectedColumns_WithAffectedColumns_ShouldReturnTrue()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>
            {
                AffectedColumns = new List<string> { "Name", "UpdatedAt" }
            };

            // Act
            var hasAffectedColumns = eventArgs.HasAffectedColumns;

            // Assert
            Assert.True(hasAffectedColumns);
        }

        [Fact]
        public void HasAffectedColumns_WithoutAffectedColumns_ShouldReturnFalse()
        {
            // Arrange
            var eventArgs = new EnhancedRecordChangedEventArgs<TestEntity>();

            // Act
            var hasAffectedColumns = eventArgs.HasAffectedColumns;

            // Assert
            Assert.False(hasAffectedColumns);
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