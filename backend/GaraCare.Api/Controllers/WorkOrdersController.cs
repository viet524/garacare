using System.Security.Claims;
using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace GaraCare.Api.Controllers;

[ApiController]
[Route("api/workorders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderService _workOrderService;

    public WorkOrdersController(IWorkOrderService workOrderService)
    {
        _workOrderService = workOrderService;
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    public async Task<ActionResult<WorkOrderResponse>> CreateWalkIn(CreateWalkInWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _workOrderService.CreateWalkInAsync(request, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "Technician")]
    [HttpPost("{id:int}/start-diagnosis")]
    public async Task<ActionResult<WorkOrderResponse>> StartDiagnosis(int id, StartDiagnosisRequest request, CancellationToken cancellationToken)
    {
        var result = await _workOrderService.StartDiagnosisAsync(id, request, GetUserId(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Technician")]
    [HttpPost("{id:int}/confirm-diagnosis")]
    public async Task<ActionResult<WorkOrderResponse>> ConfirmDiagnosis(int id, ConfirmDiagnosisRequest request, CancellationToken cancellationToken)
    {
        var result = await _workOrderService.ConfirmDiagnosisAsync(id, request, GetUserId(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost("{id:int}/send-quote")]
    public async Task<ActionResult<WorkOrderResponse>> SendQuote(int id, SendQuoteRequest request, CancellationToken cancellationToken)
    {
        var result = await _workOrderService.SendQuoteAsync(id, request, GetUserId(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost("{id:int}/resend-quote")]
    public async Task<ActionResult<WorkOrderResponse>> ResendQuote(int id, CancellationToken cancellationToken)
    {
        var result = await _workOrderService.ResendQuoteAsync(id, GetUserId(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Staff,Technician,Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkOrderDetailResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _workOrderService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    // [EnableQuery]: OData $filter/$orderby/$top/$skip (docs/04-api-contract.md — GARA-49).
    // Áp dụng trên IQueryable đã có sẵn trong bộ nhớ (dataset hiện còn nhỏ) — không đổi sang
    // IQueryable dịch thẳng xuống SQL để không phá vỡ tầng Repository hiện có.
    [Authorize(Roles = "Staff,Technician,Admin")]
    [HttpGet]
    [EnableQuery(PageSize = 50)]
    public async Task<ActionResult<IQueryable<WorkOrderListItemResponse>>> GetList(CancellationToken cancellationToken)
    {
        var result = await _workOrderService.GetListAsync(cancellationToken);
        return Ok(result.AsQueryable());
    }

    [Authorize(Roles = "Technician")]
    [HttpGet("/api/technicians/me/queue")]
    public async Task<ActionResult<IReadOnlyList<WorkOrderListItemResponse>>> GetMyQueue(CancellationToken cancellationToken)
    {
        var result = await _workOrderService.GetTechnicianQueueAsync(GetUserId(), cancellationToken);
        return Ok(result);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
