"use client";

import { useCustomerRegisterViewModel } from "@/viewmodels/customer/useCustomerRegisterViewModel";
import { CustomerRegisterView } from "@/components/customer/CustomerRegisterView";

export default function RegisterPage() {
  const vm = useCustomerRegisterViewModel();
  return <CustomerRegisterView {...vm} />;
}
