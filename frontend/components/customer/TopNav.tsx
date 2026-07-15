"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { logout as logoutApi } from "@/lib/api/auth";
import { clearSession, getSession } from "@/lib/auth/session";
import styles from "./TopNav.module.css";

const LINKS = [
  { href: "/customer", label: "Trang chủ" },
  { href: "/customer/book", label: "Đặt lịch" },
  { href: "/customer/vehicles", label: "Xe của tôi" },
];

interface TopNavProps {
  unreadCount?: number;
}

export function TopNav({ unreadCount = 0 }: TopNavProps) {
  const pathname = usePathname();
  const router = useRouter();
  const [fullName, setFullName] = useState<string | null>(null);

  useEffect(() => {
    setFullName(getSession()?.fullName ?? null);
  }, []);

  function logout() {
    const refreshToken = getSession()?.refreshToken;
    if (refreshToken) {
      logoutApi({ refreshToken }).catch(() => {});
    }
    clearSession();
    router.replace("/");
  }

  return (
    <nav className={styles.nav}>
      <Link href="/customer" className={styles.logo}>
        GARA<span>CARE</span>
      </Link>
      <div className={styles.menu}>
        {LINKS.map((link) => (
          <Link
            key={link.href}
            href={link.href}
            className={`${styles.menuLink} ${pathname === link.href ? styles.menuLinkActive : ""}`}
          >
            {link.label}
          </Link>
        ))}
      </div>
      <div className={styles.right}>
        <Link href="/customer/notifications" className={styles.bell} aria-label="Thông báo">
          🔔
          {unreadCount > 0 && <span className={styles.badge}>{unreadCount}</span>}
        </Link>
        <div className={styles.avatar} title={fullName ?? undefined}>{fullName ? fullName.charAt(0).toUpperCase() : "?"}</div>
        <button type="button" className={styles.logoutBtn} onClick={logout}>Đăng xuất</button>
      </div>
    </nav>
  );
}
