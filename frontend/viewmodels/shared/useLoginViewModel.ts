"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import * as authApi from "@/lib/api/auth";
import { ApiError, getFieldErrors } from "@/lib/api/client";
import { homePathForRole, saveSession } from "@/lib/auth/session";

// Đăng nhập chung cho cả Customer và nội bộ (Staff/Technician/Admin) bằng Email + mật khẩu —
// sau khi xác thực, điều hướng theo Role trả về từ backend.
export function useLoginViewModel() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    setFieldErrors({});
    try {
      const result = await authApi.login({ email, password });
      saveSession(result);
      router.push(homePathForRole(result.role));
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setError("Tài khoản chưa xác minh email. Kiểm tra hộp thư hoặc yêu cầu gửi lại mã xác minh.");
      } else if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else if (err instanceof ApiError && err.status === 401) {
        // Sai email hoặc mật khẩu — vì lý do bảo mật (không tiết lộ email có tồn tại hay
        // không), tô đỏ cả 2 ô thay vì chỉ 1 ô, chỉ hiện thông báo 1 lần dưới ô mật khẩu.
        setFieldErrors({ email: err.message, password: err.message });
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Không đăng nhập được — kiểm tra lại kết nối mạng và thử lại.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return { email, setEmail, password, setPassword, error, fieldErrors, isSubmitting, submit };
}
