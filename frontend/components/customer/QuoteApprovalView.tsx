import { Button } from "@/components/shared/Button";
import { TicketCard } from "@/components/shared/TicketCard";
import { StatusStamp } from "@/components/shared/StatusStamp";
import { formatCurrency } from "@/lib/mock/data";
import type { WorkOrderView } from "@/types/domain";
import styles from "./QuoteApprovalView.module.css";

interface QuoteApprovalViewProps {
  token: string;
  expiredOrUsed: boolean;
  workOrder: WorkOrderView;
  outcome: "approved" | "rejected" | null;
  approve: () => void;
  reject: () => void;
}

export function QuoteApprovalView({ expiredOrUsed, workOrder, outcome, approve, reject }: QuoteApprovalViewProps) {
  return (
    <div className={styles.page}>
      <div className={styles.content}>
        <div className={styles.logo}>
          GARA<span>CARE</span>
        </div>

        {expiredOrUsed ? (
          <div className={styles.expiredBanner}>
            Link duyệt báo giá này đã hết hạn hoặc đã được sử dụng. Vui lòng liên hệ gara để được gửi lại link mới.
          </div>
        ) : (
          <div className={styles.layout}>
            <div style={{ position: "relative" }}>
              <TicketCard code={workOrder.code}>
                {outcome && <StatusStamp outcome={outcome} />}
                <div className={`${styles.mono} ${styles.desc}`}>{workOrder.vehicleLabel} · {workOrder.licensePlate}</div>
                <p className={styles.desc}>{workOrder.diagnosisNote}</p>
                <table className={styles.itemTable}>
                  <thead>
                    <tr>
                      <th>Mô tả</th>
                      <th className={styles.right}>SL</th>
                      <th className={styles.right}>Đơn giá</th>
                      <th className={styles.right}>Thành tiền</th>
                    </tr>
                  </thead>
                  <tbody>
                    {workOrder.items.map((item) => (
                      <tr key={item.id}>
                        <td>{item.description}</td>
                        <td className={`${styles.mono} ${styles.right}`}>{item.quantity}</td>
                        <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(item.unitPrice)}</td>
                        <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(item.quantity * item.unitPrice)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </TicketCard>
            </div>

            <div className={styles.panel}>
              <span className={styles.totalLabel}>Tổng tiền</span>
              <span className={styles.totalValue}>{formatCurrency(workOrder.totalAmount)}</span>
              {!outcome ? (
                <>
                  <Button fullWidth onClick={approve}>Đồng ý báo giá</Button>
                  <Button variant="secondary" fullWidth onClick={reject}>Từ chối</Button>
                </>
              ) : (
                <p className={styles.outcomeBanner}>
                  {outcome === "approved" ? "Đã duyệt báo giá — gara sẽ tiến hành sửa chữa." : "Đã ghi nhận từ chối báo giá."}
                </p>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
