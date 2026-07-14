# CLAUDE.md — GaraCare

Đây là file đầu tiên Claude Code phải đọc khi mở project này. Nó KHÔNG chứa toàn bộ đặc tả — nó là bản đồ, trỏ tới các file trong `docs/` chứa sự thật (source of truth). Nhiệm vụ của Claude trong project này là **cài đặt đúng những gì đã được đặc tả**, không phải sáng tạo hay "làm cho hợp lý hơn".

## 0. Nguyên tắc tối cao (đọc trước tất cả)

> **Nếu một chi tiết không có trong `docs/`, KHÔNG được tự bịa ra. Dừng lại và hỏi người dùng.**

Đây là lý do bộ tài liệu này tồn tại: dự án có một luồng nghiệp vụ khá phức tạp (state machine của WorkOrder/Appointment, cơ chế ApprovalToken, webhook thanh toán). Rất dễ để một AI agent "tô vẽ" thêm — tự thêm field, tự đổi tên endpoint cho "đẹp hơn", tự cho phép một hành động mà đặc tả cấm, tự implement tính năng chưa được yêu cầu ở giai đoạn hiện tại. Mỗi lần như vậy là một lỗi nghiệp vụ thật, không phải chi tiết vặt. Xem `docs/06-workflow-rules.md` để biết quy trình bắt buộc trước khi viết bất kỳ dòng code nào.

## 1. Dự án là gì

GaraCare là hệ thống Web API quản lý vòng đời một lượt sửa xe/thiết bị tại gara: tiếp nhận → chẩn đoán → báo giá → khách duyệt giá → sửa chữa → thanh toán → giao xe. Điểm giá trị cốt lõi: **khách hàng phải chủ động duyệt báo giá trước khi gara được phép sửa** — đây không phải một hệ thống CRUD đổi trạng thái tuỳ ý.

Đọc chi tiết đầy đủ tại `docs/01-business-spec.md` trước khi động vào bất kỳ luồng nào liên quan WorkOrder hoặc Appointment.

## 2. Tech stack (bắt buộc, không đổi khi chưa hỏi)

| Layer | Công nghệ |
| --- | --- |
| Backend | ASP.NET Core Web API — .NET 8 |
| Frontend | Next.js (App Router), TypeScript |
| Database | **SQL Server** (không phải SQLite) |
| ORM | Entity Framework Core (SQL Server provider) |
| Auth | JWT Bearer token, mật khẩu hash bằng BCrypt |
| Email | MailKit qua SMTP |
| API style | RESTful theo hành động (action-based endpoints), có OData cho list/filter |

> ⚠️ **Lưu ý khác biệt với tài liệu BA gốc:** tài liệu SRS gốc (`GaraCare_BA_SRS_v3.docx`, mục 6 và mục 9) ghi công nghệ DB là **SQLite** (dự án môn học). Theo yêu cầu thực tế của chủ dự án, DB chính thức của repo này là **SQL Server**. Mọi migration, connection string, và kiểu dữ liệu EF Core phải nhắm tới SQL Server (ví dụ dùng `datetime2`, không dùng kiểu dữ liệu chỉ SQLite hỗ trợ). Nếu tài liệu khác trong `docs/` nói SQLite, đó là do trích nguyên văn SRS gốc — SQL Server luôn được ưu tiên khi có mâu thuẫn.

## 3. Cấu trúc thư mục dự kiến

```
/backend
  /GaraCare.Api            → Controllers, Program.cs, appsettings
  /GaraCare.Application     → Services, DTOs, Interfaces (business logic ở đây)
  /GaraCare.Domain          → Entities, Enums, State machine rules
  /GaraCare.Infrastructure  → EF Core DbContext, Migrations, Repositories, EmailService
  /GaraCare.Tests           → Unit test cho Service layer (bắt buộc cho mọi transition)

/frontend                  → Next.js App Router, kiến trúc MVVM (theo yêu cầu chủ dự án)
  /app
    /staff        → route + nối ViewModel↔View, KHÔNG chứa JSX hiển thị trực tiếp
    /customer     → Customer portal (đặt lịch, duyệt giá, xem tiến trình)
    /quotes/[token] → route công khai (UC-04), không yêu cầu đăng nhập
  /viewmodels     → Model của MVVM: hook `useXxxViewModel` — state, gọi /lib/api/*,
                    không render JSX, không import component
  /components     → View của MVVM: component thuần hiển thị, nhận toàn bộ dữ liệu/
                    hành động qua props từ ViewModel, không tự gọi API
  /lib            → API client (/lib/api, một file mỗi resource), auth helpers (/lib/auth)
  /types          → Model (DTO) của MVVM — type khớp 1-1 với Response DTO backend

/docs                      → toàn bộ đặc tả, đọc trước khi code (thư mục này)
```

