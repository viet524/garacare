# 01. Business Spec — Nguồn sự thật nghiệp vụ

Nguồn: `GaraCare_BA_SRS_v3.docx` (BA/SRS v1.0, tháng 7/2026) + `luong-chinh-tong-hop-v5.md` (bản tổng hợp luồng chính v5, đã chốt với chủ dự án). File này là bản tóm lược có cấu trúc lại để AI agent dùng trong lúc code — nếu có bất kỳ mâu thuẫn nào giữa file này và tài liệu gốc, **ưu tiên hỏi lại người dùng**, không tự suy diễn.

> **Ghi chú v5 (thay thế luồng đơn giản hoá trước đó):** luồng chính của WorkOrder/Appointment trong file này đã được cập nhật theo bản tổng hợp v5 — bổ sung auto-assign Technician + Bay, tách Repair Queue theo `estimatedLaborHours`, cơ chế `WorkOrderAssignment` (reassign & chia hoa hồng), quản lý tài nguyên Bay, và `ChangeRequest` theo ngưỡng rủi ro. Hai quyết định đã chốt khi tích hợp v5 vào docs:
> - **"Manager" trong v5 = Admin hiện tại** — không thêm role mới; mọi chỗ v5 nói "Manager duyệt" nghĩa là **Admin** duyệt.
> - **Tên trạng thái khi khách từ chối báo giá giữ nguyên là `Cancelled`** — không đổi thành `Closed_Rejected` như bản v5 gốc, để không phá vỡ tên trạng thái đã dùng trong toàn bộ tài liệu/code hiện tại.

## 1. Bài toán đang giải

Gara sửa xe vừa và nhỏ quản lý bằng sổ sách/Excel → khách không biết chi phí trước khi thợ sửa, dễ tranh chấp giá; nhân viên khó theo dõi xe nào chờ phụ tùng; chủ gara không có số liệu doanh thu tổng hợp.

GaraCare số hoá đúng điểm nghẽn này bằng cách bắt buộc bước **báo giá → khách duyệt** trước khi sửa. Đây là giá trị cốt lõi — mọi thiết kế kỹ thuật phải bảo vệ đúng ràng buộc này, không được "tối ưu" theo hướng bỏ qua nó.

## 2. Actors (vai trò)

| Vai trò | Mô tả | Quyền hạn chính |
| --- | --- | --- |
| **Customer** | Chủ xe/thiết bị | Xem work order của mình, duyệt/từ chối báo giá, xem lịch sử & hoá đơn, đặt lịch trước |
| **Staff** | Lễ tân/tiếp nhận | Tạo work order, xác nhận báo giá/buffer thời gian đề xuất, cập nhật trạng thái nội bộ, ghi nhận thanh toán tiền mặt, xử lý các mục "Cần xử lý" (trễ hạn, WaitingParts quá lâu, không còn Technician khả dụng) |
| **Technician** | Thợ sửa chữa | Nhận việc từ queue cá nhân (auto-assign), chẩn đoán, nhập `estimatedLaborHours`, cập nhật tiến độ sửa (InRepair/WaitingParts/Completed), xác nhận (ký) `ChangeRequest` phát sinh |
| **Admin** | Chủ gara/quản trị (= "Manager" trong tài liệu v5) | Toàn quyền: quản lý user, quản lý phụ tùng & kho, xem báo cáo doanh thu, duyệt `ChangeRequest` vượt ngưỡng rủi ro, duyệt `commissionSplitPercent` khi reassign Technician, xử lý alert "Reassign ngay" khi Technician nghỉ đột xuất |

> Ghi chú phạm vi: nếu cần rút gọn, có thể gộp Staff + Technician thành một role "Staff" duy nhất mà không phá luồng chính. **Chỉ làm việc này nếu người dùng yêu cầu rõ** — không tự gộp role khi đang code.
>
> **Không có vai trò Foreman** và không có giao diện Kanban toàn cảnh (xem mục 11) — mô hình vận hành là **exception-based**: hệ thống tự chạy auto-assign, Staff/Admin chỉ can thiệp qua danh sách "Cần xử lý" khi có ngoại lệ.

