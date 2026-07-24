using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.DTOs.WorkOrders;

namespace GaraCare.Application.Interfaces;

public interface IWorkOrderService
{
    // requestingCustomerId: khi Customer tự gọi (xem lịch sử xe của chính mình), truyền vào để
    // Service kiểm tra sở hữu (throw ForbiddenActionException nếu xe không thuộc khách này).
    // Staff/Admin/Technician gọi thì truyền null — không giới hạn theo chủ sở hữu.
    Task<IReadOnlyList<WorkOrderSummaryResponse>> GetHistoryByVehicleAsync(int vehicleId, int? requestingCustomerId = null, CancellationToken cancellationToken = default);

    Task<WorkOrderResponse> CreateWalkInAsync(CreateWalkInWorkOrderRequest request, int actorUserId, CancellationToken cancellationToken = default);

    Task<WorkOrderResponse> StartDiagnosisAsync(int workOrderId, StartDiagnosisRequest request, int actorUserId, CancellationToken cancellationToken = default);

    // Diagnosing -> DiagnosisConfirmed: Technician ký xác nhận, nhập estimatedLaborHours, tạo
    // DiagnosisRecord bất biến (docs/02-use-cases.md UC-03 bước 3).
    Task<WorkOrderResponse> ConfirmDiagnosisAsync(int workOrderId, ConfirmDiagnosisRequest request, int actorTechnicianId, CancellationToken cancellationToken = default);

    Task<WorkOrderResponse> SendQuoteAsync(int workOrderId, SendQuoteRequest request, int actorUserId, CancellationToken cancellationToken = default);

    Task<WorkOrderResponse> ResendQuoteAsync(int workOrderId, int actorUserId, CancellationToken cancellationToken = default);

    Task<WorkOrderDetailResponse> GetByIdAsync(int workOrderId, CancellationToken cancellationToken = default);

    // Danh sách cho Staff/Technician/Admin — sắp theo ReceivedDate giảm dần (mới nhất trước).
    Task<IReadOnlyList<WorkOrderListItemResponse>> GetListAsync(CancellationToken cancellationToken = default);

    // Queue cá nhân của 1 Technician — chỉ WorkOrder đang được gán (WorkOrderAssignment chưa
    // EndedAt), sắp theo priority (docs/01-business-spec.md §15).
    Task<IReadOnlyList<WorkOrderListItemResponse>> GetTechnicianQueueAsync(int technicianUserId, CancellationToken cancellationToken = default);
}
