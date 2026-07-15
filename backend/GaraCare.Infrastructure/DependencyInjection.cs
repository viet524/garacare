using GaraCare.Application.Interfaces;
using GaraCare.Application.Services;
using GaraCare.Infrastructure.Auth;
using GaraCare.Infrastructure.Common;
using GaraCare.Infrastructure.Email;
using GaraCare.Infrastructure.Persistence;
using GaraCare.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GaraCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GaraCareDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repository pattern — DbContext là Scoped nên Repository/UnitOfWork cũng phải Scoped
        // (dùng chung 1 DbContext instance trong 1 request/1 transaction).
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Singleton pattern — một instance duy nhất cho toàn bộ vòng đời ứng dụng.
        services.AddSingleton<IDateTimeProvider>(DateTimeProvider.Instance);

        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddScoped<IEmailService, EmailService>();

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<IQuotationItemService, QuotationItemService>();

        return services;
    }
}
