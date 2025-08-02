namespace SQLDBEntityNotifier
{
    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
