import Link from "next/link";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { formatCurrency } from "@/lib/mock/data";
import type { AppointmentView, WorkOrderStatus, WorkOrderView } from "@/types/domain";
import styles from "./WorkOrderListView.module.css";

const STATUS_OPTIONS: { value: WorkOrderStatus | "all"; label: string }[] = [
  { value: "all", label: "Tất cả trạng thái" },
  { value: "Received", label: "Đã tiếp nhận" },
  { value: "Diagnosing", label: "Đang chẩn đoán" },
  { value: "QuotePending", label: "Chờ duyệt giá" },
  { value: "InRepair", label: "Đang sửa" },
  { value: "WaitingParts", label: "Chờ phụ tùng" },
  { value: "Completed", label: "Đã hoàn tất" },
  { value: "Delivered", label: "Đã giao xe" },
  { value: "Cancelled", label: "Đã huỷ" },
];

// Received/Diagnosing chưa có gì để xem ở trang chi tiết (chưa có hạng mục báo giá) — bấm vào
// mã WO ở 2 trạng thái này phải đưa thẳng Technician/Staff tới trang chẩn đoán & lập báo giá.
const DIAGNOSIS_STATUSES = new Set<WorkOrderStatus>(["Received", "Diagnosing"]);

function workOrderHref(wo: WorkOrderView): string {
  return DIAGNOSIS_STATUSES.has(wo.status)
    ? `/staff/workorders/${wo.id}/quote`
    : `/staff/workorders/${wo.id}`;
}

interface WorkOrderListViewProps {
  tab: "list" | "calls";
  setTab: (t: "list" | "calls") => void;
  statusFilter: WorkOrderStatus | "all";
  setStatusFilter: (s: WorkOrderStatus | "all") => void;
  workOrders: WorkOrderView[];
  followUpWorkOrders: WorkOrderView[];
  lateAppointments: AppointmentView[];
  callCount: number;
  loading: boolean;
  error: string | null;
}

export function WorkOrderListView({ tab, setTab, statusFilter, setStatusFilter, workOrders, followUpWorkOrders, lateAppointments, callCount, loading, error }: WorkOrderListViewProps) {
  return (
    <div>
      <div className={styles.header}>
        <h1 className={styles.title}>Work Order</h1>
      </div>

      <div className={styles.tabs}>
        <button className={`${styles.tab} ${tab === "list" ? styles.tabActive : ""}`} onClick={() => setTab("list")}>
          Danh sách
        </button>
        <button className={`${styles.tab} ${tab === "calls" ? styles.tabActive : ""}`} onClick={() => setTab("calls")}>
          Cần gọi điện ({callCount})
        </button>
      </div>

      {tab === "list" && (
        <>
          <div className={styles.filters}>
            <select className={styles.select} value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as WorkOrderStatus | "all")}>
              {STATUS_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          {error && <p className={styles.empty}>{error}</p>}
          {!error && (
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Mã WO</th>
                  <th>Biển số</th>
                  <th>Khách hàng</th>
                  <th>Trạng thái</th>
                  <th>Ngày nhận</th>
                  <th className={styles.right}>Tổng tiền</th>
                </tr>
              </thead>
              <tbody>
                {workOrders.map((wo) => (
                  <tr key={wo.id}>
                    <td className={styles.mono}>
                      <Link href={workOrderHref(wo)}>{wo.code}</Link>
                    </td>
                    <td className={styles.mono}>{wo.licensePlate}</td>
                    <td>{wo.customerName}</td>
                    <td><StatusBadge status={wo.status} onSteel /></td>
                    <td className={styles.mono}>{new Date(wo.receivedDate).toLocaleDateString("vi-VN")}</td>
                    <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(wo.totalAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          {!error && !loading && workOrders.length === 0 && <p className={styles.empty}>Không có work order nào khớp bộ lọc.</p>}
        </>
      )}

      {tab === "calls" && (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Loại</th>
              <th>Số điện thoại</th>
              <th>Nội dung</th>
              <th className={styles.right}>Hành động</th>
            </tr>
          </thead>
          <tbody>
            {followUpWorkOrders.map((wo) => (
              <tr key={`wo-${wo.id}`} className={styles.callRow}>
                <td><span className={styles.callKind}>Báo giá chưa duyệt</span></td>
                <td className={styles.phone}>{wo.customerPhone}</td>
                <td>Mã WO <Link href={`/staff/workorders/${wo.id}`} className={styles.mono}>{wo.code}</Link> — quá 24h chưa duyệt báo giá.</td>
                <td className={styles.right} />
              </tr>
            ))}
            {lateAppointments.map((ap) => (
              <tr key={`ap-${ap.id}`} className={styles.callRow}>
                <td><span className={styles.callKind}>Khách trễ hẹn</span></td>
                <td className={styles.phone}>{ap.customerPhone}</td>
                <td>{ap.customerName} — hẹn {ap.scheduledTimeSlot} ngày {ap.scheduledDate}, chưa tới.</td>
                <td>
                  <div className={styles.rowActions}>
                    <button className={styles.smallBtn}>Dời lịch</button>
                    <button className={`${styles.smallBtn} ${styles.smallBtnDanger}`}>Không tới</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {tab === "calls" && callCount === 0 && <p className={styles.empty}>Không có trường hợp nào cần gọi điện.</p>}
    </div>
  );
}
