"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import { homePathForRole, saveSession } from "@/lib/auth/session";

// Sau khi đăng ký, tài khoản chưa dùng được cho tới khi nhập đúng mã (gửi qua email).
// Xác minh thành công sẽ tự đăng nhập (backend trả AuthResponse kèm token).
export function useVerifyEmailViewModel() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [email, setEmail] = useState(searchParams.get("email") ?? "");
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isResending, setIsResending] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    setInfo(null);
    try {
      const result = await authApi.verifyEmail({ email, code });
      saveSession({ token: result.token, role: result.role, userId: result.userId, fullName: result.fullName });
      router.push(homePathForRole(result.role));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không xác minh được — kiểm tra lại kết nối mạng và thử lại.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function resend() {
    if (!email) {
      setError("Nhập email trước khi yêu cầu gửi lại mã.");
      return;
    }
    setIsResending(true);
    setError(null);
    setInfo(null);
    try {
      const result = await authApi.resendVerification({ email });
      setInfo(result.message);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không gửi lại được mã — thử lại sau.");
    } finally {
      setIsResending(false);
    }
  }

  return { email, setEmail, code, setCode, error, info, isSubmitting, isResending, submit, resend };
}
