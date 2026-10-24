"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { MailX, Save } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { ErrorState, LoadingState } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Competition } from "@/lib/contracts";

export function CompetitionVerificationSettings() {
  const cache = useQueryClient();
  const [selected, setSelected] = useState("");
  const [resetConfirmed, setResetConfirmed] = useState(false);
  const query = useQuery({ queryKey: ["competitions"], queryFn: () => apiFetch<Competition[]>("api/competitions") });
  const reset = useMutation({ mutationFn: () => apiFetch<{ updatedCompetitions: number; lockedCompetitions: number }>("api/competitions/email-verification/disable-all", { method: "POST" }), onSuccess: () => {
    setSelected(""); setResetConfirmed(false);
    void cache.invalidateQueries({ queryKey: ["competitions"] });
    void cache.invalidateQueries({ queryKey: ["competition"] });
    void cache.invalidateQueries({ queryKey: ["versions"] });
  } });
  const item = query.data?.find(value => value.id === selected);
  return <section className="mt-8 max-w-3xl border-t border-line pt-6">
    <h2 className="text-lg font-semibold">Participant email verification</h2>
    <div className="mt-3 flex flex-wrap items-center gap-3 text-sm"><span>Default for all new competitions</span><span className="status">Off</span></div>
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <>
      <form className="mt-4 grid gap-3 border-b border-line pb-5" onSubmit={event => { event.preventDefault(); reset.mutate(); }}>
        <label className="flex items-start gap-3 text-sm"><input required type="checkbox" className="mt-1" checked={resetConfirmed} disabled={reset.isPending} onChange={event => setResetConfirmed(event.target.checked)} />Disable email verification for new entries in all editable competitions in this workspace.</label>
        <button className="secondary-button w-fit" disabled={!resetConfirmed || reset.isPending}><MailX size={16} />Disable for all competitions</button>
        {reset.error && <ErrorState error={reset.error} />}
        {reset.data && <p className="text-sm text-emerald-800" role="status">Verification disabled for {reset.data.updatedCompetitions} competitions.{reset.data.lockedCompetitions > 0 ? ` ${reset.data.lockedCompetitions} locked competitions were unchanged.` : ""}</p>}
      </form>
      <label className="field mt-5"><span>Individual competition exception</span><select value={selected} onChange={event => setSelected(event.target.value)}><option value="">Select a competition</option>{query.data?.map(value => <option key={value.id} value={value.id}>{value.name} - {value.requiresEmailVerification ? "Verification enabled" : "Off"}</option>)}</select></label>
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
