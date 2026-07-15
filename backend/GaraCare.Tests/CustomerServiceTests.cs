using GaraCare.Application.DTOs.Customers;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GaraCare.Tests;

public class CustomerServiceTests
{
    private static (CustomerService Service, GaraCareDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        return (new CustomerService(unitOfWork), db);
    }

    [Fact]
    public async Task FindByPhoneAsync_NotFound_ReturnsNull()
    {
        var (service, _) = CreateService();

        var result = await service.FindByPhoneAsync("0900000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByPhoneAsync_Found_ReturnsCustomer()
    {
        var (service, _) = CreateService();
        await service.CreateAsync(new CreateCustomerRequest { FullName = "Nguyễn Văn A", Phone = "0911111111" });

        var result = await service.FindByPhoneAsync("0911111111");

        Assert.NotNull(result);
        Assert.Equal("Nguyễn Văn A", result!.FullName);
    }

    [Fact]
    public async Task CreateAsync_Success_PersistsWalkInCustomer()
    {
        var (service, db) = CreateService();

        var result = await service.CreateAsync(new CreateCustomerRequest
        {
            FullName = "Trần Thị B",
            Phone = "0922222222",
            Email = "b@example.com",
            Address = "123 Lê Lợi",
        });

        Assert.Null(result.UserId);
        var stored = await db.Customers.SingleAsync(c => c.Phone == "0922222222");
        Assert.Equal("Trần Thị B", stored.FullName);
    }

    [Fact]
    public async Task CreateAsync_DuplicatePhoneLinkedToAccount_ThrowsDuplicatePhoneException()
    {
        var (service, db) = CreateService();
        db.Users.Add(new User { Username = "u1", PasswordHash = "hash", FullName = "U1", Email = "u1@example.com" });
        await db.SaveChangesAsync();
        var userId = db.Users.Single().Id;
        db.Customers.Add(new Customer { FullName = "Đã đăng ký", Phone = "0933333333", UserId = userId });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DuplicatePhoneException>(() =>
            service.CreateAsync(new CreateCustomerRequest { FullName = "Khách mới", Phone = "0933333333" }));
    }

    [Fact]
    public async Task CreateAsync_DuplicatePhoneBetweenTwoWalkInCustomers_Allowed()
    {
        var (service, _) = CreateService();
        await service.CreateAsync(new CreateCustomerRequest { FullName = "Khách vãng lai 1", Phone = "0944444444" });

        var result = await service.CreateAsync(new CreateCustomerRequest { FullName = "Khách vãng lai 2", Phone = "0944444444" });

        Assert.Equal("Khách vãng lai 2", result.FullName);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCreatedCustomers()
    {
        var (service, _) = CreateService();
        await service.CreateAsync(new CreateCustomerRequest { FullName = "Khách A", Phone = "0955555555" });
        await service.CreateAsync(new CreateCustomerRequest { FullName = "Khách B", Phone = "0955555556" });

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }
}
