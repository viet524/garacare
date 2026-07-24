namespace GaraCare.Application.DTOs.Users;

// Không bao gồm PasswordHash/mã xác minh — chỉ thông tin hiển thị cho trang quản lý nhân viên.
public class UserResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;

    // Chỉ có giá trị khi Role == Technician (docs/01-business-spec.md §10).
    public string? TechnicianStatus { get; set; }
}
