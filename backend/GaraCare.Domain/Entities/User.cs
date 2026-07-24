using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public UserRole Role { get; set; }

    // Chỉ có giá trị khi Role == Technician — điều khiển auto-assign (docs/01-business-spec.md §11).
    public TechnicianStatus? TechnicianStatus { get; set; }

    // Đăng nhập bằng Email — tài khoản chỉ dùng được sau khi xác minh (trừ tài khoản
    // nội bộ do Admin tạo, tự động IsEmailVerified=true vì không tự đăng ký).
    public bool IsEmailVerified { get; set; }

    // Mã xác minh tài khoản (chữ + số) gửi qua email lúc đăng ký, dùng lại cho "gửi lại mã".
    public string? EmailVerificationCode { get; set; }
    public DateTime? EmailVerificationCodeExpiresAt { get; set; }

    // Mã đặt lại mật khẩu (chữ + số) gửi qua email khi quên mật khẩu.
    public string? PasswordResetCode { get; set; }
    public DateTime? PasswordResetCodeExpiresAt { get; set; }
}
