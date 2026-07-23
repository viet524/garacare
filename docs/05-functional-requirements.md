# 05. Functional Requirements (MoSCoW)

**Must** = bắt buộc để chạy đúng luồng. **Should** = nên có, tăng chất lượng. **Could** = làm thêm nếu còn thời gian/tài nguyên.

**Quy tắc dùng bảng này**: khi làm task mới mà không có chỉ định rõ, ưu tiên implement đúng thứ tự Must → Should → Could. **Không tự làm một mục Could trước khi các mục Must/Should liên quan đã xong**, trừ khi người dùng yêu cầu rõ ràng.

## Quản lý người dùng & phân quyền
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-01 | Đăng ký tài khoản Customer | Must |
| FR-02 | Đăng nhập, trả JWT token | Must |
| FR-03 | Phân quyền theo Role (Customer/Staff/Technician/Admin) | Must |
| FR-04 | Admin quản lý danh sách nhân viên (CRUD user nội bộ) | Should |

## Quản lý khách hàng & xe/thiết bị
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-05 | Tạo/tra cứu khách hàng theo số điện thoại | Must |
| FR-06 | Đăng ký xe/thiết bị gắn với khách hàng | Must |
| FR-07 | Xem lịch sử sửa chữa của một xe | Should |

## Tiếp nhận & chẩn đoán
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-08 | Tạo work order (tiếp nhận xe), mặc định Received; chụp ảnh hiện trạng bắt buộc | Must |
| FR-09 | Technician accept việc auto-assign, chuyển Diagnosing | Must |
| FR-09b | Technician ký xác nhận chẩn đoán + nhập `estimatedLaborHours`, tạo `DiagnosisRecord` bất biến, chuyển DiagnosisConfirmed | Must |

## Báo giá & phê duyệt
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-10 | Lập danh sách hạng mục báo giá | Must |
| FR-11 | Gửi báo giá, chuyển QuotePending | Must |
| FR-12 | Khách duyệt/từ chối báo giá | Must |
| FR-13 | Tính phí kiểm tra tối thiểu khi khách từ chối | Could |

## Quản lý sửa chữa & phụ tùng
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-14 | Cập nhật tiến độ (InRepair/WaitingParts/Completed) | Must |
| FR-15 | Trừ tồn kho khi hạng mục được duyệt/sử dụng | Should |
| FR-16 | Cảnh báo phụ tùng sắp hết hàng | Could |

## Thanh toán & hoá đơn
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-17 | Ghi nhận thanh toán tiền mặt/quẹt thẻ tại quầy | Must |
| FR-17b | Thanh toán online qua cổng giả lập nội bộ (webhook) | Must |
| FR-17c | Thay cổng giả lập bằng VNPay/Momo sandbox thật | Could |
| FR-18 | Xuất hoá đơn JSON và XML (content negotiation) | Should |
| FR-19 | Xuất danh sách work order dạng CSV | Could |

## Tìm kiếm, lọc & báo cáo
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-20 | Lọc/sắp xếp work order (OData) | Must |
| FR-21 | Báo cáo doanh thu theo khoảng thời gian (Admin) | Should |
| FR-22 | Thống kê top hạng mục sửa chữa phổ biến | Could |

## Đặt lịch trước & tiếp nhận qua lịch hẹn
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-23 | Đặt lịch trước, áp ưu đãi giảm giá | Must |
| FR-23b | Chọn `ServiceType` (StandardService/GeneralDiagnosis) khi đặt lịch, block khung giờ tương ứng | Must |
| FR-23c | Ẩn tuỳ chọn chọn đích danh Technician, trừ khi `ServiceCatalogItem.IsMasterTechRequired=true` | Must |
| FR-24 | Staff check-in appointment thành work order | Must |
| FR-25 | Khách huỷ lịch hẹn trước giờ hẹn | Should |
| FR-26 | Staff đánh dấu NoShow | Should |
| FR-26b | Khách trễ hẹn: tự đánh dấu IsLate, Staff gọi xử lý (UC-15) | Should |

## Auto-assign Technician & Bay (v5)
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-33 | Auto-assign Technician + Bay tự động khi WorkOrder vào Received (UC-16), không cần người duyệt | Must |
| FR-34 | Tách WorkOrder vào Repair Queue riêng khi `estimatedLaborHours > 2 giờ` (Heavy Repair) | Must |
| FR-35 | Tự tính `SystemSuggestedDate` theo công thức đã chốt (labor + queue + parts + bay + QC/wash buffer + service buffer) | Must |

## Reassign Technician & chia hoa hồng (v5)
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-36 | Reassign Technician giữa chừng, ghi vết qua `WorkOrderAssignment` (UC-17) | Must |
| FR-37 | Bắt buộc Staff/Admin duyệt tay khi reassign lúc Technician đang `IN_REPAIR`; auto-reassign khi `WAITING_PARTS` | Must |
| FR-38 | Chia hoa hồng theo `CommissionSplitPercent`, chặn chuyển **Delivered** nếu tổng ≠ 100% | Must |

## ChangeRequest theo ngưỡng rủi ro (v5)
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-39 | Technician tạo & ký xác nhận `ChangeRequest` khi phát sinh hạng mục/thời gian ngoài Quote (UC-18) | Must |
| FR-40 | Auto-approve `ChangeRequest` khi trong ngưỡng (≤10–15% VÀ ≤1.000.000đ VÀ ≤4 giờ) | Must |
| FR-41 | Gửi alert cho Admin duyệt `ChangeRequest` khi vượt bất kỳ ngưỡng nào | Must |

## Vận hành theo ngoại lệ (Exception-based, v5)
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-42 | Danh sách "Cần xử lý" cho Staff/Admin (WO trễ hạn, ChangeRequest chờ duyệt, cần reassign, WaitingParts quá lâu) | Must |
| FR-43 | Queue cá nhân cho Technician, sắp theo priority | Should |

## Thông báo & theo dõi tiến trình (Customer portal)
| Mã | Chức năng | Ưu tiên |
| --- | --- | --- |
| FR-27 | Thông báo in-app khi có báo giá mới/đổi trạng thái/gia hạn | Must |
| FR-28 | Gửi email song song (SMTP/MailKit) | Must |
| FR-29 | Trang "Tiến trình sửa xe": trạng thái + ETA | Must |
| FR-30 | Đánh dấu IsDelayed, cập nhật ETA mới khi trễ | Must |
| FR-31 | Tự động nhắc (in-app + email) nếu sau 24h chưa duyệt báo giá | Must |
| FR-32 | Đánh dấu NeedsFollowUpCall nếu sau 48h chưa duyệt | Must |

## Khi cắt giảm phạm vi (nếu trễ deadline)

Nếu cần cắt bớt vì thiếu thời gian, thứ tự cắt là: (1) toàn bộ mục **Could** trước (FR-13, FR-16, FR-19, FR-22); (2) sau đó mới cân nhắc rút gọn FR-25/FR-26 về mức tối giản. **Không cắt** Appointment (FR-23/24) hoặc Email (FR-27/28) vì đây là Must-have đã chốt. Việc cắt giảm chỉ thực hiện khi người dùng xác nhận, không tự quyết định cắt khi đang code.
