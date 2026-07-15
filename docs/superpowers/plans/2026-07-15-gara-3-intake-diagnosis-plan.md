# GARA-3 — Intake & Diagnosis (Backend) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the backend for UC-02 (walk-in intake) and UC-03 (diagnosis → quote → send/resend quote with `ApprovalToken`), matching Jira epic GARA-3 (tasks GARA-22..26) and `docs/superpowers/specs/2026-07-15-gara-3-intake-diagnosis-design.md`.

**Architecture:** Extend the existing `IWorkOrderService`/`WorkOrderService` (currently only `GetHistoryByVehicleAsync`) with 5 new methods, add a new `IQuotationItemService`/`QuotationItemService`, and wire two controllers (`WorkOrdersController` new, `QuotationItemsController` new). No EF Core migration — `WorkOrder`/`QuotationItem`/`WorkOrderStatusHistory` already have every field needed.

**Tech Stack:** ASP.NET Core 8 Web API, EF Core (SQL Server provider, InMemory provider for tests), xUnit.

## Global Constraints

- Every state transition validated in the Service layer, never the Controller (`docs/07-backend-conventions.md`).
- Every real `Status` change writes exactly one `WorkOrderStatusHistory` row; `resend-quote` does **not** change `Status`, so it writes none.
- No `QuotationItem` add/remove allowed once any item on that `WorkOrder` has `IsApproved = true` (`docs/01-business-spec.md` §5).
- `ApprovalToken` generated with `System.Security.Cryptography.RandomNumberGenerator.GetHexString(32)`, never `Guid.NewGuid()`.
- `actorUserId` is always read from the JWT `ClaimTypes.NameIdentifier` claim in the Controller, never from the request body.
- Business-rule exceptions live in `GaraCare.Application.Exceptions`, derive from `BusinessException` (or a more specific existing type), and are mapped to status codes automatically by `backend/GaraCare.Api/Middleware/ExceptionHandlingMiddleware.cs` — no per-controller try/catch needed.
- Email failures must not roll back the main transaction — catch and log, set `Notification.EmailSentSuccessfully = false` (`docs/01-business-spec.md` §9).
- Times come from `IDateTimeProvider.UtcNow`, never `DateTime.UtcNow` directly, so tests can control "now" via the existing `FakeDateTimeProvider` (`backend/GaraCare.Tests/AuthServiceTests.cs`).
- Enum request/response fields (`QuotationItemType`) use `[JsonConverter(typeof(JsonStringEnumConverter))]` so the wire format is the string name (`"Part"`/`"Labor"`), consistent with `docs/03-data-model.md`'s "enum lưu dạng string" convention; invalid values are rejected by JSON model binding with an automatic 400.

---

### Task 1: `CreateWalkInAsync` (GARA-22, UC-02)

**Files:**
- Create: `backend/GaraCare.Application/DTOs/WorkOrders/CreateWalkInWorkOrderRequest.cs`
- Create: `backend/GaraCare.Application/DTOs/WorkOrders/WorkOrderResponse.cs`
- Modify: `backend/GaraCare.Application/Interfaces/IWorkOrderService.cs`
- Modify: `backend/GaraCare.Application/Services/WorkOrderService.cs`
- Test: `backend/GaraCare.Tests/WorkOrderServiceTests.cs`

**Interfaces:**
- Produces: `IWorkOrderService.CreateWalkInAsync(CreateWalkInWorkOrderRequest request, int actorUserId, CancellationToken cancellationToken = default) : Task<WorkOrderResponse>`
- Produces: `WorkOrderResponse { Id, VehicleId, Status (string), ReceivedDate, InitialDescription, DiagnosisNote, TotalAmount, DiscountPercent, EstimatedCompletionDate, IsDelayed, HasOpenWorkOrderWarning }` — reused by every later WorkOrder task.
- Consumes: `IUnitOfWork`, `IDateTimeProvider` (existing), `EntityNotFoundException` (existing).

- [ ] **Step 1: Write the failing tests**

Create `backend/GaraCare.Application/DTOs/WorkOrders/CreateWalkInWorkOrderRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.WorkOrders;

public class CreateWalkInWorkOrderRequest
{
    [Required(ErrorMessage = "Xe là bắt buộc.")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Mô tả sự cố ban đầu là bắt buộc.")]
    public string InitialDescription { get; set; } = string.Empty;
}
```

Create `backend/GaraCare.Application/DTOs/WorkOrders/WorkOrderResponse.cs`:

```csharp
namespace GaraCare.Application.DTOs.WorkOrders;

public class WorkOrderResponse
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string? InitialDescription { get; set; }
    public string? DiagnosisNote { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public bool IsDelayed { get; set; }
    public bool HasOpenWorkOrderWarning { get; set; }
}
```

Add the method signature to `backend/GaraCare.Application/Interfaces/IWorkOrderService.cs` (keep the existing `GetHistoryByVehicleAsync`, update the header comment since transitions now live here):

```csharp
using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.DTOs.WorkOrders;

namespace GaraCare.Application.Interfaces;

public interface IWorkOrderService
{
    // requestingCustomerId: khi Customer tự gọi (xem lịch sử xe của chính mình), truyền vào để
    // Service kiểm tra sở hữu (throw ForbiddenActionException nếu xe không thuộc khách này).
    // Staff/Admin/Technician gọi thì truyền null — không giới hạn theo chủ sở hữu.
    Task<IReadOnlyList<WorkOrderSummaryResponse>> GetHistoryByVehicleAsync(int vehicleId, int? requestingCustomerId = null, CancellationToken cancellationToken = default);

    Task<WorkOrderResponse> CreateWalkInAsync(CreateWalkInWorkOrderRequest request, int actorUserId, CancellationToken cancellationToken = default);
}
```

In `backend/GaraCare.Tests/WorkOrderServiceTests.cs`, **replace the existing `CreateService` helper in place** (same method name, new signature) — this changes the tuple shape, so every existing call site in this file must be updated too:

```csharp
    private static (WorkOrderService Service, GaraCareDbContext Db, FakeDateTimeProvider Clock) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        var clock = new FakeDateTimeProvider();
        return (new WorkOrderService(unitOfWork, clock), db, clock);
    }
```

Update the 4 pre-existing tests (`GetHistoryByVehicleAsync_VehicleNotFound_ThrowsEntityNotFoundException`, `GetHistoryByVehicleAsync_NoWorkOrders_ReturnsEmptyList`, `GetHistoryByVehicleAsync_OwnerCustomer_ReturnsHistory`, `GetHistoryByVehicleAsync_NonOwnerCustomer_ThrowsForbiddenActionException`, `GetHistoryByVehicleAsync_OrdersByReceivedDateDescending`) — each currently starts with `var (service, _) = CreateService();` or `var (service, db) = CreateService();`. Change every one of these destructuring lines to add the third element, e.g. `var (service, _, _) = CreateService();` and `var (service, db, _) = CreateService();`, matching whichever elements each test actually uses.

Add the required `using` directive at the top of the file if not already present: `using GaraCare.Domain.Enums;` (needed for `WorkOrderStatus` values used below — the file already has `using GaraCare.Domain.Enums;` since the existing tests reference `WorkOrderStatus`, so no change is likely needed; only add it if the compiler complains).

Now add these new `[Fact]` methods at the end of the class, before the final closing `}`:

