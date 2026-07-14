"use client";

import { useState } from "react";

// TODO: nối lib/api/auth.ts (register) khi GARA-15/16 xong.
export function useCustomerRegisterViewModel() {
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    window.setTimeout(() => {
      setIsSubmitting(false);
      if (!fullName || !phone || password.length < 6) {
        setError("Kiểm tra lại thông tin — mật khẩu cần tối thiểu 6 ký tự.");
        return;
      }
      window.location.href = "/customer";
    }, 400);
  }

  return { fullName, setFullName, phone, setPhone, email, setEmail, password, setPassword, error, isSubmitting, submit };
}
