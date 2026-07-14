import type { AuthResponse } from "@/types/auth";

// QUYẾT ĐỊNH BẢO MẬT TẠM THỜI: dùng localStorage để không chặn việc nối Auth backend
// thật. docs/08-frontend-conventions.md khuyến nghị httpOnly cookie ưu tiên hơn — CHƯA
// được người dùng xác nhận chính thức, cần chốt lại trước khi lên production (localStorage
// lộ token trước tấn công XSS, cookie thường không có rủi ro này).

const STORAGE_KEY = "garacare.session";

export interface Session {
  token: string;
  role: AuthResponse["role"];
  userId: number;
  fullName: string;
}

export function saveSession(session: Session): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function getSession(): Session | null {
  if (typeof window === "undefined") return null;
  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as Session;
  } catch {
    return null;
  }
}

export function clearSession(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(STORAGE_KEY);
}

export function homePathForRole(role: AuthResponse["role"]): string {
  return role === "Customer" ? "/customer" : "/staff";
}
