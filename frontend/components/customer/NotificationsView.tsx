import { TopNav } from "@/components/customer/TopNav";
import type { NotificationView } from "@/types/domain";
import styles from "./NotificationsView.module.css";

const TYPE_ICON: Record<NotificationView["type"], string> = {
  QuoteReady: "🧾",
  Delayed: "⏳",
  StatusChanged: "🔧",
  AppointmentConfirmed: "📅",
};

interface NotificationsViewProps {
  notifications: NotificationView[];
  markRead: (id: number) => void;
  unreadCount: number;
}

export function NotificationsView({ notifications, markRead, unreadCount }: NotificationsViewProps) {
  return (
    <div className={styles.page}>
      <TopNav unreadCount={unreadCount} />
      <div className={styles.content}>
        <h1 className={styles.title}>Thông báo</h1>
        <div className={styles.list}>
          {notifications.map((n) => (
            <div key={n.id} className={`${styles.row} ${!n.isRead ? styles.rowUnread : ""}`} onClick={() => markRead(n.id)}>
              <span className={`${styles.dot} ${n.isRead ? styles.dotHidden : ""}`} />
              <span className={styles.icon}>{TYPE_ICON[n.type]}</span>
              <div>
                <div className={styles.message}>{n.message}</div>
                <div className={styles.time}>{n.createdAt}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
