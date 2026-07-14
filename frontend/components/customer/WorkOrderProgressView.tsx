import { TopNav } from "@/components/customer/TopNav";
import { Button } from "@/components/shared/Button";
import { ProgressGauge } from "@/components/shared/ProgressGauge";
import type { NotificationView, WorkOrderView } from "@/types/domain";
import styles from "./WorkOrderProgressView.module.css";

interface TimelineItem { label: string; time: string; note: string }

interface WorkOrderProgressViewProps {
  workOrder: WorkOrderView;
  relatedNotifications: NotificationView[];
  timeline: TimelineItem[];
  outcome: "approved" | "rejected" | null;
  approve: () => void;
  reject: () => void;
  unreadCount: number;
}

export function WorkOrderProgressView({ workOrder, timeline, outcome, approve, reject, unreadCount }: WorkOrderProgressViewProps) {
  return (
    <div className={styles.page}>
      <TopNav unreadCount={unreadCount} />
      <div className={styles.content}>
        <h1 className={styles.title}>{workOrder.code} · {workOrder.licensePlate}</h1>
        <div className={styles.layout}>
          <div className={styles.gaugeCard}>
            <ProgressGauge status={workOrder.status} isDelayed={workOrder.isDelayed} delayReason={workOrder.delayReason} size={300} />
          </div>
          <div className={styles.timelineCard}>
            {timeline.map((item, i) => (
              <div key={i} className={styles.timelineItem}>
                <div className={styles.timelineLabel}>{item.label}</div>
                <div className={styles.timelineTime}>{item.time}</div>
                <div className={styles.timelineNote}>{item.note}</div>
              </div>
            ))}

            {workOrder.status === "QuotePending" && !outcome && (
              <div className={styles.actions}>
                <Button onClick={approve}>Đồng ý báo giá</Button>
                <Button variant="secondary" onClick={reject}>Từ chối</Button>
              </div>
            )}
            {outcome && (
              <p className={styles.toast}>{outcome === "approved" ? "Đã duyệt báo giá." : "Đã từ chối báo giá."}</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
