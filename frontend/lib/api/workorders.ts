import { apiFetch } from "./client";
import type {
  CreateWalkInWorkOrderRequest,
  SendQuoteRequest,
  StartDiagnosisRequest,
  WorkOrderDetailResponse,
  WorkOrderListItemResponse,
  WorkOrderResponse,
} from "@/types/workorder";

export function createWalkIn(request: CreateWalkInWorkOrderRequest, token: string) {
  return apiFetch<WorkOrderResponse>("/api/workorders", { method: "POST", body: request, token });
}

export function startDiagnosis(workOrderId: number, request: StartDiagnosisRequest, token: string) {
  return apiFetch<WorkOrderResponse>(`/api/workorders/${workOrderId}/start-diagnosis`, {
    method: "POST",
    body: request,
    token,
  });
}

export function sendQuote(workOrderId: number, request: SendQuoteRequest, token: string) {
  return apiFetch<WorkOrderResponse>(`/api/workorders/${workOrderId}/send-quote`, {
    method: "POST",
    body: request,
    token,
  });
}

export function resendQuote(workOrderId: number, token: string) {
  return apiFetch<WorkOrderResponse>(`/api/workorders/${workOrderId}/resend-quote`, { method: "POST", token });
}

export function getById(workOrderId: number, token: string) {
  return apiFetch<WorkOrderDetailResponse>(`/api/workorders/${workOrderId}`, { token });
}

export function list(token: string) {
  return apiFetch<WorkOrderListItemResponse[]>("/api/workorders", { token });
}
