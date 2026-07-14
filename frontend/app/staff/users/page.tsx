"use client";

import { useUserManagementViewModel } from "@/viewmodels/staff/useUserManagementViewModel";
import { UserManagementView } from "@/components/staff/UserManagementView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffUsersPage() {
  const vm = useUserManagementViewModel();
  return (
    <StaffShell active="/staff/users">
      <UserManagementView {...vm} />
    </StaffShell>
  );
}
