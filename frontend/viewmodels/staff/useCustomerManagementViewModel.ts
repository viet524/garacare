"use client";

import { useEffect, useState } from "react";
import { getSession } from "@/lib/auth/session";
import { createCustomer, getAll } from "@/lib/api/customers";
import { ApiError, getFieldErrors } from "@/lib/api/client";
import type { CustomerResponse } from "@/types/customer";

// Chỉ list + tạo mới — chưa có sửa/xoá (chưa được yêu cầu, và sửa hồ sơ Customer đã có
// tài khoản cần quyết định đồng bộ với bảng User trước, xem thảo luận khi làm tính năng này).
export function useCustomerManagementViewModel() {
  const [customers, setCustomers] = useState<CustomerResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showAddModal, setShowAddModal] = useState(false);
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [address, setAddress] = useState("");
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  function token() {
    return getSession()?.token ?? "";
  }

  function loadCustomers() {
    setLoading(true);
    setError(null);
    getAll(token())
      .then(setCustomers)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Không thể tải danh sách khách hàng."))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    loadCustomers();
  }, []);

  async function createNewCustomer() {
    setSaving(true);
    setFormError(null);
    setFieldErrors({});
    try {
      const customer = await createCustomer({ fullName, phone, email, address: address || undefined }, token());
      setCustomers((prev) => [...prev, customer]);
      setFullName("");
      setPhone("");
      setEmail("");
      setAddress("");
      setShowAddModal(false);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(getFieldErrors(err)).length > 0) {
        setFieldErrors(getFieldErrors(err));
      } else {
        setFormError(err instanceof ApiError ? err.message : "Không thể tạo khách hàng mới, vui lòng thử lại.");
      }
    } finally {
      setSaving(false);
    }
  }

  return {
    customers,
    loading,
    error,
    showAddModal,
    openAddModal: () => { setFieldErrors({}); setFormError(null); setShowAddModal(true); },
    closeAddModal: () => setShowAddModal(false),
    fullName,
    setFullName,
    phone,
    setPhone,
    email,
    setEmail,
    address,
    setAddress,
    saving,
    formError,
    fieldErrors,
    createNewCustomer,
  };
}
