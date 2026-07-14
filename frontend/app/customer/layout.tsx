"use client";

import { AuthGuard } from "@/components/shared/AuthGuard";

export default function CustomerLayout({ children }: { children: React.ReactNode }) {
  return <AuthGuard allowedRoles={["Customer"]}>{children}</AuthGuard>;
}
