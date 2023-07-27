using Microsoft.EntityFrameworkCore;
using SQLEFTableNotification.Entity.Context;
using SQLEFTableNotification.Entity.UnitofWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace SQLEFTableNotification.Entity.Repository
{
    /// <summary>
    /// General repository class async
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RepositoryAsync<T> : IRepositoryAsync<T> where T : class
    {
        private readonly IUnitOfWork _unitOfWork;
        public RepositoryAsync(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _unitOfWork.Context.Set<T>().ToListAsync();
        }
        public async Task<IEnumerable<T>> Get(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return await _unitOfWork.Context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task<T> GetOne(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return await _unitOfWork.Context.Set<T>().Where(predicate).FirstOrDefaultAsync();
        }
        public async Task Insert(T entity)
        {
            if (entity != null)
                await _unitOfWork.Context.Set<T>().AddAsync(entity);
        }
        public async Task Update(object id, T entity)
        {
            if (entity != null)
            {
                //T entitytoUpdate = await _unitOfWork.Context.Set<T>().FindAsync(id);
                //if (entitytoUpdate != null)
                //	_unitOfWork.Context.Entry(entitytoUpdate).CurrentValues.SetValues(entity);
                _unitOfWork.Context.Entry(entity).State = EntityState.Modified;
            }
        }
        public async Task Delete(object id)
        {
            T entity = await _unitOfWork.Context.Set<T>().FindAsync(id);
            Delete(entity);
        }
        public void Delete(T entity)
        {
            if (entity != null) _unitOfWork.Context.Set<T>().Remove(entity);
        }

        public IQueryable<T> GetEntityWithRawSql(string query, params object[] parameters)
        {
            return _unitOfWork.Context.Database.SqlQueryRaw<T>(query, parameters);
        }

        public IQueryable<TViewModel> GetModelWithRawSql<TViewModel>(string query, params object[] parameters) where TViewModel : class
        {
            return _unitOfWork.Context.Database.SqlQueryRaw<TViewModel>(query, parameters);

        }



    }


}
