namespace GaraCare.Application.DTOs.Vehicles;

public class VehicleResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
}
