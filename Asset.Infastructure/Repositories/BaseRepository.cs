using Asset.Application.Interfaces.Comman;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Asset.Infastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        #region Fields
        protected readonly AssetManagementDbContext _dbContext;
        protected readonly DbSet<T> _dbSet;   // _dbContext.Assets
        #endregion

        #region  Constructor
        public BaseRepository(AssetManagementDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = dbContext.Set<T>();
        }
        #endregion
        public void Add(T entity)
        {
            _dbSet.AddAsync(entity);
        }
        public async Task<T?> GetByIdAsync(int id, CancellationToken ct)
        {
            return await _dbSet.FindAsync(id, ct);
        }
        public async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct)
        {
            return await _dbSet.AsNoTracking().ToListAsync(ct);
        }
        public void Update(T entity)
        {
            _dbSet.Entry(entity).State = EntityState.Modified;
        }
    }
}
