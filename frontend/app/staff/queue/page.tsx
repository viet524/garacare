"use client";

import { useTechnicianQueueViewModel } from "@/viewmodels/staff/useTechnicianQueueViewModel";
import { TechnicianQueueView } from "@/components/staff/TechnicianQueueView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function TechnicianQueuePage() {
  const vm = useTechnicianQueueViewModel();
  return (
    <StaffShell active="/staff/queue">
      <TechnicianQueueView {...vm} />
    </StaffShell>
  );
}
