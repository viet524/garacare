using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.WorkOrders;

public class CreateWalkInWorkOrderRequest
{
    [Required(ErrorMessage = "Xe là bắt buộc.")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Mô tả sự cố ban đầu là bắt buộc.")]
    public string InitialDescription { get; set; } = string.Empty;
}
