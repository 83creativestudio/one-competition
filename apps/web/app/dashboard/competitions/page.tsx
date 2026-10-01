"use client";

import { useQuery } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import Link from "next/link";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Competition } from "@/lib/contracts";

export default function CompetitionsPage() {
  const query = useQuery({ queryKey: ["competitions"], queryFn: () => apiFetch<Competition[]>("api/competitions") });
  return <main className="page">
    <PageHeading title="Competitions" description="Create, publish, close, and operate standard draw campaigns."
      action={<Link className="command-button" href="/dashboard/competitions/new"><Plus size={17} />New competition</Link>} />
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> :
      <div className="table-wrap"><table className="data-table"><thead><tr><th>Competition</th><th>Status</th><th>Opens</th><th>Closes</th><th>Winners</th></tr></thead><tbody>
        {query.data?.map(item => <tr key={item.id}><td><Link className="font-semibold text-brand hover:underline" href={`/dashboard/competitions/${item.id}`}>{item.name}</Link><div className="mt-1 text-xs text-muted">/{item.slug}</div></td><td><StatusBadge value={item.status} /></td><td>{formatDate(item.startsAt)}</td><td>{formatDate(item.endsAt)}</td><td>{item.numberOfWinners} + {item.numberOfReserveWinners} reserves</td></tr>)}
        {!query.data?.length && <tr><td colSpan={5}><div className="empty-state">No competitions have been created.</div></td></tr>}
      </tbody></table></div>}
  </main>;
}
