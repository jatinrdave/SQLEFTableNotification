using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Advanced filtering engine for CDC changes with support for complex filtering rules
    /// </summary>
    public class AdvancedChangeFilters : IDisposable
    {
        private readonly List<IFilterRule> _filterRules = new();
        private readonly List<IFilterRule> _exclusionRules = new();

        /// <summary>
        /// Gets the filter rules
        /// </summary>
        public IReadOnlyList<IFilterRule> FilterRules => _filterRules.AsReadOnly();

        /// <summary>
        /// Gets the exclusion rules
        /// </summary>
        public IReadOnlyList<IFilterRule> ExclusionRules => _exclusionRules.AsReadOnly();

        /// <summary>
        /// Gets or sets whether all rules must pass (AND logic) or any rule can pass (OR logic)
        /// </summary>
        public FilterLogic Logic { get; set; } = FilterLogic.All;

        /// <summary>
        /// Gets or sets whether the filtering is case-sensitive
        /// </summary>
        public bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include changes that don't match any filter rules
        /// </summary>
        public bool IncludeUnmatched { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of changes to return
        /// </summary>
        public int? MaxResults { get; set; }

        /// <summary>
        /// Gets or sets the maximum age of changes to consider
        /// </summary>
        public TimeSpan? MaxAge { get; set; }

        /// <summary>
        /// Adds a filter rule
        /// </summary>
        public AdvancedChangeFilters AddFilter(IFilterRule rule)
        {
            _filterRules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds an exclusion rule
        /// </summary>
        public AdvancedChangeFilters AddExclusion(IFilterRule rule)
        {
            _exclusionRules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds a column-based filter
        /// </summary>
        public AdvancedChangeFilters AddColumnFilter(string columnName, FilterOperator op, object value)
        {
            return AddFilter(new ColumnFilterRule(columnName, op, value, CaseSensitive));
        }

        /// <summary>
        /// Adds a time-based filter
        /// </summary>
        public AdvancedChangeFilters AddTimeFilter(TimeFilterType type, DateTime value)
        {
            return AddFilter(new TimeFilterRule(type, value));
        }

        /// <summary>
        /// Adds a value-based filter
        /// </summary>
        public AdvancedChangeFilters AddValueFilter(string propertyName, FilterOperator op, object value)
        {
            return AddFilter(new ValueFilterRule(propertyName, op, value, CaseSensitive));
        }

        /// <summary>
        /// Adds a composite filter
        /// </summary>
        public AdvancedChangeFilters AddCompositeFilter(CompositeFilterRule rule)
        {
            return AddFilter(rule);
        }

        /// <summary>
        /// Clears all filter rules
        /// </summary>
        public AdvancedChangeFilters ClearFilters()
        {
            _filterRules.Clear();
            return this;
        }

        /// <summary>
        /// Clears all exclusion rules
        /// </summary>
        public AdvancedChangeFilters ClearExclusions()
        {
            _exclusionRules.Clear();
            return this;
        }

        /// <summary>
        /// Applies the filters to a collection of changes
        /// </summary>
        public IEnumerable<T> ApplyFilters<T>(IEnumerable<T> changes) where T : class
        {
            if (changes == null) return Enumerable.Empty<T>();

            var filteredChanges = changes.AsEnumerable();

            // Apply inclusion filters
            if (_filterRules.Any())
            {
                if (Logic == FilterLogic.All)
                {
                    filteredChanges = filteredChanges.Where(change => _filterRules.All(rule => rule.Matches(change)));
                }
                else
                {
                    filteredChanges = filteredChanges.Where(change => _filterRules.Any(rule => rule.Matches(change)));
                }
            }

            // Apply exclusion filters
            if (_exclusionRules.Any())
            {
                filteredChanges = filteredChanges.Where(change => !_exclusionRules.Any(rule => rule.Matches(change)));
            }

            // Apply max age filter
            if (MaxAge.HasValue)
            {
                var cutoffTime = DateTime.UtcNow.Subtract(MaxAge.Value);
                filteredChanges = filteredChanges.Where(change => 
                {
                    if (change is ChangeRecord record)
                        return record.ChangeTimestamp >= cutoffTime;
                    return true;
                });
            }

            // Apply max results limit
            if (MaxResults.HasValue)
            {
                filteredChanges = filteredChanges.Take(MaxResults.Value);
            }

            return filteredChanges;
        }

        /// <summary>
        /// Creates a copy of this filter configuration
        /// </summary>
        public AdvancedChangeFilters Clone()
        {
            var clone = new AdvancedChangeFilters
            {
                Logic = this.Logic,
                CaseSensitive = this.CaseSensitive,
                IncludeUnmatched = this.IncludeUnmatched,
                MaxResults = this.MaxResults,
                MaxAge = this.MaxAge
            };

            foreach (var rule in _filterRules)
            {
                clone._filterRules.Add(rule.Clone());
            }

            foreach (var rule in _exclusionRules)
            {
                clone._exclusionRules.Add(rule.Clone());
            }

            return clone;
        }

        /// <summary>
        /// Gets a string representation of the filter configuration
        /// </summary>
        public override string ToString()
        {
            var filters = string.Join(Logic == FilterLogic.All ? " AND " : " OR ", _filterRules.Select(r => r.ToString()));
            var exclusions = string.Join(" AND NOT ", _exclusionRules.Select(r => r.ToString()));
            
            var result = filters;
            if (!string.IsNullOrEmpty(exclusions))
                result += " AND NOT " + exclusions;
            
            if (MaxAge.HasValue)
                result += $" AND Age <= {MaxAge.Value.TotalMinutes:F1} minutes";
            
            if (MaxResults.HasValue)
                result += $" LIMIT {MaxResults.Value}";
            
            return result;
        }

        /// <summary>
        /// Disposes the advanced change filters
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the advanced change filters
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _filterRules.Clear();
                _exclusionRules.Clear();
            }
        }
    }

    /// <summary>
    /// Represents the logic for combining filter rules
    /// </summary>
    public enum FilterLogic
    {
        /// <summary>
        /// All rules must pass (AND logic)
        /// </summary>
        All = 1,

        /// <summary>
        /// Any rule can pass (OR logic)
        /// </summary>
        Any = 2
    }

    /// <summary>
    /// Represents the type of time filter
    /// </summary>
    public enum TimeFilterType
    {
        /// <summary>
        /// Changes after this time
        /// </summary>
        After = 1,

        /// <summary>
        /// Changes before this time
        /// </summary>
        Before = 2,

        /// <summary>
        /// Changes between two times
        /// </summary>
        Between = 3,

        /// <summary>
        /// Changes within a time range
        /// </summary>
        Within = 4
    }

    /// <summary>
    /// Represents the filter operator
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// Equals
        /// </summary>
        Equals = 1,

        /// <summary>
        /// Not equals
        /// </summary>
        NotEquals = 2,

        /// <summary>
        /// Greater than
        /// </summary>
        GreaterThan = 3,

        /// <summary>
        /// Greater than or equal
        /// </summary>
        GreaterThanOrEqual = 4,

        /// <summary>
        /// Less than
        /// </summary>
        LessThan = 5,

        /// <summary>
        /// Less than or equal
        /// </summary>
        LessThanOrEqual = 6,

        /// <summary>
        /// Contains
        /// </summary>
        Contains = 7,

        /// <summary>
        /// Not contains
        /// </summary>
        NotContains = 8,

        /// <summary>
        /// Starts with
        /// </summary>
        StartsWith = 9,

        /// <summary>
        /// Ends with
        /// </summary>
        EndsWith = 10,

        /// <summary>
        /// Is null
        /// </summary>
        IsNull = 11,

        /// <summary>
        /// Is not null
        /// </summary>
        IsNotNull = 12,

        /// <summary>
        /// In (matches any value in a collection)
        /// </summary>
        In = 13,

        /// <summary>
        /// Not in (doesn't match any value in a collection)
        /// </summary>
        NotIn = 14,

        /// <summary>
        /// Like (pattern matching)
        /// </summary>
        Like = 15,

        /// <summary>
        /// Not like (pattern matching)
        /// </summary>
        NotLike = 16
    }

    /// <summary>
    /// Base interface for filter rules
    /// </summary>
    public interface IFilterRule
    {
        /// <summary>
        /// Determines if the change matches this filter rule
        /// </summary>
        bool Matches(object change);

        /// <summary>
        /// Creates a copy of this filter rule
        /// </summary>
        IFilterRule Clone();

        /// <summary>
        /// Gets a string representation of this filter rule
        /// </summary>
        string ToString();
    }

    /// <summary>
    /// Filter rule for column values
    /// </summary>
    public class ColumnFilterRule : IFilterRule
    {
        public string ColumnName { get; }
        public FilterOperator Operator { get; }
        public object Value { get; }
        public bool CaseSensitive { get; }

        public ColumnFilterRule(string columnName, FilterOperator op, object value, bool caseSensitive = false)
        {
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            Operator = op;
            Value = value;
            CaseSensitive = caseSensitive;
        }

        public bool Matches(object change)
        {
            if (change is not ChangeRecord record)
                return false;

            // Try to get the column value from the record
            if (record is DetailedChangeRecord detailedRecord)
            {
                // Check new values first, then old values
                if (detailedRecord.NewValues?.ContainsKey(ColumnName) == true)
                {
                    return MatchesValue(detailedRecord.NewValues[ColumnName]);
                }
                if (detailedRecord.OldValues?.ContainsKey(ColumnName) == true)
                {
                    return MatchesValue(detailedRecord.OldValues[ColumnName]);
                }
            }

            // Check metadata
            if (record.Metadata?.ContainsKey(ColumnName) == true)
            {
                return MatchesValue(record.Metadata[ColumnName]);
            }

            return false;
        }

        private bool MatchesValue(object columnValue)
        {
            if (columnValue == null)
                return Operator == FilterOperator.IsNull;

            if (Operator == FilterOperator.IsNull)
                return false;

            if (Operator == FilterOperator.IsNotNull)
                return true;

            var stringValue = columnValue.ToString();
            var filterValue = Value?.ToString();

            if (stringValue == null || filterValue == null)
                return false;

            if (!CaseSensitive)
            {
                stringValue = stringValue.ToLowerInvariant();
                filterValue = filterValue.ToLowerInvariant();
            }

            return Operator switch
            {
                FilterOperator.Equals => stringValue == filterValue,
                FilterOperator.NotEquals => stringValue != filterValue,
                FilterOperator.Contains => stringValue.Contains(filterValue),
                FilterOperator.NotContains => !stringValue.Contains(filterValue),
                FilterOperator.StartsWith => stringValue.StartsWith(filterValue),
                FilterOperator.EndsWith => stringValue.EndsWith(filterValue),
                FilterOperator.Like => MatchesPattern(stringValue, filterValue),
                FilterOperator.NotLike => !MatchesPattern(stringValue, filterValue),
                _ => false
            };
        }

        private bool MatchesPattern(string input, string pattern)
        {
            // Simple wildcard pattern matching
            var regexPattern = pattern
                .Replace("*", ".*")
                .Replace("?", ".")
                .Replace(".", "\\.");
            
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(regexPattern, 
                    CaseSensitive ? System.Text.RegularExpressions.RegexOptions.None : 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return regex.IsMatch(input);
            }
            catch
            {
                return false;
            }
        }

        public IFilterRule Clone()
        {
            return new ColumnFilterRule(ColumnName, Operator, Value, CaseSensitive);
        }

        public override string ToString()
        {
            return $"{ColumnName} {Operator} {Value}";
        }
    }

    /// <summary>
    /// Filter rule for time-based filtering
    /// </summary>
    public class TimeFilterRule : IFilterRule
    {
        public TimeFilterType Type { get; }
        public DateTime Value { get; }
        public DateTime? EndValue { get; }

        public TimeFilterRule(TimeFilterType type, DateTime value, DateTime? endValue = null)
        {
            Type = type;
            Value = value;
            EndValue = endValue;
        }

        public bool Matches(object change)
        {
            if (change is not ChangeRecord record)
                return false;

            var changeTime = record.ChangeTimestamp;

            return Type switch
            {
                TimeFilterType.After => changeTime > Value,
                TimeFilterType.Before => changeTime < Value,
                TimeFilterType.Between => EndValue.HasValue && changeTime >= Value && changeTime <= EndValue.Value,
                TimeFilterType.Within => EndValue.HasValue && changeTime >= Value && changeTime <= EndValue.Value,
                _ => false
            };
        }

        public IFilterRule Clone()
        {
            return new TimeFilterRule(Type, Value, EndValue);
        }

        public override string ToString()
        {
            return Type switch
            {
                TimeFilterType.After => $"Time > {Value:yyyy-MM-dd HH:mm:ss}",
                TimeFilterType.Before => $"Time < {Value:yyyy-MM-dd HH:mm:ss}",
                TimeFilterType.Between => $"Time BETWEEN {Value:yyyy-MM-dd HH:mm:ss} AND {EndValue:yyyy-MM-dd HH:mm:ss}",
                TimeFilterType.Within => $"Time WITHIN {Value:yyyy-MM-dd HH:mm:ss} AND {EndValue:yyyy-MM-dd HH:mm:ss}",
                _ => $"Time {Type} {Value:yyyy-MM-dd HH:mm:ss}"
            };
        }
    }

    /// <summary>
    /// Filter rule for property values
    /// </summary>
    public class ValueFilterRule : IFilterRule
    {
        public string PropertyName { get; }
        public FilterOperator Operator { get; }
        public object Value { get; }
        public bool CaseSensitive { get; }

        public ValueFilterRule(string propertyName, FilterOperator op, object value, bool caseSensitive = false)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            Operator = op;
            Value = value;
            CaseSensitive = caseSensitive;
        }

        public bool Matches(object change)
        {
            if (change == null)
                return false;

            var property = change.GetType().GetProperty(PropertyName);
            if (property == null)
                return false;

            var propertyValue = property.GetValue(change);
            return MatchesValue(propertyValue);
        }

        private bool MatchesValue(object propertyValue)
        {
            if (propertyValue == null)
                return Operator == FilterOperator.IsNull;

            if (Operator == FilterOperator.IsNull)
                return false;

            if (Operator == FilterOperator.IsNotNull)
                return true;

            var stringValue = propertyValue.ToString();
            var filterValue = Value?.ToString();

            if (stringValue == null || filterValue == null)
                return false;

            if (!CaseSensitive)
            {
                stringValue = stringValue.ToLowerInvariant();
                filterValue = filterValue.ToLowerInvariant();
            }

            return Operator switch
            {
                FilterOperator.Equals => stringValue == filterValue,
                FilterOperator.NotEquals => stringValue != filterValue,
                FilterOperator.Contains => stringValue.Contains(filterValue),
                FilterOperator.NotContains => !stringValue.Contains(filterValue),
                FilterOperator.StartsWith => stringValue.StartsWith(filterValue),
                FilterOperator.EndsWith => stringValue.EndsWith(filterValue),
                _ => false
            };
        }

        public IFilterRule Clone()
        {
            return new ValueFilterRule(PropertyName, Operator, Value, CaseSensitive);
        }

        public override string ToString()
        {
            return $"{PropertyName} {Operator} {Value}";
        }
    }

    /// <summary>
    /// Composite filter rule that combines multiple rules
    /// </summary>
    public class CompositeFilterRule : IFilterRule
    {
        public List<IFilterRule> Rules { get; }
        public FilterLogic Logic { get; }

        public CompositeFilterRule(FilterLogic logic = FilterLogic.All)
        {
            Rules = new List<IFilterRule>();
            Logic = logic;
        }

        public CompositeFilterRule AddRule(IFilterRule rule)
        {
            Rules.Add(rule);
            return this;
        }

        public bool Matches(object change)
        {
            if (!Rules.Any())
                return true;

            if (Logic == FilterLogic.All)
                return Rules.All(rule => rule.Matches(change));
            else
                return Rules.Any(rule => rule.Matches(change));
        }

        public IFilterRule Clone()
        {
            var clone = new CompositeFilterRule(Logic);
            foreach (var rule in Rules)
            {
                clone.Rules.Add(rule.Clone());
            }
            return clone;
        }

        public override string ToString()
        {
            var separator = Logic == FilterLogic.All ? " AND " : " OR ";
            return $"({string.Join(separator, Rules.Select(r => r.ToString()))})";
        }
    }

}

