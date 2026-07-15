"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError, getFieldErrors } from "@/lib/api/client";
import { homePathForRole, saveSession } from "@/lib/auth/session";

// Sau khi đăng ký, tài khoản chưa dùng được cho tới khi nhập đúng mã (gửi qua email).
// Xác minh thành công sẽ tự đăng nhập (backend trả AuthResponse kèm token).
export function useVerifyEmailViewModel() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [email, setEmail] = useState(searchParams.get("email") ?? "");
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [info, setInfo] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isResending, setIsResending] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    setFieldErrors({});
    setInfo(null);
    try {
      const result = await authApi.verifyEmail({ email, code });
      saveSession(result);
      router.push(homePathForRole(result.role));
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Không xác minh được — kiểm tra lại kết nối mạng và thử lại.");
      }
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
    setFieldErrors({});
    setInfo(null);
    try {
      const result = await authApi.resendVerification({ email });
      setInfo(result.message);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Không gửi lại được mã — thử lại sau.");
      }
    } finally {
      setIsResending(false);
    }
  }

  return { email, setEmail, code, setCode, error, fieldErrors, info, isSubmitting, isResending, submit, resend };
}
