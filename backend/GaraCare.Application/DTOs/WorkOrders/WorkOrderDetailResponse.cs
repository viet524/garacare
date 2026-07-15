using GaraCare.Application.DTOs.QuotationItems;

namespace GaraCare.Application.DTOs.WorkOrders;

public class WorkOrderDetailResponse
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string? InitialDescription { get; set; }
    public string? DiagnosisNote { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public bool IsDelayed { get; set; }
    public IReadOnlyList<QuotationItemResponse> QuotationItems { get; set; } = new List<QuotationItemResponse>();
}
