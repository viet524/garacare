"use client";

import { useHomeViewModel } from "@/viewmodels/shared/useHomeViewModel";
import { HomeView } from "@/components/shared/HomeView";

export default function Home() {
  const viewModel = useHomeViewModel();
  return <HomeView {...viewModel} />;
}
