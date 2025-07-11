using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GMAOAPI.Repository;
using GMAOAPI.Data;

namespace GMAOAPI.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly GmaoDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(GmaoDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }


        public async Task<T?> GetByIdAsync(object[] ids, string includeProperties = "", bool asNoTrack = false)
        {
            if (string.IsNullOrWhiteSpace(includeProperties))
            {
                return await _dbSet.FindAsync(ids);
            }

            var keyProperties = _context.Model.FindEntityType(typeof(T))?
                                    .FindPrimaryKey()?
                                    .Properties;

            if (keyProperties == null || keyProperties.Count != ids.Length)
                throw new Exception("Le nombre d'IDs fournis ne correspond pas au nombre de propriétés de clé primaire.");

            ParameterExpression parameter = Expression.Parameter(typeof(T), "e");
            Expression? predicate = null;
            for (int i = 0; i < ids.Length; i++)
            {
                var property = Expression.Property(parameter, keyProperties[i].Name);
                var constant = Expression.Constant(ids[i]);
                var equal = Expression.Equal(property, constant);

                predicate = predicate == null ? equal : Expression.AndAlso(predicate, equal);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(predicate!, parameter);

            IQueryable<T> query = _dbSet;

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                string[] properties = includeProperties.Split(',');
                foreach (string prop in properties)
                {
                    query = query.Include(prop.Trim());
                }
            }
            if(asNoTrack)
                return await query.AsNoTracking().FirstOrDefaultAsync(lambda);
            else
            return await query.FirstOrDefaultAsync(lambda);
        }

        public async Task<T?> GetByAsync(Expression<Func<T, bool>> filter, string includeProperties = "")
        {
            IQueryable<T> query = _dbSet;

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                string[] properties = includeProperties.Split(',');
                foreach (string prop in properties)
                {
                    query = query.Include(prop.Trim());
                }
            }

            return await query.FirstOrDefaultAsync(filter);
        }

        public async Task<List<T>> FindAllAsync(
            Expression<Func<T, bool>>? filter = null,
            string includeProperties = "",
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? pageNumber = null,
            int? pageSize = null)
        {
            IQueryable<T> query = _dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                string[] properties = includeProperties.Split(',');
                foreach (string prop in properties)
                {
                    query = query.Include(prop.Trim());
                }
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }
            if (pageNumber.HasValue && pageSize.HasValue)
            {
                int skip = (pageNumber.Value - 1) * pageSize.Value;
                query = query.Skip(skip).Take(pageSize.Value);
            }
            return await query.ToListAsync();
        }

        public async Task<T> CreateAsync(T entity)
        {
            try
            {
                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Une erreur de base de données est survenue lors de la création de l'entité. "+ex.Message);
            }
        }
        public async Task<T?> UpdateAsync(T entity)
        {
            try
            {
                var entry = _context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    _context.Attach(entity);
                }
                entry.State = EntityState.Modified;

                await _context.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Une erreur de base de données est survenue lors de la mise à jour de l'entité. " + ex.Message);
            }
        }



        public async Task<bool> DeleteAsync(T entity)
        {
            try
            {
                _dbSet.Remove(entity);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Une erreur de base de données est survenue lors de la suppression de l'entité." + ex.Message);
            }
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = _dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.CountAsync();
        }

        public async Task<bool> ExistsAsync(object[] ids)
        {
            var entity = await _dbSet.FindAsync(ids);
            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
                return true;
            }
            return false;
        }
    }
}
