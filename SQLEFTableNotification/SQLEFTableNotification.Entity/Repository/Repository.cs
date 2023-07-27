using Microsoft.EntityFrameworkCore;
using SQLEFTableNotification.Entity.Context;
using SQLEFTableNotification.Entity.UnitofWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace SQLEFTableNotification.Entity.Repository
{
    /// <summary>
    /// General repository class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly IUnitOfWork _unitOfWork;
        public Repository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public IEnumerable<T> GetAll()
        {
            return _unitOfWork.Context.Set<T>();
        }
        public IEnumerable<T> Get(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return _unitOfWork.Context.Set<T>().Where(predicate).AsEnumerable<T>();
        }
        public T GetOne(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return _unitOfWork.Context.Set<T>().Where(predicate).FirstOrDefault();
        }
        public void Insert(T entity)
        {
            if (entity != null) _unitOfWork.Context.Set<T>().Add(entity);
        }
        public void Update(object id, T entity)
        {
            if (entity != null)
            {
                //T entitytoUpdate = _unitOfWork.Context.Set<T>().Find(id);
                //if (entitytoUpdate != null)
                //	_unitOfWork.Context.Entry(entitytoUpdate).CurrentValues.SetValues(entity);
                _unitOfWork.Context.Entry(entity).State = EntityState.Modified;
            }
        }
        public void Delete(object id)
        {
            T entity = _unitOfWork.Context.Set<T>().Find(id);
            Delete(entity);
        }
        public void Delete(T entity)
        {
            if (entity != null) _unitOfWork.Context.Set<T>().Remove(entity);
        }

    }


}
