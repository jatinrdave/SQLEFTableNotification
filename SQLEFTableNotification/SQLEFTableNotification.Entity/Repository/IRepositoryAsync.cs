using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Entity.Repository
{
    public interface IRepositoryAsync<T> where T : class
    {
        Task<IEnumerable<T>> GetAll();
        Task<IEnumerable<T>> Get(Expression<Func<T, bool>> predicate);
        Task<T> GetOne(Expression<Func<T, bool>> predicate);
        Task Insert(T entity);
        void Delete(T entity);
        Task Delete(object id);
        /// <summary>
/// Asynchronously updates the entity identified by the specified id with the provided entity data.
/// </summary>
/// <param name="id">The identifier of the entity to update.</param>
/// <param name="entity">The updated entity data.</param>
Task Update(object id, T entity);

        /// <summary>
/// Executes a raw SQL query and returns an <see cref="IQueryable{T}"/> representing the result set.
/// </summary>
/// <param name="query">The raw SQL query to execute.</param>
/// <param name="parameters">The parameters to apply to the SQL query.</param>
/// <returns>An <see cref="IQueryable{T}"/> containing the entities returned by the query.</returns>
IQueryable<T> GetEntityWithRawSql(string query, params object[] parameters);
        // IQueryable<TViewModel> GetModelWithRawSql<TViewModel>(string query, params object[] parameters) where TViewModel : class; // Not supported in EF Core 5.0
        
    }
}
