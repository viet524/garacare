// Quyết định lưu JWT (httpOnly cookie ưu tiên hơn localStorage) chưa được chốt —
// xem docs/08-frontend-conventions.md. Đây chỉ là điểm nối để implement sau,
// không tự chọn cơ chế lưu token khi code chức năng auth thật.

export interface Session {
  token: string;
  role: "Customer" | "Staff" | "Technician" | "Admin";
  userId: number;
}

export function getSession(): Session | null {
  throw new Error("Not implemented — chưa chốt cơ chế lưu JWT, xem docs/08-frontend-conventions.md");
}
