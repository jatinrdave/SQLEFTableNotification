namespace SQLEFTableNotification.Models
{
    public enum TableOperationStatus
    {
        None,
        Starting,
        Started,
        WaitingForNotification,
        StopDueToCancellation,
        StopDueToError
    }

    public enum DBOperationType
    {
        None,
        Delete,
        Insert,
        Update
    }
}