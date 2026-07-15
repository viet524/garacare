using System.Security.Cryptography;

namespace GaraCare.Application.Services;

// Refresh token là chuỗi ngẫu nhiên đối lập với access token (JWT có chữ ký) — xác thực bằng
// cách tra DB theo hash, không giải mã. Chỉ lưu Hash() trong DB, không lưu token thật (rò rỉ DB
// không lộ được token dùng được).
public static class RefreshTokenGenerator
{
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
