using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GaraCare.Tests;

public class VehicleServiceTests
{
    private static (VehicleService Service, GaraCareDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        return (new VehicleService(unitOfWork, NullLogger<VehicleService>.Instance), db);
    }

    private static async Task<int> SeedCustomerAsync(GaraCareDbContext db, string fullName = "Khách A")
    {
        var customer = new Customer { FullName = fullName, Phone = "0900000000" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    [Fact]
    public async Task CreateAsync_CustomerNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.CreateAsync(new CreateVehicleRequest { CustomerId = 999, LicensePlate = "51A-12345" }));
    }

    [Fact]
    public async Task CreateAsync_Success_PersistsVehicle()
    {
        var (service, db) = CreateService();
        var customerId = await SeedCustomerAsync(db);

        var result = await service.CreateAsync(new CreateVehicleRequest
        {
            CustomerId = customerId,
            LicensePlate = "51A-11111",
            Brand = "Honda",
            Model = "Wave",
            Year = 2020,
        });

        Assert.Equal(customerId, result.CustomerId);
        var stored = await db.Vehicles.SingleAsync(v => v.LicensePlate == "51A-11111");
        Assert.Equal("Honda", stored.Brand);
    }

    [Fact]
    public async Task CreateAsync_DuplicateLicensePlateOnDifferentCustomer_AllowedNotThrown()
    {
        var (service, db) = CreateService();
        var firstCustomerId = await SeedCustomerAsync(db, "Khách A");
        await service.CreateAsync(new CreateVehicleRequest { CustomerId = firstCustomerId, LicensePlate = "51A-99999" });
        var secondCustomerId = await SeedCustomerAsync(db, "Khách B");

        var result = await service.CreateAsync(new CreateVehicleRequest { CustomerId = secondCustomerId, LicensePlate = "51A-99999" });

        Assert.Equal(secondCustomerId, result.CustomerId);
    }

    [Fact]
    public async Task GetByCustomerAsync_NoVehicles_ReturnsEmptyList()
    {
        var (service, db) = CreateService();
        var customerId = await SeedCustomerAsync(db);

        var result = await service.GetByCustomerAsync(customerId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByCustomerAsync_ReturnsOnlyVehiclesOfThatCustomer()
    {
        var (service, db) = CreateService();
        var customerId = await SeedCustomerAsync(db, "Khách A");
        var otherCustomerId = await SeedCustomerAsync(db, "Khách B");
        await service.CreateAsync(new CreateVehicleRequest { CustomerId = customerId, LicensePlate = "51A-00001" });
        await service.CreateAsync(new CreateVehicleRequest { CustomerId = otherCustomerId, LicensePlate = "51A-00002" });

        var result = await service.GetByCustomerAsync(customerId);

        Assert.Single(result);
        Assert.Equal("51A-00001", result[0].LicensePlate);
    }
}
