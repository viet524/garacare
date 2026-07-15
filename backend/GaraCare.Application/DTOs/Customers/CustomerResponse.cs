namespace GaraCare.Application.DTOs.Customers;

public class CustomerResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? UserId { get; set; }
}
