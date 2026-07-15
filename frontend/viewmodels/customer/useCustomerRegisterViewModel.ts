"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError, getFieldErrors } from "@/lib/api/client";

// Field khớp RegisterCustomerRequest backend: FullName, Phone, Email, Password, ConfirmPassword.
// Đăng ký KHÔNG đăng nhập ngay — tài khoản cần xác minh email trước, nên điều hướng sang
// /verify-email thay vì /customer.
export function useCustomerRegisterViewModel() {
  const router = useRouter();
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setFieldErrors({});
    if (password !== confirmPassword) {
      setFieldErrors({ confirmPassword: "Mật khẩu xác nhận không khớp." });
      return;
    }
    setIsSubmitting(true);
    try {
      await authApi.register({ fullName, phone, email, password, confirmPassword });
      router.push(`/verify-email?email=${encodeURIComponent(email)}`);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Không đăng ký được — kiểm tra lại kết nối mạng và thử lại.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return { fullName, setFullName, phone, setPhone, email, setEmail, password, setPassword, confirmPassword, setConfirmPassword, error, fieldErrors, isSubmitting, submit };
}
