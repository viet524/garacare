"use client";

import { useMockGatewayViewModel } from "@/viewmodels/customer/useMockGatewayViewModel";
import { MockGatewayView } from "@/components/customer/MockGatewayView";

export function MockGatewayPage({ transactionRef }: { transactionRef: string }) {
  const vm = useMockGatewayViewModel(transactionRef);
  return <MockGatewayView {...vm} />;
}
