"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, CopyX, ShieldX, X } from "lucide-react";
import { useParams } from "next/navigation";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Entry } from "@/lib/contracts";

export default function EntriesPage() {
  const id = useParams<{ id: string }>().id;
  const cache = useQueryClient();
  const query = useQuery({ queryKey: ["entries", id], queryFn: () => apiFetch<Entry[]>(`api/competitions/${id}/entries`) });
  const review = useMutation({ mutationFn: ({ entryId, action }: { entryId: string; action: string }) => {
    const needsReason = action !== "approve"; const notes = needsReason ? window.prompt("Record the review reason:") : null;
    if (needsReason && !notes) throw new Error("A review reason is required.");
    return apiFetch<Entry>(`api/competitions/${id}/entries/${entryId}/${action}`, { method: "POST", body: needsReason ? JSON.stringify({ decision: action, notes }) : undefined });
  }, onSuccess: () => cache.invalidateQueries({ queryKey: ["entries", id] }) });
  return <main className="page"><PageHeading title="Entry review" description="Inspect risk signals and make auditable eligibility decisions." />
    {review.error && <p className="mb-4 text-sm text-red-700">{review.error.message}</p>}
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="table-wrap"><table className="data-table"><thead><tr><th>Reference</th><th>Submitted</th><th>Status</th><th>Eligibility</th><th>Risk</th><th>Review</th></tr></thead><tbody>{query.data?.map(entry => <tr key={entry.id}><td className="font-mono text-xs font-semibold">{entry.entryReference}</td><td>{formatDate(entry.submittedAt)}</td><td><StatusBadge value={entry.status} /></td><td><StatusBadge value={entry.eligibilityStatus} /></td><td><StatusBadge value={`${entry.riskLevel} ${entry.riskScore}`} /></td><td><div className="flex gap-1"><Action title="Approve" icon={<Check size={15} />} onClick={() => review.mutate({ entryId: entry.id, action: "approve" })} /><Action title="Reject" icon={<X size={15} />} onClick={() => review.mutate({ entryId: entry.id, action: "reject" })} /><Action title="Mark duplicate" icon={<CopyX size={15} />} onClick={() => review.mutate({ entryId: entry.id, action: "mark-duplicate" })} /><Action title="Disqualify" icon={<ShieldX size={15} />} onClick={() => review.mutate({ entryId: entry.id, action: "disqualify" })} /></div></td></tr>)}{!query.data?.length && <tr><td colSpan={6}><div className="empty-state">No entries submitted.</div></td></tr>}</tbody></table></div>}
  </main>;
}
function Action({ title, icon, onClick }: { title: string; icon: React.ReactNode; onClick: () => void }) { return <button className="icon-button" title={title} onClick={onClick}>{icon}</button>; }
