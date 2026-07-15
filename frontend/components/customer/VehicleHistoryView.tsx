import { TopNav } from "@/components/customer/TopNav";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { formatCurrency } from "@/lib/mock/data";
import type { WorkOrderSummaryResponse } from "@/types/vehicle";
import styles from "./VehicleHistoryView.module.css";

interface VehicleHistoryViewProps {
  licensePlate: string;
  history: WorkOrderSummaryResponse[];
  loading: boolean;
  error: string | null;
}

export function VehicleHistoryView({ licensePlate, history, loading, error }: VehicleHistoryViewProps) {
  return (
    <div className={styles.page}>
      <TopNav />
      <div className={styles.content}>
        <h1 className={styles.title}>Lịch sử sửa chữa {licensePlate ? `· ${licensePlate}` : ""}</h1>

        {loading && <p className={styles.empty}>Đang tải lịch sử sửa chữa…</p>}
        {error && <p className={styles.empty}>{error}</p>}

        {!loading && !error && history.length === 0 ? (
          <p className={styles.empty}>Chưa có lịch sử sửa chữa cho xe này.</p>
        ) : (
          !loading &&
          !error && (
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Mã WO</th>
                  <th>Ngày nhận</th>
                  <th>Ngày giao</th>
                  <th>Trạng thái</th>
                  <th className={styles.right}>Tổng tiền</th>
                </tr>
              </thead>
              <tbody>
                {history.map((wo) => (
                  <tr key={wo.id}>
                    <td className={styles.mono}>#{wo.id}</td>
                    <td className={styles.mono}>{new Date(wo.receivedDate).toLocaleDateString("vi-VN")}</td>
                    <td className={styles.mono}>{wo.completedDate ? new Date(wo.completedDate).toLocaleDateString("vi-VN") : "—"}</td>
                    <td><StatusBadge status={wo.status} /></td>
                    <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(wo.totalAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )
        )}
      </div>
    </div>
  );
}
