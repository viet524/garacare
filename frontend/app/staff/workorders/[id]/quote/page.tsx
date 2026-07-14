"use client";

import { use } from "react";
import { useQuoteBuilderViewModel } from "@/viewmodels/staff/useQuoteBuilderViewModel";
import { QuoteBuilderView } from "@/components/staff/QuoteBuilderView";
import { StaffShell } from "@/components/staff/StaffShell";

export default function StaffQuoteBuilderPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const vm = useQuoteBuilderViewModel(Number(id));
  return (
    <StaffShell active="/staff">
      <QuoteBuilderView {...vm} />
    </StaffShell>
  );
}
