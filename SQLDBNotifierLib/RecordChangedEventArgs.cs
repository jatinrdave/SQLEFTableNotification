namespace SQLDBEntityNotifier
{
    public class RecordChangedEventArgs<T> : EventArgs
    {
        public IEnumerable<T> Entities { get; set; }
    }
}
