"use client";

import { MOCK_NOTIFICATIONS, MOCK_WORK_ORDERS } from "@/lib/mock/data";

// TODO: nối lib/api/workorders.ts (danh sách WorkOrder của Customer hiện tại) khi có endpoint tương ứng.
export function useCustomerHomeViewModel() {
  const openWorkOrders = MOCK_WORK_ORDERS.filter((w) => !["Delivered", "Cancelled"].includes(w.status));
  const unreadCount = MOCK_NOTIFICATIONS.filter((n) => !n.isRead).length;
  return { openWorkOrders, unreadCount };
}
