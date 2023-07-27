using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLEFTableNotificationLib.Models;

namespace SQLEFTableNotificationLib.Delegates
{

    public delegate void ErrorEventHandler(object sender, Models.ErrorEventArgs e);
    public delegate void ChangedEventHandler<T>(object sender, RecordChangedEventArgs<T> e) where T : class, new();

}
