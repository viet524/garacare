"use client";

// TODO: nối lib/api/vehicles.ts (getByCustomer) khi GARA-19 xong.
const MOCK_VEHICLES = [
  { id: 1, licensePlate: "30A-123.45", brand: "Toyota", model: "Vios", year: 2020 },
  { id: 2, licensePlate: "30A-999.88", brand: "Honda", model: "SH", year: 2019 },
];

export function useVehicleProfileViewModel() {
  return { vehicles: MOCK_VEHICLES };
}
