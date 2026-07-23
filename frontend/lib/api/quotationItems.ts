import { apiFetch } from "./client";
import type { AddQuotationItemRequest, QuotationItemResponse } from "@/types/workorder";

export function addItem(request: AddQuotationItemRequest, token: string) {
  return apiFetch<QuotationItemResponse>("/api/quotation-items", { method: "POST", body: request, token });
}

export function removeItem(itemId: number, token: string) {
  return apiFetch<void>(`/api/quotation-items/${itemId}`, { method: "DELETE", token });
}
