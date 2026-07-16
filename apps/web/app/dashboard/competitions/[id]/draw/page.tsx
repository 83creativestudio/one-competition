"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, Download, Play, Shuffle } from "lucide-react";
import { useParams } from "next/navigation";
import { useState } from "react";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Draw } from "@/lib/contracts";

export default function DrawPage() {
  const id = useParams<{ id: string }>().id; const cache = useQueryClient();
  const [winnerCount, setWinnerCount] = useState(1); const [reserveCount, setReserveCount] = useState(1);
  const query = useQuery({ queryKey: ["draws", id], queryFn: () => apiFetch<Draw[]>(`api/competitions/${id}/draws`) });
  const refresh = () => cache.invalidateQueries({ queryKey: ["draws", id] });
  const prepare = useMutation({ mutationFn: () => apiFetch<Draw>(`api/competitions/${id}/draws/prepare`, { method: "POST", body: JSON.stringify({ winnerCount, reserveCount }) }), onSuccess: refresh });
  const action = useMutation({ mutationFn: ({ drawId, command }: { drawId: string; command: string }) => apiFetch<Draw>(`api/competitions/${id}/draws/${drawId}/${command}`, { method: "POST" }), onSuccess: refresh });
  const error = prepare.error ?? action.error;
  return <main className="page"><PageHeading title="Draw control" description="Freeze the eligible pool, approve it, and execute a cryptographically secure draw." />
    <form className="panel mb-5 flex flex-wrap items-end gap-4 p-4" onSubmit={e => { e.preventDefault(); prepare.mutate(); }}><label className="field w-36"><span>Winners</span><input min={1} type="number" value={winnerCount} onChange={e => setWinnerCount(Number(e.target.value))} /></label><label className="field w-36"><span>Reserves</span><input min={0} type="number" value={reserveCount} onChange={e => setReserveCount(Number(e.target.value))} /></label><button className="command-button" disabled={prepare.isPending}><Shuffle size={17} />Prepare draw</button></form>
    {error && <p className="mb-4 text-sm text-red-700">{error.message}</p>}
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="grid gap-4">{query.data?.map(draw => <article className="panel p-5" key={draw.id}><div className="flex flex-wrap items-start justify-between gap-4"><div><div className="font-mono text-sm font-semibold">{draw.drawReference}</div><div className="mt-2"><StatusBadge value={draw.status} /></div></div><div className="flex gap-2">{["Prepared", "AwaitingApproval"].includes(draw.status) && <button className="secondary-button" onClick={() => action.mutate({ drawId: draw.id, command: "approve" })}><CheckCircle2 size={17} />Approve</button>}{draw.status === "Approved" && <button className="command-button" onClick={() => action.mutate({ drawId: draw.id, command: "execute" })}><Play size={17} />Execute</button>}{draw.status === "Completed" && <a className="secondary-button" href={`/api/backend/api/competitions/${id}/draws/${draw.id}/certificate`}><Download size={17} />Certificate</a>}</div></div><dl className="mt-5 grid gap-4 text-sm sm:grid-cols-3"><Info label="Pool" value={`${draw.eligibleEntryCount} eligible / ${draw.excludedEntryCount} excluded`} /><Info label="Selection" value={`${draw.requestedWinnerCount} winners / ${draw.requestedReserveCount} reserves`} /><Info label="Executed" value={formatDate(draw.executedAt)} /></dl></article>)}{!query.data?.length && <div className="panel empty-state">No draw has been prepared.</div>}</div>}
  </main>;
}
function Info({ label, value }: { label: string; value: string }) { return <div><dt className="text-xs font-semibold uppercase text-muted">{label}</dt><dd className="mt-1">{value}</dd></div>; }
