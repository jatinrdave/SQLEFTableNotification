using System.Collections.Generic;
using System.Threading.Tasks;
namespace SQLDBEntityNotifier
{
    public interface IChangeTableService<T> where T : class, new()
    {
        Task<List<T>> GetRecords(string commandText);
        List<T> GetRecordsSync(string commandText);
        Task<long> GetRecordCount(string commandText);
        long GetRecordCountSync(string commandText);
    }
}
