using GaraCare.Application.Interfaces;

namespace GaraCare.Infrastructure.Common;

// GoF Singleton: constructor private, chỉ một instance duy nhất truy cập qua Instance.
// ASP.NET Core DI cũng đăng ký class này với vòng đời Singleton (xem DependencyInjection.cs)
// nên toàn bộ app resolve ra đúng một instance qua constructor injection.
public sealed class DateTimeProvider : IDateTimeProvider
{
    private static readonly Lazy<DateTimeProvider> LazyInstance = new(() => new DateTimeProvider());

    public static DateTimeProvider Instance => LazyInstance.Value;

    private DateTimeProvider()
    {
    }

    public DateTime UtcNow => DateTime.UtcNow;
}
