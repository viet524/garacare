import { formatCurrency } from "@/lib/mock/data";
import styles from "./MockGatewayView.module.css";

interface MockGatewayViewProps {
  transactionRef: string;
  amount: number;
  result: "success" | "cancelled" | null;
  confirmSuccess: () => void;
  cancel: () => void;
}

export function MockGatewayView({ transactionRef, amount, result, confirmSuccess, cancel }: MockGatewayViewProps) {
  return (
    <div className={styles.wrap}>
      <div className={styles.card}>
        <div className={styles.gatewayLogo}>PayGateway Sandbox</div>
        <div className={styles.row}><span>Mã giao dịch</span><span>{transactionRef}</span></div>
        <div className={styles.amount}>{formatCurrency(amount)}</div>
        {!result ? (
          <div className={styles.buttons}>
            <button className={styles.success} onClick={confirmSuccess}>Thanh toán thành công</button>
            <button className={styles.cancel} onClick={cancel}>Huỷ giao dịch</button>
          </div>
        ) : (
          <p className={`${styles.outcome} ${result === "success" ? styles.outcomeSuccess : styles.outcomeCancel}`}>
            {result === "success" ? "Giao dịch thành công — đang chuyển về GaraCare…" : "Giao dịch đã huỷ."}
          </p>
        )}
      </div>
    </div>
  );
}