> Quy ước MVVM ở frontend: `app/**` chỉ định tuyến, resolve route params, và nối ViewModel
> với View — không chứa markup. `viewmodels/**` là nơi duy nhất gọi `/lib/api/*` và giữ
> state; trả về plain object cho View, không biết gì về JSX. `components/**` là View —
> presentational thuần, không gọi API, không chứa business logic. `types/**` là Model —
> khớp DTO backend, không phải Entity.

Nếu cấu trúc thật của repo khác đi (ví dụ đổi tên project), **cập nhật lại file này** — đừng để CLAUDE.md nói dối về cấu trúc thật.

## 4. Bản đồ tài liệu (`docs/`)

Đọc theo đúng thứ tự khi bắt đầu một task mới liên quan tới phần đó:

| File | Nội dung | Đọc khi nào |
| --- | --- | --- |
| `docs/01-business-spec.md` | Actors, quy trình nghiệp vụ, state machine đầy đủ, ràng buộc nghiệp vụ | Bất kỳ task nào đụng tới WorkOrder, Appointment, Quotation, Payment |
| `docs/02-use-cases.md` | Đặc tả chi tiết từng use case (UC-01 → UC-15): luồng chính, luồng ngoại lệ, kết quả | Trước khi implement 1 use case cụ thể |
| `docs/03-data-model.md` | ERD, entity, field, quan hệ, ghi chú cho SQL Server | Trước khi tạo/sửa migration hoặc entity |
| `docs/04-api-contract.md` | Danh sách endpoint theo hành động (action-based), request/response shape | Trước khi tạo Controller hoặc gọi API từ FE |
| `docs/05-functional-requirements.md` | Bảng FR-xx đầy đủ kèm mức ưu tiên MoSCoW | Khi cần biết cái gì Must/Should/Could |
| `docs/06-workflow-rules.md` | **Quy trình bắt buộc AI phải theo khi làm task** — đọc trước khi code | Trước MỌI task, không ngoại lệ |
| `docs/07-backend-conventions.md` | Convention ASP.NET Core: layer, naming, validation, error handling | Khi viết code backend |
| `docs/08-frontend-conventions.md` | Convention Next.js: cấu trúc, gọi API, state, style | Khi viết code frontend |
| `docs/09-non-functional-and-nfr.md` | Bảo mật, hiệu năng, audit log, phân quyền | Khi review một tính năng đã "chạy được" |

## 5. Quy tắc bất di bất dịch (tóm tắt — bản đầy đủ ở `docs/06-workflow-rules.md`)

1. **Không có API kiểu `PUT /workorders/{id}` với body `{status: "..."}`.** Mọi chuyển trạng thái đi qua endpoint hành động riêng (`POST /workorders/{id}/approve-quote`, v.v.) — danh sách đầy đủ ở `docs/04-api-contract.md`. Đây là ràng buộc kiến trúc, không phải gợi ý.
2. **Mọi validate thứ tự chuyển trạng thái nằm ở Service Layer**, không phải Controller, không phải chỉ ở frontend.
3. **Mỗi lần đổi Status phải ghi 1 dòng vào `WorkOrderStatusHistory`.** Không có transition nào được phép bỏ qua bước này.
4. **Không cho sửa/xoá `QuotationItem` sau khi khách đã Approved.**
5. **Trạng thái thanh toán online chỉ được đổi bởi hệ thống qua webhook đã xác thực chữ ký** — không có endpoint nào cho phép người dùng tự đánh dấu "đã thanh toán online".
6. **Không tự thêm entity, field, endpoint không có trong `docs/`.** Nếu thấy cần thiết để code "chạy được" hoặc "đẹp hơn", dừng lại và hỏi thay vì tự quyết.
7. **Không tự nâng cấp phạm vi** — ví dụ không tự tích hợp VNPay/Momo thật (FR-17c, mức Could) khi task đang yêu cầu cổng thanh toán giả lập (FR-17b, mức Must) nếu không được yêu cầu rõ.
8. **Khi task mơ hồ hoặc thiếu chi tiết, hỏi lại người dùng** thay vì đoán và code luôn — kể cả khi việc đoán "nhìn có vẻ hợp lý".

## 6. Việc cần làm khi bắt đầu một task

Xem checklist đầy đủ ở `docs/06-workflow-rules.md`. Tóm tắt nhanh:

1. Xác định task đụng tới use case/FR nào → đọc đúng phần liên quan trong `docs/`.
2. Nếu task yêu cầu điều gì đó mâu thuẫn hoặc không có trong đặc tả → hỏi lại trước khi code.
3. Code theo đúng layer convention (`docs/07`, `docs/08`).
4. Với mọi transition trạng thái mới/sửa: viết kèm unit test.
5. Nếu có thay đổi schema: cập nhật `docs/03-data-model.md` trong cùng lần thay đổi, không để tài liệu lệch code.
