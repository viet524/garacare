"use client";

import { useState } from "react";
import { MOCK_REVENUE } from "@/lib/mock/data";

// TODO: nối lib/api/reports.ts khi GARA-50 xong.
export function useRevenueReportViewModel() {
  const [from, setFrom] = useState("2026-07-01");
  const [to, setTo] = useState("2026-07-14");
  const [result, setResult] = useState(MOCK_REVENUE);
  const [error, setError] = useState<string | null>(null);

  function view() {
    if (from > to) {
      setError("Ngày bắt đầu phải trước ngày kết thúc.");
      return;
    }
    setError(null);
    setResult(MOCK_REVENUE);
  }

  return { from, setFrom, to, setTo, result, error, view };
}
