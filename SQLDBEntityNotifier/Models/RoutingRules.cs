using System;
using System.Collections.Generic;
using System.Linq;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Base class for routing rules
    /// </summary>
    public abstract class BaseRoutingRule : IRoutingRule
    {
        public string Name { get; }
        public int Priority { get; }

        protected BaseRoutingRule(string name, int priority = 0)
        {
            Name = name;
            Priority = priority;
        }

        public abstract bool ShouldApply(ChangeRecord change, string tableName);
        public abstract List<string> GetDestinations(ChangeRecord change, string tableName);
    }

    /// <summary>
    /// Routes changes based on table name
    /// </summary>
    public class TableBasedRoutingRule : BaseRoutingRule
    {
        private readonly List<string> _targetTables;
        private readonly List<string> _destinations;
        private readonly bool _exactMatch;

        public TableBasedRoutingRule(string name, List<string> targetTables, List<string> destinations, bool exactMatch = true, int priority = 0)
            : base(name, priority)
        {
            _targetTables = targetTables ?? new List<string>();
            _destinations = destinations ?? new List<string>();
            _exactMatch = exactMatch;
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            if (_exactMatch)
            {
                return _targetTables.Contains(tableName, StringComparer.OrdinalIgnoreCase);
            }
            return _targetTables.Any(target => tableName.Contains(target, StringComparison.OrdinalIgnoreCase));
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }
    }

    /// <summary>
    /// Routes changes based on operation type
    /// </summary>
    public class OperationBasedRoutingRule : BaseRoutingRule
    {
        private readonly List<ChangeOperation> _targetOperations;
        private readonly List<string> _destinations;

        public OperationBasedRoutingRule(string name, List<ChangeOperation> targetOperations, List<string> destinations, int priority = 0)
            : base(name, priority)
        {
            _targetOperations = targetOperations ?? new List<ChangeOperation>();
            _destinations = destinations ?? new List<string>();
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            return _targetOperations.Contains(change.Operation);
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }
    }

    /// <summary>
    /// Routes changes based on column values
    /// </summary>
    public class ColumnValueRoutingRule : BaseRoutingRule
    {
        private readonly string _columnName;
        private readonly object _expectedValue;
        private readonly List<string> _destinations;
        private readonly bool _caseSensitive;

        public ColumnValueRoutingRule(string name, string columnName, object expectedValue, List<string> destinations, bool caseSensitive = false, int priority = 0)
            : base(name, priority)
        {
            _columnName = columnName;
            _expectedValue = expectedValue;
            _destinations = destinations ?? new List<string>();
            _caseSensitive = caseSensitive;
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            if (change.Metadata == null || !change.Metadata.ContainsKey(_columnName))
                return false;

            var actualValue = change.Metadata[_columnName];
            if (_caseSensitive)
            {
                return Equals(actualValue, _expectedValue);
            }

            if (actualValue is string actualStr && _expectedValue is string expectedStr)
            {
                return string.Equals(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase);
            }

            return Equals(actualValue, _expectedValue);
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }
    }

    /// <summary>
    /// Routes changes based on time conditions
    /// </summary>
    public class TimeBasedRoutingRule : BaseRoutingRule
    {
        private readonly TimeSpan _startTime;
        private readonly TimeSpan _endTime;
        private readonly List<string> _destinations;
        private readonly bool _includeWeekends;

        public TimeBasedRoutingRule(string name, TimeSpan startTime, TimeSpan endTime, List<string> destinations, bool includeWeekends = true, int priority = 0)
            : base(name, priority)
        {
            _startTime = startTime;
            _endTime = endTime;
            _destinations = destinations ?? new List<string>();
            _includeWeekends = includeWeekends;
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            var now = DateTime.UtcNow;
            var currentTime = now.TimeOfDay;
            var isWeekend = now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

            if (!_includeWeekends && isWeekend)
                return false;

            if (_startTime <= _endTime)
            {
                // Same day range (e.g., 9 AM to 5 PM)
                return currentTime >= _startTime && currentTime <= _endTime;
            }
            else
            {
                // Overnight range (e.g., 10 PM to 6 AM)
                return currentTime >= _startTime || currentTime <= _endTime;
            }
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }
    }

    /// <summary>
    /// Routes changes based on change frequency
    /// </summary>
    public class FrequencyBasedRoutingRule : BaseRoutingRule
    {
        private readonly int _maxChangesPerMinute;
        private readonly List<string> _destinations;
        private readonly Dictionary<string, Queue<DateTime>> _changeHistory = new();

        public FrequencyBasedRoutingRule(string name, int maxChangesPerMinute, List<string> destinations, int priority = 0)
            : base(name, priority)
        {
            _maxChangesPerMinute = maxChangesPerMinute;
            _destinations = destinations ?? new List<string>();
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            if (!_changeHistory.ContainsKey(tableName))
            {
                _changeHistory[tableName] = new Queue<DateTime>();
            }

            var queue = _changeHistory[tableName];

            // Remove old entries
            while (queue.Count > 0 && queue.Peek() < oneMinuteAgo)
            {
                queue.Dequeue();
            }

            // Check if we're over the limit
            if (queue.Count >= _maxChangesPerMinute)
            {
                return false;
            }

            // Add current change
            queue.Enqueue(now);
            return true;
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }
    }

    /// <summary>
    /// Routes changes based on data size
    /// </summary>
    public class DataSizeRoutingRule : BaseRoutingRule
    {
        private readonly int _maxDataSizeBytes;
        private readonly List<string> _destinations;

        public DataSizeRoutingRule(string name, int maxDataSizeBytes, List<string> destinations, int priority = 0)
            : base(name, priority)
        {
            _maxDataSizeBytes = maxDataSizeBytes;
            _destinations = destinations ?? new List<string>();
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            // Estimate data size based on metadata and change content
            var estimatedSize = EstimateChangeSize(change);
            return estimatedSize <= _maxDataSizeBytes;
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }

        private int EstimateChangeSize(ChangeRecord change)
        {
            var size = 0;

            // Estimate size of change ID
            if (!string.IsNullOrEmpty(change.ChangeId))
                size += change.ChangeId.Length * 2; // UTF-16

            // Estimate size of metadata
            if (change.Metadata != null)
            {
                foreach (var kvp in change.Metadata)
                {
                    size += (kvp.Key?.Length ?? 0) * 2;
                    size += EstimateValueSize(kvp.Value);
                }
            }

            // Estimate size of change position
            if (!string.IsNullOrEmpty(change.ChangePosition))
                size += change.ChangePosition.Length * 2;

            return size;
        }

        private int EstimateValueSize(object? value)
        {
            if (value == null) return 0;

            return value switch
            {
                string str => str.Length * 2,
                int => 4,
                long => 8,
                double => 8,
                DateTime => 8,
                bool => 1,
                _ => 16 // Default estimate for other types
            };
        }
    }

    /// <summary>
    /// Composite routing rule that combines multiple conditions
    /// </summary>
    public class CompositeRoutingRule : BaseRoutingRule
    {
        private readonly List<IRoutingRule> _subRules;
        private readonly CompositeLogic _logic;
        private readonly List<string> _destinations;

        public enum CompositeLogic
        {
            All,    // All sub-rules must pass
            Any,    // Any sub-rule can pass
            None    // No sub-rules should pass
        }

        public CompositeRoutingRule(string name, List<IRoutingRule> subRules, CompositeLogic logic, List<string> destinations, int priority = 0)
            : base(name, priority)
        {
            _subRules = subRules ?? new List<IRoutingRule>();
            _logic = logic;
            _destinations = destinations ?? new List<string>();
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            if (!_subRules.Any())
                return false;

            var results = _subRules.Select(rule => rule.ShouldApply(change, tableName)).ToList();

            return _logic switch
            {
                CompositeLogic.All => results.All(r => r),
                CompositeLogic.Any => results.Any(r => r),
                CompositeLogic.None => !results.Any(r => r),
                _ => false
            };
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }
    }

    /// <summary>
    /// Routing rule that routes based on custom predicate
    /// </summary>
    public class CustomRoutingRule : BaseRoutingRule
    {
        private readonly Func<ChangeRecord, string, bool> _predicate;
        private readonly List<string> _destinations;

        public CustomRoutingRule(string name, Func<ChangeRecord, string, bool> predicate, List<string> destinations, int priority = 0)
            : base(name, priority)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _destinations = destinations ?? new List<string>();
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            try
            {
                return _predicate(change, tableName);
            }
            catch
            {
                return false;
            }
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }
    }

    /// <summary>
    /// Routing rule that routes based on change priority
    /// </summary>
    public class PriorityBasedRoutingRule : BaseRoutingRule
    {
        private readonly List<ChangePriority> _targetPriorities;
        private readonly List<string> _destinations;

        public PriorityBasedRoutingRule(string name, List<ChangePriority> targetPriorities, List<string> destinations, int priority = 0)
            : base(name, priority)
        {
            _targetPriorities = targetPriorities ?? new List<ChangePriority>();
            _destinations = destinations ?? new List<string>();
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            // Extract priority from metadata if available
            if (change.Metadata != null && change.Metadata.ContainsKey("Priority"))
            {
                if (change.Metadata["Priority"] is ChangePriority priority)
                {
                    return _targetPriorities.Contains(priority);
                }
            }

            // Default to low priority if not specified
            return _targetPriorities.Contains(ChangePriority.Low);
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            return _destinations;
        }
    }

    /// <summary>
    /// Routing rule that routes based on business hours
    /// </summary>
    public class BusinessHoursRoutingRule : BaseRoutingRule
    {
        private readonly List<DayOfWeek> _businessDays;
        private readonly TimeSpan _startTime;
        private readonly TimeSpan _endTime;
        private readonly List<string> _destinations;
        private readonly List<string> _afterHoursDestinations;

        public BusinessHoursRoutingRule(string name, List<DayOfWeek> businessDays, TimeSpan startTime, TimeSpan endTime, 
            List<string> destinations, List<string> afterHoursDestinations, int priority = 0)
            : base(name, priority)
        {
            _businessDays = businessDays ?? new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            _startTime = startTime;
            _endTime = endTime;
            _destinations = destinations ?? new List<string>();
            _afterHoursDestinations = afterHoursDestinations ?? new List<string>();
        }

        public override bool ShouldApply(ChangeRecord change, string tableName)
        {
            var now = DateTime.UtcNow;
            var isBusinessDay = _businessDays.Contains(now.DayOfWeek);
            var currentTime = now.TimeOfDay;
            var isBusinessHours = currentTime >= _startTime && currentTime <= _endTime;

            return isBusinessDay && isBusinessHours;
        }

        public override List<string> GetDestinations(ChangeRecord change, string tableName)
        {
            var now = DateTime.UtcNow;
            var isBusinessDay = _businessDays.Contains(now.DayOfWeek);
            var currentTime = now.TimeOfDay;
            var isBusinessHours = currentTime >= _startTime && currentTime <= _endTime;

            if (isBusinessDay && isBusinessHours)
            {
                return _destinations;
            }
            else
            {
                return _afterHoursDestinations;
            }
        }
    }
}
