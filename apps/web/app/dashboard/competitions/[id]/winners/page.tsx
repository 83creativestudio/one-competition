"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Gift, Mail, ShieldX } from "lucide-react";
import { useParams } from "next/navigation";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Winner } from "@/lib/contracts";

export default function WinnersPage() {
  const id = useParams<{ id: string }>().id; const cache = useQueryClient();
  const query = useQuery({ queryKey: ["winners", id], queryFn: () => apiFetch<Winner[]>(`api/competitions/${id}/winners`) });
  const action = useMutation({ mutationFn: ({ winnerId, command }: { winnerId: string; command: string }) => { const notes = ["contact", "disqualify"].includes(command) ? window.prompt(command === "contact" ? "Contact attempt notes:" : "Disqualification reason:") : undefined; if (command === "disqualify" && !notes) throw new Error("A disqualification reason is required."); return apiFetch<Winner>(`api/competitions/${id}/winners/${winnerId}/${command}`, { method: "POST", body: notes !== undefined ? JSON.stringify({ notes }) : undefined }); }, onSuccess: () => cache.invalidateQueries({ queryKey: ["winners", id] }) });
  return <main className="page"><PageHeading title="Winner management" description="Track contact, acceptance, eligibility decisions, and prize fulfilment without deleting selection history." />{action.error && <p className="mb-4 text-sm text-red-700">{action.error.message}</p>}{query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="table-wrap"><table className="data-table"><thead><tr><th>Claim</th><th>Status</th><th>Contacted</th><th>Accepted</th><th>Delivered</th><th>Actions</th></tr></thead><tbody>{query.data?.map(winner => <tr key={winner.id}><td className="font-mono text-xs">{winner.id.slice(0, 8)}</td><td><StatusBadge value={winner.status} /></td><td>{formatDate(winner.firstContactedAt)}</td><td>{formatDate(winner.acceptedAt)}</td><td>{formatDate(winner.prizeDeliveredAt)}</td><td><div className="flex gap-1"><Action title="Record contact" icon={<Mail size={15} />} onClick={() => action.mutate({ winnerId: winner.id, command: "contact" })} /><Action title="Accept claim" icon={<Check size={15} />} onClick={() => action.mutate({ winnerId: winner.id, command: "accept" })} /><Action title="Deliver prize" icon={<Gift size={15} />} onClick={() => action.mutate({ winnerId: winner.id, command: "deliver-prize" })} /><Action title="Disqualify" icon={<ShieldX size={15} />} onClick={() => action.mutate({ winnerId: winner.id, command: "disqualify" })} /></div></td></tr>)}{!query.data?.length && <tr><td colSpan={6}><div className="empty-state">No winner claims are available.</div></td></tr>}</tbody></table></div>}</main>;
}
function Action({ title, icon, onClick }: { title: string; icon: React.ReactNode; onClick: () => void }) { return <button className="icon-button" title={title} onClick={onClick}>{icon}</button>; }
