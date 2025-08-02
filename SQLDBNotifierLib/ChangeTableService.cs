using System;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Service for tracking and retrieving changes in a SQL table.
    /// </summary>
    public class ChangeTableService<T> : IChangeTableService<T> where T : class, new()
    {
        private readonly DbContext _dbContext;

        public ChangeTableService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Asynchronously retrieves a list of entities of type T based on the provided SQL command text.
        /// </summary>
        public async Task<List<T>> GetRecords(string commandText)
        {
            return await _dbContext.Set<T>().FromSqlRaw(commandText).ToListAsync();
        }

        /// <summary>
        /// Synchronously retrieves a list of entities of type T based on the provided SQL command text.
        /// </summary>
        public List<T> GetRecordsSync(string commandText)
        {
            return GetRecords(commandText).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously returns the count of records matching the specified SQL command.
        /// </summary>
        public async Task<long> GetRecordCount(string commandText)
        {
            var records = await GetRecords(commandText);
            return records.Count;
        }

        /// <summary>
        /// Synchronously retrieves the count of records matching the specified SQL command.
        /// </summary>
        public long GetRecordCountSync(string commandText)
        {
            return GetRecordCount(commandText).GetAwaiter().GetResult();
        }
    }
}
