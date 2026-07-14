# 01. Business Spec — Nguồn sự thật nghiệp vụ

Nguồn: `GaraCare_BA_SRS_v3.docx` (BA/SRS v1.0, tháng 7/2026). File này là bản tóm lược có cấu trúc lại để AI agent dùng trong lúc code — nếu có bất kỳ mâu thuẫn nào giữa file này và file docx gốc, **ưu tiên hỏi lại người dùng**, không tự suy diễn.

## 1. Bài toán đang giải

Gara sửa xe vừa và nhỏ quản lý bằng sổ sách/Excel → khách không biết chi phí trước khi thợ sửa, dễ tranh chấp giá; nhân viên khó theo dõi xe nào chờ phụ tùng; chủ gara không có số liệu doanh thu tổng hợp.

GaraCare số hoá đúng điểm nghẽn này bằng cách bắt buộc bước **báo giá → khách duyệt** trước khi sửa. Đây là giá trị cốt lõi — mọi thiết kế kỹ thuật phải bảo vệ đúng ràng buộc này, không được "tối ưu" theo hướng bỏ qua nó.

## 2. Actors (vai trò)

| Vai trò | Mô tả | Quyền hạn chính |
| --- | --- | --- |
| **Customer** | Chủ xe/thiết bị | Xem work order của mình, duyệt/từ chối báo giá, xem lịch sử & hoá đơn, đặt lịch trước |
| **Staff** | Lễ tân/tiếp nhận | Tạo work order, lập báo giá, cập nhật trạng thái nội bộ, ghi nhận thanh toán tiền mặt |
| **Technician** | Thợ sửa chữa | Cập nhật chẩn đoán, cập nhật tiến độ sửa (InRepair/WaitingParts/Completed) |
| **Admin** | Chủ gara/quản trị | Toàn quyền: quản lý user, quản lý phụ tùng & kho, xem báo cáo doanh thu |

> Ghi chú phạm vi: nếu cần rút gọn, có thể gộp Staff + Technician thành một role "Staff" duy nhất mà không phá luồng chính. **Chỉ làm việc này nếu người dùng yêu cầu rõ** — không tự gộp role khi đang code.

## 3. Luồng nghiệp vụ trung tâm (vòng đời WorkOrder)

1. **Booked** (tuỳ chọn) — khách đặt lịch trước qua portal, được ưu đãi giảm giá.
2. **Received** — hai nhánh tạo work order:
   - (a) Khách đã đặt lịch: Staff check-in appointment → hệ thống tự tạo WorkOrder, kế thừa ưu đãi.
   - (b) Khách vãng lai: Staff tạo WorkOrder trực tiếp, không qua appointment.
3. **Diagnosing** — Technician kiểm tra, ghi chú nguyên nhân, xác định hạng mục cần sửa/thay.
4. **QuotePending** — Staff lập danh sách hạng mục (phụ tùng + công), nhập `EstimatedCompletionDate`, gửi báo giá → hệ thống gửi thông báo in-app + email.
5. **Khách phản hồi**:
   - **Approved** → chuyển **InRepair**.
   - **Rejected** → tính phí kiểm tra (nếu có cấu hình) → **Cancelled**.
6. **InRepair** — Technician sửa. Nếu thiếu phụ tùng → **WaitingParts**, rồi quay lại **InRepair** khi có hàng. Nếu trễ so với `EstimatedCompletionDate` → đánh dấu `IsDelayed=true`, gửi thông báo gia hạn.
7. **Completed** — sửa xong, chờ thanh toán và giao xe.
8. **Delivered** — thanh toán xong, giao xe, đóng work order.

Customer có đúng 2 điểm chạm với hệ thống: (1) đặt lịch + duyệt/từ chối báo giá, (2) xem trang "Tiến trình sửa xe" (trạng thái hiện tại, ETA, lịch sử thông báo). Cả hai portal (staff, customer) dùng chung một bộ API, phân quyền qua Role.

## 4. Bảng chuyển trạng thái Appointment

