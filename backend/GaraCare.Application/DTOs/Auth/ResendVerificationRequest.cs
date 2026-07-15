using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.Auth;

public class ResendVerificationRequest
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;
}
