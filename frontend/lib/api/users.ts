import { apiFetch } from "./client";

export interface UserResponse {
  id: number;
  username: string;
  fullName: string;
  phone: string | null;
  email: string | null;
  role: "Staff" | "Technician" | "Admin";
  technicianStatus: string | null;
}

export function listInternalUsers(token: string) {
  return apiFetch<UserResponse[]>("/api/users", { token });
}
