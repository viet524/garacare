using GaraCare.Application.DTOs.QuotationItems;
using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GaraCare.Tests;

public class WorkOrderIntakeFlowTests
{
    [Fact]
    public async Task FullIntakeFlow_WalkInToResendQuote_ProducesExpectedStateAndHistory()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        var clock = new FakeDateTimeProvider();
        var email = new FakeEmailService();
        var workOrderService = new WorkOrderService(unitOfWork, clock, email, NullLogger<WorkOrderService>.Instance);
        var quotationItemService = new QuotationItemService(unitOfWork);

        var staff = new User { Username = "staff1", PasswordHash = "hash", FullName = "Staff 1", Role = UserRole.Staff, IsEmailVerified = true };
        var technician = new User { Username = "tech1", PasswordHash = "hash", FullName = "Tech 1", Role = UserRole.Technician, IsEmailVerified = true };
        db.Users.AddRange(staff, technician);
        var customer = new Customer { FullName = "Khách A", Phone = "0900000000", Email = "customer@example.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var vehicle = new Vehicle { CustomerId = customer.Id, LicensePlate = "51A-12345" };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var created = await workOrderService.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicle.Id, InitialDescription = "Kêu lạ khi phanh" },
            actorUserId: staff.Id);
        Assert.Equal("Received", created.Status);

        await workOrderService.StartDiagnosisAsync(
            created.Id, new StartDiagnosisRequest { DiagnosisNote = "Mòn má phanh" }, actorUserId: technician.Id);

        await workOrderService.ConfirmDiagnosisAsync(
            created.Id, new ConfirmDiagnosisRequest { Notes = "Mòn má phanh, cần thay", EstimatedLaborHours = 1.5m }, actorTechnicianId: technician.Id);

        await quotationItemService.AddAsync(new AddQuotationItemRequest
        {
            WorkOrderId = created.Id,
            Type = QuotationItemType.Part,
            Description = "Má phanh trước",
            Quantity = 1,
            UnitPrice = 300000,
        });
        await quotationItemService.AddAsync(new AddQuotationItemRequest
        {
            WorkOrderId = created.Id,
            Type = QuotationItemType.Labor,
            Description = "Công thay má phanh",
            Quantity = 1,
            UnitPrice = 150000,
        });

        var sent = await workOrderService.SendQuoteAsync(
            created.Id, new SendQuoteRequest { FinalEstimatedDate = clock.UtcNow.AddDays(1) }, actorUserId: staff.Id);
        Assert.Equal("QuotePending", sent.Status);
        Assert.Equal(450000, sent.TotalAmount);
        var tokenAfterSend = (await db.WorkOrders.FindAsync(created.Id))!.ApprovalToken;

        var resent = await workOrderService.ResendQuoteAsync(created.Id, actorUserId: staff.Id);
        Assert.Equal("QuotePending", resent.Status);
        var tokenAfterResend = (await db.WorkOrders.FindAsync(created.Id))!.ApprovalToken;
        Assert.NotEqual(tokenAfterSend, tokenAfterResend);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == created.Id).OrderBy(h => h.ChangedAt).ToList();
        Assert.Equal(4, history.Count);
        Assert.Equal((WorkOrderStatus.Received, WorkOrderStatus.Received), (history[0].FromStatus, history[0].ToStatus));
        Assert.Equal((WorkOrderStatus.Received, WorkOrderStatus.Diagnosing), (history[1].FromStatus, history[1].ToStatus));
        Assert.Equal((WorkOrderStatus.Diagnosing, WorkOrderStatus.DiagnosisConfirmed), (history[2].FromStatus, history[2].ToStatus));
        Assert.Equal((WorkOrderStatus.DiagnosisConfirmed, WorkOrderStatus.QuotePending), (history[3].FromStatus, history[3].ToStatus));

        var detail = await workOrderService.GetByIdAsync(created.Id);
        Assert.Equal(2, detail.QuotationItems.Count);

        var notifications = db.Notifications.Where(n => n.WorkOrderId == created.Id).ToList();
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, n => Assert.Equal(NotificationType.QuoteReady, n.Type));
    }
}
