"use client";

import { useMemo, useState } from "react";
import { MOCK_APPOINTMENTS, MOCK_WORK_ORDERS } from "@/lib/mock/data";
import type { WorkOrderStatus } from "@/types/domain";

// TODO: thay MOCK_WORK_ORDERS bằng lib/api/workorders.search() khi GARA-49 (OData) xong.
export function useWorkOrderListViewModel() {
  const [tab, setTab] = useState<"list" | "calls">("list");
  const [statusFilter, setStatusFilter] = useState<WorkOrderStatus | "all">("all");

  const workOrders = useMemo(() => {
    if (statusFilter === "all") return MOCK_WORK_ORDERS;
    return MOCK_WORK_ORDERS.filter((w) => w.status === statusFilter);
  }, [statusFilter]);

  const followUpWorkOrders = MOCK_WORK_ORDERS.filter((w) => w.needsFollowUpCall);
  const lateAppointments = MOCK_APPOINTMENTS.filter((a) => a.isLate);

  return {
    tab,
    setTab,
    statusFilter,
    setStatusFilter,
    workOrders,
    followUpWorkOrders,
    lateAppointments,
    callCount: followUpWorkOrders.length + lateAppointments.length,
  };
}
