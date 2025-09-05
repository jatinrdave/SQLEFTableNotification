using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SQLEFTableNotification.Domain;
using SQLEFTableNotification.Domain.Service;
using SQLEFTableNotification.Entity;
using SQLEFTableNotification.Entity.Entity;
using SQLEFTableNotification.Entity.UnitofWork;
using SQLEFTableNotification.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Console.Services
{
    public class ChangeTableService<T,TView> : GenericServiceAsync<TView,T>, IChangeTableService<T> where T : BaseEntity where TView : BaseDomain
    {
        public ChangeTableService(IUnitOfWork unitOfWork,IMapper mapper)
            : base(unitOfWork, mapper)
        {

        }

        /// <summary>
        /// Asynchronously retrieves a list of entities of type T based on the provided SQL command text.
        /// </summary>
        /// <param name="CommandText">The raw SQL command to execute for retrieving records.</param>
        /// <returns>A list of entities of type T. Currently returns an empty list.</returns>
        public async Task<List<T>> GetRecords(string CommandText)
        {
            // var record = await _unitOfWork.GetRepositoryAsync<T>().GetModelWithRawSql<T>(CommandText).ToListAsync();
            return new List<T>();
        }

        /// <summary>
        /// Synchronously retrieves a list of entities of type T based on the provided SQL command text.
        /// </summary>
        /// <param name="CommandText">The raw SQL command to execute for retrieving records.</param>
        /// <returns>A list of entities of type T. Returns an empty list if no records are found or if the underlying asynchronous method returns no data.</returns>
        public List<T> GetRecordsSync(string CommandText)
        {
            return Task.Run(() => GetRecords(CommandText)).Result;
        }

        /// <summary>
        /// Asynchronously returns the count of records matching the specified SQL command.
        /// </summary>
        /// <param name="CommandText">The raw SQL command to execute.</param>
        /// <returns>The number of records found; currently always returns zero.</returns>
        public async Task<long> GetRecordCount(string CommandText)
        {
            // var record = await _unitOfWork.GetRepositoryAsync<T>().GetModelWithRawSql<ChangeTableVersionCount>(CommandText).FirstOrDefaultAsync();
            return 0;
        }

        /// <summary>
        /// Synchronously retrieves the count of records matching the specified SQL command.
        /// </summary>
        /// <param name="CommandText">The raw SQL command to execute for counting records.</param>
        /// <returns>The number of records matching the SQL command, or zero if no records are found.</returns>
        public long GetRecordCountSync(string CommandText)
        {
            return Task.Run(() => GetRecordCount(CommandText)).Result;
        }

        /// <summary>
        /// Retrieves records with a context parameter for filtering.
        /// </summary>
        /// <param name="commandText">The SQL command to execute</param>
        /// <param name="context">The context parameter for filtering</param>
        /// <returns>A list of entities of type T</returns>
        public async Task<List<T>> GetRecordsWithContext(string commandText, string context)
        {
            // For now, return the same result as GetRecords
            // In a real implementation, you might use the context parameter for additional filtering
            return await GetRecords(commandText);
        }
    }
}
