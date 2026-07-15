"use client";

import { useState } from "react";
import { getSession } from "@/lib/auth/session";
import { createCustomer, findByPhone } from "@/lib/api/customers";
import { createVehicle, getByCustomer, getWorkOrderHistory } from "@/lib/api/vehicles";
import { ApiError, getFieldErrors } from "@/lib/api/client";
import type { CustomerResponse } from "@/types/customer";
import type { VehicleResponse } from "@/types/vehicle";

const OPEN_STATUSES = new Set(["Received", "Diagnosing", "QuotePending", "InRepair", "WaitingParts", "Completed"]);

export function useIntakeViewModel() {
  const [phone, setPhone] = useState("");
  const [foundCustomer, setFoundCustomer] = useState<CustomerResponse | null>(null);
  const [vehicles, setVehicles] = useState<VehicleResponse[]>([]);
  const [searched, setSearched] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const [newCustomerFullName, setNewCustomerFullName] = useState("");
  const [newCustomerEmail, setNewCustomerEmail] = useState("");
  const [newCustomerAddress, setNewCustomerAddress] = useState("");

  const [newVehiclePlate, setNewVehiclePlate] = useState("");
  const [newVehicleBrand, setNewVehicleBrand] = useState("");
  const [newVehicleModel, setNewVehicleModel] = useState("");

  const [selectedVehicleId, setSelectedVehicleIdState] = useState<number | null>(null);
  const [description, setDescription] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [hasOpenWorkOrderWarning, setHasOpenWorkOrderWarning] = useState(false);
  const [showAddVehicleModal, setShowAddVehicleModal] = useState(false);
  const [showAddCustomerModal, setShowAddCustomerModal] = useState(false);

  function token() {
    return getSession()?.token ?? "";
  }

  async function searchByPhone() {
    setLoading(true);
    setError(null);
    setFieldErrors({});
    setSelectedVehicleIdState(null);
    setHasOpenWorkOrderWarning(false);
    try {
      const customer = await findByPhone(phone.trim(), token());
      setFoundCustomer(customer);
      setSearched(true);
      if (customer) {
        const custVehicles = await getByCustomer(customer.id, token());
        setVehicles(custVehicles);
      } else {
        setVehicles([]);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Không thể tra cứu khách hàng, vui lòng thử lại.");
    } finally {
      setLoading(false);
    }
  }

  async function createNewCustomer() {
    setLoading(true);
    setError(null);
    setFieldErrors({});
    try {
      const customer = await createCustomer(
        {
          fullName: newCustomerFullName,
          phone: phone.trim(),
          email: newCustomerEmail,
          address: newCustomerAddress || undefined,
        },
        token(),
      );
      setFoundCustomer(customer);
      setVehicles([]);
      setNewCustomerFullName("");
      setNewCustomerEmail("");
      setNewCustomerAddress("");
      setShowAddCustomerModal(false);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else {
        setError(err instanceof ApiError ? err.message : "Không thể tạo khách hàng mới, vui lòng thử lại.");
      }
    } finally {
      setLoading(false);
    }
  }

  async function addNewVehicle() {
    if (!foundCustomer) return;
    setLoading(true);
    setError(null);
    setFieldErrors({});
    try {
      const vehicle = await createVehicle(
        {
          customerId: foundCustomer.id,
          licensePlate: newVehiclePlate,
          brand: newVehicleBrand || undefined,
          model: newVehicleModel || undefined,
        },
        token(),
      );
      setVehicles((prev) => [...prev, vehicle]);
      setNewVehiclePlate("");
      setNewVehicleBrand("");
      setNewVehicleModel("");
      setShowAddVehicleModal(false);
      await selectVehicle(vehicle.id);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else {
        setError(err instanceof ApiError ? err.message : "Không thể thêm xe mới, vui lòng thử lại.");
      }
    } finally {
      setLoading(false);
    }
  }

  async function selectVehicle(id: number) {
    setSelectedVehicleIdState(id);
    try {
      const history = await getWorkOrderHistory(id, token());
      setHasOpenWorkOrderWarning(history.some((wo) => OPEN_STATUSES.has(wo.status)));
    } catch {
      // Không chặn luồng tiếp nhận nếu không tra được lịch sử — chỉ đơn giản là không hiện cảnh báo.
      setHasOpenWorkOrderWarning(false);
    }
  }

  // TODO: gọi lib/api/workorders.ts (createWalkIn) khi GARA-22/26 (tạo WorkOrder) xong —
  // hiện epic đó chưa có endpoint nên bước này chỉ dừng ở tạo Customer/Vehicle thật.
  function submit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitted(true);
  }

  return {
    phone,
    setPhone,
    foundCustomer,
    vehicles,
    searched,
    loading,
    error,
    fieldErrors,
    searchByPhone,
    newCustomerFullName,
    setNewCustomerFullName,
    newCustomerEmail,
    setNewCustomerEmail,
    newCustomerAddress,
    setNewCustomerAddress,
    createNewCustomer,
    showAddCustomerModal,
    openAddCustomerModal: () => { setFieldErrors({}); setShowAddCustomerModal(true); },
    closeAddCustomerModal: () => setShowAddCustomerModal(false),
    newVehiclePlate,
    setNewVehiclePlate,
    newVehicleBrand,
    setNewVehicleBrand,
    newVehicleModel,
    setNewVehicleModel,
    addNewVehicle,
    showAddVehicleModal,
    openAddVehicleModal: () => { setFieldErrors({}); setShowAddVehicleModal(true); },
    closeAddVehicleModal: () => setShowAddVehicleModal(false),
    selectedVehicleId,
    setSelectedVehicleId: selectVehicle,
    description,
    setDescription,
    hasOpenWorkOrderWarning,
    submit,
    submitted,
  };
}
