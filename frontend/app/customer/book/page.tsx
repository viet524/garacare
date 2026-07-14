"use client";

import { useBookAppointmentViewModel } from "@/viewmodels/customer/useBookAppointmentViewModel";
import { BookAppointmentView } from "@/components/customer/BookAppointmentView";

export default function CustomerBookPage() {
  const vm = useBookAppointmentViewModel();
  return <BookAppointmentView {...vm} />;
}
