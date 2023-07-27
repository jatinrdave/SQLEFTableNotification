using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Entity.Repository
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        IEnumerable<T> Get(Expression<Func<T, bool>> predicate);
        T GetOne(Expression<Func<T, bool>> predicate);
        void Insert(T entity);
        void Delete(T entity);
        void Delete(object id);
        void Update(object id, T entity);
    }
}
