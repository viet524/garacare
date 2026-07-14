# 07. Backend Conventions — ASP.NET Core (.NET 8)

## Layering (bắt buộc tách lớp, không gộp logic vào Controller)

```
GaraCare.Api             Controllers mỏng — chỉ nhận request, gọi Service, trả response.
                          Không chứa business logic, không chứa validate nghiệp vụ.
GaraCare.Application      Services (business logic), DTOs (Request/Response tách riêng
                          khỏi Entity), Interfaces (IWorkOrderService, IEmailService...).
GaraCare.Domain           Entities, Enums, hằng số trạng thái, quy tắc state machine
                          thuần (không phụ thuộc EF Core/ASP.NET).
GaraCare.Infrastructure   DbContext, Migrations, Repository implementation,
                          EmailService (MailKit), PaymentGateway client/mock.
GaraCare.Tests            Unit test cho Service layer — ưu tiên test Application layer,
                          không cần spin up toàn bộ API để test business rule.
```

**Lý do tách lớp này quan trọng với dự án cụ thể này**: state machine của WorkOrder/Appointment phải nằm ở đúng MỘT chỗ (Application layer, dạng service dùng chung — xem `04-api-contract.md`), không rải ra nhiều Controller action. Nếu logic transition xuất hiện lặp lại ở nhiều nơi, đó là dấu hiệu vi phạm convention này.

## Controller

- Attribute routing theo resource: `[Route("api/workorders")]`.
- Action đặt tên theo hành động nghiệp vụ khi là transition (`start-diagnosis`, `send-quote`...), khớp đúng bảng ở `04-api-contract.md`.
- `[Authorize(Roles = "...")]` áp đúng role được phép cho từng action — đối chiếu `01-business-spec.md` §2 và §5.
- Không chứa `if/else` kiểm tra trạng thái nghiệp vụ trong Controller — việc đó thuộc Service.

## DTO

- Tách riêng Request DTO và Response DTO, không dùng thẳng Entity làm response (tránh lộ field nội bộ như `PasswordHash`, `ApprovalToken` không cần thiết).
- Validate input bằng DataAnnotations trên Request DTO (`[Required]`, `[Range]`...). Validate nghiệp vụ (thứ tự trạng thái, quyền sở hữu...) luôn ở Service layer, không phải DataAnnotations.

## Service Layer

- Interface + implementation, inject qua constructor (DI chuẩn ASP.NET Core).
- Method xử lý transition trả về kết quả rõ ràng (ví dụ `Result<T>` hoặc throw exception nghiệp vụ riêng như `InvalidTransitionException`) để Controller map đúng status code, không dùng `bool` trả về mập mờ.
- `IEmailService` — interface riêng, implementation dùng MailKit qua SMTP, inject vào `NotificationService`. Lỗi gửi email không được ném exception làm rollback transaction chính (xem `01-business-spec.md` §9).

## EF Core / SQL Server

- Dùng SQL Server provider (`Microsoft.EntityFrameworkCore.SqlServer`), không dùng gói SQLite.
- Enum nghiệp vụ lưu dạng string qua `HasConversion<string>()` — xem `03-data-model.md`.
- Migration: đặt tên rõ ràng theo thay đổi (`AddApprovalTokenToWorkOrder`, không đặt tên chung chung `Update1`).
- Seed data tối thiểu để demo được luồng chính (ít nhất 1 user mỗi role, vài Part, vài WorkOrder ở các trạng thái khác nhau).

## Error handling

- Dùng exception nghiệp vụ riêng (namespace `GaraCare.Application.Exceptions`) cho các lỗi có thể đoán trước (invalid transition, token expired, insufficient permission) — middleware chung map sang đúng status code (400/403/404) theo convention ở `04-api-contract.md`.
- Không nuốt exception im lặng — log lại, đặc biệt với lỗi webhook (chữ ký sai) cần log cảnh báo rõ ràng để điều tra sau.

## Background jobs (UC-13, UC-15)

- Dùng cơ chế job nền chuẩn của .NET (`IHostedService`/`BackgroundService`, hoặc Hangfire/Quartz nếu team quyết định dùng — nêu rõ lựa chọn trong PR nếu thêm package mới).
- Logic kiểm tra "quá 24h/48h chưa duyệt", "quá giờ hẹn 15 phút" nằm trong Service layer, job chỉ gọi service, không tự chứa business rule.

## Testing

- Unit test Service layer bằng xUnit (hoặc framework team đã chọn), mock Repository/DbContext (in-memory provider hoặc mock interface).
- Bắt buộc test cho mọi checklist ở `06-workflow-rules.md` §B và §F.
