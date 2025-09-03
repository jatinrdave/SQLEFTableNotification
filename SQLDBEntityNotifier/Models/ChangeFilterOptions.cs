using System.Collections.Generic;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Configuration options for filtering database changes based on their context
    /// </summary>
    public class ChangeFilterOptions
    {
        /// <summary>
        /// Gets or sets the list of change contexts that should trigger notifications
        /// If null or empty, all changes will trigger notifications
        /// </summary>
        public List<ChangeContext>? AllowedChangeContexts { get; set; }

        /// <summary>
        /// Gets or sets the list of change contexts that should be excluded from notifications
        /// This takes precedence over AllowedChangeContexts
        /// </summary>
        public List<ChangeContext>? ExcludedChangeContexts { get; set; }

        /// <summary>
        /// Gets or sets whether to include change context information in the notification events
        /// </summary>
        public bool IncludeChangeContext { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include the original SQL query that caused the change
        /// </summary>
        public bool IncludeChangeQuery { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include user information if available
        /// </summary>
        public bool IncludeUserInfo { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include application name if available
        /// </summary>
        public bool IncludeApplicationName { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include host name if available
        /// </summary>
        public bool IncludeHostName { get; set; } = false;

        /// <summary>
        /// Creates a new instance with default settings
        /// </summary>
        public ChangeFilterOptions()
        {
        }

        /// <summary>
        /// Creates a new instance with specific allowed change contexts
        /// </summary>
        /// <param name="allowedContexts">The change contexts that should trigger notifications</param>
        public ChangeFilterOptions(params ChangeContext[] allowedContexts)
        {
            AllowedChangeContexts = new List<ChangeContext>(allowedContexts);
        }

        /// <summary>
        /// Creates a new instance that excludes specific change contexts
        /// </summary>
        /// <param name="excludeContexts">The change contexts that should not trigger notifications</param>
        /// <param name="exclude">Whether to exclude the specified contexts (default: true)</param>
        public static ChangeFilterOptions Exclude(params ChangeContext[] excludeContexts)
        {
            return new ChangeFilterOptions
            {
                ExcludedChangeContexts = new List<ChangeContext>(excludeContexts)
            };
        }

        /// <summary>
        /// Creates a new instance that only allows specific change contexts
        /// </summary>
        /// <param name="allowedContexts">The change contexts that should trigger notifications</param>
        public static ChangeFilterOptions AllowOnly(params ChangeContext[] allowedContexts)
        {
            return new ChangeFilterOptions(allowedContexts);
        }
    }
}
