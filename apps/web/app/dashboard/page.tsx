"use client";

import { useQuery } from "@tanstack/react-query";
import { Activity, ArrowUpRight, CalendarClock, Globe2, Megaphone, Plus } from "lucide-react";
import Link from "next/link";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Competition, Domain, Tenant } from "@/lib/contracts";

export default function DashboardPage() {
  const tenant = useQuery({ queryKey: ["tenant"], queryFn: () => apiFetch<Tenant>("api/tenants/current") });
  const competitions = useQuery({ queryKey: ["competitions"], queryFn: () => apiFetch<Competition[]>("api/competitions") });
  const domains = useQuery({ queryKey: ["domains"], queryFn: () => apiFetch<Domain[]>("api/domains") });

  if (tenant.isLoading) return <main className="page"><LoadingState /></main>;
  if (tenant.error) return <main className="page"><ErrorState error={tenant.error} /></main>;

  const campaigns = competitions.data ?? [];
  const activeDomains = (domains.data ?? []).filter(domain => domain.status === "Active");
  const live = campaigns.filter(campaign => campaign.status === "Live");
  const scheduled = campaigns.filter(campaign => campaign.status === "Scheduled");

  return <main className="page">
    <PageHeading title="Overview" description={tenant.data?.name}
      action={<Link className="command-button" href="/dashboard/competitions/new"><Plus size={17} />New competition</Link>} />

    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4" aria-label="Workspace summary">
      <Metric label="Competitions" value={competitions.isLoading ? "..." : campaigns.length} icon={<Megaphone size={19} />} color="blue" />
      <Metric label="Live now" value={competitions.isLoading ? "..." : live.length} icon={<Activity size={19} />} color="teal" />
      <Metric label="Scheduled" value={competitions.isLoading ? "..." : scheduled.length} icon={<CalendarClock size={19} />} color="amber" />
      <Metric label="Active domains" value={domains.isLoading ? "..." : activeDomains.length} icon={<Globe2 size={19} />} color="coral" />
    </section>

    <div className="mt-6 grid gap-5 xl:grid-cols-[minmax(0,1.7fr)_minmax(280px,1fr)]">
      <section className="panel min-w-0">
        <div className="panel-header"><h2 className="text-sm font-semibold">Recent competitions</h2><Link className="overview-link" href="/dashboard/competitions">View all <ArrowUpRight className="inline" size={14} /></Link></div>
        {competitions.error ? <ErrorState error={competitions.error} /> : campaigns.length ? campaigns.slice(0, 6).map(campaign =>
          <Link className="overview-row" href={`/dashboard/competitions/${campaign.id}`} key={campaign.id}>
            <div className="min-w-0"><div className="overview-row-title">{campaign.name}</div><div className="overview-row-meta">Closes {formatDate(campaign.endsAt)}</div></div>
            <StatusBadge value={campaign.status} />
          </Link>) : <div className="empty-state">No competitions yet.</div>}
      </section>

      <section className="panel min-w-0">
        <div className="panel-header"><h2 className="text-sm font-semibold">Workspace</h2><StatusBadge value={tenant.data?.status ?? "Unknown"} /></div>
        <dl className="divide-y divide-line px-5 text-sm">
          <div className="flex items-center justify-between gap-4 py-4"><dt className="text-muted">Organisation</dt><dd className="max-w-[65%] truncate font-semibold" title={tenant.data?.name}>{tenant.data?.name}</dd></div>
          <div className="flex items-center justify-between gap-4 py-4"><dt className="text-muted">Workspace URL</dt><dd className="max-w-[65%] truncate font-semibold">/c/{tenant.data?.slug}</dd></div>
          <div className="flex items-center justify-between gap-4 py-4"><dt className="text-muted">Domains</dt><dd className="font-semibold">{domains.isLoading ? "..." : activeDomains.length} active</dd></div>
        </dl>
        <div className="flex flex-wrap gap-4 border-t border-line px-5 py-4"><Link className="overview-link" href="/dashboard/domains">Manage domains <ArrowUpRight className="inline" size={14} /></Link><Link className="overview-link" href="/dashboard/brands">Manage brand <ArrowUpRight className="inline" size={14} /></Link></div>
      </section>
    </div>
  </main>;
}

function Metric({ label, value, icon, color }: { label: string; value: number | string; icon: React.ReactNode; color: "blue" | "teal" | "amber" | "coral" }) {
  return <div className="metric-card"><div className="flex items-start justify-between gap-3"><span className="metric-label">{label}</span><span className={`metric-icon metric-${color}`}>{icon}</span></div><div className="metric-value">{value}</div></div>;
}
