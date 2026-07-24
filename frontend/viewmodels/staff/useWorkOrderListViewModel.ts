"use client";

import { useEffect, useState } from "react";
import { getSession } from "@/lib/auth/session";
import { list as listWorkOrders } from "@/lib/api/workorders";
import { ApiError } from "@/lib/api/client";
import type { AppointmentView, WorkOrderStatus, WorkOrderView } from "@/types/domain";

const PAGE_SIZE = 10;

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

// Danh sách phân trang qua OData $top/$skip/$filter (docs/04-api-contract.md — GARA-49).
export function useWorkOrderListViewModel() {
  const [tab, setTab] = useState<"list" | "calls">("list");
  const [statusFilter, setStatusFilterState] = useState<WorkOrderStatus | "all">("all");
  const [page, setPage] = useState(1);
  const [workOrders, setWorkOrders] = useState<WorkOrderView[]>([]);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Cần toàn bộ danh sách (không phân trang) để tính đúng số lượng "cần gọi điện" — dataset
  // hiện còn nhỏ nên chấp nhận gọi thêm 1 request riêng thay vì tự tính từ trang hiện tại.
  const [followUpWorkOrders, setFollowUpWorkOrders] = useState<WorkOrderView[]>([]);

  function setStatusFilter(status: WorkOrderStatus | "all") {
    setStatusFilterState(status);
    setPage(1);
  }

  useEffect(() => {
    const token = getSession()?.token ?? "";
    setLoading(true);
    setError(null);
    listWorkOrders(token, { page, pageSize: PAGE_SIZE, status: statusFilter === "all" ? undefined : statusFilter })
      .then((items) => {
        setWorkOrders(items.map(toWorkOrderView));
        setHasNextPage(items.length === PAGE_SIZE);
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : "Không thể tải danh sách work order."))
      .finally(() => setLoading(false));
  }, [page, statusFilter]);

  useEffect(() => {
    const token = getSession()?.token ?? "";
    listWorkOrders(token, { page: 1, pageSize: 100 })
      .then((items) => setFollowUpWorkOrders(items.map(toWorkOrderView).filter((w) => w.needsFollowUpCall)))
      .catch(() => setFollowUpWorkOrders([]));
  }, []);

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
    page,
    setPage,
    hasNextPage,
  };
}
