"use client";

import { useCustomerHomeViewModel } from "@/viewmodels/customer/useCustomerHomeViewModel";
import { CustomerHomeView } from "@/components/customer/CustomerHomeView";

export default function CustomerHomePage() {
  const viewModel = useCustomerHomeViewModel();
  return <CustomerHomeView {...viewModel} />;
}
