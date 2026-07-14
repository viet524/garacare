"use client";

// ViewModel: giữ state + gọi /lib/api/*, không render JSX, không import component.
export function useStaffHomeViewModel() {
  return {
    title: "GaraCare — Staff Portal",
  };
}
