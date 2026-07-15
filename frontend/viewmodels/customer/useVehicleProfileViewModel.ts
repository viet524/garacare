"use client";

import { useEffect, useState } from "react";
import { getSession } from "@/lib/auth/session";
import { createMine, getMine } from "@/lib/api/vehicles";
import { ApiError, getFieldErrors } from "@/lib/api/client";
import type { VehicleResponse } from "@/types/vehicle";

export function useVehicleProfileViewModel() {
  const [vehicles, setVehicles] = useState<VehicleResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showAddModal, setShowAddModal] = useState(false);
  const [licensePlate, setLicensePlate] = useState("");
  const [brand, setBrand] = useState("");
  const [model, setModel] = useState("");
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  function token() {
    return getSession()?.token ?? "";
  }

  useEffect(() => {
    getMine(token())
      .then(setVehicles)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Không thể tải danh sách xe."))
      .finally(() => setLoading(false));
  }, []);

  async function createNewVehicle() {
    setSaving(true);
    setFormError(null);
    setFieldErrors({});
    try {
      const vehicle = await createMine({ licensePlate, brand: brand || undefined, model: model || undefined }, token());
      setVehicles((prev) => [...prev, vehicle]);
      setLicensePlate("");
      setBrand("");
      setModel("");
      setShowAddModal(false);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else {
        setFormError(err instanceof ApiError ? err.message : "Không thể thêm xe mới, vui lòng thử lại.");
      }
    } finally {
      setSaving(false);
    }
  }

  return {
    vehicles,
    loading,
    error,
    showAddModal,
    openAddModal: () => { setFieldErrors({}); setFormError(null); setShowAddModal(true); },
    closeAddModal: () => setShowAddModal(false),
    licensePlate,
    setLicensePlate,
    brand,
    setBrand,
    model,
    setModel,
    saving,
    formError,
    fieldErrors,
    createNewVehicle,
  };
}
