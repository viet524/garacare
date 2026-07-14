"use client";

import { useState } from "react";
import { MOCK_USERS } from "@/lib/mock/data";

type InternalRole = "Staff" | "Technician" | "Admin";

// TODO: nối lib/api/users.ts khi task Admin CRUD user (GARA-66) xong.
export function useUserManagementViewModel() {
  const [users, setUsers] = useState(MOCK_USERS);
  const [showForm, setShowForm] = useState(false);
  const [username, setUsername] = useState("");
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [role, setRole] = useState<InternalRole>("Staff");

  function addUser(e: React.FormEvent) {
    e.preventDefault();
    if (!username || !fullName) return;
    setUsers((prev) => [...prev, { id: Date.now(), username, fullName, phone, role }]);
    setUsername("");
    setFullName("");
    setPhone("");
    setRole("Staff");
    setShowForm(false);
  }

  return { users, showForm, setShowForm, username, setUsername, fullName, setFullName, phone, setPhone, role, setRole, addUser };
}
