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
| UC-16 | Auto-assign Technician + Bay khi tiếp nhận | System |
| UC-17 | Reassign Technician giữa chừng & chia hoa hồng | Staff, Admin, System |
| UC-18 | Phát sinh & duyệt ChangeRequest trong lúc sửa | Technician, Staff, Admin |

> UC-16/17/18 là các use case mới bổ sung theo bản tổng hợp luồng chính v5 (`luong-chinh-tong-hop-v5.md`) — không có trong SRS v3 gốc.

---

## UC-02. Tiếp nhận xe (tạo work order)

**Điều kiện tiên quyết**: Khách hàng & xe đã tồn tại (hoặc tạo mới ngay bước này).

**Luồng chính**:
1. Staff tra cứu khách theo số điện thoại; chưa có thì tạo mới.
2. Staff chọn/thêm xe gắn với khách hàng.
3. Staff nhập mô tả sự cố ban đầu do khách khai báo.
4. Staff chụp ảnh hiện trạng xe (**bắt buộc**).
5. Hệ thống tạo WorkOrder mới, trạng thái mặc định **Received**.
6. Hệ thống chạy **UC-16 (Auto-assign Technician + Bay)** ngay lập tức, không cần Staff kích hoạt riêng.

**Ngoại lệ**:
- Nếu biển số xe đã có work order đang mở (chưa Delivered/Cancelled) → cảnh báo trước khi cho tạo mới.
- Auto-assign không tìm được Technician/Bay khả dụng → WorkOrder vẫn ở **Received**, vào hàng chờ chung, xem UC-16.

**Kết quả**: WorkOrder ở Received, đã (hoặc đang chờ) được gán Technician + Bay cho bước chẩn đoán.

---

## UC-03. Chẩn đoán & lập báo giá

**Điều kiện tiên quyết**: WorkOrder đang **Received**, đã được auto-assign Technician (UC-16).

**Luồng chính**:
1. Technician được gán bấm **Accept** → chuyển **Diagnosing**, Technician chuyển trạng thái nội bộ `DIAGNOSING`.
2. Technician chọn hạng mục nghi cần sửa từ ServiceCatalog, quét mã phụ tùng, chụp ảnh/video minh chứng, ghi chú (gõ tay/voice-to-text).
3. Technician nhập `estimatedLaborHours` (chỉ thời gian thao tác kỹ thuật thuần, không phải ngày giao xe) và ký xác nhận điện tử → hệ thống tạo `DiagnosisRecord` **bất biến** (immutable, có timestamp + technicianId) → WorkOrder chuyển **DiagnosisConfirmed**.
4. Hệ thống kiểm tra `estimatedLaborHours`:
   - `≤ 2 giờ` → **Fast-lane**, vào hàng chờ chung, xử lý ngay khi có Technician + Bay phù hợp trống.
   - `> 2 giờ` → **Heavy Repair**, tự tách khỏi khung giờ đặt lịch ban đầu, đẩy vào **Repair Queue** riêng.
5. Hệ thống tự sinh `Quote` từ `DiagnosisRecord` (đơn giá lấy từ ServiceCatalog, Staff **không** tự nhập/sửa số liệu) và tự tính `systemSuggestedDate` (công thức ở `01-business-spec.md` mục 12).
6. Staff xem `systemSuggestedDate`, xác nhận hoặc **tăng thêm buffer** (không được giảm xuống dưới mức đề xuất) → gửi báo giá → chuyển **QuotePending**. Đồng thời sinh `ApprovalToken` (xem `01-business-spec.md` mục 7).
7. Technician chuyển sang `WAITING_ON_CUSTOMER` — được nhận Diagnosing xe khác trong lúc chờ, chưa nhận `InRepair` mới.

**Ngoại lệ**:
- Phụ tùng không đủ tồn kho → cảnh báo nhưng **vẫn cho phép** báo giá (đặt hàng sau); `partsWaitTime` được cộng vào `systemSuggestedDate`.
- Staff nhập `finalEstimatedDate` nhỏ hơn `systemSuggestedDate` → **400 Bad Request**, không cho gửi báo giá.

