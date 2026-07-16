"use client";

import { useQuery } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import { ErrorState, LoadingState, PageHeading, humanize } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Analytics } from "@/lib/contracts";

export default function AnalyticsPage() {
  const id = useParams<{ id: string }>().id;
  const query = useQuery({ queryKey: ["analytics", id], queryFn: () => apiFetch<Analytics>(`api/competitions/${id}/analytics/overview`) });
  return <main className="page"><PageHeading title="Analytics" description="Current scan, conversion, entry, and risk performance." />{query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{Object.entries(query.data ?? {}).map(([key, value]) => <section className="panel p-5" key={key}><div className="text-xs font-semibold uppercase text-muted">{humanize(key)}</div><div className="mt-3 text-3xl font-semibold">{key === "conversionRate" ? `${Number(value).toFixed(1)}%` : Number(value).toLocaleString()}</div></section>)}</div>}</main>;
}
