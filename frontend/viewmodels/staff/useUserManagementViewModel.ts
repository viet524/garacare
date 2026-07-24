"use client";

import { useEffect, useState } from "react";
import { getSession } from "@/lib/auth/session";
import { listInternalUsers, type UserResponse } from "@/lib/api/users";
import { ApiError } from "@/lib/api/client";

type InternalRole = "Staff" | "Technician" | "Admin";

// TODO: addUser vẫn chỉ thêm tạm ở client, chưa gọi API thật — tạo user nội bộ (Admin CRUD,
// GARA-66) là task riêng, chưa có endpoint POST /api/users ở backend.
export function useUserManagementViewModel() {
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [username, setUsername] = useState("");
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [role, setRole] = useState<InternalRole>("Staff");

  useEffect(() => {
    const token = getSession()?.token ?? "";
    listInternalUsers(token)
      .then(setUsers)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Không thể tải danh sách nhân viên."))
      .finally(() => setLoading(false));
  }, []);

  function addUser(e: React.FormEvent) {
    e.preventDefault();
    if (!username || !fullName) return;
    setUsers((prev) => [
      ...prev,
      { id: Date.now(), username, fullName, phone, email: null, role, technicianStatus: role === "Technician" ? "Free" : null },
    ]);
    setUsername("");
    setFullName("");
    setPhone("");
    setRole("Staff");
    setShowForm(false);
  }

  return { users, loading, error, showForm, setShowForm, username, setUsername, fullName, setFullName, phone, setPhone, role, setRole, addUser };
}
