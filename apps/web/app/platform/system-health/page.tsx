"use client";

import { useQuery } from "@tanstack/react-query";
import { RefreshCw } from "lucide-react";
import { PageHeading, StatusBadge } from "@/components/operations-ui";

export default function SystemHealthPage() {
  const query = useQuery({ queryKey: ["system-health"], queryFn: async () => { const response = await fetch("/api/backend/health/ready", { cache: "no-store" }); return { status: response.ok ? "Ready" : "Degraded", code: response.status, checkedAt: new Date().toISOString() }; }, refetchInterval: 30000 });
  return <main className="page"><PageHeading title="System health" description="API readiness, including database, Redis and object-storage dependencies." action={<button className="secondary-button" onClick={() => query.refetch()}><RefreshCw size={17} />Refresh</button>} /><section className="panel p-5"><div className="flex items-center justify-between"><div><div className="text-xs font-semibold uppercase text-muted">Readiness endpoint</div><div className="mt-2 text-lg font-semibold">API and dependencies</div></div><StatusBadge value={query.data?.status ?? "Checking"} /></div><div className="mt-4 text-sm text-muted">HTTP {query.data?.code ?? "-"} · checked {query.data?.checkedAt ? new Date(query.data.checkedAt).toLocaleTimeString() : "now"}</div></section></main>;
}
