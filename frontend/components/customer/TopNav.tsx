"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import styles from "./TopNav.module.css";

const LINKS = [
  { href: "/customer", label: "Trang chủ" },
  { href: "/customer/book", label: "Đặt lịch" },
  { href: "/customer/vehicles", label: "Xe của tôi" },
];

interface TopNavProps {
  unreadCount?: number;
  customerInitial?: string;
}

export function TopNav({ unreadCount = 0, customerInitial = "K" }: TopNavProps) {
  const pathname = usePathname();
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
        <div className={styles.avatar}>{customerInitial}</div>
      </div>
    </nav>
  );
}
