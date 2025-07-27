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
        Task Update(object id, T entity);

        IQueryable<T> GetEntityWithRawSql(string query, params object[] parameters);
        // IQueryable<TViewModel> GetModelWithRawSql<TViewModel>(string query, params object[] parameters) where TViewModel : class; // Not supported in EF Core 5.0
        
    }
}
