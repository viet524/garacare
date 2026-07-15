namespace GaraCare.Infrastructure.Auth;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    // Access token sống rất ngắn — hết hạn thì FE âm thầm gọi /auth/refresh-token bằng
    // refresh token, không bắt đăng nhập lại ngay.
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    // Refresh token sống lâu hơn nhiều — chỉ khi cái này cũng hết hạn (hoặc bị thu hồi lúc
    // đăng xuất) thì mới bắt đăng nhập lại.
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
