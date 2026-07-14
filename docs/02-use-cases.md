# 02. Use Cases chi tiết

Đọc phần use case tương ứng **trước khi** implement, kể cả khi đã đọc `01-business-spec.md`. File đó cho bức tranh tổng thể; file này cho luồng chính/ngoại lệ chi tiết theo từng chức năng — implement thiếu một nhánh ngoại lệ ở đây là bug nghiệp vụ, không phải thiếu sót nhỏ.

## Danh sách use case

| Mã | Tên | Actor chính |
| --- | --- | --- |
| UC-01 | Đăng ký / Đăng nhập | Customer, Staff, Technician, Admin |
| UC-02 | Tiếp nhận xe (tạo work order) | Staff |
| UC-03 | Chẩn đoán & lập báo giá | Technician, Staff |
| UC-04 | Khách duyệt/từ chối báo giá | Customer |
| UC-05 | Cập nhật tiến độ sửa chữa | Technician |
| UC-06 | Ghi nhận thanh toán & giao xe | Staff |
| UC-07 | Tìm kiếm/lọc work order (OData) | Staff, Admin |
| UC-08 | Xem báo cáo doanh thu | Admin |
| UC-09 | Đặt lịch trước (có ưu đãi) | Customer |
| UC-10 | Check-in từ lịch hẹn thành work order | Staff |
| UC-11 | Xem tiến trình sửa xe & ETA | Customer |
| UC-12 | Hệ thống gửi thông báo (in-app + email) | System |
| UC-13 | Nhắc nhở & gắn cờ gọi điện khi báo giá quá hạn | System + Staff |
| UC-14 | Thanh toán online qua cổng thanh toán (webhook) | Customer, Payment Gateway |
| UC-15 | Xử lý khách trễ hẹn (gọi điện, dời lịch, NoShow) | System + Staff |

---

## UC-02. Tiếp nhận xe (tạo work order)

**Điều kiện tiên quyết**: Khách hàng & xe đã tồn tại (hoặc tạo mới ngay bước này).

**Luồng chính**:
1. Staff tra cứu khách theo số điện thoại; chưa có thì tạo mới.
2. Staff chọn/thêm xe gắn với khách hàng.
3. Staff nhập mô tả sự cố ban đầu do khách khai báo.
4. Hệ thống tạo WorkOrder mới, trạng thái mặc định **Received**.

**Ngoại lệ**: Nếu biển số xe đã có work order đang mở (chưa Delivered/Cancelled) → cảnh báo trước khi cho tạo mới.

**Kết quả**: WorkOrder ở Received, sẵn sàng cho Technician chẩn đoán.

---

## UC-03. Chẩn đoán & lập báo giá

**Điều kiện tiên quyết**: WorkOrder đang **Received**.

**Luồng chính**:
1. Technician kiểm tra xe, ghi chú nguyên nhân → chuyển **Diagnosing**.
2. Staff thêm hạng mục báo giá: loại (Phụ tùng/Công), mô tả, số lượng, đơn giá.
3. Hệ thống tự tính tổng tiền từ các hạng mục.
4. Staff gửi báo giá → chuyển **QuotePending**. Đồng thời sinh `ApprovalToken` (xem `01-business-spec.md` mục 7).

**Ngoại lệ**: Phụ tùng không đủ tồn kho → cảnh báo nhưng **vẫn cho phép** báo giá (đặt hàng sau).

**Kết quả**: Khách nhận báo giá, xem được chi tiết từng hạng mục qua API.

---

## UC-04. Khách duyệt/từ chối báo giá

**Điều kiện tiên quyết**: WorkOrder đang **QuotePending**. Khách xác thực 1 trong 2 cách: (a) đăng nhập Customer portal, hoặc (b) link `ApprovalToken` gửi kèm email/thông báo — cho khách vãng lai không có tài khoản, kể cả sau khi đã rời gara.

**Luồng chính**:
1. Khi Staff gửi báo giá (UC-03 bước 4), hệ thống sinh `ApprovalToken` ngẫu nhiên, hạn dùng ~72h.
2. Khách xem chi tiết báo giá qua 1 trong 2 kênh trên.
3. Khách chọn **Approve** → chuyển **InRepair**.

**Ngoại lệ**:
- Khách chọn **Reject** (qua tài khoản hoặc token) → hỏi lý do (tuỳ chọn) → chuyển **Cancelled**; nếu có cấu hình phí kiểm tra → tự tạo khoản phí tối thiểu.
- Link token hết hạn/không hợp lệ → báo lỗi, không cho duyệt; Staff có thể yêu cầu gửi lại link mới (sinh token mới).
- Token đã dùng để Approve/Reject một lần → chặn dùng lại (double-submit).
- Khách cố duyệt báo giá không thuộc về mình → **403 Forbidden**.

**Kết quả**: WorkOrder chuyển trạng thái phù hợp; lịch sử duyệt giá lưu lại kèm hình thức xác thực (tài khoản hay token) để đối chiếu tranh chấp.

