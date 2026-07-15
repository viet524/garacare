using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    // Mã đặt lại mật khẩu gồm chữ và số, gửi qua email (giống cơ chế xác minh tài khoản).
    [Required(ErrorMessage = "Mã đặt lại mật khẩu là bắt buộc.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
