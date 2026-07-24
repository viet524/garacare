import type { WorkOrderStatus } from "@/types/domain";

// Received/Diagnosing/DiagnosisConfirmed chưa có gì để xem ở trang chi tiết (chưa gửi báo giá)
// — bấm vào mã WO ở các trạng thái này phải đưa thẳng tới trang chẩn đoán & lập báo giá.
const DIAGNOSIS_STATUSES = new Set<WorkOrderStatus>(["Received", "Diagnosing", "DiagnosisConfirmed"]);

export function workOrderHref(id: number, status: WorkOrderStatus): string {
  return DIAGNOSIS_STATUSES.has(status) ? `/staff/workorders/${id}/quote` : `/staff/workorders/${id}`;
}
