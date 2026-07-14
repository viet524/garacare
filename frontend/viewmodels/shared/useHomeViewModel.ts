"use client";

export function useHomeViewModel() {
  return {
    links: [
      { href: "/staff", label: "Staff Portal" },
      { href: "/customer", label: "Customer Portal" },
    ],
  };
}
