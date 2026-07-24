import type { QuotationItemType, WorkOrderStatus } from "./domain";

// Khớp 1-1 với DTO backend GaraCare.Application/DTOs/WorkOrders + DTOs/QuotationItems.

export interface CreateWalkInWorkOrderRequest {
  vehicleId: number;
  initialDescription: string;
}

export interface StartDiagnosisRequest {
  diagnosisNote?: string;
}

export interface ConfirmDiagnosisRequest {
  notes: string;
  estimatedLaborHours: number;
}

export interface SendQuoteRequest {
  finalEstimatedDate: string;
}

export interface WorkOrderResponse {
  id: number;
  vehicleId: number;
  status: WorkOrderStatus;
  receivedDate: string;
  initialDescription: string | null;
  diagnosisNote: string | null;
  totalAmount: number;
  discountPercent: number;
  systemSuggestedDate: string | null;
  finalEstimatedDate: string | null;
  isHeavyRepair: boolean;
  isDelayed: boolean;
  hasOpenWorkOrderWarning: boolean;
}

export interface QuotationItemResponse {
  id: number;
  workOrderId: number;
  partId: number | null;
  type: QuotationItemType;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  isApproved: boolean;
  isUsed: boolean;
  lowStockWarning: boolean;
}

export interface WorkOrderListItemResponse {
  id: number;
  status: WorkOrderStatus;
  receivedDate: string;
  totalAmount: number;
  needsFollowUpCall: boolean;
  licensePlate: string;
  vehicleLabel: string;
  customerName: string;
  customerPhone: string | null;
}

export interface WorkOrderDetailResponse {
  id: number;
  vehicleId: number;
  status: WorkOrderStatus;
  receivedDate: string;
  initialDescription: string | null;
  diagnosisNote: string | null;
  totalAmount: number;
  discountPercent: number;
  systemSuggestedDate: string | null;
  finalEstimatedDate: string | null;
  isHeavyRepair: boolean;
  isDelayed: boolean;
  assignedTechnicianId: number | null;
  quotationItems: QuotationItemResponse[];
}

export interface AddQuotationItemRequest {
  workOrderId: number;
  partId?: number;
  type: QuotationItemType;
  description: string;
  quantity: number;
  unitPrice: number;
}
