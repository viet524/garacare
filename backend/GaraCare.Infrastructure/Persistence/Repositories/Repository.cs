using System.Linq.Expressions;
using GaraCare.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GaraCare.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly GaraCareDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(GaraCareDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.Where(predicate).ToListAsync(cancellationToken);

    public IQueryable<T> Query() => _dbSet.AsQueryable();

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public void Update(T entity) => _dbSet.Update(entity);

    public void Remove(T entity) => _dbSet.Remove(entity);
}
