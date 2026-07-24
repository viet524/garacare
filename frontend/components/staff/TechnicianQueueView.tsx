import Link from "next/link";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { workOrderHref } from "@/lib/workorders/routing";
import type { TechnicianQueueItem } from "@/viewmodels/staff/useTechnicianQueueViewModel";
import styles from "./TechnicianQueueView.module.css";

interface TechnicianQueueViewProps {
  items: TechnicianQueueItem[];
  loading: boolean;
  error: string | null;
}

export function TechnicianQueueView({ items, loading, error }: TechnicianQueueViewProps) {
  return (
    <div>
      <h1 className={styles.title}>Queue của tôi</h1>

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
            </tr>
          </thead>
          <tbody>
            {items.map((wo) => (
              <tr key={wo.id}>
                <td className={styles.mono}>
                  <Link href={workOrderHref(wo.id, wo.status)}>{wo.code}</Link>
                </td>
                <td className={styles.mono}>{wo.licensePlate}</td>
                <td>{wo.customerName}</td>
                <td><StatusBadge status={wo.status} onSteel /></td>
                <td className={styles.mono}>{new Date(wo.receivedDate).toLocaleDateString("vi-VN")}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {!error && !loading && items.length === 0 && <p className={styles.empty}>Chưa có xe nào được gán cho bạn.</p>}
    </div>
  );
}
