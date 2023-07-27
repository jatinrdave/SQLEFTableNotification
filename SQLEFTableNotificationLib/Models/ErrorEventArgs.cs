using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotificationLib.Models
{
    public class ErrorEventArgs //: DBBaseEventArgs
    {
        #region Properties

        public string Message { get; }

        public Exception Error { get; protected set; }

        #endregion

        #region Constructors

        public ErrorEventArgs(
            Exception e)
        //string server,
        //string database,
        //string sender) : this("SQL monitoring stopped working", e, server, database, sender)
        {
            this.Error = e;
        }

        public ErrorEventArgs(
            string message,
            Exception e)
        //string server,
        //string database,
        //string sender) : base(server, database, sender)
        {
            this.Message = message;
            this.Error = e;
        }

        #endregion
    }
}
