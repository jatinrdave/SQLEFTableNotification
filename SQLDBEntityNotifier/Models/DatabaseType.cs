namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Represents the type of database being used for CDC operations
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// Microsoft SQL Server with Change Data Capture
        /// </summary>
        SqlServer = 1,
        
        /// <summary>
        /// MySQL with Binary Log Change Data Capture
        /// </summary>
        MySql = 2,
        
        /// <summary>
        /// PostgreSQL with Logical Replication and WAL
        /// </summary>
        PostgreSql = 3
    }
}