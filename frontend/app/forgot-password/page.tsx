"use client";

import { useForgotPasswordViewModel } from "@/viewmodels/shared/useForgotPasswordViewModel";
import { ForgotPasswordView } from "@/components/shared/ForgotPasswordView";

export default function ForgotPasswordPage() {
  const vm = useForgotPasswordViewModel();
  return <ForgotPasswordView {...vm} />;
}
