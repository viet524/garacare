"use client";

import { useState } from "react";
import { MOCK_WORK_ORDERS } from "@/lib/mock/data";
import type { WorkOrderView } from "@/types/domain";

// TODO: nối lib/api/workorders.ts (getHistoryByVehicle, getInvoice) khi GARA-21/38 xong.
export function useVehicleHistoryViewModel(vehicleId: number) {
  const licensePlate = vehicleId === 1 ? "30A-123.45" : "30A-999.88";
  const history = MOCK_WORK_ORDERS.filter((w) => w.licensePlate === licensePlate);
  const [invoiceFor, setInvoiceFor] = useState<WorkOrderView | null>(null);

  return { licensePlate, history, invoiceFor, openInvoice: setInvoiceFor, closeInvoice: () => setInvoiceFor(null) };
}
