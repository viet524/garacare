using System.Security.Cryptography;
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

        return ToResponse(workOrder, hasOpenWorkOrderWarning: openWorkOrders.Count > 0);
    }

    public async Task<WorkOrderResponse> StartDiagnosisAsync(int workOrderId, StartDiagnosisRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.Received)
        {
            throw new InvalidTransitionException($"Không thể bắt đầu chẩn đoán từ trạng thái {workOrder.Status}.");
        }

        var now = _dateTimeProvider.UtcNow;
        workOrder.Status = WorkOrderStatus.Diagnosing;
        if (!string.IsNullOrWhiteSpace(request.DiagnosisNote))
        {
            workOrder.DiagnosisNote = request.DiagnosisNote;
        }
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);
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

    public async Task<WorkOrderResponse> SendQuoteAsync(int workOrderId, SendQuoteRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.Diagnosing)
        {
            var hint = workOrder.Status == WorkOrderStatus.QuotePending
                ? " Báo giá đã được gửi trước đó — dùng resend-quote để gửi lại link mới."
                : string.Empty;
            throw new InvalidTransitionException($"Không thể gửi báo giá từ trạng thái {workOrder.Status}.{hint}");
        }

        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrderId, cancellationToken);
        if (items.Count == 0)
        {
            throw new EmptyQuotationException("Chưa có hạng mục báo giá nào.");
        }

        var now = _dateTimeProvider.UtcNow;
        workOrder.ApprovalToken = RandomNumberGenerator.GetHexString(32);
        workOrder.ApprovalTokenExpiresAt = now.AddHours(72);
        workOrder.ApprovalTokenUsedAt = null;
        workOrder.EstimatedCompletionDate = request.EstimatedCompletionDate;
        workOrder.QuoteSentAt = now;
        workOrder.Status = WorkOrderStatus.QuotePending;
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.Repository<WorkOrderStatusHistory>().AddAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Diagnosing,
            ToStatus = WorkOrderStatus.QuotePending,
            ChangedByUserId = actorUserId,
            ApprovedViaToken = false,
            ChangedAt = now,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyQuoteReadyAsync(workOrder, cancellationToken);

        return ToResponse(workOrder);
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
        EstimatedCompletionDate = workOrder.EstimatedCompletionDate,
        IsDelayed = workOrder.IsDelayed,
        HasOpenWorkOrderWarning = hasOpenWorkOrderWarning,
    };
}
