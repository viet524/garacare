"use client";

import { useState } from "react";
import { MOCK_WORK_ORDERS } from "@/lib/mock/data";

interface FoundCustomer {
  fullName: string;
  phone: string;
  email: string;
  vehicles: { id: number; licensePlate: string; label: string }[];
}

const MOCK_CUSTOMER: FoundCustomer = {
  fullName: "Nguyễn Văn An",
  phone: "0912 345 678",
  email: "an.nguyen@email.com",
  vehicles: [
    { id: 1, licensePlate: "30A-123.45", label: "Toyota Vios 2020" },
  ],
};

// TODO: nối lib/api/customers.ts + lib/api/vehicles.ts + lib/api/workorders.ts khi GARA-18/19/22/26 xong.
export function useIntakeViewModel() {
  const [phone, setPhone] = useState("");
  const [foundCustomer, setFoundCustomer] = useState<FoundCustomer | null>(null);
  const [searched, setSearched] = useState(false);
  const [selectedVehicleId, setSelectedVehicleId] = useState<number | null>(null);
  const [description, setDescription] = useState("");
  const [submitted, setSubmitted] = useState(false);

  function searchByPhone() {
    setSearched(true);
    setFoundCustomer(phone.trim() === "0912345678" || phone.trim() === "0912 345 678" ? MOCK_CUSTOMER : null);
  }

  const selectedVehicle = foundCustomer?.vehicles.find((v) => v.id === selectedVehicleId) ?? null;
  const hasOpenWorkOrderWarning = selectedVehicle
    ? MOCK_WORK_ORDERS.some((w) => w.licensePlate === selectedVehicle.licensePlate && !["Delivered", "Cancelled"].includes(w.status))
    : false;

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitted(true);
  }

  return {
    phone,
    setPhone,
    foundCustomer,
    searched,
    searchByPhone,
    selectedVehicleId,
    setSelectedVehicleId,
    description,
    setDescription,
    hasOpenWorkOrderWarning,
    submit,
    submitted,
  };
}
