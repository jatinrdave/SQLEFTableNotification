using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Options for filtering changes based on specific columns
    /// </summary>
    public class ColumnChangeFilterOptions
    {
        /// <summary>
        /// Gets or sets the columns to monitor for changes.
        /// If null or empty, all columns are monitored.
        /// </summary>
        public List<string>? MonitoredColumns { get; set; }

        /// <summary>
        /// Gets or sets the columns to exclude from monitoring.
        /// These columns will never trigger change notifications.
        /// </summary>
        public List<string>? ExcludedColumns { get; set; }

        /// <summary>
        /// Gets or sets whether to include column-level change information in notifications.
        /// When true, the AffectedColumns property will contain only the columns that actually changed.
        /// </summary>
        public bool IncludeColumnLevelChanges { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include old and new values for changed columns.
        /// When true, OldValues and NewValues will contain only the monitored columns.
        /// </summary>
        public bool IncludeColumnValues { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum number of column changes required to trigger a notification.
        /// Default is 1 (any change triggers notification).
        /// </summary>
        public int MinimumColumnChanges { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether to treat column changes as case-sensitive.
        /// Default is false (case-insensitive).
        /// </summary>
        public bool CaseSensitiveColumnNames { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to normalize column names (trim whitespace, etc.).
        /// Default is true.
        /// </summary>
        public bool NormalizeColumnNames { get; set; } = true;

        /// <summary>
        /// Gets or sets custom column name mappings.
        /// Useful for mapping database column names to entity property names.
        /// </summary>
        public Dictionary<string, string>? ColumnNameMappings { get; set; }

        /// <summary>
        /// Gets or sets whether to include computed columns in change detection.
        /// Default is false (computed columns are excluded).
        /// </summary>
        public bool IncludeComputedColumns { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include identity columns in change detection.
        /// Default is false (identity columns are excluded).
        /// </summary>
        public bool IncludeIdentityColumns { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include timestamp columns in change detection.
        /// Default is false (timestamp columns are excluded).
        /// </summary>
        public bool IncludeTimestampColumns { get; set; } = false;

        /// <summary>
        /// Creates a new instance with default settings (monitor all columns)
        /// </summary>
        public ColumnChangeFilterOptions()
        {
        }

        /// <summary>
        /// Creates a new instance that monitors only specific columns
        /// </summary>
        /// <param name="monitoredColumns">Columns to monitor for changes</param>
        public ColumnChangeFilterOptions(params string[] monitoredColumns)
        {
            MonitoredColumns = new List<string>(monitoredColumns);
        }

        /// <summary>
        /// Creates a new instance with custom configuration
        /// </summary>
        /// <param name="monitoredColumns">Columns to monitor for changes</param>
        /// <param name="excludedColumns">Columns to exclude from monitoring</param>
        /// <param name="includeColumnLevelChanges">Whether to include column-level change information</param>
        public ColumnChangeFilterOptions(
            List<string>? monitoredColumns = null,
            List<string>? excludedColumns = null,
            bool includeColumnLevelChanges = true)
        {
            MonitoredColumns = monitoredColumns;
            ExcludedColumns = excludedColumns;
            IncludeColumnLevelChanges = includeColumnLevelChanges;
        }

        /// <summary>
        /// Adds a column to the monitored columns list
        /// </summary>
        /// <param name="columnName">Name of the column to monitor</param>
        /// <returns>This instance for method chaining</returns>
        public ColumnChangeFilterOptions AddMonitoredColumn(string columnName)
        {
            MonitoredColumns ??= new List<string>();
            if (!MonitoredColumns.Contains(columnName))
            {
                MonitoredColumns.Add(columnName);
            }
            return this;
        }

        /// <summary>
        /// Adds multiple columns to the monitored columns list
        /// </summary>
        /// <param name="columnNames">Names of the columns to monitor</param>
        /// <returns>This instance for method chaining</returns>
        public ColumnChangeFilterOptions AddMonitoredColumns(params string[] columnNames)
        {
            foreach (var columnName in columnNames)
            {
                AddMonitoredColumn(columnName);
            }
            return this;
        }

        /// <summary>
        /// Adds a column to the excluded columns list
        /// </summary>
        /// <param name="columnName">Name of the column to exclude</param>
        /// <returns>This instance for method chaining</returns>
        public ColumnChangeFilterOptions AddExcludedColumn(string columnName)
        {
            ExcludedColumns ??= new List<string>();
            if (!ExcludedColumns.Contains(columnName))
            {
                ExcludedColumns.Add(columnName);
            }
            return this;
        }

        /// <summary>
        /// Adds multiple columns to the excluded columns list
        /// </summary>
        /// <param name="columnNames">Names of the columns to exclude</param>
        /// <returns>This instance for method chaining</returns>
        public ColumnChangeFilterOptions AddExcludedColumns(params string[] columnNames)
        {
            foreach (var columnName in columnNames)
            {
                AddExcludedColumn(columnName);
            }
            return this;
        }

        /// <summary>
        /// Adds a column name mapping
        /// </summary>
        /// <param name="databaseColumnName">Name of the column in the database</param>
        /// <param name="entityPropertyName">Name of the property in the entity</param>
        /// <returns>This instance for method chaining</returns>
        public ColumnChangeFilterOptions AddColumnMapping(string databaseColumnName, string entityPropertyName)
        {
            ColumnNameMappings ??= new Dictionary<string, string>();
            ColumnNameMappings[databaseColumnName] = entityPropertyName;
            return this;
        }

        /// <summary>
        /// Checks if a column should be monitored based on current configuration
        /// </summary>
        /// <param name="columnName">Name of the column to check</param>
        /// <returns>True if the column should be monitored, false otherwise</returns>
        public bool ShouldMonitorColumn(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            var normalizedColumnName = NormalizeColumnName(columnName);

            // Check if column is explicitly excluded
            if (ExcludedColumns?.Contains(normalizedColumnName, GetColumnNameComparer()) == true)
                return false;

            // If no monitored columns specified, monitor all (except excluded)
            if (MonitoredColumns == null || MonitoredColumns.Count == 0)
                return true;

            // Check if column is explicitly monitored
            // Need to normalize both the input and the stored column names for comparison
            return MonitoredColumns.Any(storedColumn => 
                GetColumnNameComparer().Equals(NormalizeColumnName(storedColumn), normalizedColumnName));
        }

        /// <summary>
        /// Normalizes a column name based on current configuration
        /// </summary>
        /// <param name="columnName">Original column name</param>
        /// <returns>Normalized column name</returns>
        public string NormalizeColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return columnName;

            var normalized = columnName;

            if (NormalizeColumnNames)
            {
                normalized = normalized.Trim();
            }

            if (!CaseSensitiveColumnNames)
            {
                normalized = normalized.ToLowerInvariant();
            }

            return normalized;
        }

        /// <summary>
        /// Maps a database column name to an entity property name
        /// </summary>
        /// <param name="databaseColumnName">Name of the column in the database</param>
        /// <returns>Mapped entity property name or original column name if no mapping exists</returns>
        public string MapColumnName(string databaseColumnName)
        {
            if (ColumnNameMappings?.TryGetValue(databaseColumnName, out var mappedName) == true)
            {
                return mappedName;
            }

            return databaseColumnName;
        }

        /// <summary>
        /// Gets the appropriate string comparer for column names based on case sensitivity setting
        /// </summary>
        /// <returns>String comparer for column name comparisons</returns>
        private IEqualityComparer<string> GetColumnNameComparer()
        {
            return CaseSensitiveColumnNames 
                ? StringComparer.Ordinal 
                : StringComparer.OrdinalIgnoreCase;
        }

        /// <summary>
        /// Creates a copy of this configuration
        /// </summary>
        /// <returns>New instance with copied configuration</returns>
        public ColumnChangeFilterOptions Clone()
        {
            return new ColumnChangeFilterOptions
            {
                MonitoredColumns = MonitoredColumns?.ToList(),
                ExcludedColumns = ExcludedColumns?.ToList(),
                IncludeColumnLevelChanges = IncludeColumnLevelChanges,
                IncludeColumnValues = IncludeColumnValues,
                MinimumColumnChanges = MinimumColumnChanges,
                CaseSensitiveColumnNames = CaseSensitiveColumnNames,
                NormalizeColumnNames = NormalizeColumnNames,
                ColumnNameMappings = ColumnNameMappings?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                IncludeComputedColumns = IncludeComputedColumns,
                IncludeIdentityColumns = IncludeIdentityColumns,
                IncludeTimestampColumns = IncludeTimestampColumns
            };
        }

        /// <summary>
        /// Creates a configuration that monitors only specific columns
        /// </summary>
        /// <param name="columns">Columns to monitor</param>
        /// <returns>New configuration instance</returns>
        public static ColumnChangeFilterOptions MonitorOnly(params string[] columns)
        {
            return new ColumnChangeFilterOptions(columns.ToList());
        }

        /// <summary>
        /// Creates a configuration that excludes specific columns
        /// </summary>
        /// <param name="columns">Columns to exclude</param>
        /// <returns>New configuration instance</returns>
        public static ColumnChangeFilterOptions ExcludeColumns(params string[] columns)
        {
            return new ColumnChangeFilterOptions
            {
                ExcludedColumns = columns.ToList()
            };
        }

        /// <summary>
        /// Creates a configuration that monitors all columns except specified ones
        /// </summary>
        /// <param name="excludedColumns">Columns to exclude</param>
        /// <returns>New configuration instance</returns>
        public static ColumnChangeFilterOptions MonitorAllExcept(params string[] excludedColumns)
        {
            return new ColumnChangeFilterOptions
            {
                ExcludedColumns = excludedColumns.ToList()
            };
        }
    }
}