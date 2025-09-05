namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Represents the type of database change operation
    /// </summary>
    public enum ChangeOperation
    {
        /// <summary>
        /// Record was inserted
        /// </summary>
        Insert = 1,
        
        /// <summary>
        /// Record was updated
        /// </summary>
        Update = 2,
        
        /// <summary>
        /// Record was deleted
        /// </summary>
        Delete = 3,
        
        /// <summary>
        /// Schema change occurred
        /// </summary>
        SchemaChange = 4,
        
        /// <summary>
        /// Unknown or unspecified operation
        /// </summary>
        Unknown = 99
    }
}