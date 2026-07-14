"use client";

import { useWorkOrderListViewModel } from "@/viewmodels/staff/useWorkOrderListViewModel";
import { WorkOrderListView } from "@/components/staff/WorkOrderListView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffWorkOrdersPage() {
  const vm = useWorkOrderListViewModel();
  return (
    <StaffShell active="/staff">
      <WorkOrderListView {...vm} />
    </StaffShell>
  );
}