## 3. Luồng nghiệp vụ trung tâm (vòng đời WorkOrder) — theo v5

1. **Booked** (tuỳ chọn) — khách đặt lịch trước qua portal, chọn 1 trong 2 loại:
   - **`StandardService`** (dịch vụ có định mức sẵn trong ServiceCatalog, VD: thay dầu, đảo lốp) → hệ thống biết chắc thời lượng, block đúng khung giờ đó cho 1 Technician trong Pool.
   - **`GeneralDiagnosis`** (báo lỗi chung, chưa rõ nguyên nhân) → hệ thống **chỉ block ~30 phút** để tiếp nhận + khám sơ bộ, không cam kết giờ sửa xong.

   Mặc định **không cho khách chọn đích danh Technician** — chỉ hiện tuỳ chọn chọn Technician khi dịch vụ được đánh dấu `isMasterTechRequired = true` (VIP/thợ chuyên biệt). Được ưu đãi giảm giá đặt trước nếu có.

2. **Received** — hai nhánh tạo work order:
   - (a) Khách đã đặt lịch: Staff check-in appointment → hệ thống tự tạo WorkOrder, kế thừa ưu đãi.
   - (b) Khách vãng lai: Staff tạo WorkOrder trực tiếp, không qua appointment.

   Chụp ảnh hiện trạng xe là bắt buộc ở bước này. Ngay sau khi tạo, hệ thống **tự động auto-assign Technician + Bay** (không cần người duyệt) — xem chi tiết thuật toán ở mục 12.

3. **Diagnosing** — Technician được auto-assign bấm **Accept** → `WorkOrder.status = Diagnosing`, Technician chuyển trạng thái nội bộ `DIAGNOSING`. Technician chọn hạng mục nghi cần sửa từ ServiceCatalog, quét mã phụ tùng, chụp ảnh/video minh chứng, ghi chú, và nhập `estimatedLaborHours` (chỉ thời gian thao tác kỹ thuật thuần, không phải ngày giao xe). Ký xác nhận điện tử → tạo `DiagnosisRecord` **bất biến** (immutable).

4. **DiagnosisConfirmed** — ngay sau khi Technician ký xác nhận:
   - Nếu `estimatedLaborHours ≤ 2 giờ` → **Fast-lane**, đi thẳng vào hàng chờ chung, xử lý ngay khi có Technician + Bay phù hợp trống.
   - Nếu `estimatedLaborHours > 2 giờ` → **Heavy Repair**, hệ thống tự tách WorkOrder khỏi khung giờ đặt lịch ban đầu, đẩy vào **Repair Queue** riêng, yêu cầu xếp lịch Bay/Technician riêng, không xen ngang vào khung Auto-Assign mặc định.
   - Technician quay về `FREE` hoặc chuyển sang task tiếp theo trong queue cá nhân.

5. **QuotePending** — hệ thống tự sinh `Quote` từ `DiagnosisRecord` (đơn giá lấy từ ServiceCatalog, Staff **không** tự nhập/sửa số liệu). Hệ thống tự tính `systemSuggestedDate` theo công thức ở mục 12. Staff xem con số đề xuất, **xác nhận hoặc tăng thêm buffer** (guard cứng: `finalEstimatedDate ≥ systemSuggestedDate`, không được giảm xuống dưới mức hệ thống đề xuất). Gửi Quote + `finalEstimatedDate` cho khách (in-app + email), đồng thời sinh `ApprovalToken`. Technician chuyển sang `WAITING_ON_CUSTOMER` — được nhận Diagnosing xe khác trong lúc chờ, chưa nhận `InRepair` mới.

