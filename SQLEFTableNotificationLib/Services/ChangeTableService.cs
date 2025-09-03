
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SQLEFTableNotification.Interfaces;

namespace SQLEFTableNotification.Services
{
    public class ChangeTableService<T> : IChangeTableService<T> where T : class, new()
    {
        private readonly DbContext _dbContext;

        public ChangeTableService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<T>> GetRecords(string commandText)
        {
            return await _dbContext.Set<T>().FromSqlRaw(commandText).ToListAsync();
        }

        public List<T> GetRecordsSync(string commandText)
        {
            return GetRecords(commandText).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<long> GetRecordCount(string commandText)
        {
            var records = await GetRecords(commandText);
            return records.Count;
        }

        public long GetRecordCountSync(string commandText)
        {
            return GetRecordCount(commandText).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<List<T>> GetRecordsWithContext(string commandText, string context)
        {
            return await _dbContext.Set<T>().FromSqlRaw(commandText, new SqlParameter("@ChangeContext", context)).ToListAsync();
        }
    }
}
