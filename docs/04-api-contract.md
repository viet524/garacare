# 04. API Contract

Quy tắc nền tảng: **mọi chuyển trạng thái đi qua endpoint hành động riêng**, không có endpoint chung kiểu `PUT .../{id} {status: ...}`. Xem lý do ở `01-business-spec.md` §6. Danh sách dưới đây là **toàn bộ endpoint chuyển trạng thái đã được đặc tả** — không tự thêm/đổi tên khi chưa hỏi.

## Endpoint chuyển trạng thái (action-based)

| Chuyển trạng thái | Actor kích hoạt | Endpoint |
| --- | --- | --- |
| Booked → CheckedIn | Staff | `POST /appointments/{id}/check-in` |
| Booked → Cancelled | Customer hoặc Staff | `POST /appointments/{id}/cancel` |
| Booked → NoShow | Staff | `POST /appointments/{id}/mark-no-show` |
| Received → Diagnosing | Technician | `POST /workorders/{id}/start-diagnosis` |
| Diagnosing → QuotePending | Staff | `POST /workorders/{id}/send-quote` |
| QuotePending → InRepair | Customer | `POST /workorders/{id}/approve-quote` |
| QuotePending → Cancelled | Customer | `POST /workorders/{id}/reject-quote` |
| InRepair → WaitingParts | Technician | `POST /workorders/{id}/mark-waiting-parts` |
| WaitingParts → InRepair | Technician | `POST /workorders/{id}/resume-repair` |
| InRepair → Completed | Technician | `POST /workorders/{id}/mark-completed` |
| Completed → Delivered (tiền mặt) | Staff | `POST /workorders/{id}/record-cash-payment` |
| Completed → Delivered (online) | Hệ thống (webhook) | `POST /payments/webhook` |
| — nhắc/gắn cờ gọi điện | Hệ thống (job nền) | Không có endpoint — chạy nền định kỳ |

`{id}` = id nội bộ. Với `approve-quote`/`reject-quote` khi khách vào qua magic link (không đăng nhập), cần biến thể xác thực bằng token thay vì JWT — ví dụ `POST /workorders/quotes/{token}/approve` và `POST /workorders/quotes/{token}/reject` (đặt tên chính xác nên thống nhất với team trước khi code, đây chỉ là gợi ý theo đúng tinh thần "một hành động = một endpoint").

## Quy tắc bắt buộc trong mỗi handler chuyển trạng thái

Service Layer thực hiện đúng thứ tự sau (một hàm dùng chung, không rải logic ra nhiều Controller):

1. Load WorkOrder/Appointment, kiểm tra trạng thái hiện tại có hợp lệ để thực hiện transition này không → không hợp lệ trả **400 Bad Request** với message rõ ràng.
2. Kiểm tra quyền của actor gọi request (đúng vai trò, đúng chủ sở hữu nếu là Customer) → không đủ quyền trả **403 Forbidden**.
3. Thực hiện side effect nghiệp vụ của action đó (ví dụ trừ kho, tính phí kiểm tra, xác thực chữ ký webhook...).
4. Đổi `Status`.
5. Ghi 1 dòng vào `WorkOrderStatusHistory` (hoặc bảng tương đương cho Appointment nếu có).
6. Tạo `Notification` + gửi email nếu sự kiện này cần thông báo khách (theo UC-12).

## Các endpoint khác (không phải chuyển trạng thái)

Đây là các nhóm chức năng còn lại — tên cụ thể (route, verb) do team quyết định theo convention REST chuẩn ở `07-backend-conventions.md`, miễn giữ đúng nghiệp vụ dưới đây:

- Auth: đăng ký Customer, đăng nhập trả JWT.
- CRUD Customer/Vehicle (tra cứu theo số điện thoại là thao tác quan trọng trong UC-02).
- Tạo WorkOrder walk-in (UC-02) — không đi qua Appointment.
- Thêm/sửa `QuotationItem` (chỉ khi WorkOrder ở `Diagnosing`/`QuotePending` và **chưa** Approved — xem ràng buộc ở `01-business-spec.md` §5).
- Đặt lịch hẹn (UC-09).
- `GET /workorders/{id}/invoice` — content negotiation JSON/XML qua `Accept` header (FR-18).
- `GET /odata/WorkOrders` — hỗ trợ `$filter`, `$orderby`, `$top`, `$skip` (FR-20). Nếu OData khó cấu hình trong giai đoạn đầu, phương án dự phòng là query string filter thường (`?status=&from=&to=`) — chỉ dùng khi được yêu cầu chuyển hướng.
- Báo cáo doanh thu (Admin only, FR-21).
- `GET` trang tiến trình sửa xe cho Customer (UC-11) — trả kèm `EstimatedCompletionDate` + danh sách Notification liên quan.

## Response convention

- Lỗi nghiệp vụ (transition không hợp lệ, token hết hạn, tồn kho không đủ...) → **400 Bad Request** kèm message tiếng Việt hoặc mã lỗi rõ ràng, không trả lỗi chung chung "Something went wrong".
- Không đủ quyền → **403 Forbidden**.
- Không đăng nhập / token JWT không hợp lệ → **401 Unauthorized**.
- Không tìm thấy resource → **404 Not Found**.
- Không tự thêm status code hoặc response shape khác với convention đã thống nhất trong `07-backend-conventions.md` khi chưa hỏi.
