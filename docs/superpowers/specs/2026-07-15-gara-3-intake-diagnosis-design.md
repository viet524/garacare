# GARA-3 — [BE] Tiếp nhận & Chẩn đoán — Design Spec

Nguồn: Jira Epic [GARA-3](https://tuut6161.atlassian.net/browse/GARA-3), 5 task con GARA-22..26.
Nghiệp vụ nguồn: `docs/02-use-cases.md` UC-02, UC-03; `docs/05-functional-requirements.md` FR-08/09/10/11.

## Phạm vi

Hiện thực backend cho đoạn nghiệp vụ: Staff tiếp nhận xe → Technician chẩn đoán → Staff lập báo
giá → Staff gửi báo giá (sinh `ApprovalToken`) → có thể gửi lại link báo giá.

**Ngoài phạm vi** (không đụng trong epic này):
- UC-04 (khách Approve/Reject qua token hoặc portal) — epic riêng.
- `NotificationService` đầy đủ (GARA-8) — tạm gọi `IEmailService` trực tiếp, để TODO nối lại.
- Bất kỳ entity/field mới ngoài những gì `docs/03-data-model.md` đã liệt kê — entity `WorkOrder`,
  `QuotationItem`, `WorkOrderStatusHistory` đã có đủ field cần thiết (đã xác nhận đọc code hiện
  tại), **không cần migration EF Core mới** cho epic này.

## Trạng thái code hiện tại (đã kiểm tra trước khi thiết kế)

- `IWorkOrderService`/`WorkOrderService` hiện chỉ có `GetHistoryByVehicleAsync` (GARA-21) — các
  method mới ở epic này **bổ sung thêm**, không sửa method cũ.
- Exception base có sẵn: `BusinessException`, `InvalidTransitionException`,
  `EntityNotFoundException`, `ForbiddenActionException`. Cần thêm mới: `QuotationLockedException`,
  `EmptyQuotationException` (cả hai kế thừa `BusinessException`, map 400).
- `IDateTimeProvider` có sẵn — mọi thời điểm dùng qua đây, không dùng `DateTime.UtcNow` trực tiếp
  (để test mock được).
- Chưa có `GaraCare.Application/DTOs/WorkOrders/` hay `QuotationItemsController` — tạo mới theo
  đúng cấu trúc thư mục hiện có (xem `VehiclesController.cs`, `CustomersController.cs` làm mẫu).

## Trình tự thực hiện

Làm tuần tự, mỗi bước có unit test đi kèm trước khi sang bước sau:

### 1. GARA-22 — `CreateWalkInAsync` (UC-02)
- DTO: `CreateWalkInWorkOrderRequest(VehicleId, InitialDescription [Required])`,
  `WorkOrderResponse(Id, VehicleId, Status, ReceivedDate, InitialDescription, DiagnosisNote,
  TotalAmount, DiscountPercent, EstimatedCompletionDate, IsDelayed, HasOpenWorkOrderWarning)`.
- Logic: kiểm tra Vehicle tồn tại (404 nếu không) → kiểm tra WO đang mở (Status NOT IN
  [Delivered, Cancelled]) cho cùng Vehicle → **không chặn**, chỉ set cờ cảnh báo trong response →
  tạo WorkOrder Status=Received, ReceivedDate=`IDateTimeProvider.UtcNow`, AppointmentId=null →
  ghi 1 dòng `WorkOrderStatusHistory` (From=To=Received, quyết định: coi "tạo mới" là 1 dòng
  lịch sử tự tham chiếu, không phải transition thật).
- Test: Vehicle không tồn tại → 404; xe có WO mở → vẫn tạo được kèm cờ warning; description rỗng
  → 400 DataAnnotations; ghi đúng 1 dòng lịch sử.

### 2. GARA-23 — `StartDiagnosisAsync` (`Received → Diagnosing`)
- DTO: `StartDiagnosisRequest(DiagnosisNote?)`.
- Logic: load WO (404 nếu không có) → Status phải là `Received` (khác → `InvalidTransitionException`)
  → set Status=Diagnosing, DiagnosisNote nếu có → ghi `WorkOrderStatusHistory`
  (From=Received, To=Diagnosing, ApprovedViaToken=false).
- Role check ở Controller (`[Authorize(Roles="Technician")]`), Service không check lại role.
- Không tạo Notification (không nằm trong danh sách sự kiện cần thông báo ở UC-12).
- Test: 3 case bắt buộc — transition hợp lệ / sai trạng thái nguồn bị chặn / WO không tồn tại 404.

### 3. GARA-24 — `QuotationItemService` (thêm/sửa hạng mục báo giá)
- DTO: `AddQuotationItemRequest(WorkOrderId, PartId?, Type, Description [Required],
  Quantity [Range(1,int.MaxValue)], UnitPrice [Range(0,double.MaxValue)])`,
  `QuotationItemResponse(Id, WorkOrderId, PartId, Type, Description, Quantity, UnitPrice,
  LineTotal, IsApproved, IsUsed, LowStockWarning)`.
- Interface `IQuotationItemService`: `AddAsync`, `RemoveAsync`, `RecalculateWorkOrderTotalAsync`.
- Rule khoá dữ liệu (quan trọng nhất): WO status phải IN [Diagnosing, QuotePending]; nếu **bất kỳ**
  item nào của WO đã `IsApproved=true` → chặn cứng toàn bộ add/remove trên WO đó (kể cả item khác
  chưa approved) → throw `QuotationLockedException`.
- Thiếu tồn kho phụ tùng → không chặn, chỉ set `LowStockWarning=true`.
- Sau mỗi Add/Remove: `RecalculateWorkOrderTotalAsync` — `TotalAmount = SUM(Quantity*UnitPrice)`
  toàn bộ item của WO, lưu lại.
- Test: sai trạng thái WO bị chặn; có item approved chặn toàn bộ thao tác; thiếu tồn kho vẫn tạo
  được kèm cảnh báo; TotalAmount cập nhật đúng theo số học cụ thể; Quantity/UnitPrice âm bị chặn.

### 4. GARA-25 — `SendQuoteAsync` + `ResendQuoteAsync` (sinh/làm mới `ApprovalToken`)

**`SendQuoteAsync`** (`Diagnosing → QuotePending`, lần đầu):
- DTO: `SendQuoteRequest(EstimatedCompletionDate [Required])`.
- Bắt buộc WO đang `Diagnosing` — nếu đang `QuotePending` rồi, báo lỗi gợi ý dùng resend-quote;
  trạng thái khác → `InvalidTransitionException` thông thường.
- Bắt buộc có ≥1 QuotationItem, rỗng → `EmptyQuotationException`.
- Sinh `ApprovalToken = RandomNumberGenerator.GetHexString(32)` (không dùng `Guid.NewGuid()`).
- `ApprovalTokenExpiresAt = IDateTimeProvider.UtcNow.AddHours(72)`, `ApprovalTokenUsedAt = null`.
- Set `EstimatedCompletionDate`, `QuoteSentAt = UtcNow`, `Status = QuotePending`.
- Ghi `WorkOrderStatusHistory` (Diagnosing → QuotePending).
- Gửi thông báo `QuoteReady` cho Customer qua `IEmailService` trực tiếp (TODO nối `INotificationService`
  khi GARA-8 xong).

**`ResendQuoteAsync`** (mới, quyết định của phiên brainstorming này — không đổi Status):
- Endpoint: `POST /workorders/{id}/resend-quote`, role `Staff,Admin`.
- Bắt buộc WO đang **`QuotePending`** (đã gửi ít nhất 1 lần) — khác → `InvalidTransitionException`.
- Sinh token mới đè token cũ (token cũ mất hiệu lực ngay): `ApprovalToken`,
  `ApprovalTokenExpiresAt = now+72h`, `ApprovalTokenUsedAt = null`.
- **Không** ghi `WorkOrderStatusHistory` — không có transition trạng thái thật.
- Gửi lại thông báo `QuoteReady` với link mới (bắt buộc — nếu không thì tính năng vô nghĩa).
- Test: gọi khi WO không phải QuotePending → 400; gọi thành công → token mới khác token cũ, hạn
  dùng tính lại đúng từ thời điểm gọi (mock `IDateTimeProvider`); token cũ dùng thử approve (giả lập,
  nếu có sẵn hạ tầng test) phải bị coi là không hợp lệ vì đã bị ghi đè — nếu chưa có luồng approve để
  test trực tiếp, chỉ cần assert giá trị `ApprovalToken` đã đổi.

### 5. GARA-26 — Controllers wiring
- `WorkOrdersController` (`[Route("api/workorders")]`):
  - `[Authorize(Roles="Staff,Admin")] POST /` → CreateWalkInAsync
  - `[Authorize(Roles="Technician")] POST /{id}/start-diagnosis` → StartDiagnosisAsync
  - `[Authorize(Roles="Staff,Admin")] POST /{id}/send-quote` → SendQuoteAsync
  - `[Authorize(Roles="Staff,Admin")] POST /{id}/resend-quote` → ResendQuoteAsync
  - `[Authorize(Roles="Staff,Technician,Admin")] GET /{id}` → chi tiết WO kèm QuotationItems
- `QuotationItemsController` (`[Route("api/quotation-items")]`):
  - `[Authorize(Roles="Staff,Admin")] POST /` → AddAsync
  - `[Authorize(Roles="Staff,Admin")] DELETE /{id}` → RemoveAsync
- `actorUserId` luôn lấy từ `User.FindFirstValue(ClaimTypes.NameIdentifier)`, không nhận từ body.
- Test tích hợp end-to-end: walk-in → start-diagnosis → thêm 2 item → send-quote → resend-quote →
  kiểm tra Status=QuotePending, token đổi giữa 2 lần gửi, đủ 2 dòng WorkOrderStatusHistory
  (Received→Diagnosing, Diagnosing→QuotePending) — resend không thêm dòng thứ 3.

## Ràng buộc xuyên suốt

- Không có endpoint đổi Status trực tiếp kiểu `PUT {status}` — chỉ action-based.
- Mọi validate thứ tự transition nằm ở Service layer.
- Mỗi transition thật (đổi Status) ghi đúng 1 dòng `WorkOrderStatusHistory`; resend-quote không đổi
  Status nên không ghi.
- Không sửa/xoá QuotationItem sau khi có item Approved trong cùng WO.
- Không tự thêm entity/field/endpoint ngoài danh sách trên.
