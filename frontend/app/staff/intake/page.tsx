"use client";

import { useIntakeViewModel } from "@/viewmodels/staff/useIntakeViewModel";
import { IntakeView } from "@/components/staff/IntakeView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffIntakePage() {
  const vm = useIntakeViewModel();
  return (
    <StaffShell active="/staff/intake">
      <IntakeView {...vm} />
    </StaffShell>
  );
}
