using GaraCare.Application.DTOs.Customers;
using GaraCare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GaraCare.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Staff,Admin")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _customerService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-phone")]
    public async Task<ActionResult<CustomerResponse>> FindByPhone([FromQuery] string phone, CancellationToken cancellationToken)
    {
        var result = await _customerService.FindByPhoneAsync(phone, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(FindByPhone), new { phone = result.Phone }, result);
    }
}
