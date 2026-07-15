// Khớp 1-1 với DTO backend GaraCare.Application/DTOs/Customers.

export interface CreateCustomerRequest {
  fullName: string;
  phone: string;
  email: string;
  address?: string;
}

export interface CustomerResponse {
  id: number;
  fullName: string;
  phone: string | null;
  email: string | null;
  address: string | null;
  userId: number | null;
}
