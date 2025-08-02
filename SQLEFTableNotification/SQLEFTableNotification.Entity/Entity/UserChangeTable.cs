using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Entity.Entity
{
    public class UserChangeTable : BaseEntity, IEntityPk
    {
        public int Id { get; set; }
    }

    public class ChangeTableVersionCount
    {
        public long VersionCount { get; set; }
    }
}
