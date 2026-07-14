using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Mã đặt lại mật khẩu gồm chữ và số, gửi qua email (giống cơ chế xác minh tài khoản).
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