| Từ | Sang | Điều kiện / Actor |
| --- | --- | --- |
| Booked | CheckedIn (tạo WorkOrder mới, Received) | Khách tới đúng hẹn, Staff check-in |
| Booked | Cancelled | Khách huỷ trước giờ hẹn |
| Booked | NoShow | Quá giờ hẹn ~15 phút chưa check-in → hệ thống tự `IsLate=true`, vào danh sách "cần gọi khách"; nếu khách muốn dời lịch → cập nhật lại `ScheduledDate`/`ScheduledTimeSlot` (giữ Booked); nếu không liên lạc được / khách xác nhận không tới → Staff đánh dấu NoShow |

## 5. Bảng chuyển trạng thái WorkOrder

| Từ | Sang | Điều kiện / Actor |
| --- | --- | --- |
| Received | Diagnosing | Technician bắt đầu kiểm tra |
| Diagnosing | QuotePending | Staff gửi báo giá |
| QuotePending | Approved → InRepair | Customer duyệt |
| QuotePending | Rejected → Cancelled | Customer từ chối |
| InRepair | WaitingParts | Technician đánh dấu thiếu phụ tùng |
| WaitingParts | InRepair | Phụ tùng về kho |
| InRepair | Completed | Technician xác nhận sửa xong |
| Completed | Delivered | Staff ghi nhận thanh toán tiền mặt, HOẶC hệ thống tự chuyển khi webhook cổng thanh toán xác nhận |

### Ràng buộc nghiệp vụ bắt buộc validate ở tầng API (không chỉ UI)

- Không được chuyển sang **InRepair** nếu báo giá chưa **Approved**.
- Không được tạo **Payment** nếu WorkOrder chưa **Completed**.
- Không được xoá/sửa **QuotationItem** sau khi khách đã **Approved**.
- **Customer** chỉ được Approve/Reject báo giá và thanh toán online **của chính mình**.
- **Staff/Technician** đổi các trạng thái nội bộ (chẩn đoán, sửa chữa, ghi nhận tiền mặt).
- **Trạng thái thanh toán online chỉ được đổi bởi hệ thống** khi xác thực đúng webhook — không actor người nào được tự ý đổi.

## 6. Nguyên tắc thiết kế chuyển trạng thái — QUAN TRỌNG NHẤT TRONG TOÀN BỘ SPEC

Hệ thống **KHÔNG** cho phép sửa trực tiếp field `Status` qua một API chung kiểu:

```
PUT /workorders/{id}
{ "status": "Completed" }
```

Lý do: cách này để hở lỗ hổng — bất kỳ ai gọi được API đều có thể nhảy cóc trạng thái (ví dụ từ Received thẳng lên Completed, bỏ qua báo giá) hoặc tự đánh dấu đã thanh toán mà không có giao dịch thật.

**Thay vào đó**: mỗi transition có một endpoint riêng, đặt tên theo hành động nghiệp vụ. Trạng thái chỉ là **kết quả phụ (side effect)** sau khi hành động đó được xác thực hợp lệ. Danh sách endpoint đầy đủ ở `docs/04-api-contract.md`.

**Quy tắc dùng chung cho mọi endpoint chuyển trạng thái**: Service Layer kiểm tra trạng thái hiện tại có hợp lệ để chuyển không (nếu không → 400); nếu hợp lệ thì đổi Status, ghi 1 dòng vào `WorkOrderStatusHistory`, và tạo Notification nếu cần — gói gọn trong **một hàm dùng chung**, không rải logic chuyển trạng thái ra nhiều Controller.

## 7. Cơ chế ApprovalToken (magic link) — cho khách vãng lai

Khách vãng lai không có tài khoản vẫn cần duyệt/từ chối báo giá từ xa (đã rời gara, báo giá qua điện thoại). Giải pháp:

