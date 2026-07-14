"use client";

import { useState } from "react";
import { MOCK_PARTS } from "@/lib/mock/data";

const LOW_STOCK_THRESHOLD = 5;

// TODO: nối lib/api/parts.ts khi GARA-34 xong.
export function usePartsViewModel() {
  const [parts, setParts] = useState(MOCK_PARTS);
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  const [sku, setSku] = useState("");
  const [unitPrice, setUnitPrice] = useState("");
  const [stockQuantity, setStockQuantity] = useState("");

  function addPart(e: React.FormEvent) {
    e.preventDefault();
    if (!name) return;
    setParts((prev) => [
      ...prev,
      { id: Date.now(), name, sku, unitPrice: Number(unitPrice) || 0, stockQuantity: Number(stockQuantity) || 0 },
    ]);
    setName("");
    setSku("");
    setUnitPrice("");
    setStockQuantity("");
    setShowForm(false);
  }

  return { parts, showForm, setShowForm, name, setName, sku, setSku, unitPrice, setUnitPrice, stockQuantity, setStockQuantity, addPart, lowStockThreshold: LOW_STOCK_THRESHOLD };
}
