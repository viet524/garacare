using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class QuotationItem
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public int? PartId { get; set; }
    public Part? Part { get; set; }
    public QuotationItemType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsApproved { get; set; }
    public bool IsUsed { get; set; }
}
