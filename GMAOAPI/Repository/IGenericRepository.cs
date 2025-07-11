using System.Linq.Expressions;

namespace GMAOAPI.Repository
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object[] ids, string includeProperties = "", bool asNoTrack=false);

        Task<List<T>> FindAllAsync(
            Expression<Func<T, bool>>? filter = null,
            string includeProperties = "",
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? pageNumber = null,
            int? pageSize = null
        );
        Task<T?> GetByAsync(Expression<Func<T, bool>> filter, string includeProperties = "");

        Task<T> CreateAsync(T entity);

        Task<T?> UpdateAsync(T entity);

        Task<bool> DeleteAsync(T entity);

        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

        Task<bool> ExistsAsync(object[] ids);
    }
}
