import { Button } from "@/components/shared/Button";
import { formatCurrency } from "@/lib/mock/data";
import styles from "./RevenueReportView.module.css";

interface RevenueBreakdown { method: string; amount: number; percent: number }
interface RevenueResult { totalRevenue: number; totalTransactions: number; breakdownByMethod: RevenueBreakdown[] }

interface RevenueReportViewProps {
  from: string;
  setFrom: (v: string) => void;
  to: string;
  setTo: (v: string) => void;
  result: RevenueResult;
  error: string | null;
  view: () => void;
}

export function RevenueReportView({ from, setFrom, to, setTo, result, error, view }: RevenueReportViewProps) {
  return (
    <div>
      <h1 className={styles.title}>Báo cáo doanh thu</h1>
      <div className={styles.filterRow}>
        <input type="date" className={styles.dateInput} value={from} onChange={(e) => setFrom(e.target.value)} />
        <span style={{ color: "#A9B0B4" }}>–</span>
        <input type="date" className={styles.dateInput} value={to} onChange={(e) => setTo(e.target.value)} />
        <Button onClick={view}>Xem báo cáo</Button>
      </div>
      {error && <p className={styles.errorText}>{error}</p>}

      <div className={styles.cards}>
        <div className={styles.card}>
          <div className={styles.cardLabel}>Tổng doanh thu</div>
          <div className={styles.cardValue}>{formatCurrency(result.totalRevenue)}</div>
        </div>
        <div className={styles.card}>
          <div className={styles.cardLabel}>Tổng giao dịch</div>
          <div className={styles.cardValue} style={{ color: "#EDEFEE" }}>{result.totalTransactions}</div>
        </div>
      </div>

      <table className={styles.table}>
        <thead>
          <tr>
            <th>Phương thức</th>
            <th className={styles.right}>Doanh thu</th>
            <th className={styles.right}>Tỷ trọng</th>
          </tr>
        </thead>
        <tbody>
          {result.breakdownByMethod.map((b) => (
            <tr key={b.method}>
              <td>{b.method}</td>
              <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(b.amount)}</td>
              <td className={`${styles.mono} ${styles.right}`}>{b.percent}%</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
