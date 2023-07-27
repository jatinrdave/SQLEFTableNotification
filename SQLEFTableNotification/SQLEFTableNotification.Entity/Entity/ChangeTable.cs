using SQLEFTableNotification.Entity.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Entity.Entity
{
    public class ChangeTable 
    {
        public long? SYS_CHANGE_VERSION { get; set; }
        public long? SYS_CHANGE_CREATION_VERSION { get; set; }
        public string SYS_CHANGE_OPERATION { get; set; }
        public byte[] SYS_CHANGE_COLUMNS { get; set; }
        public byte[] SYS_CHANGE_CONTEXT { get; set; }
        [NotMapped]
        public DBOperationType OperationType
        {
            get
            {
                if (SYS_CHANGE_OPERATION == "I")
                {
                    return DBOperationType.Insert;
                }
                else if (SYS_CHANGE_OPERATION == "U")
                {
                    return DBOperationType.Update;
                }
                else if (SYS_CHANGE_OPERATION == "D")
                {
                    return DBOperationType.Delete;
                }
                else
                {
                    return DBOperationType.None;
                }
            }
        }

    }
}
