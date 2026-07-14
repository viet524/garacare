using System.Linq.Expressions;

namespace GaraCare.Application.Interfaces;

// Repository pattern: tách logic truy vấn/ghi dữ liệu khỏi Service layer.
// Service chỉ làm việc qua interface này, không biết gì về EF Core/DbContext.
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    IQueryable<T> Query();

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