> Ghi chú vận hành: cơ chế magic link giải quyết case phổ biến nhất — khách vãng lai để xe rồi về, gara báo giá qua điện thoại, khách xác nhận từ xa. Với khách đang chờ tại gara, Staff có thể đưa tablet mở sẵn link đó để khách tự bấm — vẫn là khách thao tác, không phải Staff quyết định hộ. **Không cần luồng riêng cho tại-quầy và từ-xa** — cả hai dùng chung một cơ chế xác thực.

---

## UC-05. Cập nhật tiến độ sửa chữa

**Điều kiện tiên quyết**: WorkOrder đang **InRepair**.

**Luồng chính**:
1. Technician sửa theo các hạng mục đã được khách duyệt.
2. Cần phụ tùng → hệ thống kiểm tra tồn kho, trừ kho khi hạng mục được đánh dấu đã dùng.
3. Hoàn tất toàn bộ hạng mục → chuyển **Completed**.

**Ngoại lệ**: Thiếu phụ tùng giữa chừng → chuyển **WaitingParts**; phụ tùng về → chuyển lại **InRepair**.

**Kết quả**: WorkOrder ở **Completed**, sẵn sàng cho thanh toán.

---

## UC-06. Ghi nhận thanh toán & giao xe

**Điều kiện tiên quyết**: WorkOrder đang **Completed**. Luôn trả đủ 1 lần lúc nhận xe, không đặt cọc trước.

**Luồng chính**:
1. **Nhánh tiền mặt**: Staff xem tổng tiền (tự tính từ hạng mục đã duyệt), khách trả tiền mặt/quẹt thẻ, Staff bấm "Ghi nhận thanh toán tiền mặt".
2. **Nhánh online**: Customer bấm "Thanh toán online" → chuyển tới cổng thanh toán; sau khi thành công, cổng gọi webhook (UC-14) — Staff không thao tác gì, chỉ xem trạng thái **Paid**.
3. Payment được xác nhận (1 trong 2 nhánh) → hệ thống tự chuyển WorkOrder sang **Delivered** và xuất hoá đơn.

**Ngoại lệ**:
- Tiền mặt nhập không khớp tổng tiền hạng mục → cảnh báo trước khi xác nhận.
- Khách huỷ giữa chừng hoặc cổng lỗi → WorkOrder giữ nguyên **Completed**, khách thử lại hoặc chuyển sang trả tiền mặt.

**Kết quả**: WorkOrder đóng ở **Delivered**, hoá đơn xuất JSON/XML; Payment ghi rõ ai xác nhận.

---

## UC-14. Khách thanh toán online qua cổng thanh toán

**Điều kiện tiên quyết**: WorkOrder đang **Completed**, khách chọn thanh toán online.

**Luồng chính**:
1. Customer bấm "Thanh toán online" → hệ thống tạo Payment nháp, trả về URL cổng thanh toán.
2. Customer chuyển sang trang cổng thanh toán, nhập thông tin **trên hệ thống của cổng** (không phải trên GaraCare).
3. Cổng gọi webhook (IPN) về server GaraCare kèm chữ ký HMAC + mã giao dịch.
4. Hệ thống xác thực chữ ký; hợp lệ + giao dịch thành công → Payment xác nhận, kích hoạt UC-06 (chuyển **Delivered**).

**Ngoại lệ**: Chữ ký không hợp lệ (nghi giả mạo webhook) → từ chối, ghi log cảnh báo, **không đổi trạng thái thanh toán**.

**Kết quả**: Payment xác nhận hoàn toàn tự động — **không ai bấm tay** đánh dấu "đã thanh toán" cho giao dịch online.

> Nếu chưa kịp tích hợp VNPay/Momo sandbox thật, dùng cổng giả lập nội bộ (một endpoint tự viết đóng vai cổng thanh toán) để **giữ đúng kiến trúc webhook** — chỉ thay implementation, không đổi luồng nghiệp vụ. Việc thay bằng gateway thật là FR-17c, mức **Could**.

---

## UC-09. Đặt lịch trước (có ưu đãi)

**Điều kiện tiên quyết**: Khách đã đăng nhập Customer portal.

**Luồng chính**:
1. Khách chọn xe (hoặc nhập mới), chọn ngày + khung giờ.
2. Hệ thống kiểm tra số lượng lịch hẹn đã đặt trong khung giờ đó (giới hạn theo năng lực gara).
3. Tạo Appointment trạng thái **Booked**, áp `DiscountPercent` ưu đãi đặt trước.
4. Gửi thông báo xác nhận (in-app + email).

**Ngoại lệ**: Khung giờ đầy → báo lỗi, gợi ý khung giờ gần nhất còn trống.

---

## UC-10. Check-in từ lịch hẹn thành work order

**Điều kiện tiên quyết**: Khách có Appointment **Booked**, đã tới gara đúng ngày hẹn.

