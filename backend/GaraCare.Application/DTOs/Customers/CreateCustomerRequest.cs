using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.Customers;

public class CreateCustomerRequest
{
    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    public string? Address { get; set; }
}
