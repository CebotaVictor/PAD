using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Interface
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        public Task AddEntity(TEntity entity);
        public Task<bool> DeleteByIdGenericAsync(ushort Id);
        public Task<IEnumerable<TEntity>> GetAllEntities();
        public Task<TEntity> GetEntityById(ushort Id);
    }
}
    