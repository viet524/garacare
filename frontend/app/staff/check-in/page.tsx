"use client";

import { useCheckInViewModel } from "@/viewmodels/staff/useCheckInViewModel";
import { CheckInView } from "@/components/staff/CheckInView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffCheckInPage() {
  const vm = useCheckInViewModel();
  return (
    <StaffShell active="/staff/check-in">
      <CheckInView {...vm} />
    </StaffShell>
  );
}
