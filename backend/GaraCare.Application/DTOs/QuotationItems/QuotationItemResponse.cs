using System.Text.Json.Serialization;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.DTOs.QuotationItems;

public class QuotationItemResponse
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public int? PartId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuotationItemType Type { get; set; }

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsApproved { get; set; }
    public bool IsUsed { get; set; }
    public bool LowStockWarning { get; set; }
}