```csharp
    [Fact]
    public async Task CreateWalkInAsync_VehicleNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.CreateWalkInAsync(new CreateWalkInWorkOrderRequest { VehicleId = 999, InitialDescription = "Kêu lạ" }, actorUserId: 1));
    }

    [Fact]
    public async Task CreateWalkInAsync_NoOpenWorkOrder_CreatesWithoutWarning()
    {
        var (service, db, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" },
            actorUserId: userId);

        Assert.Equal("Received", result.Status);
        Assert.False(result.HasOpenWorkOrderWarning);
    }

    [Fact]
    public async Task CreateWalkInAsync_HasOpenWorkOrder_CreatesWithWarning()
    {
        var (service, db, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        db.WorkOrders.Add(new WorkOrder
        {
            VehicleId = vehicleId,
            CreatedByUserId = userId,
            Status = WorkOrderStatus.Diagnosing,
            ReceivedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" },
            actorUserId: userId);

        Assert.True(result.HasOpenWorkOrderWarning);
    }

    [Fact]
    public async Task CreateWalkInAsync_DeliveredOrCancelledWorkOrder_DoesNotCountAsOpen()
    {
        var (service, db, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        db.WorkOrders.Add(new WorkOrder
        {
            VehicleId = vehicleId,
            CreatedByUserId = userId,
            Status = WorkOrderStatus.Delivered,
            ReceivedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync();

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" },
            actorUserId: userId);

        Assert.False(result.HasOpenWorkOrderWarning);
    }

    [Fact]
    public async Task CreateWalkInAsync_WritesExactlyOneStatusHistoryRow()
    {
        var (service, db, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);

        var result = await service.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicleId, InitialDescription = "Kêu lạ" },
            actorUserId: userId);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == result.Id).ToList();
        Assert.Single(history);
        Assert.Equal(WorkOrderStatus.Received, history[0].FromStatus);
        Assert.Equal(WorkOrderStatus.Received, history[0].ToStatus);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests"`
