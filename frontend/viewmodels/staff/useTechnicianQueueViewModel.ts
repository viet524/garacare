"use client";

import { useEffect, useState } from "react";
import { getSession } from "@/lib/auth/session";
import { getMyQueue } from "@/lib/api/workorders";
import { ApiError } from "@/lib/api/client";
import type { WorkOrderStatus } from "@/types/domain";

export interface TechnicianQueueItem {
  id: number;
  code: string;
  licensePlate: string;
  customerName: string;
  status: WorkOrderStatus;
  receivedDate: string;
}

// docs/01-business-spec.md §15: queue cá nhân của Technician, sắp theo priority (BE trả sẵn
// thứ tự: InRepair/WaitingParts trước, Received/Diagnosing sau).
export function useTechnicianQueueViewModel() {
  const [items, setItems] = useState<TechnicianQueueItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = getSession()?.token ?? "";
    getMyQueue(token)
      .then((result) =>
        setItems(
          result.map((w) => ({
            id: w.id,
            code: `WO-${w.id}`,
            licensePlate: w.licensePlate,
            customerName: w.customerName,
            status: w.status,
            receivedDate: w.receivedDate,
          })),
        ),
      )
      .catch((err) => setError(err instanceof ApiError ? err.message : "Không thể tải queue cá nhân."))
      .finally(() => setLoading(false));
  }, []);

  return { items, loading, error };
}
