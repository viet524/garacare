"use client";

import { Suspense } from "react";
import { useVerifyEmailViewModel } from "@/viewmodels/shared/useVerifyEmailViewModel";
import { VerifyEmailView } from "@/components/shared/VerifyEmailView";

function VerifyEmailPageInner() {
  const vm = useVerifyEmailViewModel();
  return <VerifyEmailView {...vm} />;
}

export default function VerifyEmailPage() {
  return (
    <Suspense>
      <VerifyEmailPageInner />
    </Suspense>
  );
}