**Luồng chính**:
1. Staff tra cứu Appointment theo tên/SĐT/mã lịch hẹn.
2. Staff bấm Check-in → tạo WorkOrder mới, gắn `AppointmentId`, kế thừa `DiscountPercent`.
3. Appointment chuyển **CheckedIn**; WorkOrder khởi tạo **Received** (tiếp luồng UC-02).

**Ngoại lệ**:
- Khách tới không đúng ngày hẹn → Staff tạo work order như khách vãng lai (không hưởng ưu đãi).
- Quá giờ hẹn mà khách không tới → Staff (hoặc job nền) đánh dấu **NoShow**.

---

## UC-11. Xem tiến trình sửa xe & ETA

**Điều kiện tiên quyết**: Khách có ít nhất một WorkOrder đang mở.

**Luồng chính**:
1. Khách vào trang "Tiến trình sửa xe", chọn work order.
2. Hệ thống hiển thị trạng thái hiện tại, `EstimatedCompletionDate`, danh sách thông báo (đã đọc/chưa đọc).
3. Nếu **WaitingParts** hoặc `IsDelayed=true` → hiển thị rõ lý do + ngày dự kiến mới.

**Ngoại lệ**: Khách cố xem work order không thuộc về mình → **403 Forbidden**.

---

## UC-12. Hệ thống gửi thông báo (in-app + email)

**Điều kiện tiên quyết**: Có sự kiện cần thông báo (báo giá mới, gia hạn, hoàn tất, xác nhận đặt lịch).

**Luồng chính**:
1. Service Layer tạo bản ghi `Notification` (Type, WorkOrderId/AppointmentId, Message) lưu DB — khách xem ngay trong portal.
2. Song song, gọi Email Service (MailKit/SMTP) gửi email cùng nội dung.

**Ngoại lệ**: Gửi email thất bại (SMTP lỗi/timeout) → giữ nguyên Notification in-app đã tạo, ghi log lỗi email riêng — **không rollback** thao tác nghiệp vụ chính.

---

## UC-13. Nhắc nhở & gắn cờ gọi điện khi báo giá quá hạn

**Điều kiện tiên quyết**: WorkOrder đang **QuotePending**, có `QuoteSentAt`.

**Luồng chính**:
1. Job kiểm tra định kỳ các WorkOrder QuotePending quá 24h chưa duyệt.
2. Gửi nhắc lần 1 (in-app + email), cập nhật `ReminderSentAt`.
3. Quá 48h vẫn chưa phản hồi → đánh dấu `NeedsFollowUpCall=true`.
4. Staff xem danh sách cần gọi điện (lọc `NeedsFollowUpCall=true`, ưu tiên đầu danh sách), gọi thủ công (ngoài phạm vi hệ thống).

**Ngoại lệ**: Khách duyệt/từ chối trước mốc nhắc → không gửi nhắc, không gắn cờ.

---

## UC-15. Xử lý khách trễ hẹn (gọi điện, dời lịch hoặc NoShow)

**Điều kiện tiên quyết**: Appointment đang **Booked**, đã quá `ScheduledTimeSlot`.

**Luồng chính**:
1. Job kiểm tra định kỳ Appointment còn Booked mà quá `ScheduledTimeSlot` 15 phút → tự `IsLate=true`, `LateNotifiedStaffAt` = hiện tại.
2. Staff xem danh sách "Khách trễ hẹn" (`IsLate=true`), gọi điện theo SĐT trong Customer.
3a. Khách muốn dời lịch → Staff cập nhật `ScheduledDate`/`ScheduledTimeSlot` mới, `IsLate=false`; Appointment giữ **Booked**, gửi lại xác nhận lịch hẹn mới (tái dùng UC-12).
3b. Khách xác nhận không tới/không liên lạc được → Staff đánh dấu **NoShow**.

**Ngoại lệ**:
- Khách tự tới trước khi Staff kịp gọi → check-in bình thường (UC-10), `IsLate` không còn ý nghĩa.
- Quá mốc dài hơn (ví dụ 60 phút) mà Staff chưa xử lý → hệ thống có thể tự động chuyển **NoShow** (tương tự cơ chế `NeedsFollowUpCall`).

---

## UC-01, UC-07, UC-08 (tóm tắt — không có luồng ngoại lệ phức tạp)

- **UC-01 Đăng ký/Đăng nhập**: JWT bearer token, mật khẩu hash BCrypt, phân quyền theo Role — chi tiết ở `01-business-spec.md` §2 và `09-non-functional-and-nfr.md`.
- **UC-07 Tìm kiếm/lọc work order**: OData `$filter/$orderby/$top/$skip` theo Status, ngày, khách hàng — chi tiết `04-api-contract.md`. Nếu OData khó cấu hình, phương án dự phòng là query string filter thường (`?status=&from=&to=`) — chỉ áp dụng khi được yêu cầu, xem `docs/10-risks.md` nếu có.
- **UC-08 Báo cáo doanh thu**: chỉ Admin, theo khoảng thời gian — mức ưu tiên Should (FR-21).
