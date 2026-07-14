"use client";

import { useLoginViewModel } from "@/viewmodels/shared/useLoginViewModel";
import { LoginView } from "@/components/shared/LoginView";

export default function LoginPage() {
  const viewModel = useLoginViewModel();
  return <LoginView {...viewModel} />;
}
