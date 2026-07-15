using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WorkOrderService(IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
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
