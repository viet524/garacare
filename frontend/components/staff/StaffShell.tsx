"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { logout as logoutApi } from "@/lib/api/auth";
import { clearSession, getSession } from "@/lib/auth/session";
import styles from "./Sidebar.module.css";

const LINKS = [
  { href: "/staff", label: "Work Order" },
  { href: "/staff/check-in", label: "Check-in lịch hẹn" },
  { href: "/staff/intake", label: "Tiếp nhận xe" },
  { href: "/staff/customers", label: "Khách hàng" },
  { href: "/staff/parts", label: "Phụ tùng" },
  { href: "/staff/reports/revenue", label: "Báo cáo" },
  { href: "/staff/users", label: "Nhân viên" },
];

// docs/01-business-spec.md §15: "Giao diện Technician: List view rút gọn, chỉ hiện queue cá
// nhân" — chỉ Technician mới thấy link này, Staff/Admin không có ý nghĩa gì với họ.
const TECHNICIAN_LINK = { href: "/staff/queue", label: "Queue của tôi" };

export function StaffShell({ children, active }: { children: ReactNode; active?: string }) {
  const pathname = usePathname();
  const router = useRouter();
  const activeHref = active ?? pathname;
  const [fullName, setFullName] = useState<string | null>(null);
  const [isTechnician, setIsTechnician] = useState(false);

  useEffect(() => {
    const session = getSession();
    setFullName(session?.fullName ?? null);
    setIsTechnician(session?.role === "Technician");
  }, []);

  // Technician gần như không dùng danh sách Work Order chung (đã có queue cá nhân riêng) —
  // bỏ hẳn link "Work Order" khỏi nav của họ thay vì chỉ thêm queue vào cạnh.
  const links = isTechnician ? [TECHNICIAN_LINK, ...LINKS.slice(1)] : LINKS;

  function logout() {
    const refreshToken = getSession()?.refreshToken;
    // Thu hồi refresh token ở server (best-effort) — không chặn đăng xuất nếu request lỗi,
    // vì client vẫn xoá session local ngay lập tức.
    if (refreshToken) {
      logoutApi({ refreshToken }).catch(() => {});
    }
    clearSession();
    router.replace("/");
  }

  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <div className={styles.logo}>
          GARA<span>CARE</span>
        </div>
        <nav className={styles.nav}>
          {links.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={`${styles.link} ${activeHref === link.href ? styles.linkActive : ""}`}
            >
              {link.label}
            </Link>
          ))}
        </nav>
        <div className={styles.userBox}>
          {fullName && <div className={styles.userName}>{fullName}</div>}
          <button type="button" className={styles.logoutBtn} onClick={logout}>Đăng xuất</button>
        </div>
      </aside>
      <main className={styles.content}>{children}</main>
    </div>
  );
}
