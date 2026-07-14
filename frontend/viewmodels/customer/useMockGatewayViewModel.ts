"use client";

import { useState } from "react";

// TODO: gọi Route Handler app/api/mock-gateway/callback (tính HMAC server-side) khi
// GARA-61/36/37 xong — KHÔNG tính chữ ký ở client.
export function useMockGatewayViewModel(transactionRef: string) {
  const [result, setResult] = useState<"success" | "cancelled" | null>(null);

  return {
    transactionRef,
    amount: 1050000,
    result,
    confirmSuccess: () => setResult("success"),
    cancel: () => setResult("cancelled"),
  };
}
