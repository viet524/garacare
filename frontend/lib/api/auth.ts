import { apiFetch } from "./client";
import type {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  MessageResponse,
  RefreshTokenRequest,
  RegisterCustomerRequest,
  ResendVerificationRequest,
  ResetPasswordRequest,
  VerifyEmailRequest,
} from "@/types/auth";

export function register(request: RegisterCustomerRequest) {
  return apiFetch<MessageResponse>("/api/auth/register", { method: "POST", body: request });
}

export function login(request: LoginRequest) {
  return apiFetch<AuthResponse>("/api/auth/login", { method: "POST", body: request });
}

export function verifyEmail(request: VerifyEmailRequest) {
  return apiFetch<AuthResponse>("/api/auth/verify-email", { method: "POST", body: request });
}

export function resendVerification(request: ResendVerificationRequest) {
  return apiFetch<MessageResponse>("/api/auth/resend-verification", { method: "POST", body: request });
}

export function forgotPassword(request: ForgotPasswordRequest) {
  return apiFetch<MessageResponse>("/api/auth/forgot-password", { method: "POST", body: request });
}

export function resetPassword(request: ResetPasswordRequest) {
  return apiFetch<MessageResponse>("/api/auth/reset-password", { method: "POST", body: request });
}

export function refreshToken(request: RefreshTokenRequest) {
  return apiFetch<AuthResponse>("/api/auth/refresh-token", { method: "POST", body: request });
}

export function logout(request: RefreshTokenRequest) {
  return apiFetch<void>("/api/auth/logout", { method: "POST", body: request });
}
