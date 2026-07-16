"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import { useState } from "react";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { PlatformTenantDetail } from "@/lib/contracts";

export default function PlatformTenantDetailPage() {
  const { id } = useParams<{ id: string }>(); const cache = useQueryClient(); const [reason, setReason] = useState("");
  const query = useQuery({ queryKey: ["platform-tenant", id], queryFn: () => apiFetch<PlatformTenantDetail>(`api/platform/tenants/${id}`) });
  const status = useMutation({ mutationFn: (value: string) => apiFetch<void>(`api/platform/tenants/${id}/status`, { method: "PATCH", body: JSON.stringify({ status: value, reason }) }), onSuccess: () => { setReason(""); cache.invalidateQueries({ queryKey: ["platform-tenant", id] }); } });
  if (query.isLoading) return <main className="page"><LoadingState /></main>; if (query.error || !query.data) return <main className="page"><ErrorState error={query.error} /></main>;
  const data = query.data;
  return <main className="page"><PageHeading title={data.tenant.name} description={data.tenant.legalName} action={<StatusBadge value={data.tenant.status} />} /><div className="grid gap-5 lg:grid-cols-[1fr_320px]"><section className="table-wrap"><table className="data-table"><thead><tr><th>Hostname</th><th>Type</th><th>Status</th><th>Certificate</th></tr></thead><tbody>{data.domains.map(x => <tr key={x.id}><td className="font-semibold">{x.hostname}</td><td>{x.domainType}</td><td><StatusBadge value={x.status} /></td><td>{formatDate(x.certificateExpiresAt)}</td></tr>)}</tbody></table></section><aside className="panel self-start p-5"><h2 className="font-semibold">Account controls</h2><dl className="mt-4 grid gap-3 text-sm"><div><dt className="text-muted">Active users</dt><dd className="font-semibold">{data.activeUsers}</dd></div><div><dt className="text-muted">Subscription</dt><dd>{data.subscription ? <StatusBadge value={data.subscription.status} /> : "None"}</dd></div></dl><label className="field mt-5"><span>Required reason</span><textarea value={reason} onChange={e => setReason(e.target.value)} /></label><div className="mt-3 flex gap-2"><button className="danger-button" disabled={!reason || status.isPending} onClick={() => status.mutate("Suspended")}>Suspend</button><button className="secondary-button" disabled={!reason || status.isPending} onClick={() => status.mutate("Active")}>Activate</button></div></aside></div></main>;
}
