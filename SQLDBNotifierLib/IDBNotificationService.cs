namespace SQLDBEntityNotifier
{
    public interface IDBNotificationService<T>
    {
        Task StartNotify();
        event EventHandler<RecordChangedEventArgs<T>> OnChanged;
        event EventHandler<ErrorEventArgs> OnError;
    }
}
