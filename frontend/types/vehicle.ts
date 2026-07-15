import type { WorkOrderStatus } from "./domain";

// Khớp 1-1 với DTO backend GaraCare.Application/DTOs/Vehicles.

export interface CreateVehicleRequest {
  customerId: number;
  licensePlate: string;
  brand?: string;
  model?: string;
  year?: number;
}

// Customer tự đăng ký xe của chính mình — không có customerId (lấy từ claim JWT ở backend).
export interface CreateOwnVehicleRequest {
  licensePlate: string;
  brand?: string;
  model?: string;
  year?: number;
}

export interface VehicleResponse {
  id: number;
  customerId: number;
  licensePlate: string;
  brand: string | null;
  model: string | null;
  year: number | null;
}

export interface WorkOrderSummaryResponse {
  id: number;
  status: WorkOrderStatus;
  receivedDate: string;
  completedDate: string | null;
  totalAmount: number;
}
