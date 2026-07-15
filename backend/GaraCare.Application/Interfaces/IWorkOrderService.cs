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
}
