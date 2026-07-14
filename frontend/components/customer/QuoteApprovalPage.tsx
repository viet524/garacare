"use client";

// Client wrapper: app/quotes/[token]/page.tsx là Server Component (resolve route param),
// nối sang ViewModel (hook, cần "use client") ở đây.
import { useQuoteApprovalViewModel } from "@/viewmodels/customer/useQuoteApprovalViewModel";
import { QuoteApprovalView } from "@/components/customer/QuoteApprovalView";

interface QuoteApprovalPageProps {
  token: string;
}

export function QuoteApprovalPage({ token }: QuoteApprovalPageProps) {
  const viewModel = useQuoteApprovalViewModel(token);
  return <QuoteApprovalView {...viewModel} />;
}
