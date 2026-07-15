"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError, getFieldErrors } from "@/lib/api/client";

export function useResetPasswordViewModel() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [email, setEmail] = useState(searchParams.get("email") ?? "");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setFieldErrors({});
    if (newPassword !== confirmNewPassword) {
      setFieldErrors({ confirmNewPassword: "Mật khẩu xác nhận không khớp." });
      return;
    }
    setIsSubmitting(true);
    try {
      await authApi.resetPassword({ email, code, newPassword, confirmNewPassword });
      setDone(true);
      window.setTimeout(() => router.push("/"), 1500);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Không đặt lại được mật khẩu — kiểm tra lại kết nối mạng và thử lại.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return { email, setEmail, code, setCode, newPassword, setNewPassword, confirmNewPassword, setConfirmNewPassword, error, fieldErrors, isSubmitting, done, submit };
}
