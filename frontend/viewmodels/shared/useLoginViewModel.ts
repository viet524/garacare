"use client";

import { useState } from "react";
import { MOCK_LOGIN_ACCOUNTS } from "@/lib/mock/data";

// Đăng nhập chung cho cả Customer và nội bộ (Staff/Technician/Admin) — sau khi xác thực,
// điều hướng theo Role trả về từ backend. Đây là điểm vào duy nhất, không tách 2 trang login.
// TODO: nối lib/api/auth.ts (LoginAsync) khi GARA-15/16 xong.
export function useLoginViewModel() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    window.setTimeout(() => {
      setIsSubmitting(false);
      const account = MOCK_LOGIN_ACCOUNTS.find((a) => a.username === username);
      if (!account || !password) {
        setError("Sai tên đăng nhập hoặc mật khẩu.");
        return;
      }
      window.location.href = account.role === "Customer" ? "/customer" : "/staff";
    }, 400);
  }

  return { username, setUsername, password, setPassword, error, isSubmitting, submit, demoAccounts: MOCK_LOGIN_ACCOUNTS };
}
