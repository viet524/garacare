"use client";

import { useState } from "react";
import { MOCK_WORK_ORDERS } from "@/lib/mock/data";
import type { WorkOrderStatus } from "@/types/domain";

// TODO: nối lib/api/workorders.ts (markWaitingParts/resumeRepair/markCompleted/recordCashPayment)
// + lib/api/quotationItems.ts (markUsed) khi GARA-32/33/35 xong.
export function useWorkOrderDetailViewModel(workOrderId: number) {
  const base = MOCK_WORK_ORDERS.find((w) => w.id === workOrderId) ?? MOCK_WORK_ORDERS[0];
  const [status, setStatus] = useState<WorkOrderStatus>(base.status);
  const [usedIds, setUsedIds] = useState<number[]>(base.items.filter((i) => i.isUsed).map((i) => i.id));
  const [cashAmount, setCashAmount] = useState("");
  const [mismatchWarning, setMismatchWarning] = useState(false);
  const [delivered, setDelivered] = useState(false);

  function toggleUsed(id: number) {
    setUsedIds((prev) => (prev.includes(id) ? prev : [...prev, id]));
  }

  function markWaitingParts() { setStatus("WaitingParts"); }
  function resumeRepair() { setStatus("InRepair"); }
  function markCompleted() { setStatus("Completed"); }

  function recordCashPayment(force = false) {
    const amount = Number(cashAmount);
    if (!force && Math.abs(amount - base.totalAmount) > 1) {
      setMismatchWarning(true);
      return;
    }
    setMismatchWarning(false);
    setStatus("Delivered");
    setDelivered(true);
  }

  return {
    workOrder: base,
    status,
    usedIds,
    toggleUsed,
    markWaitingParts,
    resumeRepair,
    markCompleted,
    cashAmount,
    setCashAmount,
    mismatchWarning,
    recordCashPayment,
    delivered,
  };
}
