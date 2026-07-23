"use client";

import { useEffect, useMemo, useState } from "react";
import { getSession } from "@/lib/auth/session";
import { list as listWorkOrders } from "@/lib/api/workorders";
import { ApiError } from "@/lib/api/client";
import type { AppointmentView, WorkOrderStatus, WorkOrderView } from "@/types/domain";

function toWorkOrderView(item: {
  id: number;
  status: WorkOrderStatus;
  receivedDate: string;
  totalAmount: number;
  needsFollowUpCall: boolean;
  licensePlate: string;
  vehicleLabel: string;
  customerName: string;
  customerPhone: string | null;
}): WorkOrderView {
  return {
    id: item.id,
    code: `WO-${item.id}`,
    licensePlate: item.licensePlate,
    vehicleLabel: item.vehicleLabel,
    customerName: item.customerName,
    customerPhone: item.customerPhone ?? "",
    status: item.status,
    receivedDate: item.receivedDate,
    isDelayed: false,
    initialDescription: "",
    items: [],
    totalAmount: item.totalAmount,
    discountPercent: 0,
    needsFollowUpCall: item.needsFollowUpCall,
  };
}

// TODO: GARA-49 (OData $filter/$orderby/$top/$skip) sẽ thay list() hiện tại (trả toàn bộ,
// không phân trang) khi danh sách work order lớn hơn.
export function useWorkOrderListViewModel() {
  const [tab, setTab] = useState<"list" | "calls">("list");
  const [statusFilter, setStatusFilter] = useState<WorkOrderStatus | "all">("all");
  const [allWorkOrders, setAllWorkOrders] = useState<WorkOrderView[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = getSession()?.token ?? "";
    listWorkOrders(token)
      .then((items) => setAllWorkOrders(items.map(toWorkOrderView)))
      .catch((err) => setError(err instanceof ApiError ? err.message : "Không thể tải danh sách work order."))
      .finally(() => setLoading(false));
  }, []);

  const workOrders = useMemo(() => {
    if (statusFilter === "all") return allWorkOrders;
    return allWorkOrders.filter((w) => w.status === statusFilter);
  }, [allWorkOrders, statusFilter]);

  const followUpWorkOrders = allWorkOrders.filter((w) => w.needsFollowUpCall);
  // Chưa có API Appointment (GARA-43/48 chưa làm) — chưa có dữ liệu "khách trễ hẹn" thật.
  const lateAppointments: AppointmentView[] = [];

  return {
    tab,
    setTab,
    statusFilter,
    setStatusFilter,
    workOrders,
    followUpWorkOrders,
    lateAppointments,
    callCount: followUpWorkOrders.length + lateAppointments.length,
    loading,
    error,
  };
}
