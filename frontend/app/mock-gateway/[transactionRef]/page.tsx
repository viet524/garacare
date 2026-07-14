import { MockGatewayPage } from "@/components/customer/MockGatewayPage";

interface RouteProps {
  params: Promise<{ transactionRef: string }>;
}

export default async function Page({ params }: RouteProps) {
  const { transactionRef } = await params;
  return <MockGatewayPage transactionRef={transactionRef} />;
}
