"use client";

import { useQuery } from "@tanstack/react-query";
import { Activity, Globe2, Megaphone, Users } from "lucide-react";
import Link from "next/link";
import { ErrorState, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Competition, Domain, Tenant } from "@/lib/contracts";

export default function DashboardPage() {
  const tenant = useQuery({ queryKey: ["tenant"], queryFn: () => apiFetch<Tenant>("api/tenants/current") });
  const competitions = useQuery({ queryKey: ["competitions"], queryFn: () => apiFetch<Competition[]>("api/competitions") });
  const domains = useQuery({ queryKey: ["domains"], queryFn: () => apiFetch<Domain[]>("api/domains") });
  if (tenant.isLoading) return <main className="page"><LoadingState /></main>; if (tenant.error) return <main className="page"><ErrorState error={tenant.error} /></main>;
  return <main className="page"><PageHeading title={tenant.data?.name ?? "Dashboard"} description="Operational overview for the current tenant." /><section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4"><Metric icon={<Megaphone />} label="Competitions" value={competitions.data?.length ?? 0} /><Metric icon={<Activity />} label="Live" value={competitions.data?.filter(x => x.status === "Live").length ?? 0} /><Metric icon={<Globe2 />} label="Active domains" value={domains.data?.filter(x => x.status === "Active").length ?? 0} /><Metric icon={<Users />} label="Tenant state" value={<StatusBadge value={tenant.data?.status ?? "Unknown"} />} /></section><section className="mt-5 panel"><div className="panel-header"><h2 className="font-semibold">Recent competitions</h2><Link className="text-sm font-semibold text-emerald-800" href="/dashboard/competitions">View all</Link></div><div className="divide-y divide-line">{competitions.data?.slice(0, 5).map(item => <Link className="flex items-center justify-between px-4 py-4 text-sm hover:bg-neutral-50" href={`/dashboard/competitions/${item.id}`} key={item.id}><span className="font-medium">{item.name}</span><StatusBadge value={item.status} /></Link>)}{!competitions.data?.length && <div className="empty-state">No competitions available.</div>}</div></section></main>;
}
function Metric({ icon, label, value }: { icon: React.ReactNode; label: string; value: React.ReactNode }) { return <section className="panel p-5"><div className="flex items-center gap-2 text-muted">{icon}<span className="text-xs font-semibold uppercase">{label}</span></div><div className="mt-4 text-2xl font-semibold">{value}</div></section>; }
