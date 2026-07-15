"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError, getFieldErrors } from "@/lib/api/client";

export function useForgotPasswordViewModel() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    setFieldErrors({});
    try {
      const result = await authApi.forgotPassword({ email });
      setMessage(result.message);
      window.setTimeout(() => {
        router.push(`/reset-password?email=${encodeURIComponent(email)}`);
      }, 1200);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Không gửi được yêu cầu — kiểm tra lại kết nối mạng và thử lại.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return { email, setEmail, message, error, fieldErrors, isSubmitting, submit };
}
