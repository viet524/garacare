"use client";

import { useState } from "react";
import { MOCK_NOTIFICATIONS, MOCK_WORK_ORDERS } from "@/lib/mock/data";

// TODO: nối lib/api/workorders.ts (getProgress/approveQuote/rejectQuote) khi GARA-45/27 xong.
export function useWorkOrderProgressViewModel(workOrderId: number) {
  const workOrder = MOCK_WORK_ORDERS.find((w) => w.id === workOrderId) ?? MOCK_WORK_ORDERS[0];
  const relatedNotifications = MOCK_NOTIFICATIONS.filter((n) => n.workOrderId === workOrderId);
  const [outcome, setOutcome] = useState<"approved" | "rejected" | null>(null);
  const unreadCount = MOCK_NOTIFICATIONS.filter((n) => !n.isRead).length;

  const timeline = [
    { label: "Đã tiếp nhận", time: workOrder.receivedDate, note: workOrder.initialDescription },
    ...(workOrder.diagnosisNote ? [{ label: "Đã chẩn đoán", time: workOrder.receivedDate, note: workOrder.diagnosisNote }] : []),
  ];

  return { workOrder, relatedNotifications, timeline, outcome, approve: () => setOutcome("approved"), reject: () => setOutcome("rejected"), unreadCount };
}
