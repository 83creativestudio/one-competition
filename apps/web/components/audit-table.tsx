"use client";

import { useQuery } from "@tanstack/react-query";
import { ErrorState, formatDate, LoadingState } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { AuditEvent } from "@/lib/contracts";

export function AuditTable({ endpoint, queryKey }: { endpoint: string; queryKey: string }) {
  const query = useQuery({ queryKey: [queryKey], queryFn: () => apiFetch<AuditEvent[]>(endpoint) });
  if (query.isLoading) return <LoadingState />;
  if (query.error) return <ErrorState error={query.error} />;
  return <div className="table-wrap"><table className="data-table"><thead><tr><th>Time</th><th>Action</th><th>Entity</th><th>Actor</th><th>Correlation</th></tr></thead><tbody>{query.data?.map(item => <tr key={item.id}><td>{formatDate(item.occurredAt)}</td><td className="font-semibold">{item.action}</td><td>{item.entityType}{item.entityId ? ` · ${item.entityId}` : ""}</td><td>{item.actorType}</td><td><code className="text-xs">{item.correlationId}</code></td></tr>)}</tbody></table>{!query.data?.length && <div className="empty-state">No audit events found.</div>}</div>;
}