**Kết quả**: Khách nhận báo giá kèm `finalEstimatedDate`, xem được chi tiết từng hạng mục qua API.

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
1. Technician chuyển trạng thái nội bộ `IN_REPAIR` (khoá cứng — không nhận Diagnosing/InRepair mới khác) và sửa theo các hạng mục đã được khách duyệt.
2. Cần phụ tùng → hệ thống kiểm tra tồn kho, xuất phụ tùng qua quét mã → tự tạo `InventoryTransaction`, trừ kho khi hạng mục được đánh dấu đã dùng.
3. Nếu phát sinh hạng mục/thời gian mới ngoài Quote gốc → xử lý qua **UC-18 (ChangeRequest)**, không tự ý sửa `QuotationItem` đã Approved.
4. Trễ so với `finalEstimatedDate` → hệ thống tự đánh dấu `IsDelayed=true`, gửi thông báo gia hạn (tái dùng UC-12).
5. Hoàn tất toàn bộ hạng mục → Technician chụp ảnh sau sửa → chuyển **Completed**, Technician quay về `FREE`, hệ thống tự đẩy task tiếp theo trong queue cá nhân lên (nếu có).

**Ngoại lệ**:
- Thiếu phụ tùng giữa chừng → chuyển **WaitingParts**, Technician chuyển `WAITING_PARTS` (được nhận Diagnosing xe khác trong lúc chờ hàng); hàng về → tự chuyển lại **InRepair**, Technician quay lại `IN_REPAIR`.
- Technician nghỉ đột xuất giữa chừng → xử lý qua **UC-17 (Reassign Technician)**.

**Kết quả**: WorkOrder ở **Completed**, sẵn sàng cho thanh toán.

---

## UC-16. Auto-assign Technician + Bay khi tiếp nhận

**Điều kiện tiên quyết**: WorkOrder vừa được tạo, đang **Received**.

**Luồng chính**:
1. Hệ thống lọc danh sách Technician ứng viên: ưu tiên `FREE` → ít việc nhất → đúng chuyên môn (nếu có phân loại kỹ năng). Technician đang `IN_REPAIR` bị loại hoàn toàn (khoá cứng), kể cả để chen `Diagnosing`.
2. Song song, hệ thống lọc Bay đúng `type` theo `requiredBayType` của hạng mục dự kiến, đang `FREE`.
3. Hệ thống gán **đồng thời** 1 Technician ứng viên hợp lệ VÀ 1 Bay phù hợp đang trống — không gán tách rời.
4. WorkOrder giữ **Received**, Technician nhận việc vào queue cá nhân, chờ Accept (tiếp UC-03).

**Ngoại lệ**:
- Dịch vụ VIP có `requestedTechnicianId` (`isMasterTechRequired = true`) nhưng người đó đang bận → gán tạm Technician khác trong Pool, gửi thông báo cho khách/Staff.
- Technician rảnh nhưng không có Bay phù hợp → WorkOrder vào hàng chờ Bay (không gán Technician đứng chờ); `bayWaitTime` tính từ thời điểm Bay phù hợp gần nhất dự kiến trống.
- Không còn Technician khả dụng (kể cả để chen `Diagnosing`) → đẩy WorkOrder vào hàng chờ chung, gửi alert cho Staff (danh sách "Cần xử lý").

**Kết quả**: WorkOrder có Technician + Bay được gán (hoặc nằm trong hàng chờ có lý do rõ ràng cho Staff xử lý).

---

## UC-17. Reassign Technician giữa chừng & chia hoa hồng

**Điều kiện tiên quyết**: WorkOrder đang có Technician phụ trách (`Diagnosing`/`InRepair`/`WaitingParts`), Technician đó cần được thay (nghỉ đột xuất, hết ca...).

