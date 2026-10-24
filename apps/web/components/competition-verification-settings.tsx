"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Save } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { ErrorState, LoadingState } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Competition } from "@/lib/contracts";

export function CompetitionVerificationSettings() {
  const [selected, setSelected] = useState("");
  const query = useQuery({ queryKey: ["competitions"], queryFn: () => apiFetch<Competition[]>("api/competitions") });
  const item = query.data?.find(value => value.id === selected);
  return <section className="mt-8 max-w-3xl border-t border-line pt-6">
    <h2 className="text-lg font-semibold">Participant email verification</h2>
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <>
      <label className="field mt-4"><span>Competition</span><select value={selected} onChange={event => setSelected(event.target.value)}><option value="">Select a competition</option>{query.data?.map(value => <option key={value.id} value={value.id}>{value.name}</option>)}</select></label>
      {query.data?.length === 0 && <p className="mt-3 text-sm text-muted">No competitions yet.</p>}
      {item && <VerificationEditor key={item.id} item={item} />}
    </>}
  </section>;
}

function VerificationEditor({ item }: { item: Competition }) {
  const cache = useQueryClient();
  const [required, setRequired] = useState(item.requiresEmailVerification);
  const [confirmed, setConfirmed] = useState(false);
  const [saved, setSaved] = useState(false);
  const locked = ["DrawPreparation", "DrawApproved", "WinnerVerification", "Completed", "Archived", "Cancelled"].includes(item.status);
  const save = useMutation({ mutationFn: () => apiFetch<Competition>(`api/competitions/${item.id}/email-verification`, { method: "PATCH", body: JSON.stringify({ requiresEmailVerification: required }) }), onSuccess: value => {
    cache.setQueryData(["competition", item.id], value);
    setSaved(true);
    void cache.invalidateQueries({ queryKey: ["competitions"] });
    void cache.invalidateQueries({ queryKey: ["versions", item.id] });
  } });
  return <form className="mt-4 grid gap-4" onSubmit={event => { event.preventDefault(); save.mutate(); }}>
    <label className="flex items-start gap-3 text-sm"><input type="checkbox" className="mt-1" checked={required} disabled={locked || save.isPending} onChange={event => { setRequired(event.target.checked); setSaved(false); }} /><span>Require email verification for new entries</span></label>
    {item.publishedAt && <label className="flex items-start gap-3 text-sm"><input type="checkbox" className="mt-1" checked={confirmed} disabled={locked || save.isPending} onChange={event => setConfirmed(event.target.checked)} /><span>Apply this change to the published competition.</span></label>}
    {locked && <p className="text-sm text-amber-800">Settings are locked for this competition stage.</p>}
    {save.error && <ErrorState error={save.error} />}
    {saved && <p className="text-sm text-emerald-800" role="status">Verification setting saved.</p>}
    <div className="flex flex-wrap gap-3"><button className="command-button" disabled={locked || save.isPending || required === item.requiresEmailVerification || (!!item.publishedAt && !confirmed)}><Save size={16} />Save verification setting</button><Link className="secondary-button" href={`/dashboard/competitions/${item.id}/fields`}>Edit entry fields</Link></div>
  </form>;
}
