using System;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace SQLDBEntityNotifier
{
    public interface IDBNotificationService<T>
    {
        Task StartNotify();
        event EventHandler<RecordChangedEventArgs<T>> OnChanged;
        event EventHandler<ErrorEventArgs> OnError;
    }
}
