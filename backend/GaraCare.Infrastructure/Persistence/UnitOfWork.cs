using System.Collections.Concurrent;
using GaraCare.Application.Interfaces;
using GaraCare.Infrastructure.Persistence.Repositories;

namespace GaraCare.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly GaraCareDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public UnitOfWork(GaraCareDbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : class
        => (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new Repository<T>(_context));

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
