# 03. Data Model (ERD) — SQL Server

Nguồn nghiệp vụ: SRS mục 6. Đã điều chỉnh kiểu dữ liệu cho **SQL Server** (SRS gốc viết cho SQLite — xem cảnh báo ở `CLAUDE.md` mục 2). Đây là danh sách entity **tối thiểu bắt buộc**; không tự thêm bảng/field ngoài danh sách này khi chưa được yêu cầu — nếu thấy thiếu gì đó để "code chạy được", hỏi trước.

## Quy ước chung

- Khoá chính: `Id` kiểu `INT IDENTITY(1,1)` (hoặc `UNIQUEIDENTIFIER` nếu team quyết định dùng GUID — chỉ đổi khi có quyết định rõ, đừng trộn lẫn 2 kiểu trong cùng schema).
- Ngày giờ: dùng `datetime2` (không dùng `datetime` cũ, không có kiểu SQLite-only).
- Tiền: `decimal(18,2)`.
- Chuỗi ngắn (mã, token, biển số): `nvarchar(n)` với độ dài rõ ràng, không dùng `nvarchar(max)` tuỳ tiện.
- Text tự do (mô tả, ghi chú): `nvarchar(max)`.
- Enum nghiệp vụ (Status, Role, Type...): lưu dạng `nvarchar` (tên trạng thái dạng string, ví dụ `"QuotePending"`) — **không** dùng số nguyên khó đọc, trừ khi được yêu cầu đổi. Enum ở tầng C# ánh xạ qua `HasConversion<string>()` trong EF Core.

## Entities

### User
`Id, Username, PasswordHash, FullName, Phone, Email, Role (Customer/Staff/Technician/Admin), TechnicianStatus (FREE/DIAGNOSING/WAITING_ON_CUSTOMER/WAITING_PARTS/IN_REPAIR, nullable — chỉ có giá trị khi Role=Technician), IsEmailVerified (bool), EmailVerificationCode (nullable), EmailVerificationCodeExpiresAt (nullable), PasswordResetCode (nullable), PasswordResetCodeExpiresAt (nullable)`

> `TechnicianStatus` điều khiển auto-assign (mục 11) và bảng nhận việc (mục 10 của `01-business-spec.md`) — `IN_REPAIR` khoá cứng không nhận việc mới, các trạng thái khác có thể nhận `Diagnosing` chen thêm tuỳ quy tắc.

> Đăng nhập bằng **Email** + mật khẩu (không phải Username). `Username` vẫn là field bắt buộc/duy nhất trong schema — với Customer tự đăng ký, hệ thống tự gán `Username = Email` (không có ô "tên đăng nhập" riêng trên form đăng ký); tài khoản nội bộ (Staff/Technician/Admin) do Admin tạo vẫn nhập Username riêng và `IsEmailVerified` được set `true` ngay vì không qua luồng tự đăng ký.
>
> `EmailVerificationCode`/`PasswordResetCode`: mã 6 ký tự gồm chữ hoa + số (loại bỏ ký tự dễ nhầm 0/O, 1/I), sinh bằng `RandomNumberGenerator`, gửi qua email. Tài khoản Customer chỉ đăng nhập được sau khi `IsEmailVerified = true`. Hạn dùng: mã xác minh tài khoản 24h, mã đặt lại mật khẩu 1h (ngắn hơn vì nhạy cảm hơn).

### Customer
`Id, FullName, Phone, Email, Address, UserId (FK, nullable — null nếu khách vãng lai)`

### Vehicle
`Id, CustomerId (FK), LicensePlate, Brand, Model, Year`

### Appointment
`Id, CustomerId (FK), VehicleId (FK), ServiceType (StandardService/GeneralDiagnosis), ServiceCatalogItemId (FK, nullable — bắt buộc nếu ServiceType=StandardService), RequestedTechnicianId (FK User, nullable — chỉ dùng được khi ServiceCatalogItem.IsMasterTechRequired=true), ScheduledDate, ScheduledTimeSlot, Status (Booked/CheckedIn/Cancelled/NoShow), DiscountPercent, CreatedAt, IsLate (bool), LateNotifiedStaffAt (nullable)`

> `ServiceType` quyết định khung giờ block: `StandardService` block đúng thời lượng định mức trong `ServiceCatalogItem`; `GeneralDiagnosis` chỉ block ~30 phút (xem `01-business-spec.md` mục 3 bước 1).

### WorkOrder
`Id, VehicleId (FK), AppointmentId (FK, nullable), CreatedByUserId (FK), Status, ReceivedDate, InitialDescription, DiagnosisNote, TotalAmount, DiscountPercent, IsHeavyRepair (bool — true nếu estimatedLaborHours > 2 giờ, tách vào Repair Queue), SystemSuggestedDate, FinalEstimatedDate, IsDelayed, QuoteSentAt, ReminderSentAt, NeedsFollowUpCall, CompletedDate, ApprovalToken, ApprovalTokenExpiresAt, ApprovalTokenUsedAt (nullable)`