**Luồng chính**:
1. Staff/Admin (hoặc alert hệ thống) phát hiện Technician cần thay → mở màn hình reassign, hệ thống gợi ý Technician rảnh trong Pool.
2. Hệ thống kiểm tra trạng thái Technician cũ để quyết định luồng duyệt (xem bảng guard ở `01-business-spec.md` mục 13):
   - `WAITING_PARTS` → cho phép auto-reassign ngay cho Technician rảnh, không cần duyệt tay.
   - `IN_REPAIR` → bắt buộc Staff/Admin duyệt tay, nhập ghi chú/ảnh hiện trạng bàn giao.
3. Hệ thống đóng dòng `WorkOrderAssignment` của Technician cũ (`endedAt`, `laborHoursLogged`), tạo dòng mới cho Technician mới (`role: HANDOFF`, `handoffReason`).
4. Staff/Admin xác nhận `commissionSplitPercent` cho từng dòng `WorkOrderAssignment` (hệ thống gợi ý theo tỷ lệ `laborHoursLogged`) — `approvedBy` ghi lại người duyệt.

**Ngoại lệ**:
- WorkOrder đã **Completed** → không cho reassign (chặn cứng, tránh gian lận số liệu lương).
- Tổng `commissionSplitPercent` chưa = 100% → chặn chuyển WorkOrder sang **Delivered** (UC-06).

**Kết quả**: `WorkOrderAssignment` phản ánh đúng ai làm phần nào, làm cơ sở chia hoa hồng công bằng; mọi thay đổi % ghi vào `AuditLog`.

---

## UC-18. Phát sinh & duyệt ChangeRequest trong lúc sửa

**Điều kiện tiên quyết**: WorkOrder đang **InRepair**, phát sinh hạng mục/thời gian ngoài Quote gốc.

**Luồng chính**:
1. Technician tạo `ChangeRequest` (`status = Draft`), ghi `costDeltaPercent`, `costDeltaAbsolute`, `timeDeltaHours` so với Quote/`finalEstimatedDate` hiện tại.
2. Technician ký xác nhận → `ChangeRequest.status = PendingTechnicianConfirm → Confirmed` (bước ký là bắt buộc, không phân biệt có vượt ngưỡng hay không).
3. Hệ thống kiểm tra ngưỡng: `costDeltaPercent ≤ 10–15%` **VÀ** `costDeltaAbsolute ≤ 1.000.000đ` **VÀ** `timeDeltaHours ≤ 4 giờ`:
   - Thoả cả 3 → **auto-approve**, merge thẳng vào Quote, thông báo khách, Staff có thể tự gọi điện xin lỗi khách.
   - Vượt bất kỳ điều kiện nào → gửi alert cho **Admin** duyệt trước khi merge vào Quote và thông báo khách.

**Ngoại lệ**: Admin từ chối `ChangeRequest` → `status = Rejected`, không merge vào Quote, Technician được thông báo để điều chỉnh phương án sửa.

**Kết quả**: Quote/`finalEstimatedDate` được cập nhật đúng theo `ChangeRequest` đã duyệt, có audit trail đầy đủ.

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
1. Khách chọn xe (hoặc nhập mới), chọn loại lịch — `StandardService` (dịch vụ có định mức sẵn trong ServiceCatalog) hoặc `GeneralDiagnosis` (báo lỗi chung, chưa rõ nguyên nhân) — rồi chọn ngày + khung giờ.
2. Hệ thống kiểm tra số lượng lịch hẹn đã đặt trong khung giờ đó (giới hạn theo năng lực gara). `StandardService` block đúng khung giờ theo định mức; `GeneralDiagnosis` chỉ block ~30 phút để tiếp nhận + khám sơ bộ, không cam kết giờ sửa xong.
3. Mặc định **không cho chọn đích danh Technician**; chỉ hiện tuỳ chọn này nếu dịch vụ được đánh dấu `isMasterTechRequired = true` (VIP/thợ chuyên biệt).
4. Tạo Appointment trạng thái **Booked**, áp `DiscountPercent` ưu đãi đặt trước.
5. Gửi thông báo xác nhận (in-app + email).

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