6. **Khách phản hồi**:
   - **Approved** → chuyển **InRepair**. Nếu Technician đang `FREE` → bắt đầu sửa ngay; nếu đang bận `DIAGNOSING` xe khác → WorkOrder xếp vào hàng đợi ưu tiên cao của Technician đó, tự kích hoạt khi xong việc hiện tại.
   - **Rejected** → tính phí kiểm tra (nếu có cấu hình) → **Cancelled**.

7. **InRepair** — Technician chuyển `IN_REPAIR` — khoá cứng, không nhận việc mới (Diagnosing lẫn InRepair khác). Xuất phụ tùng qua quét mã → tự tạo `InventoryTransaction`. Thiếu phụ tùng → **WaitingParts**, Technician chuyển `WAITING_PARTS` (được nhận Diagnosing xe khác trong lúc chờ hàng), hàng về → tự quay lại **InRepair**. Trễ so với `finalEstimatedDate` → tự đánh dấu `IsDelayed=true`, gửi thông báo gia hạn. Phát sinh hạng mục/thời gian mới → xử lý qua `ChangeRequest` (mục 14).

8. **Completed** — Technician đánh dấu hoàn tất từng hạng mục, chụp ảnh sau sửa. `WorkOrder.status = Completed`, chờ khách thanh toán và nhận xe. Technician quay về `FREE`, hệ thống tự đẩy task tiếp theo trong queue cá nhân lên (nếu có).

9. **Delivered** — Staff xử lý thanh toán (số tiền lấy từ Quote đã duyệt + `ChangeRequest` đã xác nhận, không sửa tay). Validate: tổng `commissionSplitPercent` của mọi `WorkOrderAssignment` thuộc WO này phải = 100% trước khi cho đóng. Giao xe, đóng `WorkOrder`.

Customer có đúng 2 điểm chạm với hệ thống: (1) đặt lịch + duyệt/từ chối báo giá, (2) xem trang "Tiến trình sửa xe" (trạng thái hiện tại, ETA, lịch sử thông báo). Cả hai portal (staff, customer) dùng chung một bộ API, phân quyền qua Role.

## 4. Bảng chuyển trạng thái Appointment

`Appointment.ServiceType` (`StandardService` | `GeneralDiagnosis`) quyết định khung giờ block — xem mục 3 bước 1.

| Từ | Sang | Điều kiện / Actor |
| --- | --- | --- |
| Booked | CheckedIn (tạo WorkOrder mới, Received) | Khách tới đúng hẹn, Staff check-in |
| Booked | Cancelled | Khách huỷ trước giờ hẹn |
| Booked | NoShow | Quá giờ hẹn ~15 phút chưa check-in → hệ thống tự `IsLate=true`, vào danh sách "cần gọi khách"; nếu khách muốn dời lịch → cập nhật lại `ScheduledDate`/`ScheduledTimeSlot` (giữ Booked); nếu không liên lạc được / khách xác nhận không tới → Staff đánh dấu NoShow |

## 5. Bảng chuyển trạng thái WorkOrder

| Từ | Sang | Điều kiện / Actor |
| --- | --- | --- |
| Received | Diagnosing | Technician (được auto-assign) bấm Accept |
| Diagnosing | DiagnosisConfirmed | Technician ký xác nhận, nhập `estimatedLaborHours` |
| DiagnosisConfirmed | QuotePending | Hệ thống tự sinh Quote + `systemSuggestedDate`; Staff xác nhận/tăng buffer rồi gửi báo giá |
| QuotePending | Approved → InRepair | Customer duyệt |
| QuotePending | Rejected → Cancelled | Customer từ chối |
| InRepair | WaitingParts | Technician đánh dấu thiếu phụ tùng |
| WaitingParts | InRepair | Phụ tùng về kho |
| InRepair | Completed | Technician xác nhận sửa xong |
| Completed | Delivered | Staff ghi nhận thanh toán tiền mặt (guard: tổng `commissionSplitPercent` của `WorkOrderAssignment` = 100%), HOẶC hệ thống tự chuyển khi webhook cổng thanh toán xác nhận |

