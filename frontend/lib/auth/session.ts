import type { AuthResponse } from "@/types/auth";

// QUYẾT ĐỊNH BẢO MẬT TẠM THỜI: dùng localStorage để không chặn việc nối Auth backend
// thật. docs/08-frontend-conventions.md khuyến nghị httpOnly cookie ưu tiên hơn — CHƯA
// được người dùng xác nhận chính thức, cần chốt lại trước khi lên production (localStorage
// lộ token trước tấn công XSS, cookie thường không có rủi ro này).

const STORAGE_KEY = "garacare.session";

export interface Session {
  token: string;
  refreshToken: string;
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
  if (role === "Customer") return "/customer";
  // Technician gần như không dùng trang danh sách Work Order chung — vào thẳng queue cá nhân.
  if (role === "Technician") return "/staff/queue";
  return "/staff";
}

// Đọc claim "exp" của JWT (không xác thực chữ ký — chỉ dùng để quyết định có nên chủ động
// refresh trước khi render trang hay không; backend vẫn là nơi xác thực token thật sự).
export function isTokenExpired(token: string): boolean {
  try {
    const payloadBase64 = token.split(".")[1];
    const normalized = payloadBase64.replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");
    const payload = JSON.parse(atob(padded)) as { exp?: number };
    if (!payload.exp) return false;
    return Date.now() >= payload.exp * 1000;
  } catch {
    // Token không đọc được (hỏng/không đúng định dạng) — coi như hết hạn để an toàn.
    return true;
  }
}
