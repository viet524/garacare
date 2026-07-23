namespace GaraCare.Application.DTOs.WorkOrders;

// Tóm tắt cho màn hình danh sách work order của Staff/Technician — không trả QuotationItem
// chi tiết (tránh over-fetch, giống WorkOrderSummaryResponse).
public class WorkOrderListItemResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public bool NeedsFollowUpCall { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleLabel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
}
