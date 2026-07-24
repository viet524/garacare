using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GaraCare.Tests;

public class UserServiceTests
{
    private static (UserService Service, GaraCareDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        return (new UserService(unitOfWork), db);
    }

    [Fact]
    public async Task GetInternalUsersAsync_ExcludesCustomerRole()
    {
        var (service, db) = CreateService();
        db.Users.AddRange(
            new User { Username = "staff1", PasswordHash = "hash", FullName = "Staff 1", Role = UserRole.Staff, IsEmailVerified = true },
            new User { Username = "customer1", PasswordHash = "hash", FullName = "Customer 1", Role = UserRole.Customer, IsEmailVerified = true });
        await db.SaveChangesAsync();

        var result = await service.GetInternalUsersAsync();

        Assert.Single(result);
        Assert.Equal("Staff", result[0].Role);
    }

    [Fact]
    public async Task GetInternalUsersAsync_IncludesTechnicianStatus()
    {
        var (service, db) = CreateService();
        db.Users.Add(new User { Username = "tech1", PasswordHash = "hash", FullName = "Tech 1", Role = UserRole.Technician, TechnicianStatus = TechnicianStatus.WaitingOnCustomer, IsEmailVerified = true });
        await db.SaveChangesAsync();

        var result = await service.GetInternalUsersAsync();

        Assert.Single(result);
        Assert.Equal("WaitingOnCustomer", result[0].TechnicianStatus);
    }

    [Fact]
    public async Task GetInternalUsersAsync_NonTechnician_TechnicianStatusIsNull()
    {
        var (service, db) = CreateService();
        db.Users.Add(new User { Username = "staff1", PasswordHash = "hash", FullName = "Staff 1", Role = UserRole.Staff, IsEmailVerified = true });
        await db.SaveChangesAsync();

        var result = await service.GetInternalUsersAsync();

        Assert.Single(result);
        Assert.Null(result[0].TechnicianStatus);
    }
}