> `ApprovalToken`: chuỗi ngẫu nhiên (khuyến nghị sinh bằng `RandomNumberGenerator`/GUID-based, không đoán được), sinh khi gửi báo giá, dùng để khách vãng lai duyệt/từ chối qua link không cần đăng nhập.
>
> **Thay đổi so với bản trước v5**: field `EstimatedCompletionDate` cũ được tách thành `SystemSuggestedDate` (hệ thống tự tính, công thức ở `01-business-spec.md` mục 12 — không sửa tay được) và `FinalEstimatedDate` (Staff xác nhận/tăng buffer, gửi cho khách; guard `FinalEstimatedDate ≥ SystemSuggestedDate`). Mọi chỗ code cũ dùng `EstimatedCompletionDate` phải đổi sang `FinalEstimatedDate` khi migrate.
>
> WorkOrder **không có field `TechnicianId` cố định** — Technician phụ trách hiện tại suy ra từ dòng `WorkOrderAssignment` mới nhất chưa `endedAt` (xem entity `WorkOrderAssignment` bên dưới). Đây là chủ đích của v5 để hỗ trợ reassign & chia hoa hồng nhiều Technician trên cùng một WorkOrder.

### DiagnosisRecord
`Id, WorkOrderId (FK), TechnicianId (FK User), Notes, EstimatedLaborHours (decimal — chỉ giờ thao tác kỹ thuật thuần), SignedAt (datetime2)`

> Bất biến (immutable) sau khi tạo — không có API sửa/xoá. Sinh khi Technician ký xác nhận chẩn đoán (UC-03 bước 3), là nguồn duy nhất để hệ thống tự sinh `Quote`/`QuotationItem`. Ảnh/video minh chứng đính kèm — lưu trữ file cụ thể (đường dẫn/blob storage) do team quyết định theo convention `07-backend-conventions.md`, không tự thêm bảng con khi chưa hỏi nếu chưa rõ yêu cầu lưu trữ.

### ServiceCatalogItem
`Id, Name, Description, UnitPrice, EstimatedDurationMinutes (nullable — dùng khi Appointment.ServiceType=StandardService), RequiredBayType (nullable — LiftBay/TireBay/GeneralBay...), IsMasterTechRequired (bool)`

> Nguồn đơn giá cho `Quote`/`QuotationItem` tự sinh (Staff không tự nhập/sửa số liệu — `01-business-spec.md` mục 3 bước 5). `RequiredBayType` dùng cho auto-assign Bay (mục 12) — **danh sách cụ thể hạng mục nào bắt buộc loại Bay gì chưa chốt**, xem ghi chú ở entity `Bay` bên dưới.

### QuotationItem
`Id, WorkOrderId (FK), PartId (FK, nullable), ServiceCatalogItemId (FK, nullable), Type (Part/Labor), Description, Quantity, UnitPrice, IsApproved, IsUsed`

### Part
`Id, Name, SKU, UnitPrice, StockQuantity`

### Bay
`Id, Type (LiftBay/TireBay/GeneralBay...), Status (FREE/OCCUPIED/MAINTENANCE), CurrentWorkOrderId (FK WorkOrder, nullable)`