> `DiagnosisConfirmed` là trạng thái mới so với bản trước v5 — không được bỏ qua khi implement transition `send-quote`; `estimatedLaborHours > 2 giờ` chỉ đổi routing (tách vào Repair Queue), **không** đổi tên trạng thái WorkOrder.

### Ràng buộc nghiệp vụ bắt buộc validate ở tầng API (không chỉ UI)

- Không được chuyển sang **InRepair** nếu báo giá chưa **Approved**.
- Không được tạo **Payment** nếu WorkOrder chưa **Completed**.
- Không được xoá/sửa **QuotationItem** sau khi khách đã **Approved**.
- **Customer** chỉ được Approve/Reject báo giá và thanh toán online **của chính mình**.
- **Staff/Technician** đổi các trạng thái nội bộ (chẩn đoán, sửa chữa, ghi nhận tiền mặt).
- **Trạng thái thanh toán online chỉ được đổi bởi hệ thống** khi xác thực đúng webhook — không actor người nào được tự ý đổi.
- **`finalEstimatedDate` không được nhỏ hơn `systemSuggestedDate`** — Staff chỉ được tăng buffer, không được giảm (mục 3 bước 5).
- **Auto-assign Technician + Bay là bắt buộc join 2 điều kiện cùng lúc** (Technician rảnh VÀ Bay đúng loại đang `FREE`) — không được gán Technician đứng chờ Bay (mục 12).
- **Reassign Technician khi WorkOrder đang `IN_REPAIR`** bắt buộc Staff/Admin duyệt tay kèm ghi chú hiện trạng bàn giao — không cho auto-reassign (mục 13).
- **Không cho reassign sau khi WorkOrder đã `Completed`** — rủi ro gian lận số liệu chia hoa hồng (mục 13).
- **`ChangeRequest` vượt bất kỳ ngưỡng nào trong 3 ngưỡng đã chốt** (mục 14) bắt buộc Admin duyệt qua alert trước khi merge vào Quote — không được auto-approve.
- **Technician luôn phải xác nhận (ký) `ChangeRequest` trước**, bất kể có vượt ngưỡng hay không.
- **Tổng `commissionSplitPercent` của mọi `WorkOrderAssignment` thuộc một WorkOrder phải = 100%** trước khi cho phép chuyển sang **Delivered**.

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

## 10. Bảng trạng thái Technician

| Trạng thái | Nhận Diagnosing mới? | Nhận InRepair mới? |
| --- | --- | --- |
| `FREE` | ✅ | ✅ |
| `DIAGNOSING` | ⚠️ Có thể chen thêm | ❌ |
| `WAITING_ON_CUSTOMER` | ✅ | ❌ (ưu tiên quay lại xe này khi được duyệt) |
| `WAITING_PARTS` | ✅ | ❌ (ưu tiên quay lại xe này khi hàng về) |
| `IN_REPAIR` | ❌ | ❌ (khoá cứng) |

## 11. Auto-assign Technician + Bay (hoàn toàn tự động, không cần người duyệt)

Chạy ngay khi WorkOrder vào **Received**:

- Ưu tiên Technician đang `FREE` → ít việc nhất → đúng chuyên môn (nếu có phân loại kỹ năng).
- `IN_REPAIR` bị loại hoàn toàn khỏi danh sách ứng viên (khoá cứng) — kể cả để chen `Diagnosing`.
- Nếu dịch vụ là VIP có `requestedTechnicianId` (do `isMasterTechRequired = true`) và người đó đang bận → gán tạm người khác trong Pool, gửi thông báo cho khách/Staff biết.
- Nếu không còn ai khả dụng → đẩy WorkOrder vào hàng chờ chung, gửi alert cho Staff (danh sách "Cần xử lý" — mục 15).
- **Bắt buộc join đồng thời 2 điều kiện**: Technician rảnh (theo quy tắc trên) **VÀ** Bay đúng loại đang `FREE` (xem mục 12). Nếu Technician rảnh nhưng không có Bay phù hợp → WorkOrder vào hàng chờ Bay (không gán Technician đứng chờ), `bayWaitTime` được tính từ thời điểm Bay phù hợp gần nhất dự kiến trống.

