// Model (MVVM) — khớp enum backend (GaraCare.Domain/Enums). Dùng cho cả mock data
// hiện tại lẫn Response DTO thật sau này.

export type WorkOrderStatus =
  | "Received"
  | "Diagnosing"
  | "DiagnosisConfirmed"
  | "QuotePending"
  | "InRepair"
  | "WaitingParts"
  | "Completed"
  | "Delivered"
  | "Cancelled";

export type AppointmentStatus = "Booked" | "CheckedIn" | "Cancelled" | "NoShow";

export type QuotationItemType = "Part" | "Labor";

export interface QuotationItemView {
  id: number;
  type: QuotationItemType;
  description: string;
  quantity: number;
  unitPrice: number;
  isApproved: boolean;
  isUsed: boolean;
  lowStockWarning?: boolean;
}

export interface WorkOrderView {
  id: number;
  code: string;
  licensePlate: string;
  vehicleLabel: string;
  customerName: string;
  customerPhone: string;
  status: WorkOrderStatus;
  receivedDate: string;
  estimatedCompletionDate?: string;
  completedDate?: string;
  isDelayed: boolean;
  delayReason?: string;
  initialDescription: string;
  diagnosisNote?: string;
  items: QuotationItemView[];
  totalAmount: number;
  discountPercent: number;
  needsFollowUpCall?: boolean;
}

export interface AppointmentView {
  id: number;
  customerName: string;
  customerPhone: string;
  vehicleLabel: string;
  licensePlate: string;
  scheduledDate: string;
  scheduledTimeSlot: string;
  status: AppointmentStatus;
  discountPercent: number;
  isLate: boolean;
}

export interface NotificationView {
  id: number;
  type: "QuoteReady" | "Delayed" | "StatusChanged" | "AppointmentConfirmed";
  message: string;
  createdAt: string;
  isRead: boolean;
  workOrderId?: number;
  appointmentId?: number;
}

export const GAUGE_STEPS: WorkOrderStatus[] = [
  "Received",
  "Diagnosing",
  "DiagnosisConfirmed",
  "QuotePending",
  "InRepair",
  "Completed",
  "Delivered",
];

export const STATUS_LABEL_VI: Record<WorkOrderStatus, string> = {
  Received: "Đã tiếp nhận",
  Diagnosing: "Đang chẩn đoán",
  DiagnosisConfirmed: "Đã xác nhận chẩn đoán",
  QuotePending: "Chờ duyệt giá",
  InRepair: "Đang sửa",
  WaitingParts: "Chờ phụ tùng",
  Completed: "Đã hoàn tất",
  Delivered: "Đã giao xe",
  Cancelled: "Đã huỷ",
};

export const APPOINTMENT_STATUS_LABEL_VI: Record<AppointmentStatus, string> = {
  Booked: "Đã đặt",
  CheckedIn: "Đã check-in",
  Cancelled: "Đã huỷ",
  NoShow: "Không tới",
};
