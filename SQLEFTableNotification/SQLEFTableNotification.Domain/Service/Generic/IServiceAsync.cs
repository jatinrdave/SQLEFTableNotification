using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Domain.Service
{
    public interface IServiceAsync<Tv, Te>
    {
        Task<IEnumerable<Tv>> GetAll();
        Task<int> Add(Tv obj);
        Task<int> Update(Tv obj);
        Task<int> Remove(int id);
        Task<Tv> GetOne(int id);
        Task<IEnumerable<Tv>> Get(Expression<Func<Te, bool>> predicate);
    }

}
