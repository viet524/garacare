using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GaraCare.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly IWorkOrderService _workOrderService;

    public VehiclesController(IVehicleService vehicleService, IWorkOrderService workOrderService)
    {
        _vehicleService = vehicleService;
        _workOrderService = workOrderService;
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByCustomer), new { customerId = result.CustomerId }, result);
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpGet("by-customer/{customerId:int}")]
    public async Task<ActionResult<IReadOnlyList<VehicleResponse>>> GetByCustomer(int customerId, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetByCustomerAsync(customerId, cancellationToken);
        return Ok(result);
    }

    // Customer tự xem xe của chính mình — CustomerId lấy từ claim JWT (gắn lúc login), không
    // nhận customerId từ client để tránh khách A xem được xe khách B.
    [Authorize(Roles = "Customer")]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<VehicleResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdClaim()
            ?? throw new ForbiddenActionException("Tài khoản chưa liên kết hồ sơ khách hàng.");
        var result = await _vehicleService.GetByCustomerAsync(customerId, cancellationToken);
        return Ok(result);
    }

    // Customer tự đăng ký xe của chính mình — cùng lý do lấy CustomerId từ claim thay vì body.
    [Authorize(Roles = "Customer")]
    [HttpPost("mine")]
    public async Task<ActionResult<VehicleResponse>> CreateMine(CreateOwnVehicleRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdClaim()
            ?? throw new ForbiddenActionException("Tài khoản chưa liên kết hồ sơ khách hàng.");

        var result = await _vehicleService.CreateAsync(
            new CreateVehicleRequest
            {
                CustomerId = customerId,
                LicensePlate = request.LicensePlate,
                Brand = request.Brand,
                Model = request.Model,
                Year = request.Year,
            },
            cancellationToken);

        return CreatedAtAction(nameof(GetMine), result);
    }

    [Authorize(Roles = "Staff,Admin,Technician,Customer")]
    [HttpGet("{id:int}/workorders")]
    public async Task<ActionResult<IReadOnlyList<WorkOrderSummaryResponse>>> GetWorkOrderHistory(int id, CancellationToken cancellationToken)
    {
        // Customer chỉ xem được lịch sử xe của chính mình — Service kiểm tra sở hữu qua
        // requestingCustomerId; Staff/Admin/Technician truyền null (không giới hạn).
        var requestingCustomerId = User.IsInRole("Customer") ? GetCustomerIdClaim() : null;
        var result = await _workOrderService.GetHistoryByVehicleAsync(id, requestingCustomerId, cancellationToken);
        return Ok(result);
    }

    private int? GetCustomerIdClaim()
    {
        var claim = User.FindFirst("CustomerId")?.Value;
        return int.TryParse(claim, out var customerId) ? customerId : null;
    }
}
