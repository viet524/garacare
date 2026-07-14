"use client";

// app/**: chỉ định tuyến (routing) + nối ViewModel với View, không chứa JSX hiển thị trực tiếp.
import { useStaffHomeViewModel } from "@/viewmodels/staff/useStaffHomeViewModel";
import { StaffHomeView } from "@/components/staff/StaffHomeView";

export default function StaffHomePage() {
  const viewModel = useStaffHomeViewModel();
  return <StaffHomeView {...viewModel} />;
}
