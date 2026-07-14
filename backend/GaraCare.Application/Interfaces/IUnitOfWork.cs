namespace GaraCare.Application.Interfaces;

// Đảm bảo mọi Repository trong cùng một Service method dùng chung một DbContext
// và commit chung một transaction (SaveChangesAsync gọi một lần duy nhất).
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
