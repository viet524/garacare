"use client";

import { useState } from "react";
import { MOCK_APPOINTMENTS } from "@/lib/mock/data";
import type { AppointmentView } from "@/types/domain";

// TODO: nối lib/api/appointments.ts (getByPhone, checkIn) khi GARA-43/67 xong.
export function useCheckInViewModel() {
  const [phone, setPhone] = useState("");
  const [results, setResults] = useState<AppointmentView[] | null>(null);
  const [checkedInId, setCheckedInId] = useState<number | null>(null);

  function searchByPhone() {
    const found = MOCK_APPOINTMENTS.filter((a) => a.status === "Booked" && a.customerPhone.replace(/\s/g, "").includes(phone.replace(/\s/g, "")));
    setResults(found);
  }

  function checkIn(id: number) {
    setCheckedInId(id);
  }

  return { phone, setPhone, results, searchByPhone, checkedInId, checkIn };
}
