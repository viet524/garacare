import { Button } from "@/components/shared/Button";
import { ProgressGauge } from "@/components/shared/ProgressGauge";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { TicketCard } from "@/components/shared/TicketCard";
import { formatCurrency } from "@/lib/mock/data";
import type { WorkOrderStatus, WorkOrderView } from "@/types/domain";
import styles from "./WorkOrderDetailView.module.css";

interface WorkOrderDetailViewProps {
  workOrder: WorkOrderView;
  status: WorkOrderStatus;
  usedIds: number[];
  toggleUsed: (id: number) => void;
  markWaitingParts: () => void;
  resumeRepair: () => void;
  markCompleted: () => void;
  cashAmount: string;
  setCashAmount: (v: string) => void;
  mismatchWarning: boolean;
  recordCashPayment: (force?: boolean) => void;
  delivered: boolean;
}

export function WorkOrderDetailView({ workOrder, status, usedIds, toggleUsed, markWaitingParts, resumeRepair, markCompleted, cashAmount, setCashAmount, mismatchWarning, recordCashPayment, delivered }: WorkOrderDetailViewProps) {
  return (
    <div className={styles.wrap}>
      <div>
        <TicketCard code={workOrder.code} onSteel headerRight={<StatusBadge status={status} onSteel />}>
          <p className={styles.desc}>{workOrder.initialDescription}</p>
          <ProgressGauge status={status} isDelayed={workOrder.isDelayed} delayReason={workOrder.delayReason} onSteel size={280} />
        </TicketCard>

        <div style={{ marginTop: 24 }}>
          <div className={styles.panelLabel} style={{ marginBottom: 8 }}>Hạng mục báo giá</div>
          <div className={styles.checklist}>
            {workOrder.items.map((item) => {
              const used = usedIds.includes(item.id);
              return (
                <div key={item.id} className={`${styles.checkRow} ${used ? styles.checkRowUsed : ""}`}>
                  <div className={styles.checkLeft}>
                    <input type="checkbox" checked={used} readOnly />
                    <div>
                      <div className={styles.itemDesc}>{item.description}</div>
                      <div className={styles.itemMeta}>{item.quantity} × {formatCurrency(item.unitPrice)}</div>
                    </div>
                  </div>
                  <button className={styles.useBtn} disabled={used} onClick={() => toggleUsed(item.id)}>
                    {used ? "Đã dùng" : "Đánh dấu đã dùng"}
                  </button>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      <div className={styles.panel}>
        {status === "InRepair" && (
          <>
            <div className={styles.panelLabel}>Hành động</div>
            <Button variant="secondary" onSteel onClick={markWaitingParts}>Đánh dấu thiếu phụ tùng</Button>
            <Button onClick={markCompleted}>Hoàn tất sửa chữa</Button>
          </>
        )}
        {status === "WaitingParts" && (
          <>
            <div className={styles.panelLabel}>Hành động</div>
            <Button onClick={resumeRepair}>Tiếp tục sửa</Button>
          </>
        )}
        {status === "Completed" && !delivered && (
          <>
            <div className={styles.panelLabel}>Ghi nhận thanh toán</div>
            <div className={styles.field}>
              <span className={styles.panelLabel} style={{ fontSize: 11 }}>Số tiền thu</span>
              <input className={styles.input} value={cashAmount} onChange={(e) => setCashAmount(e.target.value)} placeholder={String(workOrder.totalAmount)} />
            </div>
            <select className={styles.select} defaultValue="Cash">
              <option value="Cash">Tiền mặt</option>
              <option value="Card">Quẹt thẻ</option>
            </select>
            {mismatchWarning && (
              <div className={styles.mismatch}>
                Số tiền không khớp tổng {formatCurrency(workOrder.totalAmount)} — kiểm tra lại hoặc xác nhận có chủ đích.
                <div style={{ marginTop: 8 }}>
                  <Button variant="danger" onClick={() => recordCashPayment(true)}>Xác nhận dù không khớp</Button>
                </div>
              </div>
            )}
            <Button onClick={() => recordCashPayment(false)}>Ghi nhận thanh toán</Button>
          </>
        )}
        {status === "Delivered" && (
          <div className={styles.deliveredBox}>Đã giao xe — thanh toán hoàn tất.</div>
        )}
      </div>
    </div>
  );
}
