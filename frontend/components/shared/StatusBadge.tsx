import styles from "./StatusBadge.module.css";
import type { AppointmentStatus, WorkOrderStatus } from "@/types/domain";
import { APPOINTMENT_STATUS_LABEL_VI, STATUS_LABEL_VI } from "@/types/domain";

type Tone = "neutral" | "amber" | "teal" | "red";

const WORK_ORDER_TONE: Record<WorkOrderStatus, Tone> = {
  Received: "neutral",
  Diagnosing: "neutral",
  QuotePending: "amber",
  InRepair: "amber",
  WaitingParts: "amber",
  Completed: "teal",
  Delivered: "teal",
  Cancelled: "red",
};

const APPOINTMENT_TONE: Record<AppointmentStatus, Tone> = {
  Booked: "teal",
  CheckedIn: "teal",
  Cancelled: "red",
  NoShow: "red",
};

interface StatusBadgeProps {
  status: WorkOrderStatus | AppointmentStatus;
  kind?: "workOrder" | "appointment";
  onSteel?: boolean;
}

export function StatusBadge({ status, kind = "workOrder", onSteel = false }: StatusBadgeProps) {
  const tone: Tone =
    kind === "appointment"
      ? APPOINTMENT_TONE[status as AppointmentStatus]
      : WORK_ORDER_TONE[status as WorkOrderStatus];
  const label =
    kind === "appointment"
      ? APPOINTMENT_STATUS_LABEL_VI[status as AppointmentStatus]
      : STATUS_LABEL_VI[status as WorkOrderStatus];

  return (
    <span className={`${styles.badge} ${styles[tone]} ${onSteel ? styles.onSteel : ""}`}>
      {label}
    </span>
  );
}