Expected: compile error across the whole file (`CreateWalkInAsync` and the 2-arg `WorkOrderService` constructor don't exist yet, and the old 4 tests' destructuring no longer matches the helper's new return shape until you finish Step 3 below — this is expected, both problems resolve together once the service and helper are aligned). If you'd rather see a clean compile before implementing, do Step 3 first, then come back and confirm the new tests fail on assertions rather than compilation.

- [ ] **Step 3: Implement `CreateWalkInAsync`**

Replace the contents of `backend/GaraCare.Application/Services/WorkOrderService.cs` with:

```csharp
using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WorkOrderService(IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<WorkOrderSummaryResponse>> GetHistoryByVehicleAsync(int vehicleId, int? requestingCustomerId = null, CancellationToken cancellationToken = default)
    {
        var vehicle = await _unitOfWork.Repository<Vehicle>().GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy xe.");

        if (requestingCustomerId.HasValue && vehicle.CustomerId != requestingCustomerId.Value)
        {
            throw new ForbiddenActionException("Bạn không có quyền xem lịch sử sửa chữa của xe này.");
        }

        var workOrders = await _unitOfWork.Repository<WorkOrder>().FindAsync(w => w.VehicleId == vehicleId, cancellationToken);

        return workOrders
            .OrderByDescending(w => w.ReceivedDate)
            .Select(w => new WorkOrderSummaryResponse
            {
                Id = w.Id,
                Status = w.Status.ToString(),
                ReceivedDate = w.ReceivedDate,
                CompletedDate = w.CompletedDate,
                TotalAmount = w.TotalAmount,
            })
            .ToList();
    }

    public async Task<WorkOrderResponse> CreateWalkInAsync(CreateWalkInWorkOrderRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _unitOfWork.Repository<Vehicle>().GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy xe.");

        var openWorkOrders = await _unitOfWork.Repository<WorkOrder>().FindAsync(
            w => w.VehicleId == vehicle.Id && w.Status != WorkOrderStatus.Delivered && w.Status != WorkOrderStatus.Cancelled,
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var workOrder = new WorkOrder
        {
            VehicleId = vehicle.Id,
            CreatedByUserId = actorUserId,
            Status = WorkOrderStatus.Received,
            ReceivedDate = now,
            InitialDescription = request.InitialDescription,
            AppointmentId = null,
        };
        await _unitOfWork.Repository<WorkOrder>().AddAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // "Tạo mới" không phải transition thật — ghi 1 dòng lịch sử tự tham chiếu (From=To=Received)
        // để đủ audit log mà không cần một trạng thái "trước khi tạo" không tồn tại.
        await _unitOfWork.Repository<WorkOrderStatusHistory>().AddAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Received,
            ToStatus = WorkOrderStatus.Received,
            ChangedByUserId = actorUserId,
            ApprovedViaToken = false,
            ChangedAt = now,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(workOrder, hasOpenWorkOrderWarning: openWorkOrders.Count > 0);
    }

    private static WorkOrderResponse ToResponse(WorkOrder workOrder, bool hasOpenWorkOrderWarning = false) => new()
    {
        Id = workOrder.Id,
        VehicleId = workOrder.VehicleId,
        Status = workOrder.Status.ToString(),
        ReceivedDate = workOrder.ReceivedDate,
        InitialDescription = workOrder.InitialDescription,
        DiagnosisNote = workOrder.DiagnosisNote,
        TotalAmount = workOrder.TotalAmount,
        DiscountPercent = workOrder.DiscountPercent,
        EstimatedCompletionDate = workOrder.EstimatedCompletionDate,
        IsDelayed = workOrder.IsDelayed,
        HasOpenWorkOrderWarning = hasOpenWorkOrderWarning,
    };
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests"`
Expected: PASS (all existing `GetHistoryByVehicleAsync` tests still pass plus the 5 new `CreateWalkInAsync` tests).

- [ ] **Step 5: Commit**

```bash
git add backend/GaraCare.Application/DTOs/WorkOrders/CreateWalkInWorkOrderRequest.cs backend/GaraCare.Application/DTOs/WorkOrders/WorkOrderResponse.cs backend/GaraCare.Application/Interfaces/IWorkOrderService.cs backend/GaraCare.Application/Services/WorkOrderService.cs backend/GaraCare.Tests/WorkOrderServiceTests.cs
git commit -m "feat(workorder): add CreateWalkInAsync (GARA-22, UC-02)"
```

---

### Task 2: `StartDiagnosisAsync` (GARA-23, `Received → Diagnosing`)

**Files:**
- Create: `backend/GaraCare.Application/DTOs/WorkOrders/StartDiagnosisRequest.cs`
- Modify: `backend/GaraCare.Application/Interfaces/IWorkOrderService.cs`
- Modify: `backend/GaraCare.Application/Services/WorkOrderService.cs`
- Test: `backend/GaraCare.Tests/WorkOrderServiceTests.cs`

**Interfaces:**
- Produces: `IWorkOrderService.StartDiagnosisAsync(int workOrderId, StartDiagnosisRequest request, int actorUserId, CancellationToken cancellationToken = default) : Task<WorkOrderResponse>`
- Consumes: `WorkOrderResponse`, `InvalidTransitionException` (existing), `WorkOrderService.CreateService()` test helper from Task 1.

- [ ] **Step 1: Write the failing tests**

Create `backend/GaraCare.Application/DTOs/WorkOrders/StartDiagnosisRequest.cs`:

```csharp
namespace GaraCare.Application.DTOs.WorkOrders;

public class StartDiagnosisRequest
{
    public string? DiagnosisNote { get; set; }
}
```

Add to `IWorkOrderService`:

```csharp
    Task<WorkOrderResponse> StartDiagnosisAsync(int workOrderId, StartDiagnosisRequest request, int actorUserId, CancellationToken cancellationToken = default);
```

Append to `backend/GaraCare.Tests/WorkOrderServiceTests.cs`:

```csharp
    [Fact]
    public async Task StartDiagnosisAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.StartDiagnosisAsync(999, new StartDiagnosisRequest(), actorUserId: 1));
    }

    [Fact]
    public async Task StartDiagnosisAsync_FromReceived_TransitionsToDiagnosing()
    {
        var (service, db, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Received, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var result = await service.StartDiagnosisAsync(workOrder.Id, new StartDiagnosisRequest { DiagnosisNote = "Hỏng bugi" }, actorUserId: userId);

        Assert.Equal("Diagnosing", result.Status);
        Assert.Equal("Hỏng bugi", result.DiagnosisNote);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Diagnosing)]
    [InlineData(WorkOrderStatus.QuotePending)]
    [InlineData(WorkOrderStatus.InRepair)]
    public async Task StartDiagnosisAsync_FromNonReceivedStatus_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.StartDiagnosisAsync(workOrder.Id, new StartDiagnosisRequest(), actorUserId: userId));
    }

    [Fact]
    public async Task StartDiagnosisAsync_WritesExactlyOneStatusHistoryRow()
    {
        var (service, db, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Received, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await service.StartDiagnosisAsync(workOrder.Id, new StartDiagnosisRequest(), actorUserId: userId);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == workOrder.Id).ToList();
        Assert.Single(history);
        Assert.Equal(WorkOrderStatus.Received, history[0].FromStatus);
        Assert.Equal(WorkOrderStatus.Diagnosing, history[0].ToStatus);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests.StartDiagnosisAsync"`
Expected: compile error (`StartDiagnosisAsync` doesn't exist yet).

- [ ] **Step 3: Implement `StartDiagnosisAsync`**

In `backend/GaraCare.Application/Services/WorkOrderService.cs`, add this method after `CreateWalkInAsync`:

```csharp
    public async Task<WorkOrderResponse> StartDiagnosisAsync(int workOrderId, StartDiagnosisRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.Received)
        {
            throw new InvalidTransitionException($"Không thể bắt đầu chẩn đoán từ trạng thái {workOrder.Status}.");
        }

        var now = _dateTimeProvider.UtcNow;
        workOrder.Status = WorkOrderStatus.Diagnosing;
        if (!string.IsNullOrWhiteSpace(request.DiagnosisNote))
        {
            workOrder.DiagnosisNote = request.DiagnosisNote;
        }
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.Repository<WorkOrderStatusHistory>().AddAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Received,
            ToStatus = WorkOrderStatus.Diagnosing,
            ChangedByUserId = actorUserId,
            ApprovedViaToken = false,
            ChangedAt = now,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(workOrder);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/GaraCare.Application/DTOs/WorkOrders/StartDiagnosisRequest.cs backend/GaraCare.Application/Interfaces/IWorkOrderService.cs backend/GaraCare.Application/Services/WorkOrderService.cs backend/GaraCare.Tests/WorkOrderServiceTests.cs
git commit -m "feat(workorder): add StartDiagnosisAsync (GARA-23)"
```

---

### Task 3: New business exceptions for quotation rules

**Files:**
- Create: `backend/GaraCare.Application/Exceptions/QuotationLockedException.cs`
- Create: `backend/GaraCare.Application/Exceptions/EmptyQuotationException.cs`
- Modify: `backend/GaraCare.Api/Middleware/ExceptionHandlingMiddleware.cs`

**Interfaces:**
- Produces: `QuotationLockedException(string message)`, `EmptyQuotationException(string message)` — both `: BusinessException`, consumed by Task 4 and Task 5.

- [ ] **Step 1: Write the failing test**

Create `backend/GaraCare.Tests/ExceptionHandlingMiddlewareTests.cs`:

```csharp
using System.Net;
using GaraCare.Api.Middleware;
using GaraCare.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GaraCare.Tests;

public class ExceptionHandlingMiddlewareTests
{
    [Theory]
    [InlineData(typeof(QuotationLockedException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(EmptyQuotationException), HttpStatusCode.BadRequest)]
    public async Task InvokeAsync_BusinessException_MapsToExpectedStatusCode(Type exceptionType, HttpStatusCode expected)
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw (Exception)Activator.CreateInstance(exceptionType, "lỗi")!,
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)expected, context.Response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~ExceptionHandlingMiddlewareTests"`
Expected: compile error (`QuotationLockedException`/`EmptyQuotationException` don't exist yet).

- [ ] **Step 3: Add the exception types**

Create `backend/GaraCare.Application/Exceptions/QuotationLockedException.cs`:

```csharp
namespace GaraCare.Application.Exceptions;

// Thrown when adding/editing/removing a QuotationItem on a WorkOrder that already has at
// least one approved item. Maps to 400 — see docs/01-business-spec.md §5.
public class QuotationLockedException : BusinessException
{
    public QuotationLockedException(string message) : base(message)
    {
    }
}
```

Create `backend/GaraCare.Application/Exceptions/EmptyQuotationException.cs`:

```csharp
namespace GaraCare.Application.Exceptions;

// Thrown when sending a quote for a WorkOrder that has no QuotationItem yet. Maps to 400.
public class EmptyQuotationException : BusinessException
{
    public EmptyQuotationException(string message) : base(message)
    {
    }
}
```

No change is needed in `ExceptionHandlingMiddleware.cs` — both new types fall through to the existing `BusinessException => HttpStatusCode.BadRequest` arm.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~ExceptionHandlingMiddlewareTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/GaraCare.Application/Exceptions/QuotationLockedException.cs backend/GaraCare.Application/Exceptions/EmptyQuotationException.cs backend/GaraCare.Tests/ExceptionHandlingMiddlewareTests.cs
git commit -m "feat(errors): add QuotationLockedException and EmptyQuotationException"
```

---

### Task 4: `QuotationItemService` — add/remove hạng mục báo giá (GARA-24)

**Files:**
- Create: `backend/GaraCare.Application/DTOs/QuotationItems/AddQuotationItemRequest.cs`
- Create: `backend/GaraCare.Application/DTOs/QuotationItems/QuotationItemResponse.cs`
- Create: `backend/GaraCare.Application/Interfaces/IQuotationItemService.cs`
- Create: `backend/GaraCare.Application/Services/QuotationItemService.cs`
- Modify: `backend/GaraCare.Infrastructure/DependencyInjection.cs`
- Test: `backend/GaraCare.Tests/QuotationItemServiceTests.cs`

**Interfaces:**
- Produces: `IQuotationItemService.AddAsync(AddQuotationItemRequest, CancellationToken) : Task<QuotationItemResponse>`
- Produces: `IQuotationItemService.RemoveAsync(int itemId, CancellationToken) : Task`
- Produces: `QuotationItemResponse { Id, WorkOrderId, PartId, Type (QuotationItemType), Description, Quantity, UnitPrice, LineTotal, IsApproved, IsUsed, LowStockWarning }` — consumed by Task 7 (`WorkOrderDetailResponse.QuotationItems`).
- Consumes: `QuotationLockedException`, `EntityNotFoundException`, `InvalidTransitionException` (existing/Task 3), `Part` entity (existing).
- **Design decision:** the Jira ticket text lists `RecalculateWorkOrderTotalAsync` as a public interface member; it is kept **private** here since nothing outside `QuotationItemService` calls it (YAGNI — no public consumer exists in this epic). If a later epic needs to trigger a recalculation externally, promote it to the interface then.

- [ ] **Step 1: Write the failing tests**

Create `backend/GaraCare.Application/DTOs/QuotationItems/AddQuotationItemRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.DTOs.QuotationItems;

public class AddQuotationItemRequest
{
    [Required(ErrorMessage = "WorkOrder là bắt buộc.")]
    public int WorkOrderId { get; set; }

    public int? PartId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuotationItemType Type { get; set; }

    [Required(ErrorMessage = "Mô tả hạng mục là bắt buộc.")]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm.")]
    public decimal UnitPrice { get; set; }
}
```

Create `backend/GaraCare.Application/DTOs/QuotationItems/QuotationItemResponse.cs`:

```csharp
using System.Text.Json.Serialization;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.DTOs.QuotationItems;

public class QuotationItemResponse
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public int? PartId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuotationItemType Type { get; set; }

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsApproved { get; set; }
    public bool IsUsed { get; set; }
    public bool LowStockWarning { get; set; }
}
```

Create `backend/GaraCare.Application/Interfaces/IQuotationItemService.cs`:

```csharp
using GaraCare.Application.DTOs.QuotationItems;

namespace GaraCare.Application.Interfaces;

public interface IQuotationItemService
{
    Task<QuotationItemResponse> AddAsync(AddQuotationItemRequest request, CancellationToken cancellationToken = default);
    Task RemoveAsync(int itemId, CancellationToken cancellationToken = default);
}
```

Create `backend/GaraCare.Tests/QuotationItemServiceTests.cs`:

```csharp
using GaraCare.Application.DTOs.QuotationItems;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GaraCare.Tests;

public class QuotationItemServiceTests
{
    private static (QuotationItemService Service, GaraCareDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        return (new QuotationItemService(unitOfWork), db);
    }

    private static async Task<int> SeedWorkOrderAsync(GaraCareDbContext db, WorkOrderStatus status)
    {
        var user = new User { Username = "staff1", PasswordHash = "hash", FullName = "Staff 1", Role = UserRole.Staff, IsEmailVerified = true };
        db.Users.Add(user);
        var customer = new Customer { FullName = "Khách A", Phone = "0900000000" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var vehicle = new Vehicle { CustomerId = customer.Id, LicensePlate = "51A-12345" };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        var workOrder = new WorkOrder { VehicleId = vehicle.Id, CreatedByUserId = user.Id, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();
        return workOrder.Id;
    }

    [Fact]
    public async Task AddAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.AddAsync(new AddQuotationItemRequest { WorkOrderId = 999, Type = QuotationItemType.Labor, Description = "Công thay dầu", Quantity = 1, UnitPrice = 50000 }));
    }

    [Theory]
    [InlineData(WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.InRepair)]
    [InlineData(WorkOrderStatus.Completed)]
    public async Task AddAsync_WorkOrderNotInEditableStatus_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, status);

        await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.AddAsync(new AddQuotationItemRequest { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Công thay dầu", Quantity = 1, UnitPrice = 50000 }));
    }

    [Fact]
    public async Task AddAsync_WorkOrderHasApprovedItem_ThrowsQuotationLockedException_EvenForUnrelatedNewItem()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.QuotePending);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Đã duyệt", Quantity = 1, UnitPrice = 10000, IsApproved = true });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<QuotationLockedException>(() =>
            service.AddAsync(new AddQuotationItemRequest { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Hạng mục mới", Quantity = 1, UnitPrice = 20000 }));
    }

    [Fact]
    public async Task AddAsync_PartWithInsufficientStock_DoesNotBlock_ReturnsLowStockWarning()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.Diagnosing);
        var part = new Part { Name = "Lọc dầu", UnitPrice = 100000, StockQuantity = 1 };
        db.Parts.Add(part);
        await db.SaveChangesAsync();

        var result = await service.AddAsync(new AddQuotationItemRequest
        {
            WorkOrderId = workOrderId,
            PartId = part.Id,
            Type = QuotationItemType.Part,
            Description = "Thay lọc dầu",
            Quantity = 5,
            UnitPrice = 100000,
        });

        Assert.True(result.LowStockWarning);
        Assert.Equal(500000, result.LineTotal);
    }

    [Fact]
    public async Task AddAsync_QuantityAndUnitPriceMultiply_RecalculatesWorkOrderTotalAmount()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.Diagnosing);

        await service.AddAsync(new AddQuotationItemRequest { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Công 1", Quantity = 1, UnitPrice = 100000 });
        await service.AddAsync(new AddQuotationItemRequest { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Công 2", Quantity = 2, UnitPrice = 100000 });

        var workOrder = await db.WorkOrders.FindAsync(workOrderId);
        Assert.Equal(300000, workOrder!.TotalAmount);
    }

    [Fact]
    public async Task RemoveAsync_ItemNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.RemoveAsync(999));
    }

    [Fact]
    public async Task RemoveAsync_ExistingItem_RecalculatesWorkOrderTotalAmount()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.Diagnosing);
        var itemToKeep = new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Giữ lại", Quantity = 1, UnitPrice = 100000 };
        var itemToRemove = new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Xoá đi", Quantity = 1, UnitPrice = 200000 };
        db.QuotationItems.AddRange(itemToKeep, itemToRemove);
        var workOrder = await db.WorkOrders.FindAsync(workOrderId);
        workOrder!.TotalAmount = 300000;
        await db.SaveChangesAsync();

        await service.RemoveAsync(itemToRemove.Id);

        var updated = await db.WorkOrders.FindAsync(workOrderId);
        Assert.Equal(100000, updated!.TotalAmount);
    }

    [Fact]
    public async Task RemoveAsync_WorkOrderHasApprovedItem_ThrowsQuotationLockedException()
    {
        var (service, db) = CreateService();
        var workOrderId = await SeedWorkOrderAsync(db, WorkOrderStatus.QuotePending);
        var approved = new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Đã duyệt", Quantity = 1, UnitPrice = 10000, IsApproved = true };
        var other = new QuotationItem { WorkOrderId = workOrderId, Type = QuotationItemType.Labor, Description = "Chưa duyệt", Quantity = 1, UnitPrice = 20000 };
        db.QuotationItems.AddRange(approved, other);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<QuotationLockedException>(() => service.RemoveAsync(other.Id));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~QuotationItemServiceTests"`
Expected: compile error (`QuotationItemService` doesn't exist yet).

- [ ] **Step 3: Implement `QuotationItemService`**

Create `backend/GaraCare.Application/Services/QuotationItemService.cs`:

```csharp
using GaraCare.Application.DTOs.QuotationItems;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;

namespace GaraCare.Application.Services;

public class QuotationItemService : IQuotationItemService
{
    private static readonly WorkOrderStatus[] EditableStatuses = { WorkOrderStatus.Diagnosing, WorkOrderStatus.QuotePending };

    private readonly IUnitOfWork _unitOfWork;

    public QuotationItemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<QuotationItemResponse> AddAsync(AddQuotationItemRequest request, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        await EnsureEditableAsync(workOrder, cancellationToken);

        var lowStockWarning = false;
        if (request.Type == QuotationItemType.Part && request.PartId.HasValue)
        {
            var part = await _unitOfWork.Repository<Part>().GetByIdAsync(request.PartId.Value, cancellationToken)
                ?? throw new EntityNotFoundException("Không tìm thấy phụ tùng.");
            lowStockWarning = part.StockQuantity < request.Quantity;
        }

        var item = new QuotationItem
        {
            WorkOrderId = workOrder.Id,
            PartId = request.PartId,
            Type = request.Type,
            Description = request.Description,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
        };
        await _unitOfWork.Repository<QuotationItem>().AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecalculateWorkOrderTotalAsync(workOrder.Id, cancellationToken);

        return ToResponse(item, lowStockWarning);
    }

    public async Task RemoveAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Repository<QuotationItem>().GetByIdAsync(itemId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy hạng mục báo giá.");

        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(item.WorkOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        await EnsureEditableAsync(workOrder, cancellationToken);

        _unitOfWork.Repository<QuotationItem>().Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecalculateWorkOrderTotalAsync(workOrder.Id, cancellationToken);
    }

    private async Task EnsureEditableAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        if (!EditableStatuses.Contains(workOrder.Status))
        {
            throw new InvalidTransitionException(
                $"Không thể thêm/sửa hạng mục báo giá khi WorkOrder đang ở trạng thái {workOrder.Status}.");
        }

        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrder.Id, cancellationToken);
        if (items.Any(q => q.IsApproved))
        {
            throw new QuotationLockedException("Báo giá đã được khách duyệt, không thể chỉnh sửa.");
        }
    }

    private async Task RecalculateWorkOrderTotalAsync(int workOrderId, CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrderId, cancellationToken);
        var total = items.Sum(q => q.Quantity * q.UnitPrice);

        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");
        workOrder.TotalAmount = total;
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static QuotationItemResponse ToResponse(QuotationItem item, bool lowStockWarning = false) => new()
    {
        Id = item.Id,
        WorkOrderId = item.WorkOrderId,
        PartId = item.PartId,
        Type = item.Type,
        Description = item.Description,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        LineTotal = item.Quantity * item.UnitPrice,
        IsApproved = item.IsApproved,
        IsUsed = item.IsUsed,
        LowStockWarning = lowStockWarning,
    };
}
```

Register the service in `backend/GaraCare.Infrastructure/DependencyInjection.cs` — add this line after `services.AddScoped<IWorkOrderService, WorkOrderService>();`:

```csharp
        services.AddScoped<IQuotationItemService, QuotationItemService>();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~QuotationItemServiceTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/GaraCare.Application/DTOs/QuotationItems backend/GaraCare.Application/Interfaces/IQuotationItemService.cs backend/GaraCare.Application/Services/QuotationItemService.cs backend/GaraCare.Infrastructure/DependencyInjection.cs backend/GaraCare.Tests/QuotationItemServiceTests.cs
git commit -m "feat(quotation): add QuotationItemService add/remove (GARA-24)"
```

---

### Task 5: `SendQuoteAsync` — sinh `ApprovalToken` (GARA-25, `Diagnosing → QuotePending`)

**Files:**
- Create: `backend/GaraCare.Application/DTOs/WorkOrders/SendQuoteRequest.cs`
- Modify: `backend/GaraCare.Application/Interfaces/IWorkOrderService.cs`
- Modify: `backend/GaraCare.Application/Services/WorkOrderService.cs`
- Test: `backend/GaraCare.Tests/WorkOrderServiceTests.cs`

**Interfaces:**
- Produces: `IWorkOrderService.SendQuoteAsync(int workOrderId, SendQuoteRequest request, int actorUserId, CancellationToken cancellationToken = default) : Task<WorkOrderResponse>`
- Consumes: `EmptyQuotationException` (Task 3), `IEmailService.SendAsync(string, string, string, CancellationToken)` (existing), `Notification`/`NotificationType.QuoteReady` entities (existing), `FakeEmailService` test double (`backend/GaraCare.Tests/AuthServiceTests.cs`).
- This step changes the `WorkOrderService` constructor again (adds `IEmailService`, `ILogger<WorkOrderService>`) — update the `CreateService` test helper in the same commit.

- [ ] **Step 1: Write the failing tests**

Create `backend/GaraCare.Application/DTOs/WorkOrders/SendQuoteRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.WorkOrders;

public class SendQuoteRequest
{
    [Required(ErrorMessage = "Ngày dự kiến hoàn thành là bắt buộc.")]
    public DateTime EstimatedCompletionDate { get; set; }
}
```

Add to `IWorkOrderService`:

```csharp
    Task<WorkOrderResponse> SendQuoteAsync(int workOrderId, SendQuoteRequest request, int actorUserId, CancellationToken cancellationToken = default);
```

In `backend/GaraCare.Tests/WorkOrderServiceTests.cs`, replace `CreateService` (from Task 1) with a version that also returns the fake email service, and add the new tests:

```csharp
    private static (WorkOrderService Service, GaraCareDbContext Db, FakeDateTimeProvider Clock, FakeEmailService Email) CreateService()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        var clock = new FakeDateTimeProvider();
        var email = new FakeEmailService();
        return (new WorkOrderService(unitOfWork, clock, email, NullLogger<WorkOrderService>.Instance), db, clock, email);
    }
```

> Every existing call site of `CreateService()` in this file (from Task 1 and Task 2) destructures 3 values (`service, db, clock`) — update each to destructure 4 (`service, db, clock, _`) since the tuple shape changed. This only touches the destructuring pattern, not the assertions.

Add these tests at the end of the class:

```csharp
    [Fact]
    public async Task SendQuoteAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.SendQuoteAsync(999, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: 1));
    }

    [Theory]
    [InlineData(WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.InRepair)]
    public async Task SendQuoteAsync_NotDiagnosing_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
    }

    [Fact]
    public async Task SendQuoteAsync_NoQuotationItems_ThrowsEmptyQuotationException()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<EmptyQuotationException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
    }

    [Fact]
    public async Task SendQuoteAsync_ValidRequest_GeneratesTokenWith72HourExpiryAndTransitionsToQuotePending()
    {
        var (service, db, clock, _) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        db.Customers.First(c => c.Id == customerId).Email = "customer@example.com";
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();
        var estimatedCompletion = clock.UtcNow.AddDays(2);

        var result = await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = estimatedCompletion }, actorUserId: userId);

        Assert.Equal("QuotePending", result.Status);
        var updated = await db.WorkOrders.FindAsync(workOrder.Id);
        Assert.False(string.IsNullOrEmpty(updated!.ApprovalToken));
        Assert.Equal(clock.UtcNow.AddHours(72), updated.ApprovalTokenExpiresAt);
        Assert.Null(updated.ApprovalTokenUsedAt);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == workOrder.Id).ToList();
        Assert.Single(history);
        Assert.Equal(WorkOrderStatus.Diagnosing, history[0].FromStatus);
        Assert.Equal(WorkOrderStatus.QuotePending, history[0].ToStatus);
    }

    [Fact]
    public async Task SendQuoteAsync_ValidRequest_SendsQuoteReadyNotificationAndEmail()
    {
        var (service, db, _, email) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        var customer = db.Customers.First(c => c.Id == customerId);
        customer.Email = "customer@example.com";
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId);

        var notification = db.Notifications.Single(n => n.WorkOrderId == workOrder.Id);
        Assert.Equal(NotificationType.QuoteReady, notification.Type);
        Assert.True(notification.EmailSentSuccessfully);
        Assert.Equal("customer@example.com", email.LastToEmail);
    }

    [Fact]
    public async Task SendQuoteAsync_CustomerHasNoEmail_StillCreatesNotification_EmailNotSent()
    {
        var (service, db, _, email) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        await service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId);

        var notification = db.Notifications.Single(n => n.WorkOrderId == workOrder.Id);
        Assert.False(notification.EmailSentSuccessfully);
        Assert.Null(email.LastToEmail);
    }

    [Fact]
    public async Task SendQuoteAsync_CalledTwiceOnAlreadyQuotePending_ThrowsWithResendHint()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.QuotePending, ReceivedDate = DateTime.UtcNow, ApprovalToken = "existing-token" };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidTransitionException>(() =>
            service.SendQuoteAsync(workOrder.Id, new SendQuoteRequest { EstimatedCompletionDate = DateTime.UtcNow.AddDays(3) }, actorUserId: userId));
        Assert.Contains("resend-quote", ex.Message);
    }
```

Add the required `using` directives at the top of `WorkOrderServiceTests.cs` if not already present: `using GaraCare.Domain.Enums;` and `using Microsoft.Extensions.Logging.Abstractions;` (the file already has `using GaraCare.Domain.Entities;` and `using GaraCare.Domain.Enums;` from Task 1/2 changes — only add `Microsoft.Extensions.Logging.Abstractions` if missing).

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests.SendQuoteAsync"`
Expected: compile error (`SendQuoteAsync`, the 4-arg constructor, and the 4-tuple `CreateService` don't exist yet).

- [ ] **Step 3: Implement `SendQuoteAsync`**

Replace the top of `backend/GaraCare.Application/Services/WorkOrderService.cs` (usings, fields, constructor) with:

```csharp
using System.Security.Cryptography;
using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GaraCare.Application.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmailService _emailService;
    private readonly ILogger<WorkOrderService> _logger;

    public WorkOrderService(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEmailService emailService,
        ILogger<WorkOrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _emailService = emailService;
        _logger = logger;
    }
```

Add `SendQuoteAsync` and the shared `NotifyQuoteReadyAsync` helper after `StartDiagnosisAsync`:

```csharp
    public async Task<WorkOrderResponse> SendQuoteAsync(int workOrderId, SendQuoteRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.Diagnosing)
        {
            var hint = workOrder.Status == WorkOrderStatus.QuotePending
                ? " Báo giá đã được gửi trước đó — dùng resend-quote để gửi lại link mới."
                : string.Empty;
            throw new InvalidTransitionException($"Không thể gửi báo giá từ trạng thái {workOrder.Status}.{hint}");
        }

        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrderId, cancellationToken);
        if (items.Count == 0)
        {
            throw new EmptyQuotationException("Chưa có hạng mục báo giá nào.");
        }

        var now = _dateTimeProvider.UtcNow;
        workOrder.ApprovalToken = RandomNumberGenerator.GetHexString(32);
        workOrder.ApprovalTokenExpiresAt = now.AddHours(72);
        workOrder.ApprovalTokenUsedAt = null;
        workOrder.EstimatedCompletionDate = request.EstimatedCompletionDate;
        workOrder.QuoteSentAt = now;
        workOrder.Status = WorkOrderStatus.QuotePending;
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.Repository<WorkOrderStatusHistory>().AddAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Diagnosing,
            ToStatus = WorkOrderStatus.QuotePending,
            ChangedByUserId = actorUserId,
            ApprovedViaToken = false,
            ChangedAt = now,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyQuoteReadyAsync(workOrder, cancellationToken);

        return ToResponse(workOrder);
    }

    private async Task NotifyQuoteReadyAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var vehicle = await _unitOfWork.Repository<Vehicle>().GetByIdAsync(workOrder.VehicleId, cancellationToken);
        var customer = vehicle is null
            ? null
            : await _unitOfWork.Repository<Customer>().GetByIdAsync(vehicle.CustomerId, cancellationToken);

        if (customer is null)
        {
            return;
        }

        var message = $"Báo giá cho work order #{workOrder.Id} đã sẵn sàng, vui lòng xem và duyệt.";
        var emailSent = false;
        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            try
            {
                await _emailService.SendAsync(
                    customer.Email,
                    "Báo giá sửa xe từ GaraCare",
                    $"<p>{message}</p><p>Mã duyệt: {workOrder.ApprovalToken}</p>",
                    cancellationToken);
                emailSent = true;
            }
            catch (Exception ex)
            {
                // Lỗi gửi email không được rollback transaction chính — chỉ log.
                // Xem docs/01-business-spec.md §9.
                _logger.LogWarning(ex, "Gửi email báo giá thất bại cho WorkOrder {WorkOrderId}", workOrder.Id);
            }
        }

        await _unitOfWork.Repository<Notification>().AddAsync(new Notification
        {
            CustomerId = customer.Id,
            WorkOrderId = workOrder.Id,
            Type = NotificationType.QuoteReady,
            Message = message,
            EmailSentSuccessfully = emailSent,
            IsRead = false,
            CreatedAt = _dateTimeProvider.UtcNow,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests"`
Expected: PASS (all tests from Task 1, 2, and 5).

- [ ] **Step 5: Commit**

```bash
git add backend/GaraCare.Application/DTOs/WorkOrders/SendQuoteRequest.cs backend/GaraCare.Application/Interfaces/IWorkOrderService.cs backend/GaraCare.Application/Services/WorkOrderService.cs backend/GaraCare.Tests/WorkOrderServiceTests.cs
git commit -m "feat(workorder): add SendQuoteAsync with ApprovalToken generation (GARA-25)"
```

---

### Task 6: `ResendQuoteAsync` (GARA-25 extension)

**Files:**
- Modify: `backend/GaraCare.Application/Interfaces/IWorkOrderService.cs`
- Modify: `backend/GaraCare.Application/Services/WorkOrderService.cs`
- Test: `backend/GaraCare.Tests/WorkOrderServiceTests.cs`

**Interfaces:**
- Produces: `IWorkOrderService.ResendQuoteAsync(int workOrderId, int actorUserId, CancellationToken cancellationToken = default) : Task<WorkOrderResponse>`
- Consumes: `NotifyQuoteReadyAsync` (private helper added in Task 5).

- [ ] **Step 1: Write the failing tests**

Add to `IWorkOrderService`:

```csharp
    Task<WorkOrderResponse> ResendQuoteAsync(int workOrderId, int actorUserId, CancellationToken cancellationToken = default);
```

Append to `backend/GaraCare.Tests/WorkOrderServiceTests.cs`:

```csharp
    [Theory]
    [InlineData(WorkOrderStatus.Diagnosing)]
    [InlineData(WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.InRepair)]
    public async Task ResendQuoteAsync_NotQuotePending_ThrowsInvalidTransitionException(WorkOrderStatus status)
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = status, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidTransitionException>(() => service.ResendQuoteAsync(workOrder.Id, actorUserId: userId));
    }

    [Fact]
    public async Task ResendQuoteAsync_QuotePending_GeneratesNewTokenAndDoesNotWriteStatusHistory()
    {
        var (service, db, clock, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder
        {
            VehicleId = vehicleId,
            CreatedByUserId = userId,
            Status = WorkOrderStatus.QuotePending,
            ReceivedDate = DateTime.UtcNow,
            ApprovalToken = "old-token",
            ApprovalTokenExpiresAt = clock.UtcNow.AddHours(1),
        };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();
        clock.UtcNow = clock.UtcNow.AddHours(10);

        var result = await service.ResendQuoteAsync(workOrder.Id, actorUserId: userId);

        Assert.Equal("QuotePending", result.Status);
        var updated = await db.WorkOrders.FindAsync(workOrder.Id);
        Assert.NotEqual("old-token", updated!.ApprovalToken);
        Assert.Equal(clock.UtcNow.AddHours(72), updated.ApprovalTokenExpiresAt);
        Assert.Empty(db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == workOrder.Id));
    }

    [Fact]
    public async Task ResendQuoteAsync_QuotePending_SendsQuoteReadyNotificationAgain()
    {
        var (service, db, _, email) = CreateService();
        var (vehicleId, userId, customerId) = await SeedVehicleAsync(db);
        db.Customers.First(c => c.Id == customerId).Email = "customer@example.com";
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.QuotePending, ReceivedDate = DateTime.UtcNow, ApprovalToken = "old-token" };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        await service.ResendQuoteAsync(workOrder.Id, actorUserId: userId);

        var notifications = db.Notifications.Where(n => n.WorkOrderId == workOrder.Id).ToList();
        Assert.Single(notifications);
        Assert.Equal("customer@example.com", email.LastToEmail);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests.ResendQuoteAsync"`
Expected: compile error (`ResendQuoteAsync` doesn't exist yet).

- [ ] **Step 3: Implement `ResendQuoteAsync`**

Add to `backend/GaraCare.Application/Services/WorkOrderService.cs`, after `SendQuoteAsync`:

```csharp
    public async Task<WorkOrderResponse> ResendQuoteAsync(int workOrderId, int actorUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        if (workOrder.Status != WorkOrderStatus.QuotePending)
        {
            throw new InvalidTransitionException(
                $"Chỉ có thể gửi lại báo giá khi WorkOrder đang ở QuotePending (hiện tại: {workOrder.Status}).");
        }

        var now = _dateTimeProvider.UtcNow;
        workOrder.ApprovalToken = RandomNumberGenerator.GetHexString(32);
        workOrder.ApprovalTokenExpiresAt = now.AddHours(72);
        workOrder.ApprovalTokenUsedAt = null;
        _unitOfWork.Repository<WorkOrder>().Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Resend quote cho WorkOrder {WorkOrderId} bởi User {ActorUserId}", workOrder.Id, actorUserId);

        await NotifyQuoteReadyAsync(workOrder, cancellationToken);

        return ToResponse(workOrder);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/GaraCare.Application/Interfaces/IWorkOrderService.cs backend/GaraCare.Application/Services/WorkOrderService.cs backend/GaraCare.Tests/WorkOrderServiceTests.cs
git commit -m "feat(workorder): add ResendQuoteAsync (GARA-25 resend-quote)"
```

---

### Task 7: `GetByIdAsync` + `WorkOrdersController` (GARA-26, part 1)

**Files:**
- Create: `backend/GaraCare.Application/DTOs/WorkOrders/WorkOrderDetailResponse.cs`
- Modify: `backend/GaraCare.Application/Interfaces/IWorkOrderService.cs`
- Modify: `backend/GaraCare.Application/Services/WorkOrderService.cs`
- Create: `backend/GaraCare.Api/Controllers/WorkOrdersController.cs`
- Test: `backend/GaraCare.Tests/WorkOrderServiceTests.cs`

**Interfaces:**
- Produces: `IWorkOrderService.GetByIdAsync(int workOrderId, CancellationToken cancellationToken = default) : Task<WorkOrderDetailResponse>`
- Produces: `WorkOrderDetailResponse { Id, VehicleId, Status, ReceivedDate, InitialDescription, DiagnosisNote, TotalAmount, DiscountPercent, EstimatedCompletionDate, IsDelayed, QuotationItems: IReadOnlyList<QuotationItemResponse> }`
- Consumes: `QuotationItemResponse` (Task 4), `WorkOrderResponse`/`CreateWalkInWorkOrderRequest`/`StartDiagnosisRequest`/`SendQuoteRequest` (Tasks 1, 2, 5).

- [ ] **Step 1: Write the failing tests**

Create `backend/GaraCare.Application/DTOs/WorkOrders/WorkOrderDetailResponse.cs`:

```csharp
using GaraCare.Application.DTOs.QuotationItems;

namespace GaraCare.Application.DTOs.WorkOrders;

public class WorkOrderDetailResponse
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string? InitialDescription { get; set; }
    public string? DiagnosisNote { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public bool IsDelayed { get; set; }
    public IReadOnlyList<QuotationItemResponse> QuotationItems { get; set; } = new List<QuotationItemResponse>();
}
```

Add to `IWorkOrderService`:

```csharp
    Task<WorkOrderDetailResponse> GetByIdAsync(int workOrderId, CancellationToken cancellationToken = default);
```

Append to `backend/GaraCare.Tests/WorkOrderServiceTests.cs`:

```csharp
    [Fact]
    public async Task GetByIdAsync_WorkOrderNotFound_ThrowsEntityNotFoundException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsWorkOrderWithQuotationItems()
    {
        var (service, db, _, _) = CreateService();
        var (vehicleId, userId, _) = await SeedVehicleAsync(db);
        var workOrder = new WorkOrder { VehicleId = vehicleId, CreatedByUserId = userId, Status = WorkOrderStatus.Diagnosing, ReceivedDate = DateTime.UtcNow };
        db.WorkOrders.Add(workOrder);
        db.QuotationItems.Add(new QuotationItem { WorkOrderId = workOrder.Id, Type = QuotationItemType.Labor, Description = "Công", Quantity = 1, UnitPrice = 100000 });
        await db.SaveChangesAsync();

        var result = await service.GetByIdAsync(workOrder.Id);

        Assert.Equal("Diagnosing", result.Status);
        Assert.Single(result.QuotationItems);
        Assert.Equal("Công", result.QuotationItems[0].Description);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests.GetByIdAsync"`
Expected: compile error (`GetByIdAsync` doesn't exist yet).

- [ ] **Step 3: Implement `GetByIdAsync` and the controller**

Add to `backend/GaraCare.Application/Services/WorkOrderService.cs`, after `ResendQuoteAsync`, plus a small private mapper (add `using GaraCare.Application.DTOs.QuotationItems;` and `using GaraCare.Domain.Entities;` is already present):

```csharp
    public async Task<WorkOrderDetailResponse> GetByIdAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.Repository<WorkOrder>().GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy work order.");

        var items = await _unitOfWork.Repository<QuotationItem>().FindAsync(q => q.WorkOrderId == workOrderId, cancellationToken);

        return new WorkOrderDetailResponse
        {
            Id = workOrder.Id,
            VehicleId = workOrder.VehicleId,
            Status = workOrder.Status.ToString(),
            ReceivedDate = workOrder.ReceivedDate,
            InitialDescription = workOrder.InitialDescription,
            DiagnosisNote = workOrder.DiagnosisNote,
            TotalAmount = workOrder.TotalAmount,
            DiscountPercent = workOrder.DiscountPercent,
            EstimatedCompletionDate = workOrder.EstimatedCompletionDate,
            IsDelayed = workOrder.IsDelayed,
            QuotationItems = items.Select(ToQuotationItemResponse).ToList(),
        };
    }

    private static QuotationItemResponse ToQuotationItemResponse(QuotationItem item) => new()
    {
        Id = item.Id,
        WorkOrderId = item.WorkOrderId,
        PartId = item.PartId,
        Type = item.Type,
        Description = item.Description,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        LineTotal = item.Quantity * item.UnitPrice,
        IsApproved = item.IsApproved,
        IsUsed = item.IsUsed,
        LowStockWarning = false,
    };
```

Add `using GaraCare.Application.DTOs.QuotationItems;` to the top of `WorkOrderService.cs`.

Create `backend/GaraCare.Api/Controllers/WorkOrdersController.cs`:

```csharp
using System.Security.Claims;
using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderServiceTests"`
Expected: PASS.

Run: `dotnet build backend/GaraCare.Api`
Expected: build succeeds (verifies `WorkOrdersController` compiles against the extended `IWorkOrderService`).

- [ ] **Step 5: Commit**

```bash
git add backend/GaraCare.Application/DTOs/WorkOrders/WorkOrderDetailResponse.cs backend/GaraCare.Application/Interfaces/IWorkOrderService.cs backend/GaraCare.Application/Services/WorkOrderService.cs backend/GaraCare.Api/Controllers/WorkOrdersController.cs backend/GaraCare.Tests/WorkOrderServiceTests.cs
git commit -m "feat(workorder): add GetByIdAsync and wire WorkOrdersController (GARA-26)"
```

---

### Task 8: `QuotationItemsController` (GARA-26, part 2) + end-to-end integration test

**Files:**
- Create: `backend/GaraCare.Api/Controllers/QuotationItemsController.cs`
- Test: `backend/GaraCare.Tests/WorkOrderIntakeFlowTests.cs`

**Interfaces:**
- Consumes: `IQuotationItemService` (Task 4), `IWorkOrderService` (Tasks 1, 2, 5, 6, 7) — no new production interfaces.

- [ ] **Step 1: Write the failing integration test**

Create `backend/GaraCare.Tests/WorkOrderIntakeFlowTests.cs` — this exercises the full GARA-3 flow through the Application-layer services (Service-level integration test, matching this project's existing "test Application layer, no full API host" convention from `docs/07-backend-conventions.md`):

```csharp
using GaraCare.Application.DTOs.QuotationItems;
using GaraCare.Application.DTOs.WorkOrders;
using GaraCare.Application.Services;
using GaraCare.Domain.Entities;
using GaraCare.Domain.Enums;
using GaraCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GaraCare.Tests;

public class WorkOrderIntakeFlowTests
{
    [Fact]
    public async Task FullIntakeFlow_WalkInToResendQuote_ProducesExpectedStateAndHistory()
    {
        var options = new DbContextOptionsBuilder<GaraCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new GaraCareDbContext(options);
        var unitOfWork = new UnitOfWork(db);
        var clock = new FakeDateTimeProvider();
        var email = new FakeEmailService();
        var workOrderService = new WorkOrderService(unitOfWork, clock, email, NullLogger<WorkOrderService>.Instance);
        var quotationItemService = new QuotationItemService(unitOfWork);

        var staff = new User { Username = "staff1", PasswordHash = "hash", FullName = "Staff 1", Role = UserRole.Staff, IsEmailVerified = true };
        var technician = new User { Username = "tech1", PasswordHash = "hash", FullName = "Tech 1", Role = UserRole.Technician, IsEmailVerified = true };
        db.Users.AddRange(staff, technician);
        var customer = new Customer { FullName = "Khách A", Phone = "0900000000", Email = "customer@example.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var vehicle = new Vehicle { CustomerId = customer.Id, LicensePlate = "51A-12345" };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var created = await workOrderService.CreateWalkInAsync(
            new CreateWalkInWorkOrderRequest { VehicleId = vehicle.Id, InitialDescription = "Kêu lạ khi phanh" },
            actorUserId: staff.Id);
        Assert.Equal("Received", created.Status);

        await workOrderService.StartDiagnosisAsync(
            created.Id, new StartDiagnosisRequest { DiagnosisNote = "Mòn má phanh" }, actorUserId: technician.Id);

        await quotationItemService.AddAsync(new AddQuotationItemRequest
        {
            WorkOrderId = created.Id,
            Type = QuotationItemType.Part,
            Description = "Má phanh trước",
            Quantity = 1,
            UnitPrice = 300000,
        });
        await quotationItemService.AddAsync(new AddQuotationItemRequest
        {
            WorkOrderId = created.Id,
            Type = QuotationItemType.Labor,
            Description = "Công thay má phanh",
            Quantity = 1,
            UnitPrice = 150000,
        });

        var sent = await workOrderService.SendQuoteAsync(
            created.Id, new SendQuoteRequest { EstimatedCompletionDate = clock.UtcNow.AddDays(1) }, actorUserId: staff.Id);
        Assert.Equal("QuotePending", sent.Status);
        Assert.Equal(450000, sent.TotalAmount);
        var tokenAfterSend = (await db.WorkOrders.FindAsync(created.Id))!.ApprovalToken;

        var resent = await workOrderService.ResendQuoteAsync(created.Id, actorUserId: staff.Id);
        Assert.Equal("QuotePending", resent.Status);
        var tokenAfterResend = (await db.WorkOrders.FindAsync(created.Id))!.ApprovalToken;
        Assert.NotEqual(tokenAfterSend, tokenAfterResend);

        var history = db.WorkOrderStatusHistories.Where(h => h.WorkOrderId == created.Id).OrderBy(h => h.ChangedAt).ToList();
        Assert.Equal(3, history.Count);
        Assert.Equal((WorkOrderStatus.Received, WorkOrderStatus.Received), (history[0].FromStatus, history[0].ToStatus));
        Assert.Equal((WorkOrderStatus.Received, WorkOrderStatus.Diagnosing), (history[1].FromStatus, history[1].ToStatus));
        Assert.Equal((WorkOrderStatus.Diagnosing, WorkOrderStatus.QuotePending), (history[2].FromStatus, history[2].ToStatus));

        var detail = await workOrderService.GetByIdAsync(created.Id);
        Assert.Equal(2, detail.QuotationItems.Count);

        var notifications = db.Notifications.Where(n => n.WorkOrderId == created.Id).ToList();
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, n => Assert.Equal(NotificationType.QuoteReady, n.Type));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/GaraCare.Tests --filter "FullyQualifiedName~WorkOrderIntakeFlowTests"`
Expected: compiles and runs (all production types already exist from Tasks 1–7) — should already PASS at this point since no controller code is exercised by this test. If it fails, the failure points at a real regression introduced in an earlier task; fix that before continuing.

- [ ] **Step 3: Add `QuotationItemsController`**

Create `backend/GaraCare.Api/Controllers/QuotationItemsController.cs`:

```csharp
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
```

- [ ] **Step 4: Run the full test suite**

Run: `dotnet test backend/GaraCare.Tests`
Expected: PASS — every test from Tasks 1–8.

Run: `dotnet build backend/GaraCare.Api`
Expected: build succeeds (verifies both new controllers compile and DI resolves `IQuotationItemService`/`IWorkOrderService`).

- [ ] **Step 5: Commit**

```bash
git add backend/GaraCare.Api/Controllers/QuotationItemsController.cs backend/GaraCare.Tests/WorkOrderIntakeFlowTests.cs
git commit -m "feat(quotation): wire QuotationItemsController and add end-to-end intake flow test (GARA-26)"
```

---

## Self-Review Notes

- **Spec coverage:** GARA-22 → Task 1; GARA-23 → Task 2; new exceptions → Task 3; GARA-24 → Task 4; GARA-25 `send-quote` → Task 5; GARA-25 `resend-quote` (this session's decision) → Task 6; GARA-26 `WorkOrdersController` + `GetByIdAsync` → Task 7; GARA-26 `QuotationItemsController` → Task 8. All 5 Jira tasks plus the resend-quote extension are covered.
- **Placeholder scan:** no TBD/TODO left unresolved except the intentionally-documented one already in the spec (`IEmailService` used directly instead of a future `INotificationService` from GARA-8) — that is a stated, deliberate scope boundary, not an unfinished step.
- **Type consistency:** `WorkOrderResponse`, `WorkOrderDetailResponse`, `QuotationItemResponse`, and the constructor signature of `WorkOrderService` are introduced once (Tasks 1, 5, 7) and reused identically in every later task — checked against each task's "Consumes" line.
- **Known deviation from ticket text (flagged, not silent):** `RecalculateWorkOrderTotalAsync` is private instead of a public `IQuotationItemService` member (Task 4) — YAGNI, no consumer needs it externally in this epic.
