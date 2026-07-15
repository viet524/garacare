using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GaraCare.Tests;

public class WorkOrderServiceTests
{
    private static (WorkOrderService Service, GaraCareDbContext Db, FakeDateTimeProvider Clock, FakeEmailService Email) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        var clock = new FakeDateTimeProvider();
        var email = new FakeEmailService();
        return (new WorkOrderService(unitOfWork, clock, email, NullLogger<WorkOrderService>.Instance), db, clock, email);
    }

    private static async Task<(int VehicleId, int UserId, int CustomerId)> SeedVehicleAsync(GaraCareDbContext db)
    {
        var user = new User { Username = "staff1", PasswordHash = "hash", FullName = "Staff 1", Role = UserRole.Staff, IsEmailVerified = true };
        db.Users.Add(user);
        var customer = new Customer { FullName = "Khách A", Phone = "0900000000" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var vehicle = new Vehicle { CustomerId = customer.Id, LicensePlate = "51A-12345" };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return (vehicle.Id, user.Id, customer.Id);
    }

    [Fact]
    public async Task GetHistoryByVehicleAsync_VehicleNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetHistoryByVehicleAsync(999));
    }

    [Fact]
    public async Task GetHistoryByVehicleAsync_NoWorkOrders_ReturnsEmptyList()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, _, _) = await SeedVehicleAsync(db);

        var result = await service.GetHistoryByVehicleAsync(vehicleId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryByVehicleAsync_OwnerCustomer_ReturnsHistory()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        db.WorkOrders.Add(new WorkOrder
        {
            VehicleId = vehicleId,
            CreatedByUserId = userId,
            Status = WorkOrderStatus.Delivered,
            ReceivedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TotalAmount = 100,
        });
        await db.SaveChangesAsync();

        var result = await service.GetHistoryByVehicleAsync(vehicleId, requestingCustomerId: customerId);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetHistoryByVehicleAsync_NonOwnerCustomer_ThrowsForbiddenActionException()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, _, customerId) = await SeedVehicleAsync(db);
        var otherCustomerId = customerId + 999;

        await Assert.ThrowsAsync<ForbiddenActionException>(() =>
            service.GetHistoryByVehicleAsync(vehicleId, requestingCustomerId: otherCustomerId));
    }

    [Fact]
    public async Task GetHistoryByVehicleAsync_OrdersByReceivedDateDescending()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        db.WorkOrders.AddRange(
            new WorkOrder
            {
                VehicleId = vehicleId,
                CreatedByUserId = userId,
                Status = WorkOrderStatus.Delivered,
                ReceivedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TotalAmount = 100,
            },
            new WorkOrder
            {
                VehicleId = vehicleId,
                CreatedByUserId = userId,
                Status = WorkOrderStatus.Received,
                ReceivedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TotalAmount = 200,
            });
        await db.SaveChangesAsync();

        var result = await service.GetHistoryByVehicleAsync(vehicleId);

        Assert.Equal(2, result.Count);
        Assert.Equal(200, result[0].TotalAmount);
        Assert.Equal(100, result[1].TotalAmount);
    }

    [Fact]
    public async Task CreateWalkInAsync_VehicleNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.CreateWalkInAsync(new CreateWalkInWorkOrderRequest { VehicleId = 999, InitialDescription = "Kêu lạ" }, actorUserId: 1));
    }

    [Fact]
    public async Task CreateWalkInAsync_NoOpenWorkOrder_CreatesWithoutWarning()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" },
            actorUserId: userId);

        Assert.Equal("Received", result.Status);
        Assert.False(result.HasOpenWorkOrderWarning);
    }

    [Fact]
    public async Task CreateWalkInAsync_HasOpenWorkOrder_CreatesWithWarning()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        db.WorkOrders.Add(new WorkOrder
        {
            VehicleId = vehicleId,
            CreatedByUserId = userId,
            Status = WorkOrderStatus.Diagnosing,
            ReceivedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" },
            actorUserId: userId);

        Assert.True(result.HasOpenWorkOrderWarning);
    }

    [Fact]
    public async Task CreateWalkInAsync_DeliveredOrCancelledWorkOrder_DoesNotCountAsOpen()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        db.WorkOrders.Add(new WorkOrder
        {
            VehicleId = vehicleId,
            CreatedByUserId = userId,
            Status = WorkOrderStatus.Delivered,
            ReceivedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" },
            actorUserId: userId);

        Assert.False(result.HasOpenWorkOrderWarning);
    }

    [Fact]
    public async Task CreateWalkInAsync_WritesExactlyOneStatusHistoryRow()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" },
            actorUserId: userId);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == result.Id).ToList();
        Assert.Single(history);
        Assert.Equal(WorkOrderStatus.Received, history[0].FromStatus);
        Assert.Equal(WorkOrderStatus.Received, history[0].ToStatus);
    }

    [Fact]
    public async Task StartDiagnosisAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.StartDiagnosisAsync(999, new StartDiagnosisRequest(), actorUserId: 1));
    }

    [Fact]
    public async Task StartDiagnosisAsync_FromReceived_TransitionsToDiagnosing()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Received, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var result = await service.StartDiagnosisAsync(workOrder.Id, new StartDiagnosisRequest { DiagnosisNote = "Hỏng bugi" }, actorUserId: userId);

        Assert.Equal("Diagnosing", result.Status);
        Assert.Equal("Hỏng bugi", result.DiagnosisNote);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Diagnosing)]
    [InlineData(WorkOrderStatus.QuotePending)]
    [InlineData(WorkOrderStatus.InRepair)]
    public async Task StartDiagnosisAsync_FromNonReceivedStatus_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.StartDiagnosisAsync(workOrder.Id, new StartDiagnosisRequest(), actorUserId: userId));
    }

    [Fact]
    public async Task StartDiagnosisAsync_WritesExactlyOneStatusHistoryRow()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Received, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await service.StartDiagnosisAsync(workOrder.Id, new StartDiagnosisRequest(), actorUserId: userId);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == workOrder.Id).ToList();
        Assert.Single(history);
        Assert.Equal(WorkOrderStatus.Received, history[0].FromStatus);
        Assert.Equal(WorkOrderStatus.Diagnosing, history[0].ToStatus);
    }

    [Fact]
    public async Task SendQuoteAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.SendQuoteAsync(999, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: 1));
    }

    [Theory]
    [InlineData(WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.InRepair)]
    public async Task SendQuoteAsync_NotDiagnosing_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
    }

    [Fact]
    public async Task SendQuoteAsync_NoQuotationItems_ThrowsEmptyQuotationException()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<EmptyQuotationException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
    }

    [Fact]
    public async Task SendQuoteAsync_ValidRequest_GeneratesTokenWith72HourExpiryAndTransitionsToQuotePending()
    {
        var (service, db, clock, _) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        db.Customers.First(c => c.Id == customerId).Email = "customer@example.com";
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();
        var estimatedCompletion = clock.UtcNow.AddDays(2);

        var result = await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = estimatedCompletion }, actorUserId: userId);

        Assert.Equal("QuotePending", result.Status);
        var updated = await db.WorkOrders.FindAsync(workOrder.Id);
        Assert.False(string.IsNullOrEmpty(updated!.ApprovalToken));
        Assert.Equal(clock.UtcNow.AddHours(72), updated.ApprovalTokenExpiresAt);
        Assert.Null(updated.ApprovalTokenUsedAt);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == workOrder.Id).ToList();
        Assert.Single(history);
        Assert.Equal(WorkOrderStatus.Diagnosing, history[0].FromStatus);
        Assert.Equal(WorkOrderStatus.QuotePending, history[0].ToStatus);
    }

    [Fact]
    public async Task SendQuoteAsync_ValidRequest_SendsQuoteReadyNotificationAndEmail()
    {
        var (service, db, _, email) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        var customer = db.Customers.First(c => c.Id == customerId);
        customer.Email = "customer@example.com";
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId);

        var notification = db.Notifications.Single(n => n.WorkOrderId == workOrder.Id);
        Assert.Equal(NotificationType.QuoteReady, notification.Type);
        Assert.True(notification.EmailSentSuccessfully);
        Assert.Equal("customer@example.com", email.LastToEmail);
    }

    [Fact]
    public async Task SendQuoteAsync_CustomerHasNoEmail_StillCreatesNotification_EmailNotSent()
    {
        var (service, db, _, email) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId);

        var notification = db.Notifications.Single(n => n.WorkOrderId == workOrder.Id);
        Assert.False(notification.EmailSentSuccessfully);
        Assert.Null(email.LastToEmail);
    }

    [Fact]
    public async Task SendQuoteAsync_CalledTwiceOnAlreadyQuotePending_ThrowsWithResendHint()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.QuotePending, ReceivedDate = DateTime.UtcNow, ApprovalToken = "existing-token" };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
        Assert.Contains("resend-quote", ex.Message);
    }
}
