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

export function StaffShell({ children, active }: { children: ReactNode; active?: string }) {
  const pathname = usePathname();
  const router = useRouter();
  const activeHref = active ?? pathname;
  const [fullName, setFullName] = useState<string | null>(null);

  useEffect(() => {
    setFullName(getSession()?.fullName ?? null);
  }, []);

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
          {LINKS.map((link) => (
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
