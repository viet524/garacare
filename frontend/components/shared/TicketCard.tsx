import type { ReactNode } from "react";
import styles from "./TicketCard.module.css";

interface TicketCardProps {
  code: string;
  title?: string;
  onSteel?: boolean;
  headerRight?: ReactNode;
  children: ReactNode;
  className?: string;
}

// Thẻ phiếu sửa chữa — component đặc trưng (design.md §5.1): vòng lỗ đục góc trái,
// đường viền đứt nét thay cho divider thường.
export function TicketCard({ code, title = "Phiếu sửa chữa", onSteel = false, headerRight, children, className }: TicketCardProps) {
  return (
    <div className={`${styles.ticket} ${onSteel ? styles.onSteel : ""} ${className ?? ""}`}>
      <div className={styles.head}>
        <span className={styles.hole} aria-hidden="true" />
        <div className={styles.headText}>
          <div className={styles.title}>{title}</div>
          <div className={styles.code}>#{code}</div>
        </div>
        {headerRight}
      </div>
      <div className={styles.body}>{children}</div>
    </div>
  );
}
