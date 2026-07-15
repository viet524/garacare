using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.Vehicles;

public class CreateVehicleRequest
{
    [Required(ErrorMessage = "Khách hàng là bắt buộc.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Biển số là bắt buộc.")]
    public string LicensePlate { get; set; } = string.Empty;

    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
}
