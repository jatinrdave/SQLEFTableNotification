using System;
using System.Collections.Generic;
using System.Linq;
using SQLDBEntityNotifier.Models;
using Xunit;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class ColumnChangeFilterOptionsTests
    {
        [Fact]
        public void Constructor_WithDefaultSettings_ShouldCreateInstance()
        {
            // Act
            var options = new ColumnChangeFilterOptions();

            // Assert
            Assert.NotNull(options);
            Assert.Null(options.MonitoredColumns);
            Assert.Null(options.ExcludedColumns);
            Assert.True(options.IncludeColumnLevelChanges);
            Assert.True(options.IncludeColumnValues);
            Assert.Equal(1, options.MinimumColumnChanges);
            Assert.False(options.CaseSensitiveColumnNames);
            Assert.True(options.NormalizeColumnNames);
            Assert.Null(options.ColumnNameMappings);
            Assert.False(options.IncludeComputedColumns);
            Assert.False(options.IncludeIdentityColumns);
            Assert.False(options.IncludeTimestampColumns);
        }

        [Fact]
        public void Constructor_WithMonitoredColumns_ShouldCreateInstance()
        {
            // Arrange
            var columns = new[] { "Name", "Email", "Status" };

            // Act
            var options = new ColumnChangeFilterOptions(columns);

            // Assert
            Assert.NotNull(options.MonitoredColumns);
            Assert.Equal(3, options.MonitoredColumns.Count);
            Assert.Contains("Name", options.MonitoredColumns);
            Assert.Contains("Email", options.MonitoredColumns);
            Assert.Contains("Status", options.MonitoredColumns);
        }

        [Fact]
        public void Constructor_WithCustomSettings_ShouldCreateInstance()
        {
            // Arrange
            var monitoredColumns = new List<string> { "Name", "Email" };
            var excludedColumns = new List<string> { "Password", "InternalId" };

            // Act
            var options = new ColumnChangeFilterOptions(monitoredColumns, excludedColumns, false);

            // Assert
            Assert.Equal(monitoredColumns, options.MonitoredColumns);
            Assert.Equal(excludedColumns, options.ExcludedColumns);
            Assert.False(options.IncludeColumnLevelChanges);
        }

        [Fact]
        public void MonitorOnly_WithColumns_ShouldCreateInstance()
        {
            // Act
            var options = ColumnChangeFilterOptions.MonitorOnly("Name", "Email", "Status");

            // Assert
            Assert.NotNull(options.MonitoredColumns);
            Assert.Equal(3, options.MonitoredColumns.Count);
            Assert.Contains("Name", options.MonitoredColumns);
            Assert.Contains("Email", options.MonitoredColumns);
            Assert.Contains("Status", options.MonitoredColumns);
        }

        [Fact]
        public void ExcludeColumns_WithColumns_ShouldCreateInstance()
        {
            // Act
            var options = ColumnChangeFilterOptions.ExcludeColumns("Password", "InternalId", "AuditData");

            // Assert
            Assert.NotNull(options.ExcludedColumns);
            Assert.Equal(3, options.ExcludedColumns.Count);
            Assert.Contains("Password", options.ExcludedColumns);
            Assert.Contains("InternalId", options.ExcludedColumns);
            Assert.Contains("AuditData", options.ExcludedColumns);
        }

        [Fact]
        public void MonitorAllExcept_WithColumns_ShouldCreateInstance()
        {
            // Act
            var options = ColumnChangeFilterOptions.MonitorAllExcept("CreatedAt", "UpdatedAt", "Version");

            // Assert
            Assert.NotNull(options.ExcludedColumns);
            Assert.Equal(3, options.ExcludedColumns.Count);
            Assert.Contains("CreatedAt", options.ExcludedColumns);
            Assert.Contains("UpdatedAt", options.ExcludedColumns);
            Assert.Contains("Version", options.ExcludedColumns);
        }

        [Fact]
        public void AddMonitoredColumn_WithValidColumn_ShouldAddColumn()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act
            options.AddMonitoredColumn("Name");

            // Assert
            Assert.NotNull(options.MonitoredColumns);
            Assert.Single(options.MonitoredColumns);
            Assert.Contains("Name", options.MonitoredColumns);
        }

        [Fact]
        public void AddMonitoredColumn_WithDuplicateColumn_ShouldNotAddDuplicate()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act
            options.AddMonitoredColumn("Name");
            options.AddMonitoredColumn("Name");

            // Assert
            Assert.NotNull(options.MonitoredColumns);
            Assert.Single(options.MonitoredColumns);
            Assert.Contains("Name", options.MonitoredColumns);
        }

        [Fact]
        public void AddMonitoredColumns_WithMultipleColumns_ShouldAddAllColumns()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act
            options.AddMonitoredColumns("Name", "Email", "Status");

            // Assert
            Assert.NotNull(options.MonitoredColumns);
            Assert.Equal(3, options.MonitoredColumns.Count);
            Assert.Contains("Name", options.MonitoredColumns);
            Assert.Contains("Email", options.MonitoredColumns);
            Assert.Contains("Status", options.MonitoredColumns);
        }

        [Fact]
        public void AddExcludedColumn_WithValidColumn_ShouldAddColumn()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act
            options.AddExcludedColumn("Password");

            // Assert
            Assert.NotNull(options.ExcludedColumns);
            Assert.Single(options.ExcludedColumns);
            Assert.Contains("Password", options.ExcludedColumns);
        }

        [Fact]
        public void AddExcludedColumn_WithDuplicateColumn_ShouldNotAddDuplicate()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act
            options.AddExcludedColumn("Password");
            options.AddExcludedColumn("Password");

            // Assert
            Assert.NotNull(options.ExcludedColumns);
            Assert.Single(options.ExcludedColumns);
            Assert.Contains("Password", options.ExcludedColumns);
        }

        [Fact]
        public void AddExcludedColumns_WithMultipleColumns_ShouldAddAllColumns()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act
            options.AddExcludedColumns("Password", "InternalId", "AuditData");

            // Assert
            Assert.NotNull(options.ExcludedColumns);
            Assert.Equal(3, options.ExcludedColumns.Count);
            Assert.Contains("Password", options.ExcludedColumns);
            Assert.Contains("InternalId", options.ExcludedColumns);
            Assert.Contains("AuditData", options.ExcludedColumns);
        }

        [Fact]
        public void AddColumnMapping_WithValidMapping_ShouldAddMapping()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act
            options.AddColumnMapping("user_name", "Name");

            // Assert
            Assert.NotNull(options.ColumnNameMappings);
            Assert.Single(options.ColumnNameMappings);
            Assert.Equal("Name", options.ColumnNameMappings["user_name"]);
        }

        [Fact]
        public void AddColumnMapping_WithExistingMapping_ShouldUpdateMapping()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act
            options.AddColumnMapping("user_name", "Name");
            options.AddColumnMapping("user_name", "FullName");

            // Assert
            Assert.NotNull(options.ColumnNameMappings);
            Assert.Single(options.ColumnNameMappings);
            Assert.Equal("FullName", options.ColumnNameMappings["user_name"]);
        }

        [Fact]
        public void ShouldMonitorColumn_WithNoFilter_ShouldReturnTrue()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act & Assert
            Assert.True(options.ShouldMonitorColumn("Name"));
            Assert.True(options.ShouldMonitorColumn("Email"));
            Assert.True(options.ShouldMonitorColumn("Status"));
        }

        [Fact]
        public void ShouldMonitorColumn_WithMonitoredColumns_ShouldReturnTrueForMonitored()
        {
            // Arrange
            var options = ColumnChangeFilterOptions.MonitorOnly("Name", "Email");

            // Act & Assert
            Assert.True(options.ShouldMonitorColumn("Name"));
            Assert.True(options.ShouldMonitorColumn("Email"));
            Assert.False(options.ShouldMonitorColumn("Status"));
            Assert.False(options.ShouldMonitorColumn("Phone"));
        }

        [Fact]
        public void ShouldMonitorColumn_WithExcludedColumns_ShouldReturnFalseForExcluded()
        {
            // Arrange
            var options = ColumnChangeFilterOptions.ExcludeColumns("Password", "InternalId");

            // Act & Assert
            Assert.True(options.ShouldMonitorColumn("Name"));
            Assert.True(options.ShouldMonitorColumn("Email"));
            Assert.False(options.ShouldMonitorColumn("Password"));
            Assert.False(options.ShouldMonitorColumn("InternalId"));
        }

        [Fact]
        public void ShouldMonitorColumn_WithMonitoredAndExcluded_ShouldRespectBoth()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("Name", "Email", "Status")
                .AddExcludedColumns("Password", "InternalId");

            // Act & Assert
            Assert.True(options.ShouldMonitorColumn("Name"));
            Assert.True(options.ShouldMonitorColumn("Email"));
            Assert.True(options.ShouldMonitorColumn("Status"));
            Assert.False(options.ShouldMonitorColumn("Password"));
            Assert.False(options.ShouldMonitorColumn("InternalId"));
            Assert.False(options.ShouldMonitorColumn("Phone")); // Not in monitored list
        }

        [Fact]
        public void ShouldMonitorColumn_WithNullColumn_ShouldReturnFalse()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act & Assert
            Assert.False(options.ShouldMonitorColumn(null!));
            Assert.False(options.ShouldMonitorColumn(""));
            Assert.False(options.ShouldMonitorColumn("   "));
        }

        [Fact]
        public void NormalizeColumnName_WithCaseInsensitive_ShouldReturnLowercase()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions
            {
                CaseSensitiveColumnNames = false,
                NormalizeColumnNames = true
            };

            // Act & Assert
            Assert.Equal("name", options.NormalizeColumnName("Name"));
            Assert.Equal("email", options.NormalizeColumnName("EMAIL"));
            Assert.Equal("status", options.NormalizeColumnName("Status"));
        }

        [Fact]
        public void NormalizeColumnName_WithCaseSensitive_ShouldPreserveCase()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions
            {
                CaseSensitiveColumnNames = true,
                NormalizeColumnNames = true
            };

            // Act & Assert
            Assert.Equal("Name", options.NormalizeColumnName("Name"));
            Assert.Equal("EMAIL", options.NormalizeColumnName("EMAIL"));
            Assert.Equal("Status", options.NormalizeColumnName("Status"));
        }

        [Fact]
        public void NormalizeColumnName_WithNormalizationDisabled_ShouldNotNormalize()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions
            {
                NormalizeColumnNames = false,
                CaseSensitiveColumnNames = true
            };

            // Act
            var result = options.NormalizeColumnName("  Name  ");

            // Assert
            // With NormalizeColumnNames = false and CaseSensitiveColumnNames = true,
            // the method should not trim whitespace or change case
            Assert.Equal("  Name  ", result);
        }

        [Fact]
        public void MapColumnName_WithExistingMapping_ShouldReturnMappedName()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions()
                .AddColumnMapping("user_name", "Name")
                .AddColumnMapping("email_address", "Email");

            // Act & Assert
            Assert.Equal("Name", options.MapColumnName("user_name"));
            Assert.Equal("Email", options.MapColumnName("email_address"));
        }

        [Fact]
        public void MapColumnName_WithNoMapping_ShouldReturnOriginalName()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions();

            // Act & Assert
            Assert.Equal("Name", options.MapColumnName("Name"));
            Assert.Equal("Email", options.MapColumnName("Email"));
        }

        [Fact]
        public void Clone_ShouldCreateDeepCopy()
        {
            // Arrange
            var original = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("Name", "Email")
                .AddExcludedColumns("Password")
                .AddColumnMapping("user_name", "Name");

            original.IncludeColumnLevelChanges = false;
            original.IncludeColumnValues = false;
            original.MinimumColumnChanges = 5;
            original.CaseSensitiveColumnNames = true;
            original.NormalizeColumnNames = false;

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            
            // Properties should be copied
            Assert.Equal(original.IncludeColumnLevelChanges, cloned.IncludeColumnLevelChanges);
            Assert.Equal(original.IncludeColumnValues, cloned.IncludeColumnValues);
            Assert.Equal(original.MinimumColumnChanges, cloned.MinimumColumnChanges);
            Assert.Equal(original.CaseSensitiveColumnNames, cloned.CaseSensitiveColumnNames);
            Assert.Equal(original.NormalizeColumnNames, cloned.NormalizeColumnNames);
            
            // Collections should be new instances
            Assert.NotSame(original.MonitoredColumns, cloned.MonitoredColumns);
            Assert.NotSame(original.ExcludedColumns, cloned.ExcludedColumns);
            Assert.NotSame(original.ColumnNameMappings, cloned.ColumnNameMappings);
            
            // But content should be the same
            Assert.Equal(original.MonitoredColumns?.Count, cloned.MonitoredColumns?.Count);
            Assert.Equal(original.ExcludedColumns?.Count, cloned.ExcludedColumns?.Count);
            Assert.Equal(original.ColumnNameMappings?.Count, cloned.ColumnNameMappings?.Count);
        }

        [Fact]
        public void Clone_WithNullCollections_ShouldHandleCorrectly()
        {
            // Arrange
            var original = new ColumnChangeFilterOptions
            {
                MonitoredColumns = null,
                ExcludedColumns = null,
                ColumnNameMappings = null
            };

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.NotNull(cloned);
            Assert.Null(cloned.MonitoredColumns);
            Assert.Null(cloned.ExcludedColumns);
            Assert.Null(cloned.ColumnNameMappings);
        }

        [Fact]
        public void ShouldMonitorColumn_WithCaseInsensitive_ShouldWorkCorrectly()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("Name", "Email")
                .AddExcludedColumns("Password");

            options.CaseSensitiveColumnNames = false;

            // Act & Assert
            Assert.True(options.ShouldMonitorColumn("name"));
            Assert.True(options.ShouldMonitorColumn("NAME"));
            Assert.True(options.ShouldMonitorColumn("Name"));
            Assert.True(options.ShouldMonitorColumn("email"));
            Assert.True(options.ShouldMonitorColumn("EMAIL"));
            Assert.True(options.ShouldMonitorColumn("Email"));
            Assert.False(options.ShouldMonitorColumn("password"));
            Assert.False(options.ShouldMonitorColumn("PASSWORD"));
            Assert.False(options.ShouldMonitorColumn("Password"));
        }

        [Fact]
        public void ShouldMonitorColumn_WithCaseSensitive_ShouldWorkCorrectly()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("Name", "Email")
                .AddExcludedColumns("Password");

            options.CaseSensitiveColumnNames = true;

            // Act & Assert
            Assert.True(options.ShouldMonitorColumn("Name"));
            Assert.False(options.ShouldMonitorColumn("name"));
            Assert.False(options.ShouldMonitorColumn("NAME"));
            Assert.True(options.ShouldMonitorColumn("Email"));
            Assert.False(options.ShouldMonitorColumn("email"));
            Assert.False(options.ShouldMonitorColumn("EMAIL"));
            Assert.False(options.ShouldMonitorColumn("Password"));
            Assert.False(options.ShouldMonitorColumn("password"));
            Assert.False(options.ShouldMonitorColumn("PASSWORD"));
        }

        [Fact]
        public void ShouldMonitorColumn_WithWhitespace_ShouldHandleCorrectly()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("  Name  ", "  Email  ");

            options.NormalizeColumnNames = true;
            options.CaseSensitiveColumnNames = false; // Need this for case-insensitive comparison

            // Act & Assert
            Assert.True(options.ShouldMonitorColumn("Name"));
            Assert.True(options.ShouldMonitorColumn("  Name  "));
            Assert.True(options.ShouldMonitorColumn("Email"));
            Assert.True(options.ShouldMonitorColumn("  Email  "));
        }

        [Fact]
        public void ShouldMonitorColumn_WithComplexScenario_ShouldWorkCorrectly()
        {
            // Arrange
            var options = new ColumnChangeFilterOptions()
                .AddMonitoredColumns("Name", "Email", "Status")
                .AddExcludedColumns("Password", "InternalId", "AuditData");

            options.CaseSensitiveColumnNames = false;
            options.NormalizeColumnNames = true;

            // Act & Assert
            // Monitored columns (case-insensitive)
            Assert.True(options.ShouldMonitorColumn("Name"));
            Assert.True(options.ShouldMonitorColumn("name"));
            Assert.True(options.ShouldMonitorColumn("NAME"));
            Assert.True(options.ShouldMonitorColumn("Email"));
            Assert.True(options.ShouldMonitorColumn("email"));
            Assert.True(options.ShouldMonitorColumn("EMAIL"));
            Assert.True(options.ShouldMonitorColumn("Status"));
            Assert.True(options.ShouldMonitorColumn("status"));
            Assert.True(options.ShouldMonitorColumn("STATUS"));

            // Excluded columns (case-insensitive)
            Assert.False(options.ShouldMonitorColumn("Password"));
            Assert.False(options.ShouldMonitorColumn("password"));
            Assert.False(options.ShouldMonitorColumn("PASSWORD"));
            Assert.False(options.ShouldMonitorColumn("InternalId"));
            Assert.False(options.ShouldMonitorColumn("internalid"));
            Assert.False(options.ShouldMonitorColumn("INTERNALID"));

            // Other columns (not monitored)
            Assert.False(options.ShouldMonitorColumn("Phone"));
            Assert.False(options.ShouldMonitorColumn("Address"));
            Assert.False(options.ShouldMonitorColumn("CreatedAt"));
        }
    }
}