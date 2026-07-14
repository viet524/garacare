"use client";

import { useState } from "react";
import { MOCK_LOGIN_ACCOUNTS } from "@/lib/mock/data";

// TODO: nối lib/api/auth.ts (register) khi GARA-15/16 xong. Field khớp đúng
// RegisterCustomerRequest (GARA-14): Username, Password, FullName, Phone, Email.
export function useCustomerRegisterViewModel() {
  const [username, setUsername] = useState("");
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
      if (!username || !fullName || !phone || password.length < 6) {
        setError("Kiểm tra lại thông tin — mật khẩu cần tối thiểu 6 ký tự.");
        return;
      }
      if (MOCK_LOGIN_ACCOUNTS.some((a) => a.username === username)) {
        setError("Tên đăng nhập đã tồn tại — vui lòng chọn tên khác.");
        return;
      }
      window.location.href = "/customer";
    }, 400);
  }

  return { username, setUsername, fullName, setFullName, phone, setPhone, email, setEmail, password, setPassword, error, isSubmitting, submit };
}
