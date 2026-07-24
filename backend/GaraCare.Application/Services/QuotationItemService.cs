using GaraCare.Application.DTOs.QuotationItems;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.Services;

public class QuotationItemService : IQuotationItemService
{
    // DiagnosisConfirmed: Staff lập hạng mục báo giá sau khi Technician đã ký xác nhận chẩn đoán
    // (docs/02-use-cases.md UC-03) — Diagnosing tự nó chỉ dành cho Technician ghi chú/chẩn đoán.
    private static readonly WorkOrderStatus[] EditableStatuses = { WorkOrderStatus.DiagnosisConfirmed, WorkOrderStatus.QuotePending };

    private readonly IUnitOfWork _unitOfWork;

    public QuotationItemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<QuotationItemResponse> AddAsync(AddQuotationItemRequest request, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        await EnsureEditableAsync(workOrder, cancellationToken);

        var lowStockWarning = false;
        if (request.Type == QuotationItemType.Part && request.PartId.HasValue)
        {
            var part = await _unitOfWork.Repository<Part>().GetByIdAsync(request.PartId.Value, cancellationToken)
                ?? throw new EntityNotFoundException("Không tìm thấy phụ tùng.");
            lowStockWarning = part.StockQuantity < request.Quantity;
        }

        var item = new QuotationItem
        {
            WorkOrderId = workOrder.Id,
            PartId = request.PartId,
            Type = request.Type,
            Description = request.Description,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
        };
        await _unitOfWork.Repository<QuotationItem>().AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecalculateWorkOrderTotalAsync(workOrder.Id, cancellationToken);

        return ToResponse(item, lowStockWarning);
    }

    public async Task RemoveAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Repository<QuotationItem>().GetByIdAsync(itemId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy hạng mục báo giá.");

        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(item.WorkOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        await EnsureEditableAsync(workOrder, cancellationToken);

        _unitOfWork.Repository<QuotationItem>().Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecalculateWorkOrderTotalAsync(workOrder.Id, cancellationToken);
    }

    private async Task EnsureEditableAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        if (!EditableStatuses.Contains(workOrder.Status))
        {
            throw new InvalidTransitionException(
                $"Không thể thêm/sửa hạng mục báo giá khi WorkOrder đang ở trạng thái {workOrder.Status}.");
        }

        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrder.Id, cancellationToken);
        if (items.Any(q => q.IsApproved))
        {
            throw new QuotationLockedException("Báo giá đã được khách duyệt, không thể chỉnh sửa.");
        }
    }

    private async Task RecalculateWorkOrderTotalAsync(int workOrderId, CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrderId, cancellationToken);
        var total = items.Sum(q => q.Quantity * q.UnitPrice);

        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");
        workOrder.TotalAmount = total;
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static QuotationItemResponse ToResponse(QuotationItem item, bool lowStockWarning = false) => new()
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
        LowStockWarning = lowStockWarning,
    };
}