- Khi Staff gửi báo giá, hệ thống sinh `ApprovalToken` ngẫu nhiên, không đoán được, gắn với WorkOrder, có hạn dùng (mặc định 72 giờ).
- Link dạng `.../quotes/{token}` gửi qua email/SMS, không cần đăng nhập để xem/duyệt/từ chối.
- Token dùng một lần — sau khi Approve/Reject, token bị khoá, không cho dùng lại (chặn double-submit).
- Token hết hạn/không hợp lệ → báo lỗi, không cho duyệt; Staff có thể yêu cầu sinh token mới.
- Khách cố duyệt báo giá không thuộc về mình → 403 Forbidden.
- Lịch sử duyệt giá lưu rõ hình thức xác thực (qua tài khoản hay qua token) để đối chiếu khi có tranh chấp.

Khách có tài khoản Customer portal thì dùng đăng nhập bình thường — không cần luồng riêng, cả hai đều dùng chung 1 API, chỉ khác cách xác thực.

## 8. Thanh toán

- Luôn trả **đủ một lần lúc nhận xe** — không có đặt cọc trước (xe đang do gara giữ).
- **Nhánh tiền mặt/quẹt thẻ tại quầy**: Staff xem tổng tiền, khách trả, Staff bấm "Ghi nhận thanh toán tiền mặt" → `Payment.ConfirmedByUserId` = Staff.
- **Nhánh online**: Customer bấm thanh toán online → chuyển sang cổng thanh toán (VNPay/Momo sandbox thật, hoặc cổng giả lập nội bộ nếu chưa kịp tích hợp) → cổng gọi **webhook** về hệ thống kèm chữ ký HMAC → hệ thống xác thực chữ ký → nếu hợp lệ, xác nhận Payment, tự chuyển WorkOrder sang **Delivered**. Staff không thao tác gì ở nhánh này.
- Chữ ký không hợp lệ (nghi giả mạo webhook) → từ chối, ghi log cảnh báo, **không đổi trạng thái thanh toán**.
- Nếu số tiền tiền mặt Staff nhập không khớp tổng hạng mục → cảnh báo trước khi xác nhận.
- Payment luôn ghi rõ do ai xác nhận (Staff cho tiền mặt, hệ thống cho online) để tránh tranh chấp.

## 9. Thông báo & nhắc nhở tự động

- Mọi sự kiện quan trọng (báo giá mới, đổi trạng thái, gia hạn, xác nhận lịch hẹn) tạo **Notification** in-app (lưu DB) **và** gửi email song song (MailKit/SMTP).
- Gửi email thất bại **không được rollback** thao tác nghiệp vụ chính — chỉ ghi log lỗi riêng, Notification in-app vẫn giữ nguyên.
- Job nền kiểm tra định kỳ:
  - WorkOrder ở **QuotePending** quá 24h chưa duyệt → gửi nhắc lần 1, ghi `ReminderSentAt`.
  - Quá 48h vẫn chưa phản hồi → đánh dấu `NeedsFollowUpCall=true`, hiện ưu tiên cho Staff (Staff gọi điện thủ công, ngoài phạm vi hệ thống).
  - Appointment **Booked** quá giờ hẹn ~15 phút chưa check-in → tự `IsLate=true`, `LateNotifiedStaffAt` set → vào danh sách "khách trễ hẹn" cho Staff xử lý (dời lịch hoặc NoShow). Có thể tự động NoShow sau một mốc dài hơn (ví dụ 60 phút) nếu Staff chưa xử lý.

## 10. Phạm vi (scope) — bám sát để không tự mở rộng

**Trong phạm vi (in-scope)**: quản lý user & phân quyền, khách hàng & xe, work order/chẩn đoán/báo giá, duyệt/từ chối giá, tiến độ sửa & phụ tùng, thanh toán & hoá đơn, tìm kiếm/lọc/báo cáo cơ bản, đặt lịch trước & check-in.

**Ngoài phạm vi ở giai đoạn hiện tại (out-of-scope trừ khi được yêu cầu rõ)**: cổng thanh toán online thật (VNPay/Momo) trừ khi task nói rõ nâng cấp; app mobile riêng; tích hợp SMS thật; các mục mức **Could** (xem `docs/05-functional-requirements.md`) không tự làm trước khi các mục **Must**/**Should** xong.
