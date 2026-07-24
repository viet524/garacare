namespace GaraCare.Application.DTOs.WorkOrders;

public class WorkOrderResponse
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string? InitialDescription { get; set; }
    public string? DiagnosisNote { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? SystemSuggestedDate { get; set; }
    public DateTime? FinalEstimatedDate { get; set; }
    public bool IsHeavyRepair { get; set; }
    public bool IsDelayed { get; set; }
    public bool HasOpenWorkOrderWarning { get; set; }
}
