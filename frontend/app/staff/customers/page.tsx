"use client";

import { useCustomerManagementViewModel } from "@/viewmodels/staff/useCustomerManagementViewModel";
import { CustomerManagementView } from "@/components/staff/CustomerManagementView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffCustomersPage() {
  const vm = useCustomerManagementViewModel();
  return (
    <StaffShell active="/staff/customers">
      <CustomerManagementView {...vm} />
    </StaffShell>
  );
}
