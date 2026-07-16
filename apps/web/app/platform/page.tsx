"use client";

import { useQuery } from "@tanstack/react-query";
import { Building2, Cable, Flag, Network, ReceiptText, Store } from "lucide-react";
import Link from "next/link";
import { ErrorState, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { FeatureFlag, Plan, PlatformDomain, Reseller, TenantSummary } from "@/lib/contracts";

export default function PlatformPage() {
  const query = useQuery({
    queryKey: ["platform-overview"],
    queryFn: async () => {
      const [tenants, domains, features, plans, resellers] = await Promise.all([
        apiFetch<TenantSummary[]>("api/platform/tenants"), apiFetch<PlatformDomain[]>("api/platform/domains"),
        apiFetch<FeatureFlag[]>("api/platform/features"), apiFetch<Plan[]>("api/platform/plans"), apiFetch<Reseller[]>("api/platform/resellers")
      ]);
      return { tenants, domains, features, plans, resellers };
    }
  });
  if (query.isLoading) return <main className="page"><LoadingState /></main>;
  if (query.error) return <main className="page"><ErrorState error={query.error} /></main>;
  const data = query.data!;
  const domainIssues = data.domains.filter(x => !["Active", "Verified"].includes(x.status)).length;
  const metrics = [
    ["Tenants", data.tenants.length, "/platform/tenants", <Building2 key="tenant" size={19} />],
    ["Domains", data.domains.length, "/platform/domains", <Network key="domain" size={19} />],
    ["Feature flags", data.features.length, "/platform/features", <Flag key="flag" size={19} />],
    ["Plans", data.plans.length, "/platform/plans", <ReceiptText key="plan" size={19} />],
    ["Resellers", data.resellers.length, "/platform/resellers", <Store key="reseller" size={19} />]
  ] as const;
  return <main className="page"><PageHeading title="Platform administration" description="Cross-tenant operations, commercial configuration, and infrastructure status." /><section className="grid border-y border-line sm:grid-cols-2 lg:grid-cols-5">{metrics.map(([label, value, href, icon]) => <Link className="flex min-h-28 items-start justify-between border-b border-line p-4 transition-colors hover:bg-white sm:border-r lg:border-b-0" href={href} key={label}><div><div className="text-xs font-semibold uppercase text-muted">{label}</div><div className="mt-2 text-3xl font-semibold">{value}</div></div><span className="text-muted">{icon}</span></Link>)}</section><section className="mt-7 grid gap-6 lg:grid-cols-2"><div className="panel p-5"><div className="flex items-center justify-between"><div><div className="text-xs font-semibold uppercase text-muted">Domain operations</div><h2 className="mt-1 text-lg font-semibold">Routing and certificates</h2></div><StatusBadge value={domainIssues ? "Attention" : "Healthy"} /></div><p className="mt-4 text-sm text-muted">{domainIssues ? `${domainIssues} domain records require verification or certificate attention.` : "All registered domain records are active or verified."}</p><Link className="secondary-button mt-5 w-fit" href="/platform/domains"><Cable size={16} />Review domains</Link></div><div className="panel p-5"><div className="text-xs font-semibold uppercase text-muted">Tenant lifecycle</div><h2 className="mt-1 text-lg font-semibold">Organisation status</h2><div className="mt-4 flex flex-wrap gap-2">{Array.from(new Set(data.tenants.map(x => x.status))).map(status => <span className="inline-flex items-center gap-2 text-sm" key={status}><StatusBadge value={status} /> <strong>{data.tenants.filter(x => x.status === status).length}</strong></span>)}</div><Link className="secondary-button mt-5 w-fit" href="/platform/tenants"><Building2 size={16} />Manage tenants</Link></div></section></main>;
}
