using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.Vehicles;

// Dùng cho Customer tự đăng ký xe của chính mình (POST /vehicles/mine) — không có CustomerId
// vì lấy từ claim JWT, tránh khách A tự khai báo xe gắn cho khách B.
public class CreateOwnVehicleRequest
{
    [Required(ErrorMessage = "Biển số là bắt buộc.")]
    public string LicensePlate { get; set; } = string.Empty;

    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
}
