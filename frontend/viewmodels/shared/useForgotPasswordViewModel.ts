"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

export function useForgotPasswordViewModel() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    try {
      const result = await authApi.forgotPassword({ email });
      setMessage(result.message);
      window.setTimeout(() => {
        router.push(`/reset-password?email=${encodeURIComponent(email)}`);
      }, 1200);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không gửi được yêu cầu — kiểm tra lại kết nối mạng và thử lại.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return { email, setEmail, message, error, isSubmitting, submit };
}
