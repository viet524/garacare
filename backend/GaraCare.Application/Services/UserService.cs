using GaraCare.Application.DTOs.Users;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UserResponse>> GetInternalUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Repository<User>().FindAsync(u => u.Role != UserRole.Customer, cancellationToken);

        return users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Phone = u.Phone,
                Email = u.Email,
                Role = u.Role.ToString(),
                TechnicianStatus = u.TechnicianStatus?.ToString(),
            })
            .ToList();
    }
}
