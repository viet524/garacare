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
`Id, Username, PasswordHash, FullName, Phone, Email, Role (Customer/Staff/Technician/Admin)`

### Customer
`Id, FullName, Phone, Email, Address, UserId (FK, nullable — null nếu khách vãng lai)`

### Vehicle
`Id, CustomerId (FK), LicensePlate, Brand, Model, Year`

### Appointment
`Id, CustomerId (FK), VehicleId (FK), ScheduledDate, ScheduledTimeSlot, Status (Booked/CheckedIn/Cancelled/NoShow), DiscountPercent, CreatedAt, IsLate (bool), LateNotifiedStaffAt (nullable)`

### WorkOrder
`Id, VehicleId (FK), AppointmentId (FK, nullable), CreatedByUserId (FK), Status, ReceivedDate, InitialDescription, DiagnosisNote, TotalAmount, DiscountPercent, EstimatedCompletionDate, IsDelayed, QuoteSentAt, ReminderSentAt, NeedsFollowUpCall, CompletedDate, ApprovalToken, ApprovalTokenExpiresAt, ApprovalTokenUsedAt (nullable)`

> `ApprovalToken`: chuỗi ngẫu nhiên (khuyến nghị sinh bằng `RandomNumberGenerator`/GUID-based, không đoán được), sinh khi gửi báo giá, dùng để khách vãng lai duyệt/từ chối qua link không cần đăng nhập.

### QuotationItem
`Id, WorkOrderId (FK), PartId (FK, nullable), Type (Part/Labor), Description, Quantity, UnitPrice, IsApproved, IsUsed`

### Part
`Id, Name, SKU, UnitPrice, StockQuantity`

### Payment
`Id, WorkOrderId (FK), Amount, Method (Cash/Card/VNPay/Momo), ConfirmedByUserId (FK, nullable — chỉ có giá trị nếu Cash/Card do Staff xác nhận), TransactionRef (nullable), GatewayStatus (nullable), PaidDate`

### WorkOrderStatusHistory
`Id, WorkOrderId (FK), FromStatus, ToStatus, ChangedByUserId (FK, nullable — null nếu chuyển do khách duyệt/từ chối qua ApprovalToken), ApprovedViaToken (bool), ChangedAt`

> Bảng này là audit log bắt buộc — mọi transition (kể cả do hệ thống tự động thực hiện) phải ghi vào đây. Xem quy tắc dùng chung ở `01-business-spec.md` §6.

### Notification
`Id, CustomerId (FK), WorkOrderId (FK, nullable), AppointmentId (FK, nullable), Type (QuoteReady/Delayed/StatusChanged/AppointmentConfirmed), Message, EmailSentSuccessfully (bool), IsRead, CreatedAt`

## Quan hệ chính

- Customer **1–N** Vehicle
- Customer **1–N** Appointment
- Appointment **0–1** WorkOrder (một lịch hẹn check-in tạo tối đa một work order)
- Vehicle **1–N** WorkOrder
- WorkOrder **1–N** QuotationItem
- QuotationItem **N–1** Part (tuỳ chọn — nullable khi là Labor)
- WorkOrder **1–1** Payment
- WorkOrder **1–N** WorkOrderStatusHistory
- Customer **1–N** Notification

## Ghi chú xác thực (ApprovalToken)

Khách vãng lai không có tài khoản `Customer.UserId` vẫn duyệt/từ chối được báo giá thông qua `ApprovalToken` gắn trên `WorkOrder`. Link chứa token gửi qua email khi báo giá được gửi (UC-03), cho phép xác nhận từ xa mà không cần đăng nhập. Token có hạn dùng và chỉ dùng được một lần (`ApprovalTokenUsedAt`).

## Khi thay đổi schema

Mọi migration EF Core làm thay đổi field/bảng ở trên **phải cập nhật lại file này trong cùng lần commit** — không để tài liệu lệch với migration thật. Nếu một task yêu cầu thêm field không có trong danh sách trên, xác nhận lại với người dùng trước khi tạo migration, trừ khi đó là field kỹ thuật thuần tuý (ví dụ `RowVersion` cho concurrency, `CreatedAt`/`UpdatedAt` audit chung) — loại này có thể thêm nhưng nên nêu rõ trong tóm tắt thay đổi để người dùng biết.
