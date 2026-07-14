import { TopNav } from "@/components/customer/TopNav";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { TicketCard } from "@/components/shared/TicketCard";
import { formatCurrency } from "@/lib/mock/data";
import type { WorkOrderView } from "@/types/domain";
import styles from "./VehicleHistoryView.module.css";

interface VehicleHistoryViewProps {
  licensePlate: string;
  history: WorkOrderView[];
  invoiceFor: WorkOrderView | null;
  openInvoice: (wo: WorkOrderView) => void;
  closeInvoice: () => void;
}

export function VehicleHistoryView({ licensePlate, history, invoiceFor, openInvoice, closeInvoice }: VehicleHistoryViewProps) {
  return (
    <div className={styles.page}>
      <TopNav />
      <div className={styles.content}>
        <h1 className={styles.title}>Lịch sử sửa chữa · {licensePlate}</h1>

        {history.length === 0 ? (
          <p className={styles.empty}>Chưa có lịch sử sửa chữa cho xe này.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Mã WO</th>
                <th>Ngày nhận</th>
                <th>Ngày giao</th>
                <th>Trạng thái</th>
                <th className={styles.right}>Tổng tiền</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {history.map((wo) => (
                <tr key={wo.id}>
                  <td className={styles.mono}>{wo.code}</td>
                  <td className={styles.mono}>{wo.receivedDate}</td>
                  <td className={styles.mono}>{wo.completedDate ?? "—"}</td>
                  <td><StatusBadge status={wo.status} /></td>
                  <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(wo.totalAmount)}</td>
                  <td>
                    {wo.status === "Delivered" && (
                      <button className={styles.linkBtn} onClick={() => openInvoice(wo)}>Xem hoá đơn</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {invoiceFor && (
        <div className={styles.overlay} onClick={closeInvoice}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <div className={styles.closeRow}>
              <button className={styles.closeBtn} onClick={closeInvoice}>Đóng ✕</button>
            </div>
            <TicketCard code={invoiceFor.code} title="Hoá đơn">
              <div className={styles.mono} style={{ marginBottom: 8 }}>{invoiceFor.vehicleLabel} · {invoiceFor.licensePlate}</div>
              <table className={styles.table} style={{ border: "none" }}>
                <thead>
                  <tr><th>Mô tả</th><th className={styles.right}>SL</th><th className={styles.right}>Thành tiền</th></tr>
                </thead>
                <tbody>
                  {invoiceFor.items.map((item) => (
                    <tr key={item.id}>
                      <td>{item.description}</td>
                      <td className={`${styles.mono} ${styles.right}`}>{item.quantity}</td>
                      <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(item.quantity * item.unitPrice)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <div style={{ textAlign: "right", fontFamily: "var(--font-mono)", fontSize: 20, marginTop: 12 }}>
                {formatCurrency(invoiceFor.totalAmount)}
              </div>
              <button className={styles.printBtn}>In hoá đơn</button>
            </TicketCard>
          </div>
        </div>
      )}
    </div>
  );
}
