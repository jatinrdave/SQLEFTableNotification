using System.Collections.Generic;

namespace SQLEFTableNotification.Models
{
    public class RecordChangedEventArgs<T> where T : class, new()
    {

        public List<T> Entities { get; protected set; }

        public DBOperationType ChangeType { get; protected set; }


        public RecordChangedEventArgs(DBOperationType changeType, List<T> entities)
        {
            ChangeType = changeType;
            Entities = entities;
        }


        public RecordChangedEventArgs(List<T> entities)
        {
            Entities = entities;
        }

    }
}