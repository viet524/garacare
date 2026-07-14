"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import styles from "./Sidebar.module.css";

const LINKS = [
  { href: "/staff", label: "Work Order" },
  { href: "/staff/check-in", label: "Check-in lịch hẹn" },
  { href: "/staff/intake", label: "Tiếp nhận xe" },
  { href: "/staff/parts", label: "Phụ tùng" },
  { href: "/staff/reports/revenue", label: "Báo cáo" },
  { href: "/staff/users", label: "Nhân viên" },
];

export function StaffShell({ children, active }: { children: ReactNode; active?: string }) {
  const pathname = usePathname();
  const activeHref = active ?? pathname;
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
      </aside>
      <main className={styles.content}>{children}</main>
    </div>
  );
}
