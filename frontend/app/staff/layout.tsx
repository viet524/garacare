"use client";

import { AuthGuard } from "@/components/shared/AuthGuard";

export default function StaffLayout({ children }: { children: React.ReactNode }) {
  return <AuthGuard allowedRoles={["Staff", "Technician", "Admin"]}>{children}</AuthGuard>;
}
