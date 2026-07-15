using GaraCare.Application.DTOs.QuotationItems;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GaraCare.Tests;

public class QuotationItemServiceTests
{
    private static (QuotationItemService Service, GaraCareDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        return (new QuotationItemService(unitOfWork), db);
    }

    private static async Task<int> SeedWorkOrderAsync(GaraCareDbContext db, WorkOrderStatus status)
    {
        var user = new User { Username = "staff1", PasswordHash = "hash", FullName = "Staff 1", Role = UserRole.Staff, IsEmailVerified = true };
        db.Users.Add(user);
        var customer = new Customer { FullName = "Khách A", Phone = "0900000000" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var vehicle = new Vehicle { CustomerId = customer.Id, LicensePlate = "51A-12345" };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        var workOrder = new WorkOrder { VehicleId = vehicle.Id, CreatedByUserId = user.Id, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();
        return workOrder.Id;
    }

    [Fact]
    public async Task AddAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.AddAsync(new AddQuotationItemRequest { WorkOrderId = 999, Type = QuotationItemType.Labor, Description = "Công thay dầu", Quantity = 1, UnitPrice = 50000 }));
    }

    [Theory]
    [InlineData(WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.InRepair)]
    [InlineData(WorkOrderStatus.Completed)]
    public async Task AddAsync_WorkOrderNotInEditableStatus_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, status);

        await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.AddAsync(new AddQuotationItemRequest { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Công thay dầu", Quantity = 1, UnitPrice = 50000 }));
    }

    [Fact]
    public async Task AddAsync_WorkOrderHasApprovedItem_ThrowsQuotationLockedException_EvenForUnrelatedNewItem()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.QuotePending);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Đã duyệt", Quantity = 1, UnitPrice = 10000, IsApproved = true });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<QuotationLockedException>(() =>
            service.AddAsync(new AddQuotationItemRequest { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Hạng mục mới", Quantity = 1, UnitPrice = 20000 }));
    }

    [Fact]
    public async Task AddAsync_PartWithInsufficientStock_DoesNotBlock_ReturnsLowStockWarning()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.Diagnosing);
        var part = new Part { Name = "Lọc dầu", UnitPrice = 100000, StockQuantity = 1 };
        db.Parts.Add(part);
        await db.SaveChangesAsync();

        var result = await service.AddAsync(new AddQuotationItemRequest
        {
            WorkOrderId = workOrderId,
            PartId = part.Id,
            Type = QuotationItemType.Part,
            Description = "Thay lọc dầu",
            Quantity = 5,
            UnitPrice = 100000,
        });

        Assert.True(result.LowStockWarning);
        Assert.Equal(500000, result.LineTotal);
    }

    [Fact]
    public async Task AddAsync_QuantityAndUnitPriceMultiply_RecalculatesWorkOrderTotalAmount()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.Diagnosing);

        await service.AddAsync(new AddQuotationItemRequest { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Công 1", Quantity = 1, UnitPrice = 100000 });
        await service.AddAsync(new AddQuotationItemRequest { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Công 2", Quantity = 2, UnitPrice = 100000 });

        var workOrder = await db.WorkOrders.FindAsync(workOrderId);
        Assert.Equal(300000, workOrder!.TotalAmount);
    }

    [Fact]
    public async Task RemoveAsync_ItemNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.RemoveAsync(999));
    }

    [Fact]
    public async Task RemoveAsync_ExistingItem_RecalculatesWorkOrderTotalAmount()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.Diagnosing);
        var itemToKeep = new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Giữ lại", Quantity = 1, UnitPrice = 100000 };
        var itemToRemove = new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Xoá đi", Quantity = 1, UnitPrice = 200000 };
        db.QuotationItems.AddRange(itemToKeep, itemToRemove);
        var workOrder = await db.WorkOrders.FindAsync(workOrderId);
        workOrder!.TotalAmount = 300000;
        await db.SaveChangesAsync();

        await service.RemoveAsync(itemToRemove.Id);

        var updated = await db.WorkOrders.FindAsync(workOrderId);
        Assert.Equal(100000, updated!.TotalAmount);
    }

    [Fact]
    public async Task RemoveAsync_WorkOrderHasApprovedItem_ThrowsQuotationLockedException()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.QuotePending);
        var approved = new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Đã duyệt", Quantity = 1, UnitPrice = 10000, IsApproved = true };
        var other = new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Chưa duyệt", Quantity = 1, UnitPrice = 20000 };
        db.QuotationItems.AddRange(approved, other);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<QuotationLockedException>(() => service.RemoveAsync(other.Id));
    }
}
