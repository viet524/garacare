// Dữ liệu mẫu dựng UI trước khi có Controller thật ở backend.
// TODO: thay bằng gọi /lib/api/* khi các endpoint tương ứng (GARA-*) đã code xong.
import type { AppointmentView, NotificationView, WorkOrderView } from "@/types/domain";

export const MOCK_WORK_ORDERS: WorkOrderView[] = [
  {
    id: 1,
    code: "WO-2026-0148",
    licensePlate: "30A-123.45",
    vehicleLabel: "Toyota Vios 2020",
    customerName: "Nguyễn Văn An",
    customerPhone: "0912 345 678",
    status: "InRepair",
    receivedDate: "2026-07-10",
    estimatedCompletionDate: "16/07",
    isDelayed: true,
    delayReason: "18/07",
    initialDescription: "Xe kêu lạ khi phanh, đèn báo động cơ sáng.",
    diagnosisNote: "Má phanh trước mòn quá mức, cần thay. Cảm biến oxy lỗi.",
    items: [
      { id: 1, type: "Part", description: "Má phanh trước (bộ)", quantity: 1, unitPrice: 850000, isApproved: true, isUsed: true },
      { id: 2, type: "Part", description: "Cảm biến oxy", quantity: 1, unitPrice: 1200000, isApproved: true, isUsed: false, lowStockWarning: true },
      { id: 3, type: "Labor", description: "Công thay má phanh + cảm biến", quantity: 1, unitPrice: 300000, isApproved: true, isUsed: false },
    ],
    totalAmount: 2350000,
    discountPercent: 0,
  },
  {
    id: 2,
    code: "WO-2026-0149",
    licensePlate: "51F-678.90",
    vehicleLabel: "Honda City 2019",
    customerName: "Trần Thị Bích",
    customerPhone: "0987 654 321",
    status: "QuotePending",
    receivedDate: "2026-07-12",
    estimatedCompletionDate: "17/07",
    isDelayed: false,
    initialDescription: "Bảo dưỡng định kỳ 40.000km.",
    diagnosisNote: "Thay dầu, lọc gió, lọc dầu. Lốp sau mòn không đều.",
    items: [
      { id: 4, type: "Part", description: "Dầu động cơ 5W30 (4L)", quantity: 1, unitPrice: 620000, isApproved: false, isUsed: false },
      { id: 5, type: "Part", description: "Lọc gió động cơ", quantity: 1, unitPrice: 180000, isApproved: false, isUsed: false },
      { id: 6, type: "Labor", description: "Công bảo dưỡng định kỳ", quantity: 1, unitPrice: 250000, isApproved: false, isUsed: false },
    ],
    totalAmount: 1050000,
    discountPercent: 10,
    needsFollowUpCall: true,
  },
  {
    id: 3,
    code: "WO-2026-0150",
    licensePlate: "29H-111.22",
    vehicleLabel: "Ford Ranger 2021",
    customerName: "Lê Minh Cường",
    customerPhone: "0909 111 222",
    status: "WaitingParts",
    receivedDate: "2026-07-08",
    estimatedCompletionDate: "20/07",
    isDelayed: true,
    delayReason: "20/07",
    initialDescription: "Điều hoà không mát.",
    diagnosisNote: "Thiếu gas, block điều hoà yếu — cần đặt block mới, chưa về hàng.",
    items: [
      { id: 7, type: "Part", description: "Block điều hoà", quantity: 1, unitPrice: 4200000, isApproved: true, isUsed: false, lowStockWarning: true },
      { id: 8, type: "Part", description: "Gas lạnh R134a", quantity: 2, unitPrice: 180000, isApproved: true, isUsed: true },
      { id: 9, type: "Labor", description: "Công sửa hệ thống điều hoà", quantity: 1, unitPrice: 500000, isApproved: true, isUsed: false },
    ],
    totalAmount: 5060000,
    discountPercent: 0,
  },
  {
    id: 4,
    code: "WO-2026-0142",
    licensePlate: "30A-123.45",
    vehicleLabel: "Toyota Vios 2020",
    customerName: "Nguyễn Văn An",
    customerPhone: "0912 345 678",
    status: "Delivered",
    receivedDate: "2026-06-20",
    completedDate: "2026-06-22",
    isDelayed: false,
    initialDescription: "Thay nhớt, kiểm tra tổng quát.",
    items: [
      { id: 10, type: "Part", description: "Dầu động cơ 5W30 (4L)", quantity: 1, unitPrice: 620000, isApproved: true, isUsed: true },
      { id: 11, type: "Labor", description: "Công thay dầu", quantity: 1, unitPrice: 100000, isApproved: true, isUsed: true },
    ],
    totalAmount: 720000,
    discountPercent: 0,
  },
  {
    id: 5,
    code: "WO-2026-0151",
    licensePlate: "43C-555.66",
    vehicleLabel: "Mazda CX-5 2022",
    customerName: "Phạm Thu Hà",
    customerPhone: "0977 888 999",
    status: "Received",
    receivedDate: "2026-07-14",
    isDelayed: false,
    initialDescription: "Kiểm tra tiếng động lạ ở gầm xe.",
    items: [],
    totalAmount: 0,
    discountPercent: 0,
  },
  {
    id: 6,
    code: "WO-2026-0139",
    licensePlate: "51F-678.90",
    vehicleLabel: "Honda City 2019",
    customerName: "Trần Thị Bích",
    customerPhone: "0987 654 321",
    status: "Cancelled",
    receivedDate: "2026-06-05",
    isDelayed: false,
    initialDescription: "Khách báo tiếng kêu ở hộp số.",
    diagnosisNote: "Báo giá vượt ngân sách khách, khách từ chối sửa.",
    items: [
      { id: 12, type: "Labor", description: "Kiểm tra hộp số", quantity: 1, unitPrice: 200000, isApproved: false, isUsed: false },
    ],
    totalAmount: 200000,
    discountPercent: 0,
  },
];

