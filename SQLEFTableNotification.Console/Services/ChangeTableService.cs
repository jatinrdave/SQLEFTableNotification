using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SQLEFTableNotification.Domain;
using SQLEFTableNotification.Domain.Service;
using SQLEFTableNotification.Entity;
using SQLEFTableNotification.Entity.Entity;
using SQLEFTableNotification.Entity.UnitofWork;
using SQLEFTableNotificationLib.Interfaces;
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

        public async Task<List<T>> GetRecords(string CommandText)
        {
            var record = await _unitOfWork.GetRepositoryAsync<T>().GetModelWithRawSql<T>(CommandText).ToListAsync();
            return record;
        }

        public List<T> GetRecordsSync(string CommandText)
        {
            return Task.Run(() => GetRecords(CommandText)).Result;
        }

        public async Task<long> GetRecordCount(string CommandText)
        {
            var record = await _unitOfWork.GetRepositoryAsync<T>().GetModelWithRawSql<ChangeTableVersionCount>(CommandText).FirstOrDefaultAsync(); ;
            return record != null ? record.VersionCount : 0;
        }

        public long GetRecordCountSync(string CommandText)
        {
            return Task.Run(() => GetRecordCount(CommandText)).Result;
        }
    }
}
