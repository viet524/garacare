namespace GaraCare.Application.DTOs.Vehicles;

// Tóm tắt cho màn hình lịch sử sửa chữa của 1 xe — không trả QuotationItem chi tiết (tránh over-fetch).
public class WorkOrderSummaryResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public decimal TotalAmount { get; set; }
}
