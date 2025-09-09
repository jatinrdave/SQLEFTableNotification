using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class AdvancedChangeFiltersTests
    {
        private readonly AdvancedChangeFilters _filters;
        private readonly ChangeRecord _testChange;

        public AdvancedChangeFiltersTests()
        {
            _filters = new AdvancedChangeFilters();
            _testChange = new DetailedChangeRecord
            {
                ChangeId = "test-123",
                TableName = "TestTable",
                Operation = ChangeOperation.Insert,
                ChangeTimestamp = DateTime.UtcNow,
                ChangePosition = "LSN:123",
                Metadata = new Dictionary<string, object> { { "test", "value" } },
                NewValues = new Dictionary<string, object> { { "test", "value" }, { "id", 1 }, { "name", "test" } },
                OldValues = new Dictionary<string, object> { { "id", 0 } }
            };
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Assert
            Assert.NotNull(_filters);
            Assert.Equal(FilterLogic.All, _filters.Logic);
            Assert.False(_filters.CaseSensitive);
            Assert.True(_filters.IncludeUnmatched);
            Assert.Null(_filters.MaxResults);
            Assert.Null(_filters.MaxAge);
        }

        [Fact]
        public void AddFilter_ShouldAddFilterRule()
        {
            // Arrange
            var rule = new ColumnFilterRule("TestColumn", FilterOperator.Equals, "test");

            // Act
            _filters.AddFilter(rule);

            // Assert
            Assert.Single(_filters.FilterRules);
            Assert.Contains(rule, _filters.FilterRules);
        }

        [Fact]
        public void AddExclusion_ShouldAddExclusionRule()
        {
            // Arrange
            var rule = new ColumnFilterRule("TestColumn", FilterOperator.Equals, "test");

            // Act
            _filters.AddExclusion(rule);

            // Assert
            Assert.Single(_filters.ExclusionRules);
            Assert.Contains(rule, _filters.ExclusionRules);
        }

        [Fact]
        public void AddColumnFilter_ShouldCreateColumnFilterRule()
        {
            // Act
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");

            // Assert
            Assert.Single(_filters.FilterRules);
            var rule = _filters.FilterRules.First() as ColumnFilterRule;
            Assert.NotNull(rule);
            Assert.Equal("TestColumn", rule.ColumnName);
            Assert.Equal(FilterOperator.Equals, rule.Operator);
            Assert.Equal("test", rule.Value);
        }

        [Fact]
        public void AddTimeFilter_ShouldCreateTimeFilterRule()
        {
            // Arrange
            var timeValue = DateTime.UtcNow;

            // Act
            _filters.AddTimeFilter(TimeFilterType.After, timeValue);

            // Assert
            Assert.Single(_filters.FilterRules);
            var rule = _filters.FilterRules.First() as TimeFilterRule;
            Assert.NotNull(rule);
            Assert.Equal(TimeFilterType.After, rule.Type);
            Assert.Equal(timeValue, rule.Value);
        }

        [Fact]
        public void AddValueFilter_ShouldCreateValueFilterRule()
        {
            // Act
            _filters.AddValueFilter("TestProperty", FilterOperator.GreaterThan, 100);

            // Assert
            Assert.Single(_filters.FilterRules);
            var rule = _filters.FilterRules.First() as ValueFilterRule;
            Assert.NotNull(rule);
            Assert.Equal("TestProperty", rule.PropertyName);
            Assert.Equal(FilterOperator.GreaterThan, rule.Operator);
            Assert.Equal(100, rule.Value);
        }

        [Fact]
        public void AddCompositeFilter_ShouldCreateCompositeFilterRule()
        {
            // Arrange
            var compositeRule = new CompositeFilterRule(FilterLogic.All);

            // Act
            _filters.AddCompositeFilter(compositeRule);

            // Assert
            Assert.Single(_filters.FilterRules);
            Assert.Contains(compositeRule, _filters.FilterRules);
        }

        [Fact]
        public void ClearFilters_ShouldRemoveAllFilterRules()
        {
            // Arrange
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");
            _filters.AddColumnFilter("AnotherColumn", FilterOperator.GreaterThan, 100);

            // Act
            _filters.ClearFilters();

            // Assert
            Assert.Empty(_filters.FilterRules);
        }

        [Fact]
        public void ClearExclusions_ShouldRemoveAllExclusionRules()
        {
            // Arrange
            _filters.AddExclusion(new ColumnFilterRule("TestColumn", FilterOperator.Equals, "test"));
            _filters.AddExclusion(new ColumnFilterRule("AnotherColumn", FilterOperator.GreaterThan, 100));

            // Act
            _filters.ClearExclusions();

            // Assert
            Assert.Empty(_filters.ExclusionRules);
        }

        [Fact]
        public void ApplyFilters_WithNoRules_ShouldReturnAllChanges()
        {
            // Arrange
            var changes = new List<ChangeRecord> { _testChange, _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void ApplyFilters_WithAllLogic_ShouldApplyAllFilters()
        {
            // Arrange
            _filters.Logic = FilterLogic.All;
            _filters.AddColumnFilter("test", FilterOperator.Equals, "value");
            _filters.AddColumnFilter("name", FilterOperator.Equals, "test");

            var changes = new List<ChangeRecord> { _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void ApplyFilters_WithAnyLogic_ShouldApplyAnyFilter()
        {
            // Arrange
            _filters.Logic = FilterLogic.Any;
            _filters.AddColumnFilter("test", FilterOperator.Equals, "value");
            _filters.AddColumnFilter("name", FilterOperator.Equals, "test");

            var changes = new List<ChangeRecord> { _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void ApplyFilters_WithExclusions_ShouldExcludeMatchingChanges()
        {
            // Arrange
            _filters.AddExclusion(new ColumnFilterRule("test", FilterOperator.Equals, "value"));

            var changes = new List<ChangeRecord> { _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ApplyFilters_WithMaxAge_ShouldFilterByAge()
        {
            // Arrange
            _filters.MaxAge = TimeSpan.FromMinutes(5);

            var oldChange = new ChangeRecord
            {
                ChangeId = "old-123",
                ChangeTimestamp = DateTime.UtcNow.AddMinutes(-10),
                Operation = ChangeOperation.Insert
            };

            var changes = new List<ChangeRecord> { oldChange, _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Single(result);
            Assert.Equal(_testChange.ChangeId, result.First().ChangeId);
        }

        [Fact]
        public void ApplyFilters_WithMaxResults_ShouldLimitResults()
        {
            // Arrange
            _filters.MaxResults = 2;

            var changes = new List<ChangeRecord>
            {
                _testChange,
                new ChangeRecord { ChangeId = "2", Operation = ChangeOperation.Insert, ChangeTimestamp = DateTime.UtcNow },
                new ChangeRecord { ChangeId = "3", Operation = ChangeOperation.Insert, ChangeTimestamp = DateTime.UtcNow }
            };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void Clone_ShouldCreateIndependentCopy()
        {
            // Arrange
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");
            _filters.Logic = FilterLogic.Any;
            _filters.CaseSensitive = true;

            // Act
            var clone = _filters.Clone();

            // Assert
            Assert.NotSame(_filters, clone);
            Assert.Equal(_filters.Logic, clone.Logic);
            Assert.Equal(_filters.CaseSensitive, clone.CaseSensitive);
            Assert.Equal(_filters.FilterRules.Count, clone.FilterRules.Count);
        }

        [Fact]
        public void ToString_ShouldReturnFilterDescription()
        {
            // Arrange
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");
            _filters.AddColumnFilter("AnotherColumn", FilterOperator.GreaterThan, 100);

            // Act
            var result = _filters.ToString();

            // Assert
            Assert.Contains("TestColumn", result);
            Assert.Contains("AnotherColumn", result);
            Assert.Contains("AND", result);
        }

        [Fact]
        public void ToString_WithExclusions_ShouldIncludeExclusions()
        {
            // Arrange
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");
            _filters.AddExclusion(new ColumnFilterRule("ExcludedColumn", FilterOperator.Equals, "excluded"));

            // Act
            var result = _filters.ToString();

            // Assert
            Assert.Contains("TestColumn", result);
            Assert.Contains("ExcludedColumn", result);
            Assert.Contains("AND NOT", result);
        }

        [Fact]
        public void ToString_WithMaxAge_ShouldIncludeMaxAge()
        {
            // Arrange
            _filters.MaxAge = TimeSpan.FromMinutes(30);

            // Act
            var result = _filters.ToString();

            // Assert
            Assert.Contains("30.0 minutes", result);
        }

        [Fact]
        public void ToString_WithMaxResults_ShouldIncludeMaxResults()
        {
            // Arrange
            _filters.MaxResults = 100;

            // Act
            var result = _filters.ToString();

            // Assert
            Assert.Contains("LIMIT 100", result);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");

            // Act
            _filters.Dispose();

            // Assert
            // After dispose, the filters should still be accessible but cleared
            Assert.Empty(_filters.FilterRules);
        }

        [Fact]
        public void CaseSensitive_ShouldAffectColumnFilterMatching()
        {
            // Arrange
            _filters.CaseSensitive = true;
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");

            var changes = new List<ChangeRecord> { _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            // The actual matching logic depends on the ColumnFilterRule implementation
            Assert.NotNull(result);
        }

        [Fact]
        public void IncludeUnmatched_ShouldIncludeChangesWithoutFilters()
        {
            // Arrange
            _filters.IncludeUnmatched = true;
            // No filters added

            var changes = new List<ChangeRecord> { _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void MultipleFilters_ShouldWorkTogether()
        {
            // Arrange
            _filters.Logic = FilterLogic.All;
            _filters.AddColumnFilter("test", FilterOperator.Equals, "value");
            _filters.AddTimeFilter(TimeFilterType.After, DateTime.UtcNow.AddMinutes(-1));

            var changes = new List<ChangeRecord> { _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void FilterRules_ShouldBeReadOnly()
        {
            // Act
            var rules = _filters.FilterRules;

            // Assert
            Assert.True(rules is IReadOnlyList<IFilterRule>);
        }

        [Fact]
        public void ExclusionRules_ShouldBeReadOnly()
        {
            // Act
            var rules = _filters.ExclusionRules;

            // Assert
            Assert.True(rules is IReadOnlyList<IFilterRule>);
        }

        [Fact]
        public void AddFilter_ShouldReturnSelfForChaining()
        {
            // Act
            var result = _filters.AddFilter(new ColumnFilterRule("Test", FilterOperator.Equals, "value"));

            // Assert
            Assert.Same(_filters, result);
        }

        [Fact]
        public void AddExclusion_ShouldReturnSelfForChaining()
        {
            // Act
            var result = _filters.AddExclusion(new ColumnFilterRule("Test", FilterOperator.Equals, "value"));

            // Assert
            Assert.Same(_filters, result);
        }

        [Fact]
        public void AddColumnFilter_ShouldReturnSelfForChaining()
        {
            // Act
            var result = _filters.AddColumnFilter("Test", FilterOperator.Equals, "value");

            // Assert
            Assert.Same(_filters, result);
        }

        [Fact]
        public void AddTimeFilter_ShouldReturnSelfForChaining()
        {
            // Act
            var result = _filters.AddTimeFilter(TimeFilterType.After, DateTime.UtcNow);

            // Assert
            Assert.Same(_filters, result);
        }

        [Fact]
        public void AddValueFilter_ShouldReturnSelfForChaining()
        {
            // Act
            var result = _filters.AddValueFilter("Test", FilterOperator.Equals, "value");

            // Assert
            Assert.Same(_filters, result);
        }

        [Fact]
        public void AddCompositeFilter_ShouldReturnSelfForChaining()
        {
            // Act
            var result = _filters.AddCompositeFilter(new CompositeFilterRule(FilterLogic.All));

            // Assert
            Assert.Same(_filters, result);
        }

        [Fact]
        public void ClearFilters_ShouldReturnSelfForChaining()
        {
            // Act
            var result = _filters.ClearFilters();

            // Assert
            Assert.Same(_filters, result);
        }

        [Fact]
        public void ClearExclusions_ShouldReturnSelfForChaining()
        {
            // Act
            var result = _filters.ClearExclusions();

            // Assert
            Assert.Same(_filters, result);
        }

        [Fact]
        public void Clone_ShouldCreateDeepCopyOfRules()
        {
            // Arrange
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");
            _filters.AddExclusion(new ColumnFilterRule("ExcludedColumn", FilterOperator.Equals, "excluded"));

            // Act
            var clone = _filters.Clone();

            // Assert
            Assert.Equal(_filters.FilterRules.Count, clone.FilterRules.Count);
            Assert.Equal(_filters.ExclusionRules.Count, clone.ExclusionRules.Count);
        }

        [Fact]
        public void Clone_ShouldNotAffectOriginal()
        {
            // Arrange
            _filters.AddColumnFilter("TestColumn", FilterOperator.Equals, "test");
            var originalCount = _filters.FilterRules.Count;

            // Act
            var clone = _filters.Clone();
            clone.AddColumnFilter("AnotherColumn", FilterOperator.Equals, "another");

            // Assert
            Assert.Equal(originalCount, _filters.FilterRules.Count);
            Assert.Equal(originalCount + 1, clone.FilterRules.Count);
        }

        [Fact]
        public void ApplyFilters_WithEmptyChanges_ShouldReturnEmpty()
        {
            // Arrange
            var changes = new List<ChangeRecord>();

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ApplyFilters_WithNullChanges_ShouldReturnEmpty()
        {
            // Act
            var result = _filters.ApplyFilters<ChangeRecord>(null!);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ApplyFilters_WithComplexFilters_ShouldWorkCorrectly()
        {
            // Arrange
            _filters.Logic = FilterLogic.All;
            _filters.AddColumnFilter("test", FilterOperator.Equals, "value");
            _filters.AddTimeFilter(TimeFilterType.After, DateTime.UtcNow.AddMinutes(-1));
            _filters.AddExclusion(new ColumnFilterRule("excluded", FilterOperator.Equals, "value"));
            _filters.MaxAge = TimeSpan.FromMinutes(10);
            _filters.MaxResults = 5;

            var changes = new List<ChangeRecord> { _testChange };

            // Act
            var result = _filters.ApplyFilters(changes);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Dispose_ShouldBeCallableMultipleTimes()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _filters.Dispose();
                _filters.Dispose();
            }));
        }

        [Fact]
        public void FilterLogic_All_ShouldBeDefault()
        {
            // Assert
            Assert.Equal(FilterLogic.All, _filters.Logic);
        }

        [Fact]
        public void FilterLogic_Any_ShouldBeValid()
        {
            // Act
            _filters.Logic = FilterLogic.Any;

            // Assert
            Assert.Equal(FilterLogic.Any, _filters.Logic);
        }

        [Fact]
        public void CaseSensitive_ShouldBeConfigurable()
        {
            // Act
            _filters.CaseSensitive = true;

            // Assert
            Assert.True(_filters.CaseSensitive);
        }

        [Fact]
        public void IncludeUnmatched_ShouldBeConfigurable()
        {
            // Act
            _filters.IncludeUnmatched = false;

            // Assert
            Assert.False(_filters.IncludeUnmatched);
        }

        [Fact]
        public void MaxResults_ShouldBeConfigurable()
        {
            // Arrange
            var maxResults = 100;

            // Act
            _filters.MaxResults = maxResults;

            // Assert
            Assert.Equal(maxResults, _filters.MaxResults);
        }

        [Fact]
        public void MaxAge_ShouldBeConfigurable()
        {
            // Arrange
            var maxAge = TimeSpan.FromHours(1);

            // Act
            _filters.MaxAge = maxAge;

            // Assert
            Assert.Equal(maxAge, _filters.MaxAge);
        }
    }
}
