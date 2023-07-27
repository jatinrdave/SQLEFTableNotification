using SQLEFTableNotification.Delegates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Interfaces
{
    public interface IDBNotificationService<T> where T : class, new()
    {
        event Delegates.ErrorEventHandler OnError;

        event ChangedEventHandler<T> OnChanged;

        //event StatusEventHandler OnStatusChanged;

        // SetFilterExpression(Expression<Func<T, bool>> expression);
        Task StartNotify();
        Task StopNotify();
    }
}
