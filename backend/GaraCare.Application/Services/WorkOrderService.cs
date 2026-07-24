using System.Security.Cryptography;
using GaraCare.Application.DTOs.QuotationItems;
using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GaraCare.Application.Services;

public class WorkOrderService : IWorkOrderService
{
    // docs/01-business-spec.md §3 bước 4: > 2 giờ công thuần => Heavy Repair.
    private const decimal HeavyRepairThresholdHours = 2m;

    // docs/01-business-spec.md §12 — buffer cố định + % "under-promise", đã chốt với người dùng.
    private static readonly TimeSpan QcAndWashBuffer = TimeSpan.FromMinutes(30);
    private const decimal ServiceBufferPercent = 0.20m;

    // Bay dùng cho bước chẩn đoán ban đầu (kiểm tra tổng quát) — chưa biết hạng mục sửa cụ thể
    // nên chưa thể chọn đúng RequiredBayType, dùng GeneralBay. Xem ghi chú ở AutoAssignAsync.
    private const BayType DiagnosisBayType = BayType.GeneralBay;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmailService _emailService;
    private readonly ILogger<WorkOrderService> _logger;

    public WorkOrderService(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEmailService emailService,
        ILogger<WorkOrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WorkOrderSummaryResponse>> GetHistoryByVehicleAsync(int vehicleId, int? requestingCustomerId = null, CancellationToken cancellationToken = default)
    {
        var vehicle = await _unitOfWork.Repository<Vehicle>().GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy xe.");

        if (requestingCustomerId.HasValue && vehicle.CustomerId != requestingCustomerId.Value)
        {
            throw new ForbiddenActionException("Bạn không có quyền xem lịch sử sửa chữa của xe này.");
        }

        var workOrders = await _unitOfWork.Repository<WorkOrder>().FindAsync(w => w.VehicleId == vehicleId, cancellationToken);

        return workOrders
            .OrderByDescending(w => w.ReceivedDate)
            .Select(w => new WorkOrderSummaryResponse
            {
                Id = w.Id,
                Status = w.Status.ToString(),
                ReceivedDate = w.ReceivedDate,
                CompletedDate = w.CompletedDate,
                TotalAmount = w.TotalAmount,
            })
            .ToList();
    }

    public async Task<WorkOrderResponse> CreateWalkInAsync(CreateWalkInWorkOrderRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _unitOfWork.Repository<Vehicle>().GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy xe.");

        var openWorkOrders = await _unitOfWork.Repository<WorkOrder>().FindAsync(
            w => w.VehicleId == vehicle.Id && w.Status != WorkOrderStatus.Delivered && w.Status != WorkOrderStatus.Cancelled,
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var workOrder = new WorkOrder
        {
            VehicleId = vehicle.Id,
            CreatedByUserId = actorUserId,
            Status = WorkOrderStatus.Received,
            ReceivedDate = now,
            InitialDescription = request.InitialDescription,
            AppointmentId = null,
        };
        await _unitOfWork.Repository<WorkOrder>().AddAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // "Tạo mới" không phải transition thật — ghi 1 dòng lịch sử tự tham chiếu (From=To=Received)
        // để đủ audit log mà không cần một trạng thái "trước khi tạo" không tồn tại.
        await _unitOfWork.Repository<WorkOrderStatusHistory>().AddAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Received,
            ToStatus = WorkOrderStatus.Received,
            ChangedByUserId = actorUserId,
            ApprovedViaToken = false,
            ChangedAt = now,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // UC-02 bước 6 / UC-16 (docs/02-use-cases.md): auto-assign chạy ngay, không cần Staff
        // kích hoạt riêng. Không tìm được Technician/Bay khả dụng thì WorkOrder vẫn ở Received,
        // vào hàng chờ chung (docs không yêu cầu 1 endpoint riêng cho việc này).
        await AutoAssignAsync(workOrder, now, cancellationToken);

        return ToResponse(workOrder, hasOpenWorkOrderWarning: openWorkOrders.Count > 0);
    }

    // docs/01-business-spec.md §11, §12 — ghép đồng thời 1 Technician đang FREE (ít việc nhất)
    // VÀ 1 Bay đúng loại đang FREE; không gán Technician đứng chờ Bay. Bay dùng ở bước chẩn
    // đoán ban đầu là GeneralBay (chưa biết hạng mục sửa cụ thể để chọn LiftBay/TireBay — việc
    // đó chỉ xác định được sau khi Technician chẩn đoán xong, xem ghi chú ở đầu file).
    private async Task AutoAssignAsync(WorkOrder workOrder, DateTime now, CancellationToken cancellationToken)
    {
        var freeBay = (await _unitOfWork.Repository<Bay>().FindAsync(
            b => b.Type == DiagnosisBayType && b.Status == BayStatus.Free, cancellationToken))
            .FirstOrDefault();
        if (freeBay is null)
        {
            return;
        }

        // Bảng nhận việc (docs/01-business-spec.md §10): FREE/DIAGNOSING/WAITING_ON_CUSTOMER/
        // WAITING_PARTS đều nhận được Diagnosing mới chen thêm — chỉ IN_REPAIR bị khoá cứng.
        var eligibleTechnicians = await _unitOfWork.Repository<User>().FindAsync(
            u => u.Role == UserRole.Technician && u.TechnicianStatus != null && u.TechnicianStatus != TechnicianStatus.InRepair,
            cancellationToken);
        if (eligibleTechnicians.Count == 0)
        {
            return;
        }

        var activeAssignments = await _unitOfWork.Repository<WorkOrderAssignment>().FindAsync(a => a.EndedAt == null, cancellationToken);
        var loadByTechnician = activeAssignments
            .GroupBy(a => a.TechnicianId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Ưu tiên FREE trước, rồi mới tới các trạng thái "có thể chen thêm" khác; trong cùng
        // nhóm ưu tiên, chọn người ít việc nhất.
        var chosenTechnician = eligibleTechnicians
            .OrderBy(t => t.TechnicianStatus == TechnicianStatus.Free ? 0 : 1)
            .ThenBy(t => loadByTechnician.TryGetValue(t.Id, out var load) ? load : 0)
            .First();

        freeBay.Status = BayStatus.Occupied;
        freeBay.CurrentWorkOrderId = workOrder.Id;
        _unitOfWork.Repository<Bay>().Update(freeBay);

        await _unitOfWork.Repository<WorkOrderAssignment>().AddAsync(new WorkOrderAssignment
        {
            WorkOrderId = workOrder.Id,
            TechnicianId = chosenTechnician.Id,
            Role = AssignmentRole.Primary,
            StageAtStart = chosenTechnician.TechnicianStatus!.Value,
            StartedAt = now,
            LaborHoursLogged = 0,
            CommissionSplitPercent = 100,
            ApprovedByUserId = workOrder.CreatedByUserId,
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkOrderResponse> StartDiagnosisAsync(int workOrderId, StartDiagnosisRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.Received)
        {
            throw new InvalidTransitionException($"Không thể bắt đầu chẩn đoán từ trạng thái {workOrder.Status}.");
        }

        var assignment = await GetActiveAssignmentAsync(workOrderId, cancellationToken);
        if (assignment is not null && assignment.TechnicianId != actorUserId)
        {
            throw new ForbiddenActionException("WorkOrder này đã được gán cho Technician khác.");
        }

        var now = _dateTimeProvider.UtcNow;
        workOrder.Status = WorkOrderStatus.Diagnosing;
        if (!string.IsNullOrWhiteSpace(request.DiagnosisNote))
        {
            workOrder.DiagnosisNote = request.DiagnosisNote;
        }
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);

        if (assignment is not null)
        {
            var technician = await _unitOfWork.Repository<User>().GetByIdAsync(actorUserId, cancellationToken);
            if (technician is not null)
            {
                technician.TechnicianStatus = TechnicianStatus.Diagnosing;
                _unitOfWork.Repository<User>().Update(technician);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.Repository<WorkOrderStatusHistory>().AddAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Received,
            ToStatus = WorkOrderStatus.Diagnosing,
            ChangedByUserId = actorUserId,
            ApprovedViaToken = false,
            ChangedAt = now,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(workOrder);
    }

    public async Task<WorkOrderResponse> ConfirmDiagnosisAsync(int workOrderId, ConfirmDiagnosisRequest request, int actorTechnicianId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.Diagnosing)
        {
            throw new InvalidTransitionException($"Không thể xác nhận chẩn đoán từ trạng thái {workOrder.Status}.");
        }

        var assignment = await GetActiveAssignmentAsync(workOrderId, cancellationToken);
        if (assignment is not null && assignment.TechnicianId != actorTechnicianId)
        {
            throw new ForbiddenActionException("WorkOrder này đã được gán cho Technician khác.");
        }

        var now = _dateTimeProvider.UtcNow;

        // Bất biến — không có API sửa/xoá (docs/03-data-model.md — entity DiagnosisRecord).
        await _unitOfWork.Repository<DiagnosisRecord>().AddAsync(new DiagnosisRecord
        {
            WorkOrderId = workOrderId,
            TechnicianId = actorTechnicianId,
            Notes = request.Notes,
            EstimatedLaborHours = request.EstimatedLaborHours,
            SignedAt = now,
        }, cancellationToken);

        workOrder.Status = WorkOrderStatus.DiagnosisConfirmed;
        workOrder.IsHeavyRepair = request.EstimatedLaborHours > HeavyRepairThresholdHours;
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);

        // Đã chẩn đoán xong — Technician quay về FREE (docs/01-business-spec.md §3 bước 4);
        // trả Bay chẩn đoán lại cho ca tiếp theo, việc sửa xe (nếu khách duyệt) cần Bay khác
        // theo hạng mục thật, không giữ Bay chẩn đoán suốt lúc chờ khách duyệt giá.
        var technician = await _unitOfWork.Repository<User>().GetByIdAsync(actorTechnicianId, cancellationToken);
        if (technician is not null)
        {
            technician.TechnicianStatus = TechnicianStatus.Free;
            _unitOfWork.Repository<User>().Update(technician);
        }

        var occupiedBay = (await _unitOfWork.Repository<Bay>().FindAsync(b => b.CurrentWorkOrderId == workOrderId, cancellationToken))
            .FirstOrDefault();
        if (occupiedBay is not null)
        {
            occupiedBay.Status = BayStatus.Free;
            occupiedBay.CurrentWorkOrderId = null;
            _unitOfWork.Repository<Bay>().Update(occupiedBay);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.Repository<WorkOrderStatusHistory>().AddAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Diagnosing,
            ToStatus = WorkOrderStatus.DiagnosisConfirmed,
            ChangedByUserId = actorTechnicianId,
            ApprovedViaToken = false,
            ChangedAt = now,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(workOrder);
    }

    public async Task<WorkOrderResponse> SendQuoteAsync(int workOrderId, SendQuoteRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.DiagnosisConfirmed)
        {
            var hint = workOrder.Status == WorkOrderStatus.QuotePending
                ? " Báo giá đã được gửi trước đó — dùng resend-quote để gửi lại link mới."
                : workOrder.Status == WorkOrderStatus.Diagnosing
                    ? " Phải xác nhận chẩn đoán (confirm-diagnosis) trước."
                    : string.Empty;
            throw new InvalidTransitionException($"Không thể gửi báo giá từ trạng thái {workOrder.Status}.{hint}");
        }

        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrderId, cancellationToken);
        if (items.Count == 0)
        {
            throw new EmptyQuotationException("Chưa có hạng mục báo giá nào.");
        }

        var now = _dateTimeProvider.UtcNow;
        var systemSuggestedDate = await CalculateSystemSuggestedDateAsync(workOrder, now, cancellationToken);
        if (request.FinalEstimatedDate < systemSuggestedDate)
        {
            throw new InvalidEstimatedDateException(
                $"Ngày dự kiến hoàn thành không được sớm hơn ngày hệ thống đề xuất ({systemSuggestedDate:yyyy-MM-dd HH:mm}).");
        }

        workOrder.ApprovalToken = RandomNumberGenerator.GetHexString(32);
        workOrder.ApprovalTokenExpiresAt = now.AddHours(72);
        workOrder.ApprovalTokenUsedAt = null;
        workOrder.SystemSuggestedDate = systemSuggestedDate;
        workOrder.FinalEstimatedDate = request.FinalEstimatedDate;
        workOrder.QuoteSentAt = now;
        workOrder.Status = WorkOrderStatus.QuotePending;
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);

        // docs/01-business-spec.md §3 bước 5: Technician chuyển WAITING_ON_CUSTOMER — được nhận
        // Diagnosing xe khác trong lúc chờ, chưa nhận InRepair mới.
        var assignment = await GetActiveAssignmentAsync(workOrderId, cancellationToken);
        if (assignment is not null)
        {
            var technician = await _unitOfWork.Repository<User>().GetByIdAsync(assignment.TechnicianId, cancellationToken);
            if (technician is not null)
            {
                technician.TechnicianStatus = TechnicianStatus.WaitingOnCustomer;
                _unitOfWork.Repository<User>().Update(technician);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.Repository<WorkOrderStatusHistory>().AddAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.DiagnosisConfirmed,
            ToStatus = WorkOrderStatus.QuotePending,
            ChangedByUserId = actorUserId,
            ApprovedViaToken = false,
            ChangedAt = now,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyQuoteReadyAsync(workOrder, cancellationToken);

        return ToResponse(workOrder);
    }

    // docs/01-business-spec.md §12 — công thức đã chốt. partsWaitTime luôn 0 (chưa có hệ thống
    // ETA nhập hàng); bayWaitTime là xấp xỉ (chưa có lịch đặt Bay theo giờ thật): 0 nếu đang có
    // Bay đúng loại chẩn đoán trống, ngược lại +2 giờ mặc định. Cả hai điểm này đã trao đổi và
    // được người dùng chấp thuận làm xấp xỉ tạm thời — xem tóm tắt trao đổi trong PR/commit.
    private async Task<DateTime> CalculateSystemSuggestedDateAsync(WorkOrder workOrder, DateTime now, CancellationToken cancellationToken)
    {
        var diagnosis = await _unitOfWork.Repository<DiagnosisRecord>().FindAsync(d => d.WorkOrderId == workOrder.Id, cancellationToken);
        var laborHours = diagnosis.FirstOrDefault()?.EstimatedLaborHours ?? 0;

        var queueDelayHours = 0m;
        var assignment = await GetActiveAssignmentAsync(workOrder.Id, cancellationToken);
        if (assignment is not null)
        {
            var otherAssignments = await _unitOfWork.Repository<WorkOrderAssignment>().FindAsync(
                a => a.TechnicianId == assignment.TechnicianId && a.EndedAt == null && a.WorkOrderId != workOrder.Id,
                cancellationToken);
            foreach (var other in otherAssignments)
            {
                var otherWorkOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(other.WorkOrderId, cancellationToken);
                if (otherWorkOrder is null || otherWorkOrder.Status is not (WorkOrderStatus.InRepair or WorkOrderStatus.WaitingParts))
                {
                    continue;
                }
                var otherDiagnosis = (await _unitOfWork.Repository<DiagnosisRecord>().FindAsync(d => d.WorkOrderId == other.WorkOrderId, cancellationToken))
                    .FirstOrDefault();
                queueDelayHours += otherDiagnosis?.EstimatedLaborHours ?? 0;
            }
        }

        var partsWaitTimeHours = 0m;

        var hasFreeBay = (await _unitOfWork.Repository<Bay>().FindAsync(
            b => b.Type == DiagnosisBayType && b.Status == BayStatus.Free, cancellationToken)).Count > 0;
        var bayWaitTimeHours = hasFreeBay ? 0m : 2m;

        var totalHoursBeforeBuffer = laborHours + queueDelayHours + partsWaitTimeHours + bayWaitTimeHours;
        var afterQcAndWash = TimeSpan.FromHours((double)totalHoursBeforeBuffer) + QcAndWashBuffer;
        var withServiceBuffer = afterQcAndWash + TimeSpan.FromTicks((long)(afterQcAndWash.Ticks * ServiceBufferPercent));

        return now + withServiceBuffer;
    }

    private async Task<WorkOrderAssignment?> GetActiveAssignmentAsync(int workOrderId, CancellationToken cancellationToken)
    {
        var assignments = await _unitOfWork.Repository<WorkOrderAssignment>().FindAsync(
            a => a.WorkOrderId == workOrderId && a.EndedAt == null, cancellationToken);
        return assignments.OrderByDescending(a => a.StartedAt).FirstOrDefault();
    }

    public async Task<WorkOrderResponse> ResendQuoteAsync(int workOrderId, int actorUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.QuotePending)
        {
            throw new InvalidTransitionException(
                $"Chỉ có thể gửi lại báo giá khi WorkOrder đang ở QuotePending (hiện tại: {workOrder.Status}).");
        }

        var now = _dateTimeProvider.UtcNow;
        workOrder.ApprovalToken = RandomNumberGenerator.GetHexString(32);
        workOrder.ApprovalTokenExpiresAt = now.AddHours(72);
        workOrder.ApprovalTokenUsedAt = null;
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Resend quote cho WorkOrder {WorkOrderId} bởi User {ActorUserId}", workOrder.Id, actorUserId);

        await NotifyQuoteReadyAsync(workOrder, cancellationToken);

        return ToResponse(workOrder);
    }

    private async Task NotifyQuoteReadyAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var vehicle = await _unitOfWork.Repository<Vehicle>().GetByIdAsync(workOrder.VehicleId, cancellationToken);
        var customer = vehicle is null
            ? null
            : await _unitOfWork.Repository<Customer>().GetByIdAsync(vehicle.CustomerId, cancellationToken);

        if (customer is null)
        {
            return;
        }

        var message = $"Báo giá cho work order #{workOrder.Id} đã sẵn sàng, vui lòng xem và duyệt.";
        var emailSent = false;
        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            try
            {
                await _emailService.SendAsync(
                    customer.Email,
                    "Báo giá sửa xe từ GaraCare",
                    $"<p>{message}</p><p>Mã duyệt: {workOrder.ApprovalToken}</p>",
                    cancellationToken);
                emailSent = true;
            }
            catch (Exception ex)
            {
                // Lỗi gửi email không được rollback transaction chính — chỉ log.
                // Xem docs/01-business-spec.md §9.
                _logger.LogWarning(ex, "Gửi email báo giá thất bại cho WorkOrder {WorkOrderId}", workOrder.Id);
            }
        }

        await _unitOfWork.Repository<Notification>().AddAsync(new Notification
        {
            CustomerId = customer.Id,
            WorkOrderId = workOrder.Id,
            Type = NotificationType.QuoteReady,
            Message = message,
            EmailSentSuccessfully = emailSent,
            IsRead = false,
            CreatedAt = _dateTimeProvider.UtcNow,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkOrderDetailResponse> GetByIdAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrderId, cancellationToken);
        var assignment = await GetActiveAssignmentAsync(workOrderId, cancellationToken);

        return new WorkOrderDetailResponse
        {
            Id = workOrder.Id,
            VehicleId = workOrder.VehicleId,
            Status = workOrder.Status.ToString(),
            ReceivedDate = workOrder.ReceivedDate,
            InitialDescription = workOrder.InitialDescription,
            DiagnosisNote = workOrder.DiagnosisNote,
            TotalAmount = workOrder.TotalAmount,
            DiscountPercent = workOrder.DiscountPercent,
            SystemSuggestedDate = workOrder.SystemSuggestedDate,
            FinalEstimatedDate = workOrder.FinalEstimatedDate,
            IsHeavyRepair = workOrder.IsHeavyRepair,
            IsDelayed = workOrder.IsDelayed,
            AssignedTechnicianId = assignment?.TechnicianId,
            QuotationItems = items.Select(ToQuotationItemResponse).ToList(),
        };
    }

    public async Task<IReadOnlyList<WorkOrderListItemResponse>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var workOrders = (await _unitOfWork.Repository<WorkOrder>().GetAllAsync(cancellationToken))
            .OrderByDescending(w => w.ReceivedDate)
            .ToList();

        return await ToListItemsAsync(workOrders, cancellationToken);
    }

    // docs/01-business-spec.md §15 — giao diện Technician: queue cá nhân, ưu tiên xe đã duyệt
    // giá/đã có phụ tùng (InRepair/WaitingParts) trước xe mới cần chẩn đoán (Received/Diagnosing).
    public async Task<IReadOnlyList<WorkOrderListItemResponse>> GetTechnicianQueueAsync(int technicianUserId, CancellationToken cancellationToken = default)
    {
        var assignments = await _unitOfWork.Repository<WorkOrderAssignment>().FindAsync(
            a => a.TechnicianId == technicianUserId && a.EndedAt == null, cancellationToken);
        var workOrderIds = assignments.Select(a => a.WorkOrderId).ToHashSet();

        var workOrders = (await _unitOfWork.Repository<WorkOrder>().FindAsync(
            w => workOrderIds.Contains(w.Id) && w.Status != WorkOrderStatus.Delivered && w.Status != WorkOrderStatus.Cancelled,
            cancellationToken))
            .OrderBy(w => QueuePriority(w.Status))
            .ThenBy(w => w.ReceivedDate)
            .ToList();

        return await ToListItemsAsync(workOrders, cancellationToken);
    }

    private static int QueuePriority(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.InRepair => 0,
        WorkOrderStatus.WaitingParts => 1,
        WorkOrderStatus.QuotePending => 2,
        WorkOrderStatus.DiagnosisConfirmed => 3,
        WorkOrderStatus.Diagnosing => 4,
        WorkOrderStatus.Received => 5,
        _ => 6,
    };

    private async Task<IReadOnlyList<WorkOrderListItemResponse>> ToListItemsAsync(IReadOnlyList<WorkOrder> workOrders, CancellationToken cancellationToken)
    {
        var vehicleIds = workOrders.Select(w => w.VehicleId).Distinct().ToList();
        var vehicles = (await _unitOfWork.Repository<Vehicle>().FindAsync(v => vehicleIds.Contains(v.Id), cancellationToken))
            .ToDictionary(v => v.Id);

        var customerIds = vehicles.Values.Select(v => v.CustomerId).Distinct().ToList();
        var customers = (await _unitOfWork.Repository<Customer>().FindAsync(c => customerIds.Contains(c.Id), cancellationToken))
            .ToDictionary(c => c.Id);

        return workOrders.Select(w =>
        {
            vehicles.TryGetValue(w.VehicleId, out var vehicle);
            Customer? customer = null;
            if (vehicle is not null)
            {
                customers.TryGetValue(vehicle.CustomerId, out customer);
            }

            return new WorkOrderListItemResponse
            {
                Id = w.Id,
                Status = w.Status.ToString(),
                ReceivedDate = w.ReceivedDate,
                TotalAmount = w.TotalAmount,
                NeedsFollowUpCall = w.NeedsFollowUpCall,
                LicensePlate = vehicle?.LicensePlate ?? string.Empty,
                VehicleLabel = vehicle is null ? string.Empty : $"{vehicle.Brand} {vehicle.Model}".Trim(),
                CustomerName = customer?.FullName ?? string.Empty,
                CustomerPhone = customer?.Phone,
            };
        }).ToList();
    }

    private static QuotationItemResponse ToQuotationItemResponse(QuotationItem item) => new()
    {
        Id = item.Id,
        WorkOrderId = item.WorkOrderId,
        PartId = item.PartId,
        Type = item.Type,
        Description = item.Description,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        LineTotal = item.Quantity * item.UnitPrice,
        IsApproved = item.IsApproved,
        IsUsed = item.IsUsed,
        LowStockWarning = false,
    };

    private static WorkOrderResponse ToResponse(WorkOrder workOrder, bool hasOpenWorkOrderWarning = false) => new()
    {
        Id = workOrder.Id,
        VehicleId = workOrder.VehicleId,
        Status = workOrder.Status.ToString(),
        ReceivedDate = workOrder.ReceivedDate,
        InitialDescription = workOrder.InitialDescription,
        DiagnosisNote = workOrder.DiagnosisNote,
        TotalAmount = workOrder.TotalAmount,
        DiscountPercent = workOrder.DiscountPercent,
        SystemSuggestedDate = workOrder.SystemSuggestedDate,
        FinalEstimatedDate = workOrder.FinalEstimatedDate,
        IsHeavyRepair = workOrder.IsHeavyRepair,
        IsDelayed = workOrder.IsDelayed,
        HasOpenWorkOrderWarning = hasOpenWorkOrderWarning,
    };
}
