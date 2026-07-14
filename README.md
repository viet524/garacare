# GaraCare

Hệ thống Web API quản lý vòng đời một lượt sửa xe/thiết bị tại gara: **tiếp nhận → chẩn đoán → báo giá → khách duyệt giá → sửa chữa → thanh toán → giao xe**.

Điểm giá trị cốt lõi: khách hàng phải **chủ động duyệt báo giá** trước khi gara được phép sửa — đây không phải một hệ thống CRUD đổi trạng thái tuỳ ý. Mọi chuyển trạng thái đi qua endpoint hành động riêng (`POST /workorders/{id}/approve-quote`, ...), được validate ở Service layer và ghi log vào `WorkOrderStatusHistory`.

Đặc tả nghiệp vụ đầy đủ nằm ở [`docs/`](docs) — đọc [`CLAUDE.md`](CLAUDE.md) trước để biết bản đồ tài liệu.

## Tech stack

| Layer | Công nghệ |
| --- | --- |
| Backend | ASP.NET Core Web API — .NET 8 |
| Frontend | Next.js (App Router), TypeScript — kiến trúc MVVM |
| Database | SQL Server |
| ORM | Entity Framework Core (SQL Server provider) |
| Auth | JWT Bearer token, mật khẩu hash bằng BCrypt |
| Email | MailKit qua SMTP |

## Cấu trúc dự án

```
/backend
  /GaraCare.Api            → Controllers, Program.cs, appsettings
  /GaraCare.Application    → Services, DTOs, Interfaces
  /GaraCare.Domain         → Entities, Enums, state machine rules
  /GaraCare.Infrastructure → EF Core DbContext, Migrations, EmailService
  /GaraCare.Tests          → Unit test cho Service layer

/frontend                  → Next.js App Router, kiến trúc MVVM
  /app                     → routing + nối ViewModel↔View
  /viewmodels              → state, gọi /lib/api/*
  /components              → View thuần hiển thị
  /lib                     → API client, auth helpers
  /types                   → type khớp DTO backend

/docs                      → đặc tả nghiệp vụ, data model, API contract, convention
```

## Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) 20+ và npm
- SQL Server (LocalDB, SQL Server Express, hoặc instance đầy đủ)
- (Tuỳ chọn) [EF Core CLI tools](https://learn.microsoft.com/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

## Cài đặt Backend

```bash
cd backend
dotnet restore
```

1. Mở `GaraCare.Api/appsettings.json`, chỉnh `ConnectionStrings:DefaultConnection` trỏ đúng SQL Server của bạn, và đổi `Jwt:Key` sang một chuỗi bí mật riêng (tối thiểu 32 ký tự). Nếu cần gửi email thật, điền thêm mục `Email`.
2. Tạo database từ migration có sẵn:

   ```bash
   dotnet ef database update --project GaraCare.Infrastructure --startup-project GaraCare.Api
   ```

3. Chạy API:

   ```bash
   dotnet run --project GaraCare.Api
   ```

   Swagger UI mặc định ở `https://localhost:<port>/swagger` khi chạy ở môi trường Development.

## Cài đặt Frontend

```bash
cd frontend
npm install
cp .env.local.example .env.local
```

Chỉnh `NEXT_PUBLIC_API_BASE_URL` trong `.env.local` trỏ đúng địa chỉ backend đang chạy, rồi:

```bash
npm run dev
```

Frontend chạy ở `http://localhost:3000`, gồm 3 khu vực route: `/staff`, `/customer`, và `/quotes/[token]` (trang duyệt báo giá công khai qua link email, không cần đăng nhập).

## Quy trình làm việc

Trước khi thêm bất kỳ tính năng nào, đọc [`CLAUDE.md`](CLAUDE.md) và [`docs/06-workflow-rules.md`](docs/06-workflow-rules.md) — dự án có state machine nghiệp vụ khá chặt (WorkOrder/Appointment), không tự thêm entity/endpoint/trạng thái ngoài đặc tả.
