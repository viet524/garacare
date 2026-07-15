using GaraCare.Application.DTOs.Auth;

namespace GaraCare.Application.Interfaces;

public interface IAuthService
{
    Task<MessageResponse> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task<MessageResponse> ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken = default);
    Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    // Access token hết hạn (rất nhanh) → FE gọi endpoint này bằng refresh token để lấy cặp
    // token mới, không bắt đăng nhập lại. Refresh token cũ bị thu hồi ngay (rotation).
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    // Thu hồi refresh token lúc đăng xuất — không cho dùng lại kể cả khi chưa hết hạn tự nhiên.
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
