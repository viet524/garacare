using GaraCare.Domain.Entities;

namespace GaraCare.Application.Interfaces;

public interface ITokenService
{
    // customerId chỉ có giá trị khi user.Role == Customer và đã có Customer liên kết —
    // gắn vào claim "CustomerId" để Customer tự truy cập dữ liệu của chính mình (vd
    // GET /vehicles/mine) mà không cần lộ endpoint tra cứu theo CustomerId tuỳ ý.
    string GenerateToken(User user, int? customerId = null);

    // Chuỗi ngẫu nhiên đối lập với access token — AuthService tự hash (RefreshTokenGenerator.Hash)
    // trước khi lưu DB, không lưu giá trị thô trả về đây.
    string GenerateRefreshToken();

    // Thời hạn sống của refresh token — AuthService dùng để tính ExpiresAt khi lưu DB.
    TimeSpan RefreshTokenLifetime { get; }
}
