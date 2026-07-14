"use client";

import { useState } from "react";
import { MOCK_WORK_ORDERS } from "@/lib/mock/data";

// TODO: nối lib/api/quoteToken.ts (getQuoteByToken/approveQuote/rejectQuote) khi GARA-28/29 xong.
export function useQuoteApprovalViewModel(token: string) {
  // Token demo: bất kỳ chuỗi nào chứa "expired" sẽ mô phỏng link hết hạn.
  const expiredOrUsed = token.toLowerCase().includes("expired");
  const workOrder = MOCK_WORK_ORDERS[1];
  const [outcome, setOutcome] = useState<"approved" | "rejected" | null>(null);

  function approve() {
    setOutcome("approved");
  }

  function reject() {
    setOutcome("rejected");
  }

  return { token, expiredOrUsed, workOrder, outcome, approve, reject };
}
