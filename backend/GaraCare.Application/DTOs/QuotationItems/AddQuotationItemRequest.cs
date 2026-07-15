using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.DTOs.QuotationItems;

public class AddQuotationItemRequest
{
    [Required(ErrorMessage = "WorkOrder là bắt buộc.")]
    public int WorkOrderId { get; set; }

    public int? PartId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuotationItemType Type { get; set; }

    [Required(ErrorMessage = "Mô tả hạng mục là bắt buộc.")]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm.")]
    public decimal UnitPrice { get; set; }
}
