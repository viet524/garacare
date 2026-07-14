"use client";

import { usePartsViewModel } from "@/viewmodels/staff/usePartsViewModel";
import { PartsView } from "@/components/staff/PartsView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffPartsPage() {
  const vm = usePartsViewModel();
  return (
    <StaffShell active="/staff/parts">
      <PartsView {...vm} />
    </StaffShell>
  );
}
