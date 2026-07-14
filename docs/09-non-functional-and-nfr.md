# 09. Yêu cầu phi chức năng (NFR)

| Nhóm | Yêu cầu |
| --- | --- |
| Bảo mật | Xác thực JWT Bearer token; mật khẩu hash BCrypt; phân quyền theo Role ở **tầng API**, không chỉ ở UI — FE ẩn nút không thay thế được check `[Authorize]` ở backend. |
| Toàn vẹn dữ liệu | Không cho sửa/xoá `QuotationItem` sau khi khách đã Approved; validate chuyển trạng thái đúng thứ tự ở tầng Service, không dựa vào client. |
| Hiệu năng | API danh sách work order hỗ trợ phân trang và lọc (OData `$top`, `$skip`, `$filter`) — tránh trả toàn bộ dữ liệu. |
| Khả năng mở rộng | Service Layer tách khỏi Controller để dễ unit test và tái sử dụng logic khi thêm client (mobile app) sau này. |
| Audit log | Ghi lại lịch sử đổi trạng thái work order (`WorkOrderStatusHistory`) để tra cứu khi có tranh chấp — không transition nào được bỏ qua bước ghi log này. |

## Ghi chú áp dụng khi review code

Khi review một tính năng "đã chạy được", kiểm tra thêm các điểm sau trước khi coi là hoàn thành — đây là những chỗ dễ bị bỏ sót vì không lỗi ngay khi test happy-path:

- [ ] `[Authorize(Roles=...)]` có đúng với actor được phép theo `01-business-spec.md` §5 không?
- [ ] Customer chỉ thao tác được trên WorkOrder/Appointment/Payment **của chính mình** — có test case 403 cho trường hợp cố truy cập dữ liệu người khác không (UC-04, UC-11)?
- [ ] Danh sách work order có phân trang/filter, không trả full bảng?
- [ ] Transition mới có ghi `WorkOrderStatusHistory` không?
- [ ] Nếu liên quan tiền/kho: có test case số liệu sai lệch (tiền mặt không khớp, tồn kho không đủ) không, đúng theo luồng ngoại lệ đã đặc tả (cảnh báo, không chặn cứng)?
