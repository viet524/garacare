"use client";

import { useVehicleProfileViewModel } from "@/viewmodels/customer/useVehicleProfileViewModel";
import { VehicleProfileView } from "@/components/customer/VehicleProfileView";

export default function CustomerVehiclesPage() {
  const vm = useVehicleProfileViewModel();
  return <VehicleProfileView {...vm} />;
}
