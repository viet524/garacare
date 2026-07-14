// Route công khai (UC-04) — khách vãng lai duyệt/từ chối báo giá qua ApprovalToken,
// KHÔNG yêu cầu đăng nhập ở route này (xem docs/08-frontend-conventions.md).
import { QuoteApprovalPage } from "@/components/customer/QuoteApprovalPage";

interface RouteProps {
  params: Promise<{ token: string }>;
}

export default async function Page({ params }: RouteProps) {
  const { token } = await params;
  return <QuoteApprovalPage token={token} />;
}
