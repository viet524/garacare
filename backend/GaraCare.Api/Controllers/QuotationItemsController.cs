using GaraCare.Application.DTOs.QuotationItems;
using GaraCare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GaraCare.Api.Controllers;

[ApiController]
[Route("api/quotation-items")]
[Authorize(Roles = "Staff,Admin")]
public class QuotationItemsController : ControllerBase
{
    private readonly IQuotationItemService _quotationItemService;

    public QuotationItemsController(IQuotationItemService quotationItemService)
    {
        _quotationItemService = quotationItemService;
    }

    [HttpPost]
    public async Task<ActionResult<QuotationItemResponse>> Add(AddQuotationItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _quotationItemService.AddAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        await _quotationItemService.RemoveAsync(id, cancellationToken);
        return NoContent();
    }
}
