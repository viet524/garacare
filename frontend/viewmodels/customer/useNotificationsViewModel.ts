"use client";

import { useState } from "react";
import { MOCK_NOTIFICATIONS } from "@/lib/mock/data";
import type { NotificationView } from "@/types/domain";

// TODO: nối lib/api/notifications.ts (list/markRead) khi GARA-46 xong.
export function useNotificationsViewModel() {
  const [notifications, setNotifications] = useState<NotificationView[]>(MOCK_NOTIFICATIONS);

  function markRead(id: number) {
    setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
  }

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  return { notifications, markRead, unreadCount };
}
