using Microsoft.EntityFrameworkCore;
using PayClaim.Data.Repositories.Interfaces;

namespace PayClaim.Data.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly PayClaimDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        public Repository(PayClaimDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }
        public async Task<TEntity?> GetByIdAsync(object[] keyValues, CancellationToken ct = default)
        {
            return await _dbSet.FindAsync(keyValues, ct);
        }
        public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        {
            await _dbSet.AddAsync(entity, ct);
        }

        public void Delete(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }
    }
}
