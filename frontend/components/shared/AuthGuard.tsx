"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { refreshToken as refreshTokenApi } from "@/lib/api/auth";
import { clearSession, getSession, homePathForRole, isTokenExpired, saveSession, type Session } from "@/lib/auth/session";

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
    let cancelled = false;

    async function verify() {
      let current = getSession();
      if (!current) {
        router.replace("/");
        return;
      }

      // Access token đã hết hạn ngay lúc mở trang (VD đóng app nhiều ngày rồi mở lại) — chủ
      // động refresh trước khi render, thay vì hiện nội dung 1 khung hình rồi mới phát hiện
      // qua 1 request API bị 401 (client.ts vẫn tự refresh khi đó, nhưng chậm hơn 1 nhịp).
      if (isTokenExpired(current.token)) {
        try {
          current = await refreshTokenApi({ refreshToken: current.refreshToken });
          saveSession(current);
        } catch {
          clearSession();
          if (!cancelled) router.replace("/");
          return;
        }
      }

      if (cancelled) return;

      if (!allowedRoles.includes(current.role)) {
        router.replace(homePathForRole(current.role));
        return;
      }

      setSession(current);
      setChecked(true);
    }

    verify();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Không render nội dung bảo vệ trước khi xác nhận xong session — tránh lộ dữ liệu
  // trong 1 khung hình rồi mới redirect.
  if (!checked || !session) {
    return null;
  }

  return <>{children}</>;
}
