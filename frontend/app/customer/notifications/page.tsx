"use client";

import { useNotificationsViewModel } from "@/viewmodels/customer/useNotificationsViewModel";
import { NotificationsView } from "@/components/customer/NotificationsView";

export default function CustomerNotificationsPage() {
  const vm = useNotificationsViewModel();
  return <NotificationsView {...vm} />;
}
