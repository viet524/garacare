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
    public async Task ConfirmDiagnosisAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.ConfirmDiagnosisAsync(999, new ConfirmDiagnosisRequest { Notes = "x", EstimatedLaborHours = 1 }, actorTechnicianId: 1));
    }

    [Theory]
    [InlineData(WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.DiagnosisConfirmed)]
    [InlineData(WorkOrderStatus.QuotePending)]
    public async Task ConfirmDiagnosisAsync_NotDiagnosing_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.ConfirmDiagnosisAsync(workOrder.Id, new ConfirmDiagnosisRequest { Notes = "x", EstimatedLaborHours = 1 }, actorTechnicianId: userId));
    }

    [Fact]
    public async Task ConfirmDiagnosisAsync_ValidRequest_CreatesImmutableDiagnosisRecordAndTransitions()
    {
        var (service, db, clock, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var result = await service.ConfirmDiagnosisAsync(
            workOrder.Id, new ConfirmDiagnosisRequest { Notes = "Mòn má phanh", EstimatedLaborHours = 1.5m }, actorTechnicianId: userId);

        Assert.Equal("DiagnosisConfirmed", result.Status);
        var record = db.DiagnosisRecords.Single(d => d.WorkOrderId == workOrder.Id);
        Assert.Equal("Mòn má phanh", record.Notes);
        Assert.Equal(1.5m, record.EstimatedLaborHours);
        Assert.Equal(userId, record.TechnicianId);
        Assert.Equal(clock.UtcNow, record.SignedAt);

        var updated = await db.WorkOrders.FindAsync(workOrder.Id);
        Assert.False(updated!.IsHeavyRepair);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == workOrder.Id).ToList();
        Assert.Single(history);
        Assert.Equal(WorkOrderStatus.Diagnosing, history[0].FromStatus);
        Assert.Equal(WorkOrderStatus.DiagnosisConfirmed, history[0].ToStatus);
    }

    [Fact]
    public async Task ConfirmDiagnosisAsync_LaborHoursOverThreshold_SetsIsHeavyRepairTrue()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await service.ConfirmDiagnosisAsync(
            workOrder.Id, new ConfirmDiagnosisRequest { Notes = "Hạ hộp số kiểm tra", EstimatedLaborHours = 3m }, actorTechnicianId: userId);

        var updated = await db.WorkOrders.FindAsync(workOrder.Id);
        Assert.True(updated!.IsHeavyRepair);
    }

    [Fact]
    public async Task ConfirmDiagnosisAsync_AssignedToOtherTechnician_ThrowsForbiddenActionException()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var otherTechnician = new User { Username = "tech2", PasswordHash = "hash", FullName = "Tech 2", Role = UserRole.Technician, IsEmailVerified = true };
        db.Users.Add(otherTechnician);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();
        db.WorkOrderAssignments.Add(new WorkOrderAssignment
        {
            WorkOrderId = workOrder.Id,
            TechnicianId = otherTechnician.Id,
            Role = AssignmentRole.Primary,
            StageAtStart = TechnicianStatus.Free,
            StartedAt = DateTime.UtcNow,
            CommissionSplitPercent = 100,
            ApprovedByUserId = userId,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenActionException>(() =>
            service.ConfirmDiagnosisAsync(workOrder.Id, new ConfirmDiagnosisRequest { Notes = "x", EstimatedLaborHours = 1 }, actorTechnicianId: userId));
    }

    [Fact]
    public async Task CreateWalkInAsync_FreeTechnicianAndBayAvailable_AutoCreatesPrimaryAssignment()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var technician = new User { Username = "tech1", PasswordHash = "hash", FullName = "Tech 1", Role = UserRole.Technician, TechnicianStatus = TechnicianStatus.Free, IsEmailVerified = true };
        db.Users.Add(technician);
        db.Bays.Add(new Bay { Type = BayType.GeneralBay, Status = BayStatus.Free });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" }, actorUserId: userId);

        var assignment = db.WorkOrderAssignments.Single(a => a.WorkOrderId == result.Id);
        Assert.Equal(technician.Id, assignment.TechnicianId);
        Assert.Equal(AssignmentRole.Primary, assignment.Role);
        Assert.Equal(100, assignment.CommissionSplitPercent);
        var bay = db.Bays.Single();
        Assert.Equal(BayStatus.Occupied, bay.Status);
        Assert.Equal(result.Id, bay.CurrentWorkOrderId);
    }

    [Fact]
    public async Task CreateWalkInAsync_NoFreeBay_LeavesWorkOrderUnassigned()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        db.Users.Add(new User { Username = "tech1", PasswordHash = "hash", FullName = "Tech 1", Role = UserRole.Technician, TechnicianStatus = TechnicianStatus.Free, IsEmailVerified = true });
        db.Bays.Add(new Bay { Type = BayType.GeneralBay, Status = BayStatus.Occupied, CurrentWorkOrderId = null });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" }, actorUserId: userId);

        Assert.Equal("Received", result.Status);
        Assert.Empty(db.WorkOrderAssignments.Where(a => a.WorkOrderId == result.Id));
    }

    [Fact]
    public async Task CreateWalkInAsync_NoFreeTechnician_LeavesWorkOrderUnassigned()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        db.Users.Add(new User { Username = "tech1", PasswordHash = "hash", FullName = "Tech 1", Role = UserRole.Technician, TechnicianStatus = TechnicianStatus.InRepair, IsEmailVerified = true });
        db.Bays.Add(new Bay { Type = BayType.GeneralBay, Status = BayStatus.Free });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" }, actorUserId: userId);

        Assert.Empty(db.WorkOrderAssignments.Where(a => a.WorkOrderId == result.Id));
    }

    [Fact]
    public async Task CreateWalkInAsync_TwoFreeTechnicians_PicksLeastBusyOne()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var busyTechnician = new User { Username = "tech-busy", PasswordHash = "hash", FullName = "Busy", Role = UserRole.Technician, TechnicianStatus = TechnicianStatus.Free, IsEmailVerified = true };
        var idleTechnician = new User { Username = "tech-idle", PasswordHash = "hash", FullName = "Idle", Role = UserRole.Technician, TechnicianStatus = TechnicianStatus.Free, IsEmailVerified = true };
        db.Users.AddRange(busyTechnician, idleTechnician);
        db.Bays.Add(new Bay { Type = BayType.GeneralBay, Status = BayStatus.Free });
        await db.SaveChangesAsync();
        // busyTechnician đã ôm 1 WorkOrder khác từ trước (assignment còn active).
        var otherWorkOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(otherWorkOrder);
        await db.SaveChangesAsync();
        db.WorkOrderAssignments.Add(new WorkOrderAssignment
        {
            WorkOrderId = otherWorkOrder.Id,
            TechnicianId = busyTechnician.Id,
            Role = AssignmentRole.Primary,
            StageAtStart = TechnicianStatus.Free,
            StartedAt = DateTime.UtcNow,
            CommissionSplitPercent = 100,
            ApprovedByUserId = userId,
        });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" }, actorUserId: userId);

        var assignment = db.WorkOrderAssignments.Single(a => a.WorkOrderId == result.Id);
        Assert.Equal(idleTechnician.Id, assignment.TechnicianId);
    }

    [Theory]
    [InlineData(TechnicianStatus.Diagnosing)]
    [InlineData(TechnicianStatus.WaitingOnCustomer)]
    [InlineData(TechnicianStatus.WaitingParts)]
    public async Task CreateWalkInAsync_TechnicianCanTakeMoreWork_StillGetsAssigned(TechnicianStatus status)
    {
        // docs/01-business-spec.md §10: mọi trạng thái trừ IN_REPAIR đều nhận được Diagnosing
        // mới chen thêm — không chỉ riêng FREE.
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var technician = new User { Username = "tech1", PasswordHash = "hash", FullName = "Tech 1", Role = UserRole.Technician, TechnicianStatus = status, IsEmailVerified = true };
        db.Users.Add(technician);
        db.Bays.Add(new Bay { Type = BayType.GeneralBay, Status = BayStatus.Free });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" }, actorUserId: userId);

        var assignment = db.WorkOrderAssignments.Single(a => a.WorkOrderId == result.Id);
        Assert.Equal(technician.Id, assignment.TechnicianId);
        Assert.Equal(status, assignment.StageAtStart);
    }

    [Fact]
    public async Task CreateWalkInAsync_FreeAndWaitingOnCustomerBothEligible_PrefersFreeRegardlessOfLoad()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var freeTechnician = new User { Username = "tech-free", PasswordHash = "hash", FullName = "Free", Role = UserRole.Technician, TechnicianStatus = TechnicianStatus.Free, IsEmailVerified = true };
        var waitingTechnician = new User { Username = "tech-waiting", PasswordHash = "hash", FullName = "Waiting", Role = UserRole.Technician, TechnicianStatus = TechnicianStatus.WaitingOnCustomer, IsEmailVerified = true };
        db.Users.AddRange(freeTechnician, waitingTechnician);
        db.Bays.Add(new Bay { Type = BayType.GeneralBay, Status = BayStatus.Free });
        await db.SaveChangesAsync();
        // freeTechnician đang ôm nhiều việc hơn nhưng vẫn được ưu tiên vì đang FREE thật sự.
        var otherWorkOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(otherWorkOrder);
        await db.SaveChangesAsync();
        db.WorkOrderAssignments.Add(new WorkOrderAssignment
        {
            WorkOrderId = otherWorkOrder.Id,
            TechnicianId = freeTechnician.Id,
            Role = AssignmentRole.Primary,
            StageAtStart = TechnicianStatus.Free,
            StartedAt = DateTime.UtcNow,
            CommissionSplitPercent = 100,
            ApprovedByUserId = userId,
        });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" }, actorUserId: userId);

        var assignment = db.WorkOrderAssignments.Single(a => a.WorkOrderId == result.Id);
        Assert.Equal(freeTechnician.Id, assignment.TechnicianId);
    }

    [Fact]
    public async Task GetTechnicianQueueAsync_OrdersInRepairBeforeReceived()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var technician = new User { Username = "tech1", PasswordHash = "hash", FullName = "Tech 1", Role = UserRole.Technician, IsEmailVerified = true };
        db.Users.Add(technician);
        var received = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Received, ReceivedDate = DateTime.UtcNow };
        var inRepair = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.InRepair, ReceivedDate = DateTime.UtcNow };
        var delivered = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Delivered, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.AddRange(received, inRepair, delivered);
        await db.SaveChangesAsync();
        foreach (var wo in new[] { received, inRepair, delivered })
        {
            db.WorkOrderAssignments.Add(new WorkOrderAssignment
            {
                WorkOrderId = wo.Id,
                TechnicianId = technician.Id,
                Role = AssignmentRole.Primary,
                StageAtStart = TechnicianStatus.Free,
                StartedAt = DateTime.UtcNow,
                CommissionSplitPercent = 100,
                ApprovedByUserId = userId,
            });
        }
        await db.SaveChangesAsync();

        var result = await service.GetTechnicianQueueAsync(technician.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(inRepair.Id, result[0].Id);
        Assert.Equal(received.Id, result[1].Id);
    }

    [Fact]
    public async Task SendQuoteAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.SendQuoteAsync(999, new SendQuoteRequest { FinalEstimatedDate = DateTime.UtcNow.AddDays(3) }, actorUserId: 1));
    }

    [Theory]
    [InlineData(WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.Diagnosing)]
    [InlineData(WorkOrderStatus.InRepair)]
    public async Task SendQuoteAsync_NotDiagnosisConfirmed_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { FinalEstimatedDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
    }

    [Fact]
    public async Task SendQuoteAsync_NoQuotationItems_ThrowsEmptyQuotationException()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.DiagnosisConfirmed, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<EmptyQuotationException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { FinalEstimatedDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
    }

    [Fact]
    public async Task SendQuoteAsync_FinalEstimatedDateBeforeSystemSuggestedDate_ThrowsInvalidEstimatedDateException()
    {
        var (service, db, clock, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.DiagnosisConfirmed, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        // Không có DiagnosisRecord => laborHours mặc định 0, nhưng vẫn còn bayWaitTime (không có
        // Bay nào trong DB test) + buffer cố định => systemSuggestedDate > now một khoảng nhất định.
        await Assert.ThrowsAsync<InvalidEstimatedDateException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { FinalEstimatedDate = clock.UtcNow }, actorUserId: userId));
    }

    [Fact]
    public async Task SendQuoteAsync_ValidRequest_GeneratesTokenWith72HourExpiryAndTransitionsToQuotePending()
    {
        var (service, db, clock, _) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        db.Customers.First(c => c.Id == customerId).Email = "customer@example.com";
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.DiagnosisConfirmed, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();
        var estimatedCompletion = clock.UtcNow.AddDays(2);

        var result = await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { FinalEstimatedDate = estimatedCompletion }, actorUserId: userId);

        Assert.Equal("QuotePending", result.Status);
        var updated = await db.WorkOrders.FindAsync(workOrder.Id);
        Assert.False(string.IsNullOrEmpty(updated!.ApprovalToken));
        Assert.Equal(clock.UtcNow.AddHours(72), updated.ApprovalTokenExpiresAt);
        Assert.Null(updated.ApprovalTokenUsedAt);
        Assert.Equal(estimatedCompletion, updated.FinalEstimatedDate);
        Assert.NotNull(updated.SystemSuggestedDate);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == workOrder.Id).ToList();
        Assert.Single(history);
        Assert.Equal(WorkOrderStatus.DiagnosisConfirmed, history[0].FromStatus);
        Assert.Equal(WorkOrderStatus.QuotePending, history[0].ToStatus);
    }

    [Fact]
    public async Task SendQuoteAsync_ValidRequest_SendsQuoteReadyNotificationAndEmail()
    {
        var (service, db, _, email) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        var customer = db.Customers.First(c => c.Id == customerId);
        customer.Email = "customer@example.com";
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.DiagnosisConfirmed, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { FinalEstimatedDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId);

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
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.DiagnosisConfirmed, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { FinalEstimatedDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId);

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
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { FinalEstimatedDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
        Assert.Contains("resend-quote", ex.Message);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Diagnosing)]
    [InlineData(WorkOrderStatus.DiagnosisConfirmed)]
    [InlineData(WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.InRepair)]
    public async Task ResendQuoteAsync_NotQuotePending_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidTransitionException>(() => service.ResendQuoteAsync(workOrder.Id, actorUserId: userId));
    }

    [Fact]
    public async Task ResendQuoteAsync_QuotePending_GeneratesNewTokenAndDoesNotWriteStatusHistory()
    {
        var (service, db, clock, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder
        {
            VehicleId = vehicleId,
            CreatedByUserId = userId,
            Status = WorkOrderStatus.QuotePending,
            ReceivedDate = DateTime.UtcNow,
            ApprovalToken = "old-token",
            ApprovalTokenExpiresAt = clock.UtcNow.AddHours(1),
        };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();
        clock.UtcNow = clock.UtcNow.AddHours(10);

        var result = await service.ResendQuoteAsync(workOrder.Id, actorUserId: userId);

        Assert.Equal("QuotePending", result.Status);
        var updated = await db.WorkOrders.FindAsync(workOrder.Id);
        Assert.NotEqual("old-token", updated!.ApprovalToken);
        Assert.Equal(clock.UtcNow.AddHours(72), updated.ApprovalTokenExpiresAt);
        Assert.Empty(db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == workOrder.Id));
    }

    [Fact]
    public async Task ResendQuoteAsync_QuotePending_SendsQuoteReadyNotificationAgain()
    {
        var (service, db, _, email) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        db.Customers.First(c => c.Id == customerId).Email = "customer@example.com";
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.QuotePending, ReceivedDate = DateTime.UtcNow, ApprovalToken = "old-token" };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await service.ResendQuoteAsync(workOrder.Id, actorUserId: userId);

        var notifications = db.Notifications.Where(n => n.WorkOrderId == workOrder.Id).ToList();
        Assert.Single(notifications);
        Assert.Equal("customer@example.com", email.LastToEmail);
    }

    [Fact]
    public async Task GetByIdAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsWorkOrderWithQuotationItems()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        var result = await service.GetByIdAsync(workOrder.Id);

        Assert.Equal("Diagnosing", result.Status);
        Assert.Single(result.QuotationItems);
        Assert.Equal("Công", result.QuotationItems[0].Description);
    }

    [Fact]
    public async Task GetListAsync_NoWorkOrders_ReturnsEmptyList()
    {
        var (service, _, _, _) = CreateService();

        var result = await service.GetListAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetListAsync_OrdersByReceivedDateDescending()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        db.WorkOrders.AddRange(
            new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Delivered, ReceivedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Received, ReceivedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync();

        var result = await service.GetListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), result[0].ReceivedDate);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), result[1].ReceivedDate);
    }

    [Fact]
    public async Task GetListAsync_IncludesVehicleAndCustomerInfo()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        vehicle!.Brand = "Honda";
        vehicle.Model = "Wave";
        db.WorkOrders.Add(new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Received, ReceivedDate = DateTime.UtcNow, TotalAmount = 500000 });
        await db.SaveChangesAsync();

        var result = await service.GetListAsync();

        Assert.Single(result);
        Assert.Equal("51A-12345", result[0].LicensePlate);
        Assert.Equal("Honda Wave", result[0].VehicleLabel);
        Assert.Equal("Khách A", result[0].CustomerName);
        Assert.Equal("0900000000", result[0].CustomerPhone);
        Assert.Equal(500000, result[0].TotalAmount);
    }
}
