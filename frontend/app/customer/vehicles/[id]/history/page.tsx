import { VehicleHistoryPage } from "@/components/customer/VehicleHistoryPage";

interface RouteProps {
  params: Promise<{ id: string }>;
}

export default async function Page({ params }: RouteProps) {
  const { id } = await params;
  return <VehicleHistoryPage vehicleId={Number(id)} />;
}
