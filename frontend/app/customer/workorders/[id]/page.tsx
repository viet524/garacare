"use client";

import { use } from "react";
import { useWorkOrderProgressViewModel } from "@/viewmodels/customer/useWorkOrderProgressViewModel";
import { WorkOrderProgressView } from "@/components/customer/WorkOrderProgressView";

export default function CustomerWorkOrderProgressPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const vm = useWorkOrderProgressViewModel(Number(id));
  return <WorkOrderProgressView {...vm} />;
}
