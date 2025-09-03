using System;
using System.Collections.Generic;
using System.Linq;
using SQLDBEntityNotifier.Models;

namespace SQLDBEntityNotifier.Helpers
{
    /// <summary>
    /// Helper class for building SQL change tracking queries with context filtering
    /// </summary>
    public static class ChangeTrackingQueryBuilder
    {
        /// <summary>
        /// Builds a basic change tracking query without context filtering
        /// </summary>
        /// <param name="tableName">The name of the table to monitor</param>
        /// <param name="fromVersion">The version to start tracking from</param>
        /// <param name="toVersion">The version to track up to</param>
        /// <returns>The SQL query string</returns>
        public static string BuildBasicQuery(string tableName, long fromVersion, long toVersion)
        {
            return $"SELECT ct.* FROM CHANGETABLE(CHANGES {tableName}, {fromVersion}) ct WHERE ct.SYS_CHANGE_VERSION <= {toVersion}";
        }

        /// <summary>
        /// Builds a change tracking query with context filtering
        /// </summary>
        /// <param name="tableName">The name of the table to monitor</param>
        /// <param name="fromVersion">The version to start tracking from</param>
        /// <param name="toVersion">The version to track up to</param>
        /// <param name="filterOptions">Options for filtering changes</param>
        /// <returns>The SQL query string</returns>
        public static string BuildContextFilteredQuery(string tableName, long fromVersion, long toVersion, ChangeFilterOptions? filterOptions)
        {
            if (filterOptions == null || 
                (filterOptions.AllowedChangeContexts == null && filterOptions.ExcludedChangeContexts == null))
            {
                return BuildBasicQuery(tableName, fromVersion, toVersion);
            }

            var baseQuery = $"SELECT ct.*, ct.SYS_CHANGE_CONTEXT as ChangeContext FROM CHANGETABLE(CHANGES {tableName}, {fromVersion}) ct WHERE ct.SYS_CHANGE_VERSION <= {toVersion}";

            var conditions = new List<string>();

            // Add context filtering conditions
            if (filterOptions.ExcludedChangeContexts?.Any() == true)
            {
                var excludedValues = string.Join(",", filterOptions.ExcludedChangeContexts.Select(c => (int)c));
                conditions.Add($"ct.SYS_CHANGE_CONTEXT NOT IN ({excludedValues})");
            }
            else if (filterOptions.AllowedChangeContexts?.Any() == true)
            {
                var allowedValues = string.Join(",", filterOptions.AllowedChangeContexts.Select(c => (int)c));
                conditions.Add($"ct.SYS_CHANGE_CONTEXT IN ({allowedValues})");
            }

            if (conditions.Any())
            {
                baseQuery += " AND " + string.Join(" AND ", conditions);
            }

            return baseQuery;
        }

        /// <summary>
        /// Builds a change tracking query with extended context information
        /// </summary>
        /// <param name="tableName">The name of the table to monitor</param>
        /// <param name="fromVersion">The version to start tracking from</param>
        /// <param name="toVersion">The version to track up to</param>
        /// <param name="filterOptions">Options for filtering changes</param>
        /// <returns>The SQL query string</returns>
        public static string BuildExtendedContextQuery(string tableName, long fromVersion, long toVersion, ChangeFilterOptions? filterOptions)
        {
            var baseQuery = BuildContextFilteredQuery(tableName, fromVersion, toVersion, filterOptions);

            if (filterOptions?.IncludeChangeContext == true)
            {
                // Add additional context information if available
                var additionalColumns = new List<string>();

                if (filterOptions.IncludeUserInfo)
                {
                    additionalColumns.Add("ct.SYS_CHANGE_CONTEXT as ChangeContext");
                    additionalColumns.Add("ISNULL(ct.SYS_CHANGE_CONTEXT, 99) as ChangeContextValue");
                }

                if (filterOptions.IncludeApplicationName)
                {
                    additionalColumns.Add("APP_NAME() as ApplicationName");
                }

                if (filterOptions.IncludeHostName)
                {
                    additionalColumns.Add("HOST_NAME() as HostName");
                }

                if (additionalColumns.Any())
                {
                    // Replace the basic select with extended select
                    var selectPart = string.Join(", ", additionalColumns);
                    baseQuery = baseQuery.Replace("ct.*", $"ct.*, {selectPart}");
                }
            }

            return baseQuery;
        }

        /// <summary>
        /// Creates a custom change tracking query function
        /// </summary>
        /// <param name="tableName">The name of the table to monitor</param>
        /// <param name="filterOptions">Options for filtering changes</param>
        /// <param name="customColumns">Custom columns to include in the query</param>
        /// <returns>A function that builds the change tracking query</returns>
        public static Func<long, long, string> CreateCustomQueryFunction(string tableName, ChangeFilterOptions? filterOptions, string? customColumns = null)
        {
            return (fromVersion, toVersion) =>
            {
                var baseQuery = BuildExtendedContextQuery(tableName, fromVersion, toVersion, filterOptions);

                if (!string.IsNullOrEmpty(customColumns))
                {
                    // Insert custom columns after the base select
                    var insertIndex = baseQuery.IndexOf("FROM");
                    if (insertIndex > 0)
                    {
                        baseQuery = baseQuery.Insert(insertIndex, $", {customColumns} ");
                    }
                }

                return baseQuery;
            };
        }

        /// <summary>
        /// Creates a simple change tracking query function for backward compatibility
        /// </summary>
        /// <param name="tableName">The name of the table to monitor</param>
        /// <returns>A function that builds the basic change tracking query</returns>
        public static Func<long, string> CreateBasicQueryFunction(string tableName)
        {
            return (fromVersion) => $"SELECT ct.* FROM CHANGETABLE(CHANGES {tableName}, {fromVersion}) ct WHERE ct.SYS_CHANGE_VERSION <= {{0}}";
        }
    }
}
