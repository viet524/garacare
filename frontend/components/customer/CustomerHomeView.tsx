import Link from "next/link";
import { TopNav } from "@/components/customer/TopNav";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { Button } from "@/components/shared/Button";
import type { WorkOrderView } from "@/types/domain";
import styles from "./CustomerHomeView.module.css";

interface CustomerHomeViewProps {
  openWorkOrders: WorkOrderView[];
  unreadCount: number;
}

export function CustomerHomeView({ openWorkOrders, unreadCount }: CustomerHomeViewProps) {
  return (
    <div className={styles.page}>
      <TopNav unreadCount={unreadCount} />
      <div className={styles.content}>
        <h1 className={styles.title}>Xe của bạn đang sửa</h1>

        {openWorkOrders.length === 0 ? (
          <div className={styles.empty}>
            <p className={styles.emptyText}>Chưa có xe nào đang sửa. Đặt lịch ngay để giữ chỗ và nhận ưu đãi.</p>
            <Link href="/customer/book">
              <Button>Đặt lịch ngay</Button>
            </Link>
          </div>
        ) : (
          <div className={styles.grid}>
            {openWorkOrders.map((wo) => (
              <Link key={wo.id} href={`/customer/workorders/${wo.id}`} className={styles.miniTicket}>
                <div className={styles.miniHead}>
                  <span className={styles.miniCode}>#{wo.code}</span>
                  <StatusBadge status={wo.status} />
                </div>
                <div className={styles.miniPlate}>{wo.licensePlate}</div>
                <div>{wo.vehicleLabel}</div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
