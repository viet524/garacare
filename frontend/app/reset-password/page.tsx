"use client";

import { Suspense } from "react";
import { useResetPasswordViewModel } from "@/viewmodels/shared/useResetPasswordViewModel";
import { ResetPasswordView } from "@/components/shared/ResetPasswordView";

function ResetPasswordPageInner() {
  const vm = useResetPasswordViewModel();
  return <ResetPasswordView {...vm} />;
}

export default function ResetPasswordPage() {
  return (
    <Suspense>
      <ResetPasswordPageInner />
    </Suspense>
  );
}
