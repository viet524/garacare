"use client";

import { useVehicleHistoryViewModel } from "@/viewmodels/customer/useVehicleHistoryViewModel";
import { VehicleHistoryView } from "@/components/customer/VehicleHistoryView";

export function VehicleHistoryPage({ vehicleId }: { vehicleId: number }) {
  const vm = useVehicleHistoryViewModel(vehicleId);
  return <VehicleHistoryView {...vm} />;
}