> ⚠️ **Chưa chốt — hỏi người dùng trước khi tạo migration/seed**: số lượng Bay từng loại xưởng hiện có (mục 12 của `01-business-spec.md`, điểm mở #2). Không tự bịa số lượng seed data.

### WorkOrderAssignment
`Id, WorkOrderId (FK), TechnicianId (FK User), Role (PRIMARY/HANDOFF), StageAtStart, StageAtEnd (nullable), StartedAt, EndedAt (nullable), HandoffReason (SickLeave/ShiftEnd/Reassigned, nullable), LaborHoursLogged (decimal), CommissionSplitPercent (decimal), ApprovedByUserId (FK User)`

> Thay cho `WorkOrder.TechnicianId` đơn — lưu vết đóng góp của từng Technician trên một WorkOrder, làm cơ sở chia hoa hồng công bằng khi reassign giữa chừng. **Tổng `CommissionSplitPercent` của mọi dòng thuộc 1 WorkOrder phải = 100%** trước khi WorkOrder chuyển **Delivered** (guard bắt buộc ở Service Layer). Guard theo trạng thái Technician cũ lúc reassign xem `01-business-spec.md` mục 13. `ApprovedByUserId` luôn là Staff/Admin — Technician không tự khai % của chính mình.

### ChangeRequest
`Id, WorkOrderId (FK), Status (Draft/PendingTechnicianConfirm/Confirmed/Rejected), CostDeltaPercent (decimal), CostDeltaAbsolute (decimal), TimeDeltaHours (decimal), CreatedByTechnicianId (FK User), ConfirmedAt (nullable), ApprovedByUserId (FK User, nullable — chỉ có giá trị khi vượt ngưỡng và Admin duyệt), CreatedAt`

> Không giới hạn số lần phát sinh trong lúc `InRepair`. Ngưỡng auto-approve đã chốt: `CostDeltaPercent ≤ 10–15%` **VÀ** `CostDeltaAbsolute ≤ 1.000.000đ` **VÀ** `TimeDeltaHours ≤ 4 giờ` → auto-approve sau khi Technician xác nhận, không cần Admin. Vượt bất kỳ ngưỡng nào → bắt buộc Admin duyệt (`ApprovedByUserId`) trước khi merge vào Quote. Chi tiết `01-business-spec.md` mục 14.

### Payment
`Id, WorkOrderId (FK), Amount, Method (Cash/Card/VNPay/Momo), ConfirmedByUserId (FK, nullable — chỉ có giá trị nếu Cash/Card do Staff xác nhận), TransactionRef (nullable), GatewayStatus (nullable), PaidDate`

### WorkOrderStatusHistory
`Id, WorkOrderId (FK), FromStatus, ToStatus, ChangedByUserId (FK, nullable — null nếu chuyển do khách duyệt/từ chối qua ApprovalToken), ApprovedViaToken (bool), ChangedAt`

> Bảng này là audit log bắt buộc — mọi transition (kể cả do hệ thống tự động thực hiện) phải ghi vào đây. Xem quy tắc dùng chung ở `01-business-spec.md` §6.

### Notification
`Id, CustomerId (FK), WorkOrderId (FK, nullable), AppointmentId (FK, nullable), Type (QuoteReady/Delayed/StatusChanged/AppointmentConfirmed), Message, EmailSentSuccessfully (bool), IsRead, CreatedAt`

### RefreshToken
`Id, UserId (FK), TokenHash, ExpiresAt, CreatedAt, RevokedAt (nullable)`

> Cặp access token (JWT, sống rất ngắn — phút) + refresh token (chuỗi ngẫu nhiên, sống dài — ngày) thay cho 1 JWT sống lâu duy nhất, giảm thời gian 1 token bị đánh cắp còn dùng được. `TokenHash` lưu SHA-256 của refresh token, không lưu giá trị thô — rò rỉ DB không lộ được token dùng được. `RevokedAt` set khi: (a) token được dùng để refresh (rotation — mỗi lần refresh phát hành token mới, thu hồi token cũ ngay, chặn replay), hoặc (b) người dùng đăng xuất. Access token hết hạn thì FE gọi `POST /auth/refresh-token`; refresh token cũng hết hạn/bị thu hồi thì mới bắt đăng nhập lại.

## Quan hệ chính

- Customer **1–N** Vehicle
- Customer **1–N** Appointment
- Appointment **0–1** WorkOrder (một lịch hẹn check-in tạo tối đa một work order)
- Vehicle **1–N** WorkOrder
- WorkOrder **1–N** QuotationItem
- QuotationItem **N–1** Part (tuỳ chọn — nullable khi là Labor)
- QuotationItem **N–1** ServiceCatalogItem (tuỳ chọn — nguồn đơn giá khi tự sinh từ DiagnosisRecord)
- WorkOrder **1–1** DiagnosisRecord
- WorkOrder **1–N** WorkOrderAssignment (User/Technician **1–N** WorkOrderAssignment)
- WorkOrder **0–N** ChangeRequest
- WorkOrder **0–1** Bay đang chiếm dụng (qua `Bay.CurrentWorkOrderId`)
- WorkOrder **1–1** Payment
- WorkOrder **1–N** WorkOrderStatusHistory
- Customer **1–N** Notification

## Điểm mở chưa chốt (kế thừa từ `luong-chinh-tong-hop-v5.md`)

Ba điểm sau **chưa có số cụ thể** — không tự chọn giá trị mặc định khi tạo migration/seed, phải hỏi người dùng trước:

1. Danh sách cụ thể `ServiceCatalogItem.RequiredBayType` cho từng nhóm hạng mục (hạng mục nào bắt buộc `LiftBay`, hạng mục nào chỉ cần `GeneralBay`).
2. Số lượng `Bay` từng loại xưởng hiện có, để cấu hình seed data ban đầu.
3. Giá trị cụ thể của `qcAndWashBuffer` và `serviceBuffer` trong công thức tính `SystemSuggestedDate` (cố định phút/giờ hay theo %) — xem `01-business-spec.md` mục 12.

## Ghi chú xác thực (ApprovalToken)

Khách vãng lai không có tài khoản `Customer.UserId` vẫn duyệt/từ chối được báo giá thông qua `ApprovalToken` gắn trên `WorkOrder`. Link chứa token gửi qua email khi báo giá được gửi (UC-03), cho phép xác nhận từ xa mà không cần đăng nhập. Token có hạn dùng và chỉ dùng được một lần (`ApprovalTokenUsedAt`).

## Khi thay đổi schema

Mọi migration EF Core làm thay đổi field/bảng ở trên **phải cập nhật lại file này trong cùng lần commit** — không để tài liệu lệch với migration thật. Nếu một task yêu cầu thêm field không có trong danh sách trên, xác nhận lại với người dùng trước khi tạo migration, trừ khi đó là field kỹ thuật thuần tuý (ví dụ `RowVersion` cho concurrency, `CreatedAt`/`UpdatedAt` audit chung) — loại này có thể thêm nhưng nên nêu rõ trong tóm tắt thay đổi để người dùng biết.