## 12. Quản lý tài nguyên Bay (khoang/cầu nâng)

Bay là tài nguyên vật lý giới hạn (VD: xưởng có 5 Technician nhưng chỉ 3 Lift Bay + 2 Tire Bay) — auto-assign không thể chỉ tính Technician, vì thợ rảnh nhưng không có Bay phù hợp thì vẫn không làm được.

- `Bay.type`: `LiftBay` | `TireBay` | `GeneralBay`... — loại khoang, khớp với loại dịch vụ cần.
- `Bay.status`: `FREE` | `OCCUPIED` | `MAINTENANCE`.
- `ServiceCatalogItem.requiredBayType` (nullable): hạng mục nào bắt buộc loại Bay gì (VD: hạ hộp số → `LiftBay`).

Công thức tính `systemSuggestedDate` khi WorkOrder vào `QuotePending`:

```
systemSuggestedDate = now()
  + estimatedLaborHours (từ Technician)
  + queueDelay (Technician đó đang ôm bao nhiêu WO trước đó)
  + partsWaitTime (nếu thiếu hàng, lấy ETA từ nhà cung cấp/Inventory)
  + bayWaitTime (thời gian chờ đến khi có Bay phù hợp trống)
  + qcAndWashBuffer (buffer cố định theo loại dịch vụ)
  + serviceBuffer (buffer "under-promise", % hoặc giờ cố định theo policy xưởng)
```

> ⚠️ **Chưa chốt, phải hỏi người dùng trước khi code phần này**: (1) danh sách cụ thể `requiredBayType` cho từng nhóm hạng mục trong ServiceCatalog; (2) số lượng Bay từng loại xưởng hiện có (để seed data); (3) giá trị cụ thể của `qcAndWashBuffer` và `serviceBuffer` (cố định phút/giờ hay theo %). Ba điểm mở này nằm ngoài phạm vi đã chốt của v5 — không tự chọn số mặc định.

## 13. Reassign Technician giữa chừng & chia hoa hồng

Thay vì `WorkOrder.technicianId` chỉ 1 người, dùng bảng `WorkOrderAssignment` riêng để lưu vết đóng góp của từng Technician (xem field đầy đủ ở `03-data-model.md`) — cơ sở chia hoa hồng công bằng khi có handoff giữa chừng.

**Guard theo trạng thái Technician cũ lúc reassign:**

| Trạng thái Technician cũ | Cách xử lý |
| --- | --- |
| `IN_REPAIR` (đã tháo dở dang) | Bắt buộc Staff/Admin duyệt tay, ghi chú tình trạng bàn giao (ảnh/note hiện trạng dở dang) |
| `WAITING_PARTS` (chưa động tay) | Cho phép auto-reassign cho Technician khác đang rảnh trong Pool, không cần duyệt tay |
| Sau khi đã `Completed` | Không cho reassign — chỉ là rủi ro gian lận số liệu lương |

% chia hoa hồng: hệ thống gợi ý theo tỷ lệ `laborHoursLogged`, Staff/Admin là người xác nhận cuối (`approvedBy`, có thể điều chỉnh theo độ khó thực tế từng giai đoạn). Mọi thay đổi % đều ghi `AuditLog`. Tổng `commissionSplitPercent` của mọi dòng `WorkOrderAssignment` thuộc 1 WorkOrder phải = 100% trước khi cho đóng ở **Delivered** (mục 5).

## 14. ChangeRequest — kiểm soát theo ngưỡng rủi ro (đã chốt số)

