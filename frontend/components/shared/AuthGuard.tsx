"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getSession, homePathForRole, type Session } from "@/lib/auth/session";

interface AuthGuardProps {
  allowedRoles: Session["role"][];
  children: React.ReactNode;
}

// Chặn truy cập khi không có session hợp lệ — không có token thì luôn văng về trang đăng
// nhập ("/"). Nếu có token nhưng sai portal (VD Customer vào /staff), điều hướng về đúng
// trang chủ của Role đó thay vì báo lỗi, vì actor đã xác thực hợp lệ, chỉ nhầm khu vực.
export function AuthGuard({ allowedRoles, children }: AuthGuardProps) {
  const router = useRouter();
  const [session, setSession] = useState<Session | null>(null);
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    const current = getSession();
    if (!current) {
      router.replace("/");
      return;
    }
    if (!allowedRoles.includes(current.role)) {
      router.replace(homePathForRole(current.role));
      return;
    }
    setSession(current);
    setChecked(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Không render nội dung bảo vệ trước khi xác nhận xong session — tránh lộ dữ liệu
  // trong 1 khung hình rồi mới redirect.
  if (!checked || !session) {
    return null;
  }

  return <>{children}</>;
}
