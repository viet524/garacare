using GaraCare.Application.DTOs.Users;
using GaraCare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GaraCare.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // Staff cần xem trạng thái Technician (rảnh/bận) để biết auto-assign hoạt động thế nào —
    // không giới hạn riêng Admin như CRUD (chưa làm ở đây, xem docs/05 — FR-04/GARA-66).
    [Authorize(Roles = "Staff,Technician,Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetInternalUsers(CancellationToken cancellationToken)
    {
        var result = await _userService.GetInternalUsersAsync(cancellationToken);
        return Ok(result);
    }
}