Không giới hạn số lần phát sinh trong lúc `InRepair`. Technician luôn phải xác nhận (ký) hạng mục phát sinh trước, bất kể ngưỡng. Sau khi Technician xác nhận, hệ thống tự **auto-approve** (merge thẳng vào Quote, không cần Admin duyệt) **nếu thoả cả 3 điều kiện**:

| Điều kiện | Kết quả |
| --- | --- |
| `costDeltaPercent ≤ 10–15%` **VÀ** `costDeltaAbsolute ≤ 1.000.000đ` **VÀ** `timeDeltaHours ≤ 4 giờ` | Auto-approve, Staff/Technician tự xử lý (Staff có thể tự gọi điện xin lỗi khách và chốt) |
| Vượt bất kỳ 1 trong 3 điều kiện trên (kể cả % nhỏ nhưng số tiền tuyệt đối lớn, VD hoá đơn 50 triệu tăng 5% = 2,5 triệu) | Bắt buộc gửi alert cho **Admin** duyệt trước khi merge vào Quote và thông báo khách |

`timeDeltaHours > 4 giờ` (khả năng xe nằm qua đêm) cũng bắt buộc Admin biết để sắp xếp lại lịch toàn xưởng. Chi tiết field `ChangeRequest` xem `03-data-model.md`.

## 15. Mô hình Exception-Based Management (bỏ Foreman/Kanban)

Không có vai trò điều phối riêng (Foreman) và không dùng giao diện Kanban toàn cảnh — hệ thống tự chạy, chỉ báo động khi cần người quyết định:

| Việc | Cơ chế thay thế |
| --- | --- |
| Cân tải, gán Technician | Thuật toán auto-assign 100% tự động, không cần người duyệt (mục 11) |
| Theo dõi ai đang bận/rảnh | Hệ thống tự tính nội bộ, không cần dashboard trực quan liên tục |
| Reassign khi Technician nghỉ đột xuất | Alert đẩy tới Staff/Admin kèm nút "Reassign ngay" + gợi ý Technician rảnh |
| Duyệt ChangeRequest vượt ngưỡng | Alert đẩy tới Admin, duyệt ngay trong thông báo |
| Không còn Technician khả dụng | Alert đẩy tới Staff, WorkOrder vào hàng chờ chung |

**Giao diện chính cho Staff/Admin**: List view, mặc định lọc tab "Cần xử lý" (WO trễ hạn, ChangeRequest chờ duyệt, Technician báo nghỉ cần reassign, WaitingParts quá lâu) — không dùng Kanban toàn cảnh.

**Giao diện Technician**: List view rút gọn, chỉ hiện queue cá nhân, sắp theo priority (ưu tiên xe đã duyệt giá/đã có phụ tùng trước xe mới cần chẩn đoán).

## 16. Phạm vi (scope) — bám sát để không tự mở rộng

**Trong phạm vi (in-scope)**: quản lý user & phân quyền, khách hàng & xe, work order/chẩn đoán/báo giá, duyệt/từ chối giá, tiến độ sửa & phụ tùng, thanh toán & hoá đơn, tìm kiếm/lọc/báo cáo cơ bản, đặt lịch trước & check-in, auto-assign Technician + Bay, tách Repair Queue theo `estimatedLaborHours`, reassign Technician & chia hoa hồng (`WorkOrderAssignment`), `ChangeRequest` theo ngưỡng rủi ro, danh sách "Cần xử lý" (exception-based, mục 15).

**Ngoài phạm vi ở giai đoạn hiện tại (out-of-scope trừ khi được yêu cầu rõ)**: cổng thanh toán online thật (VNPay/Momo) trừ khi task nói rõ nâng cấp; app mobile riêng; tích hợp SMS thật; các mục mức **Could** (xem `docs/05-functional-requirements.md`) không tự làm trước khi các mục **Must**/**Should** xong.
