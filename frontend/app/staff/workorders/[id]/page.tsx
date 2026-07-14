"use client";

import { use } from "react";
import { useWorkOrderDetailViewModel } from "@/viewmodels/staff/useWorkOrderDetailViewModel";
import { WorkOrderDetailView } from "@/components/staff/WorkOrderDetailView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffWorkOrderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const vm = useWorkOrderDetailViewModel(Number(id));
  return (
    <StaffShell active="/staff">
      <WorkOrderDetailView {...vm} />
    </StaffShell>
  );
}
