using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.Auth;

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã xác minh là bắt buộc.")]
    public string Code { get; set; } = string.Empty;
}
