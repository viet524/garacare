import { apiFetch } from "./client";
import type { CreateOwnVehicleRequest, CreateVehicleRequest, VehicleResponse, WorkOrderSummaryResponse } from "@/types/vehicle";

export function createVehicle(request: CreateVehicleRequest, token: string) {
  return apiFetch<VehicleResponse>("/api/vehicles", { method: "POST", body: request, token });
}

// Customer tự đăng ký xe của chính mình.
export function createMine(request: CreateOwnVehicleRequest, token: string) {
  return apiFetch<VehicleResponse>("/api/vehicles/mine", { method: "POST", body: request, token });
}

export function getByCustomer(customerId: number, token: string) {
  return apiFetch<VehicleResponse[]>(`/api/vehicles/by-customer/${customerId}`, { token });
}

// Customer tự xem xe của chính mình — CustomerId lấy từ claim JWT ở backend, không truyền id.
export function getMine(token: string) {
  return apiFetch<VehicleResponse[]>("/api/vehicles/mine", { token });
}

export function getWorkOrderHistory(vehicleId: number, token: string) {
  return apiFetch<WorkOrderSummaryResponse[]>(`/api/vehicles/${vehicleId}/workorders`, { token });
}
