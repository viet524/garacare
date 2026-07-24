using GaraCare.Application.DTOs.Users;

namespace GaraCare.Application.Interfaces;

public interface IUserService
{
    // Danh sách nhân viên nội bộ (Staff/Technician/Admin) cho trang quản lý — không trả Customer.
    Task<IReadOnlyList<UserResponse>> GetInternalUsersAsync(CancellationToken cancellationToken = default);
}
