using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly BonddyDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(BonddyDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> GetQueryable()
            => _dbSet.AsNoTracking();

        public async Task<IEnumerable<T>> GetAllAsync()
            => await _dbSet.AsNoTracking().ToListAsync();

        public async Task<T?> GetByIdAsync(object id)
            => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

        public async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public void Update(T entity)
            => _dbSet.Update(entity);

        public void Remove(T entity)
            => _dbSet.Remove(entity);

        public Task<int> SaveChangesAsync()
            => _context.SaveChangesAsync();
    }
}
