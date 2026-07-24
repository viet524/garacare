"use client";

import { useEffect, useState } from "react";
import { getSession } from "@/lib/auth/session";
import {
  confirmDiagnosis as confirmDiagnosisApi,
  getById,
  sendQuote as sendQuoteApi,
  startDiagnosis as startDiagnosisApi,
} from "@/lib/api/workorders";
import { addItem as addItemApi, removeItem as removeItemApi } from "@/lib/api/quotationItems";
import { ApiError } from "@/lib/api/client";
import type { QuotationItemType } from "@/types/domain";
import type { WorkOrderDetailResponse } from "@/types/workorder";

export function useQuoteBuilderViewModel(workOrderId: number) {
  const [workOrder, setWorkOrder] = useState<WorkOrderDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [diagnosisNote, setDiagnosisNote] = useState("");
  const [estimatedLaborHours, setEstimatedLaborHours] = useState("");
  const [newType, setNewType] = useState<QuotationItemType>("Part");
  const [newDescription, setNewDescription] = useState("");
  const [newQuantity, setNewQuantity] = useState(1);
  const [newUnitPrice, setNewUnitPrice] = useState(0);
  const [estimatedDate, setEstimatedDate] = useState("");
  const [sent, setSent] = useState(false);

  function token() {
    return getSession()?.token ?? "";
  }

  async function reload() {
    const detail = await getById(workOrderId, token());
    setWorkOrder(detail);
    return detail;
  }

  useEffect(() => {
    getById(workOrderId, token())
      .then((detail) => {
        setWorkOrder(detail);
        setDiagnosisNote(detail.diagnosisNote ?? "");
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : "Không thể tải work order."))
      .finally(() => setLoading(false));
  }, [workOrderId]);

  // UC-03 bước 1 (docs/02-use-cases.md): Technician ghi chú nguyên nhân thực tế rồi mới chuyển
  // Received → Diagnosing — bước riêng, Staff/Technician bấm rõ ràng, không còn ngầm chạy khi
  // thêm hạng mục báo giá như trước (đúng "một hành động = một endpoint", docs/06-workflow-rules.md).
  async function startDiagnosis() {
    if (!workOrder || workOrder.status !== "Received" || !diagnosisNote.trim()) return;
    setError(null);
    setLoading(true);
    try {
      await startDiagnosisApi(workOrderId, { diagnosisNote: diagnosisNote.trim() }, token());
      await reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không thể chuyển sang chẩn đoán.");
    } finally {
      setLoading(false);
    }
  }

  // UC-03 bước 3: Technician ký xác nhận + nhập estimatedLaborHours, tạo DiagnosisRecord bất
  // biến, chuyển Diagnosing → DiagnosisConfirmed.
  async function confirmDiagnosis() {
    const hours = Number(estimatedLaborHours);
    if (!workOrder || workOrder.status !== "Diagnosing" || !diagnosisNote.trim() || !(hours > 0)) return;
    setError(null);
    setLoading(true);
    try {
      await confirmDiagnosisApi(workOrderId, { notes: diagnosisNote.trim(), estimatedLaborHours: hours }, token());
      await reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không thể xác nhận chẩn đoán.");
    } finally {
      setLoading(false);
    }
  }

  async function addItem() {
    if (!newDescription || newUnitPrice <= 0) return;
    setError(null);
    try {
      await addItemApi(
        {
          workOrderId,
          type: newType,
          description: newDescription,
          quantity: newQuantity,
          unitPrice: newUnitPrice,
        },
        token(),
      );
      await reload();
      setNewDescription("");
      setNewQuantity(1);
      setNewUnitPrice(0);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không thể thêm hạng mục báo giá.");
    }
  }

  async function removeItem(id: number) {
    setError(null);
    try {
      await removeItemApi(id, token());
      await reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không thể xoá hạng mục báo giá.");
    }
  }

  async function sendQuote(e: React.FormEvent) {
    e.preventDefault();
    if (!estimatedDate) return;
    setError(null);
    setLoading(true);
    try {
      await sendQuoteApi(workOrderId, { finalEstimatedDate: estimatedDate }, token());
      setSent(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không thể gửi báo giá.");
    } finally {
      setLoading(false);
    }
  }

  const items = workOrder?.quotationItems ?? [];
  const totalAmount = workOrder?.totalAmount ?? 0;

  return {
    workOrder,
    loading,
    error,
    items,
    diagnosisNote,
    setDiagnosisNote,
    startDiagnosis,
    estimatedLaborHours,
    setEstimatedLaborHours,
    confirmDiagnosis,
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
