"use client";

import { useEffect, useState } from "react";
import { getSession } from "@/lib/auth/session";
import { getMine, getWorkOrderHistory } from "@/lib/api/vehicles";
import { ApiError } from "@/lib/api/client";
import type { WorkOrderSummaryResponse } from "@/types/vehicle";

// TODO: nối GET /workorders/{id}/invoice (GARA-38) khi backend có endpoint — hiện
// WorkOrderSummaryResponse chỉ trả tóm tắt, chưa có danh sách hạng mục để xem hoá đơn.
export function useVehicleHistoryViewModel(vehicleId: number) {
  const [licensePlate, setLicensePlate] = useState("");
  const [history, setHistory] = useState<WorkOrderSummaryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = getSession()?.token ?? "";
    Promise.all([getMine(token), getWorkOrderHistory(vehicleId, token)])
      .then(([vehicles, workOrders]) => {
        const vehicle = vehicles.find((v) => v.id === vehicleId);
        setLicensePlate(vehicle?.licensePlate ?? "");
        setHistory(workOrders);
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : "Không thể tải lịch sử sửa chữa."))
      .finally(() => setLoading(false));
  }, [vehicleId]);

  return { licensePlate, history, loading, error };
}
