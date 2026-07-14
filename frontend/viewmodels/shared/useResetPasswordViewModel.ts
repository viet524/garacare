"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

export function useResetPasswordViewModel() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [email, setEmail] = useState(searchParams.get("email") ?? "");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (newPassword !== confirmNewPassword) {
      setError("Mật khẩu xác nhận không khớp.");
      return;
    }
    setIsSubmitting(true);
    setError(null);
    try {
      await authApi.resetPassword({ email, code, newPassword, confirmNewPassword });
      setDone(true);
      window.setTimeout(() => router.push("/"), 1500);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không đặt lại được mật khẩu — kiểm tra lại kết nối mạng và thử lại.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return { email, setEmail, code, setCode, newPassword, setNewPassword, confirmNewPassword, setConfirmNewPassword, error, isSubmitting, done, submit };
}
