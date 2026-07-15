namespace GaraCare.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    // Lưu SHA-256 hash của token, không lưu token thật — rò rỉ DB không lộ được token dùng được.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Set khi token đã bị thay thế (rotate lúc refresh) hoặc bị thu hồi (logout).
    public DateTime? RevokedAt { get; set; }
}
