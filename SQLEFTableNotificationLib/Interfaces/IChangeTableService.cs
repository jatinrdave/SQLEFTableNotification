using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Interfaces
{
    public interface IChangeTableService<T>
    {
        Task<List<T>> GetRecords(string CommandText);
        List<T> GetRecordsSync(string CommandText);

        long GetRecordCountSync(string CommandText);

        Task<long> GetRecordCount(string CommandText);
    }
}
