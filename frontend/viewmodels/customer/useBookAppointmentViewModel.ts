"use client";

import { useState } from "react";
import { MOCK_APPOINTMENTS } from "@/lib/mock/data";

const MOCK_VEHICLES = [
  { id: 1, licensePlate: "30A-123.45", label: "Toyota Vios 2020" },
  { id: 2, licensePlate: "30A-999.88", label: "Honda SH 2019" },
];

const TIME_SLOTS = ["08:00–09:00", "09:00–10:00", "10:00–11:00", "14:00–15:00", "15:00–16:00"];
const SLOT_CAPACITY = 3;

// TODO: nối lib/api/appointments.ts (book) khi GARA-58 xong. Ngưỡng SLOT_CAPACITY
// và % ưu đãi đọc từ appsettings backend (Appointment:MaxSlotCapacity) — chưa chốt số thật.
export function useBookAppointmentViewModel() {
  const [vehicleId, setVehicleId] = useState<number | null>(null);
  const [date, setDate] = useState("2026-07-16");
  const [timeSlot, setTimeSlot] = useState<string | null>(null);
  const [confirmed, setConfirmed] = useState(false);
  const [slotFullError, setSlotFullError] = useState<string | null>(null);

  function bookedCountFor(slot: string) {
    return MOCK_APPOINTMENTS.filter((a) => a.scheduledDate === date && a.scheduledTimeSlot === slot && a.status === "Booked").length;
  }

  function selectSlot(slot: string) {
    if (bookedCountFor(slot) >= SLOT_CAPACITY) return;
    setTimeSlot(slot);
    setSlotFullError(null);
  }

  function confirm() {
    if (!timeSlot || bookedCountFor(timeSlot) >= SLOT_CAPACITY) {
      setSlotFullError("Khung giờ này vừa đầy — vui lòng chọn khung giờ khác.");
      return;
    }
    setConfirmed(true);
  }

  return {
    vehicles: MOCK_VEHICLES,
    vehicleId,
    setVehicleId,
    date,
    setDate,
    timeSlots: TIME_SLOTS,
    timeSlot,
    selectSlot,
    bookedCountFor,
    slotCapacity: SLOT_CAPACITY,
    discountPercent: 5,
    confirmed,
    confirm,
    slotFullError,
  };
}
