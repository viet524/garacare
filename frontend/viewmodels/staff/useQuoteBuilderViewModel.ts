"use client";

import { useState } from "react";
import { MOCK_WORK_ORDERS } from "@/lib/mock/data";
import type { QuotationItemType, QuotationItemView } from "@/types/domain";

// TODO: nối lib/api/workorders.ts (startDiagnosis, sendQuote) + lib/api/quotationItems.ts khi GARA-23/24/25 xong.
export function useQuoteBuilderViewModel(workOrderId: number) {
  const base = MOCK_WORK_ORDERS.find((w) => w.id === workOrderId) ?? MOCK_WORK_ORDERS[1];
  const [items, setItems] = useState<QuotationItemView[]>(base.items);
  const [diagnosisNote, setDiagnosisNote] = useState(base.diagnosisNote ?? "");
  const [newType, setNewType] = useState<QuotationItemType>("Part");
  const [newDescription, setNewDescription] = useState("");
  const [newQuantity, setNewQuantity] = useState(1);
  const [newUnitPrice, setNewUnitPrice] = useState(0);
  const [estimatedDate, setEstimatedDate] = useState("");
  const [sent, setSent] = useState(false);

  const totalAmount = items.reduce((sum, i) => sum + i.quantity * i.unitPrice, 0);

  function addItem() {
    if (!newDescription || newUnitPrice <= 0) return;
    setItems((prev) => [
      ...prev,
      {
        id: Date.now(),
        type: newType,
        description: newDescription,
        quantity: newQuantity,
        unitPrice: newUnitPrice,
        isApproved: false,
        isUsed: false,
        lowStockWarning: newType === "Part" && newDescription.toLowerCase().includes("cảm biến"),
      },
    ]);
    setNewDescription("");
    setNewQuantity(1);
    setNewUnitPrice(0);
  }

  function removeItem(id: number) {
    setItems((prev) => prev.filter((i) => i.id !== id));
  }

  function sendQuote(e: React.FormEvent) {
    e.preventDefault();
    setSent(true);
  }

  return {
    workOrder: base,
    items,
    diagnosisNote,
    setDiagnosisNote,
    newType,
    setNewType,
    newDescription,
    setNewDescription,
    newQuantity,
    setNewQuantity,
    newUnitPrice,
    setNewUnitPrice,
    addItem,
    removeItem,
    totalAmount,
    estimatedDate,
    setEstimatedDate,
    sendQuote,
    sent,
  };
}
