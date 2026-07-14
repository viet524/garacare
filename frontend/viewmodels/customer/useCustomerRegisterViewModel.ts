"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

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
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (password !== confirmPassword) {
      setError("Mật khẩu xác nhận không khớp.");
      return;
    }
    setIsSubmitting(true);
    setError(null);
    try {
      await authApi.register({ fullName, phone, email, password, confirmPassword });
      router.push(`/verify-email?email=${encodeURIComponent(email)}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không đăng ký được — kiểm tra lại kết nối mạng và thử lại.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return { fullName, setFullName, phone, setPhone, email, setEmail, password, setPassword, confirmPassword, setConfirmPassword, error, isSubmitting, submit };
}
