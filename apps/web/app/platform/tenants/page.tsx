"use client";

import { useQuery } from "@tanstack/react-query";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { TenantSummary } from "@/lib/contracts";

export default function PlatformTenantsPage() {
  const query = useQuery({ queryKey: ["platform-tenants"], queryFn: () => apiFetch<TenantSummary[]>("api/platform/tenants") });
  return <main className="page"><PageHeading title="Tenants" description="Platform-wide organisation inventory. Access requires a platform administrator role." />{query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="table-wrap"><table className="data-table"><thead><tr><th>Organisation</th><th>Slug</th><th>Status</th><th>Active users</th><th>Created</th></tr></thead><tbody>{query.data?.map(item => <tr key={item.id}><td className="font-semibold">{item.name}</td><td>{item.slug}</td><td><StatusBadge value={item.status} /></td><td>{item.activeUserCount}</td><td>{formatDate(item.createdAt)}</td></tr>)}</tbody></table></div>}</main>;
}
