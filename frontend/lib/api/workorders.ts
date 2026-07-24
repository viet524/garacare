import { apiFetch } from "./client";
import type {
  ConfirmDiagnosisRequest,
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

export function confirmDiagnosis(workOrderId: number, request: ConfirmDiagnosisRequest, token: string) {
  return apiFetch<WorkOrderResponse>(`/api/workorders/${workOrderId}/confirm-diagnosis`, {
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

export interface ListPageOptions {
  page: number;
  pageSize: number;
  status?: string;
}

// GET /api/workorders hỗ trợ OData $top/$skip/$orderby/$filter (docs/04-api-contract.md — GARA-49).
// [EnableQuery] áp dụng trước khi serialize sang JSON nên $orderby/$filter phải dùng đúng tên
// property C# (PascalCase: Status, ReceivedDate...), không phải tên field camelCase trong JSON
// trả về. Không dùng $count=true — controller chưa cấu hình OData route/EDM model đầy đủ (chỉ
// [EnableQuery] trên Web API JSON thường), tránh rủi ro response bị bọc envelope OData thay vì
// mảng JSON thường. FE tự suy ra "còn trang sau" qua độ dài mảng trả về so với pageSize.
export function list(token: string, { page, pageSize, status }: ListPageOptions) {
  const skip = (page - 1) * pageSize;
  const filter = status ? `&$filter=Status eq '${status}'` : "";
  return apiFetch<WorkOrderListItemResponse[]>(
    `/api/workorders?$top=${pageSize}&$skip=${skip}&$orderby=ReceivedDate desc${filter}`,
    { token },
  );
}

export function getMyQueue(token: string) {
  return apiFetch<WorkOrderListItemResponse[]>("/api/technicians/me/queue", { token });
}