export const MOCK_APPOINTMENTS: AppointmentView[] = [
  {
    id: 1,
    customerName: "Đỗ Văn Khoa",
    customerPhone: "0933 222 111",
    vehicleLabel: "Kia Morning 2018",
    licensePlate: "30G-888.77",
    scheduledDate: "2026-07-14",
    scheduledTimeSlot: "09:00–10:00",
    status: "Booked",
    discountPercent: 5,
    isLate: true,
  },
  {
    id: 2,
    customerName: "Vũ Thị Lan",
    customerPhone: "0966 333 444",
    vehicleLabel: "Hyundai Accent 2021",
    licensePlate: "29A-333.44",
    scheduledDate: "2026-07-14",
    scheduledTimeSlot: "14:00–15:00",
    status: "Booked",
    discountPercent: 5,
    isLate: false,
  },
  {
    id: 3,
    customerName: "Hoàng Đức Mạnh",
    customerPhone: "0922 555 666",
    vehicleLabel: "Toyota Camry 2020",
    licensePlate: "51G-222.33",
    scheduledDate: "2026-07-15",
    scheduledTimeSlot: "08:00–09:00",
    status: "Booked",
    discountPercent: 0,
    isLate: false,
  },
];

export const MOCK_NOTIFICATIONS: NotificationView[] = [
  { id: 1, type: "QuoteReady", message: "Báo giá cho xe 51F-678.90 đã sẵn sàng — vui lòng duyệt trong 72h.", createdAt: "2 giờ trước", isRead: false, workOrderId: 2 },
  { id: 2, type: "Delayed", message: "Xe 29H-111.22 gia hạn hoàn thành tới 20/07 do chờ phụ tùng.", createdAt: "5 giờ trước", isRead: false, workOrderId: 3 },
  { id: 3, type: "AppointmentConfirmed", message: "Đã xác nhận lịch hẹn 15/07 lúc 08:00 cho xe 51G-222.33.", createdAt: "1 ngày trước", isRead: true, appointmentId: 3 },
  { id: 4, type: "StatusChanged", message: "Xe 30A-123.45 đã giao — cảm ơn bạn đã tin tưởng GaraCare.", createdAt: "3 ngày trước", isRead: true, workOrderId: 4 },
];

export const MOCK_PARTS = [
  { id: 1, name: "Má phanh trước (bộ)", sku: "BRK-001", unitPrice: 850000, stockQuantity: 12 },
  { id: 2, name: "Cảm biến oxy", sku: "SNS-014", unitPrice: 1200000, stockQuantity: 2 },
  { id: 3, name: "Dầu động cơ 5W30 (4L)", sku: "OIL-005", unitPrice: 620000, stockQuantity: 28 },
  { id: 4, name: "Lọc gió động cơ", sku: "FLT-002", unitPrice: 180000, stockQuantity: 15 },
  { id: 5, name: "Block điều hoà", sku: "AC-009", unitPrice: 4200000, stockQuantity: 1 },
  { id: 6, name: "Gas lạnh R134a", sku: "AC-010", unitPrice: 180000, stockQuantity: 40 },
];

export const MOCK_USERS = [
  { id: 1, username: "admin", fullName: "Đặng Quốc Bảo", phone: "0901 000 001", role: "Admin" as const },
  { id: 2, username: "letan.mai", fullName: "Ngô Thị Mai", phone: "0901 000 002", role: "Staff" as const },
  { id: 3, username: "kythuat.hung", fullName: "Bùi Văn Hùng", phone: "0901 000 003", role: "Technician" as const },
];

export const MOCK_REVENUE = {
  totalRevenue: 48250000,
  totalTransactions: 37,
  breakdownByMethod: [
    { method: "Cash", amount: 21400000, percent: 44 },
    { method: "Card", amount: 12100000, percent: 25 },
    { method: "VNPay", amount: 9750000, percent: 20 },
    { method: "Momo", amount: 5000000, percent: 11 },
  ],
};

export function formatCurrency(amount: number): string {
  return amount.toLocaleString("vi-VN") + " đ";
}
