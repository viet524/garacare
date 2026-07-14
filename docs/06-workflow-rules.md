# 06. Workflow Rules — bắt buộc đọc trước MỌI task

File này tồn tại vì một lý do cụ thể: dự án có nghiệp vụ đủ phức tạp (state machine, token, webhook) để một AI agent dễ dàng "tô vẽ" — tự bịa chi tiết nghe có vẻ hợp lý thay vì bám đúng đặc tả. Mỗi quy tắc dưới đây nhắm thẳng vào một kiểu lỗi cụ thể đã biết trước.

## A. Trước khi viết bất kỳ dòng code nào

1. **Xác định task này chạm tới use case/FR nào.** Mở đúng phần liên quan trong `docs/01-business-spec.md` và `docs/02-use-cases.md`. Không code từ trí nhớ/suy đoán về "gara sửa xe nên hoạt động thế nào" — đặc tả đã có sẵn, dùng nó.
2. **Liệt kê lại (trong đầu hoặc trong câu trả lời) chính xác những gì task yêu cầu**, đối chiếu với đặc tả. Nếu có điểm mâu thuẫn hoặc thiếu chi tiết — dừng lại, hỏi người dùng. Không tự chọn phương án "nghe hợp lý nhất".
3. **Nếu task yêu cầu điều gì đó đặc tả nói KHÔNG được làm** (ví dụ "thêm API PUT status chung cho tiện") — không tự động làm theo. Nói rõ với người dùng đây là điều đặc tả cấm và lý do (xem `01-business-spec.md` §6), hỏi lại có thực sự muốn phá quy tắc này không.

## B. Khi implement transition trạng thái

Mọi transition WorkOrder/Appointment PHẢI đi qua đúng khuôn mẫu ở `04-api-contract.md` §"Quy tắc bắt buộc". Checklist bắt buộc cho mỗi transition mới:

- [ ] Endpoint đặt tên theo hành động, không phải `PUT {status}`.
- [ ] Validate trạng thái nguồn hợp lệ trước khi chuyển (trả 400 nếu sai).
- [ ] Validate quyền actor (role + ownership nếu là Customer).
- [ ] Ghi `WorkOrderStatusHistory` (hoặc bảng tương đương).
- [ ] Tạo Notification nếu sự kiện này cần thông báo khách.
- [ ] Có unit test cho: (a) transition hợp lệ thành công, (b) transition từ trạng thái sai bị chặn, (c) actor sai quyền bị chặn.

Nếu một trong các mục trên bị bỏ qua, coi như task chưa xong — không báo "hoàn thành" khi thiếu.

## C. Không tự bịa — danh sách cụ thể những gì KHÔNG được tự thêm

- Entity/field không có trong `03-data-model.md`.
- Endpoint không có trong `04-api-contract.md` (trừ các endpoint CRUD thông thường được liệt kê là "do team quyết định theo convention chuẩn" — vẫn phải giữ đúng nghiệp vụ mô tả kèm theo).
- Actor/quyền hạn không có trong `01-business-spec.md` §2.
- Tích hợp bên thứ ba chưa được yêu cầu (ví dụ tự động tích hợp VNPay thật khi task chỉ nói "làm thanh toán online" — mặc định dùng cổng giả lập nội bộ, xem FR-17b vs FR-17c).
- Tính năng thuộc mức Could khi task đang ở giai đoạn làm Must/Should, trừ khi được yêu cầu rõ.
- Đổi tên trạng thái, đổi thứ tự state machine, gộp/tách bước trong luồng WorkOrder — đây là hợp đồng nghiệp vụ, không phải chi tiết implementation.

Khi cảm thấy "chắc thêm cái này cho đầy đủ" hoặc "chắc user cũng muốn cái này" — đó chính là tín hiệu để dừng lại và hỏi, không phải để tự tin làm luôn.

## D. Khi thông tin thiếu hoặc mơ hồ

Ưu tiên theo thứ tự:

1. Tìm trong `docs/` xem có trả lời chưa (đọc kỹ trước khi kết luận "không có").
2. Nếu thật sự không có và ảnh hưởng tới nghiệp vụ (ví dụ: giá trị mặc định `DiscountPercent` là bao nhiêu %, hạn `ApprovalToken` chính xác bao nhiêu giờ nếu cần con số cụ thể để code) → hỏi người dùng, đừng chọn một con số ngẫu nhiên rồi coi như đã quyết.
3. Nếu là chi tiết kỹ thuật thuần tuý không ảnh hưởng nghiệp vụ (ví dụ tên biến, cấu trúc thư mục con) → có thể tự quyết theo convention ở `07`/`08`, nhưng nêu rõ giả định đó trong câu trả lời để người dùng có thể chỉnh lại.

## E. Khi sửa schema hoặc migration

1. Cập nhật `docs/03-data-model.md` **trong cùng lần thay đổi** — không tách ra làm sau.
2. Kiểm tra migration nhắm đúng SQL Server provider (không dùng cú pháp/kiểu dữ liệu chỉ SQLite hỗ trợ).
3. Nếu thay đổi ảnh hưởng tới field đã được dùng trong `WorkOrderStatusHistory`/`Notification` (audit trail), cân nhắc kỹ về backward compatibility — dữ liệu audit không nên bị phá vỡ bởi migration.

## F. Testing

- Mọi Service method xử lý transition trạng thái: bắt buộc có unit test (xem checklist B).
- Mọi validate nghiệp vụ ở §5 của `01-business-spec.md` (ví dụ "không cho sửa QuotationItem sau khi Approved"): bắt buộc có test khẳng định hành vi bị chặn đúng như mô tả — không chỉ test "happy path".
- Test đặt trong `GaraCare.Tests`, theo convention ở `07-backend-conventions.md`.

## G. Khi báo cáo kết quả cho người dùng

- Nói rõ đã implement đúng những gì (mã use case/FR liên quan), không mô tả mơ hồ kiểu "đã xong phần thanh toán" — cụ thể "đã xong UC-06 nhánh tiền mặt, UC-14 nhánh online chưa làm".
- Nếu có giả định đã tự đưa ra (mục D.3), liệt kê lại để người dùng biết và có thể chỉnh.
- Nếu có phần đặc tả không rõ và đã bỏ qua thay vì đoán, nói rõ đã bỏ qua phần nào và tại sao — không im lặng bỏ qua.

## H. Không tự sáng tạo UI/UX ngoài mô tả tối thiểu

Theo SRS, `customer.html`/`staff.html` (hoặc trang Next.js tương đương) ưu tiên chức năng hơn giao diện đẹp ở giai đoạn đầu. Không tự thêm nhiều màn hình, luồng UX phụ, hoặc animation phức tạp khi task chỉ yêu cầu "làm chức năng X chạy được" — trừ khi người dùng yêu cầu về mặt thiết kế UI.
