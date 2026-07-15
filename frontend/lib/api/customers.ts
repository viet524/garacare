import { apiFetch, ApiError } from "./client";
import type { CreateCustomerRequest, CustomerResponse } from "@/types/customer";

// Trả null khi không tìm thấy (404) — đây là nhánh hợp lệ của UC-02, không phải lỗi.
export async function findByPhone(phone: string, token: string): Promise<CustomerResponse | null> {
  try {
    return await apiFetch<CustomerResponse>(`/api/customers/by-phone?phone=${encodeURIComponent(phone)}`, { token });
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      return null;
    }
    throw err;
  }
}

export function createCustomer(request: CreateCustomerRequest, token: string) {
  return apiFetch<CustomerResponse>("/api/customers", { method: "POST", body: request, token });
}

export function getAll(token: string) {
  return apiFetch<CustomerResponse[]>("/api/customers", { token });
}
