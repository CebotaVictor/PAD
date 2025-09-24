using Microsoft.EntityFrameworkCore;
using PR_c_.EFCore;
using PR_c_.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Infrastructure
{
    public class GenericRepository<TEntity>(MBDbContext context) : IGenericRepository<TEntity> where TEntity : class
    {
        private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

        public async Task AddEntity(TEntity entity)
        {
            try
            {
                if (_dbSet != null)
                    await _dbSet.AddAsync(entity);
                else throw new NullReferenceException($"_context is null");
            }

            catch (Exception ex)
            {
                Console.WriteLine($"AddGeneric failed to create , {ex.Message}");
                return;
            }
        }

        public async Task<bool> DeleteByIdGenericAsync(ushort Id)
        {

            try
            {
                if (_dbSet != null)
                {
                    var entityToDelete = await _dbSet.FindAsync(Id);
                    if (entityToDelete != null)
                    {
                        _dbSet.Remove(entityToDelete);
                        Console.WriteLine($"Entity with id {Id} was deleted successfully");
                        return true;
                    }
                }
                throw new NullReferenceException($"Error while deleting entity {Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while deleting entity {Id} {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<TEntity>> GetAllEntities()
        {
            try
            {
                if (_dbSet != null)
                    return await _dbSet.ToListAsync() ?? throw new NullReferenceException($"GetAllEntities query returned null for the GetAllEntities");
                throw new NullReferenceException("GetAllEntities got a null _dbSet for the GetAllEntities");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllEntities method \n{ex.Message}");
                return Enumerable.Empty<TEntity>();
            }
        }

        public async Task<TEntity> GetEntityById(ushort Id)
        {
            try
            {
                if (_dbSet != null)
                    return await _dbSet.FindAsync(Id) ?? throw new NullReferenceException($"GetEntityById query returned null for the GetAllEntitiesById");
                throw new NullReferenceException("GetEntityById got a null _dbSet for the GetAllEntitiesById");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while deleting pacient {Id} {ex.Message}");
                return null!;
            }
        }
    }
}
