"use client";

import { useRevenueReportViewModel } from "@/viewmodels/staff/useRevenueReportViewModel";
import { RevenueReportView } from "@/components/staff/RevenueReportView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffRevenueReportPage() {
  const vm = useRevenueReportViewModel();
  return (
    <StaffShell active="/staff/reports/revenue">
      <RevenueReportView {...vm} />
    </StaffShell>
  );
}
