# 08. Frontend Conventions — Next.js

## Cấu trúc 2 portal dùng chung 1 API

Theo đặc tả (SRS mục 3.1, mục 8), hệ thống cần 2 giao diện phân quyền qua Role, gọi chung một bộ API backend:

- **`/app/staff`** — nội bộ: Staff/Technician/Admin (tiếp nhận, check-in, báo giá, cập nhật tiến độ, ghi nhận thanh toán, báo cáo).
- **`/app/customer`** — Customer portal: đặt lịch, xem/duyệt/từ chối báo giá, xem tiến trình sửa xe (UC-11), bao gồm cả luồng **không cần đăng nhập** qua `ApprovalToken` (magic link, UC-04) — route riêng dạng `/app/quotes/[token]` (ngoài 2 portal có auth), không được yêu cầu đăng nhập ở route này.

Không gộp 2 portal thành 1 giao diện dùng role-check ẩn/hiện tuỳ tiện — giữ tách khu vực route rõ ràng để phân quyền dễ audit.

## Gọi API

- Tập trung API client trong `/lib/api/` — mỗi resource một file (`workorders.ts`, `appointments.ts`, `payments.ts`...), không gọi `fetch` rải rác trong component.
- Endpoint và request/response shape phải khớp đúng `04-api-contract.md`. Nếu backend chưa có endpoint cần dùng, không tự "giả lập" bằng cách gọi sai endpoint hoặc mock cứng dữ liệu trong lâu dài — báo lại cho người dùng là thiếu endpoint.
- Action-based endpoint (transition trạng thái) gọi đúng tên hành động, không tự gộp thành một hàm `updateStatus(id, status)` chung ở phía FE — phá đúng nguyên tắc kiến trúc đã chọn.
- JWT lưu theo cơ chế an toàn hợp lý cho Next.js (httpOnly cookie ưu tiên hơn localStorage cho token nếu có thể — nêu rõ lựa chọn khi implement, vì đây là quyết định bảo mật cần người dùng biết).

## Type

- `/types/` định nghĩa type khớp 1-1 với DTO backend (không phải Entity). Khi backend đổi DTO, cập nhật type tương ứng cùng lúc.

## State

- Ưu tiên React state/server components của Next.js App Router cho dữ liệu đơn giản; dùng thư viện state (React Query/SWR...) nếu cần cache dữ liệu qua lại giữa các trang — chỉ thêm dependency mới khi thực sự cần, không thêm "cho chắc".

## UI

- Giai đoạn đầu ưu tiên **chức năng đúng luồng hơn giao diện đẹp** (theo đúng tinh thần SRS mục 10 — rủi ro/phương án dự phòng cho Ch7 JS Client). Không tự thêm nhiều màn hình phụ, animation, hoặc redesign khi task chỉ yêu cầu "làm chức năng X chạy trên FE".
- Với trang tiến trình sửa xe (UC-11) và trang duyệt giá qua token (UC-04): hiển thị đúng và đủ dữ liệu nghiệp vụ cần thiết (trạng thái, ETA, lý do delay, danh sách hạng mục, tổng tiền) — đây là màn hình giá trị cốt lõi của hệ thống, cần làm đúng trước khi làm đẹp.

## Lỗi & trạng thái tải

- Hiển thị đúng thông báo lỗi nghiệp vụ trả về từ backend (400 với message rõ ràng) thay vì thông báo lỗi chung chung — đặc biệt quan trọng với luồng token hết hạn/không hợp lệ (UC-04) và lỗi thanh toán (UC-06/UC-14), vì đây là các điểm khách hàng trực tiếp nhìn thấy.
