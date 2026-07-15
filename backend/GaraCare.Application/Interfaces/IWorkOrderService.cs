using GaraCare.Application.DTOs.Vehicles;

namespace GaraCare.Application.Interfaces;

// Chỉ chứa phần đọc lịch sử theo xe (GARA-21) — các method chuyển trạng thái WorkOrder
// (start-diagnosis, send-quote, ...) sẽ được bổ sung ở epic riêng ([BE] Tiếp nhận & Chẩn đoán).
public interface IWorkOrderService
{
    // requestingCustomerId: khi Customer tự gọi (xem lịch sử xe của chính mình), truyền vào để
    // Service kiểm tra sở hữu (throw ForbiddenActionException nếu xe không thuộc khách này).
    // Staff/Admin/Technician gọi thì truyền null — không giới hạn theo chủ sở hữu.
    Task<IReadOnlyList<WorkOrderSummaryResponse>> GetHistoryByVehicleAsync(int vehicleId, int? requestingCustomerId = null, CancellationToken cancellationToken = default);
}
