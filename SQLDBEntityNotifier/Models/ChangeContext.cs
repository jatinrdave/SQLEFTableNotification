namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Represents the context or source of a database change
    /// </summary>
    public enum ChangeContext
    {
        /// <summary>
        /// Change originated from an application or system
        /// </summary>
        Application = 1,
        
        /// <summary>
        /// Change originated from a user interface
        /// </summary>
        UserInterface = 2,
        
        /// <summary>
        /// Change originated from a web service or API
        /// </summary>
        WebService = 3,
        
        /// <summary>
        /// Change originated from a scheduled job or batch process
        /// </summary>
        ScheduledJob = 4,
        
        /// <summary>
        /// Change originated from a data migration or ETL process
        /// </summary>
        DataMigration = 5,
        
        /// <summary>
        /// Change originated from a replication or sync process
        /// </summary>
        Replication = 6,
        
        /// <summary>
        /// Change originated from a maintenance script or DBA operation
        /// </summary>
        Maintenance = 7,
        
        /// <summary>
        /// Change originated from an unknown or unspecified source
        /// </summary>
        Unknown = 99
    }
}
