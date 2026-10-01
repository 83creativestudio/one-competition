"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Lock, Radio, Save } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Competition, Entry } from "@/lib/contracts";

export default function CompetitionDetailPage() {
  const id = useParams<{ id: string }>().id;
  const client = useQueryClient();
  const [authProviders, setAuthProviders] = useState<string[]>([]);
  const competition = useQuery({ queryKey: ["competition", id], queryFn: () => apiFetch<Competition>(`api/competitions/${id}`) });
  useEffect(() => { if (competition.data) setAuthProviders(competition.data.allowedParticipantAuthProviders); }, [competition.data]);
  const entries = useQuery({ queryKey: ["entries", id], queryFn: () => apiFetch<Entry[]>(`api/competitions/${id}/entries`) });
  const action = useMutation({ mutationFn: (name: "publish" | "close") => apiFetch<Competition>(`api/competitions/${id}/${name}`, { method: "POST", body: name === "close" ? JSON.stringify("Closed from dashboard") : undefined }), onSuccess: value => client.setQueryData(["competition", id], value) });
  const saveAuth = useMutation({ mutationFn: () => {
    const item = competition.data!;
    return apiFetch<Competition>(`api/competitions/${id}`, { method: "PATCH", body: JSON.stringify({ name: item.name, description: item.description ?? null, startsAt: item.startsAt, endsAt: item.endsAt, entryLimit: item.entryLimit ?? null, perParticipantEntryLimit: item.perParticipantEntryLimit, numberOfWinners: item.numberOfWinners, numberOfReserveWinners: item.numberOfReserveWinners, requiresManualApproval: item.requiresManualApproval, minimumAge: item.minimumAge ?? null, allowedCountryCodes: item.allowedCountryCodes, requiresEmailVerification: item.requiresEmailVerification, requiresPhoneVerification: item.requiresPhoneVerification, allowedParticipantAuthProviders: authProviders }) });
  }, onSuccess: value => client.setQueryData(["competition", id], value) });
  if (competition.isLoading) return <main className="page"><LoadingState /></main>;
  if (competition.error || !competition.data) return <main className="page"><ErrorState error={competition.error} /></main>;
  const item = competition.data;
  const approved = entries.data?.filter(x => x.status === "Approved").length ?? 0;
  return <main className="page">
    <PageHeading title={item.name} description={`/${item.slug}`} action={<div className="flex gap-2">{item.status === "Draft" && <button className="command-button" disabled={action.isPending} onClick={() => action.mutate("publish")}><Radio size={17} />Publish</button>}{["Live", "Scheduled"].includes(item.status) && <button className="danger-button" disabled={action.isPending} onClick={() => action.mutate("close")}><Lock size={17} />Close</button>}</div>} />
    {action.error && <p className="mb-4 text-sm text-red-700">{action.error.message}</p>}
    <section className="grid gap-4 md:grid-cols-4">
      <Metric label="Status" value={<StatusBadge value={item.status} />} /><Metric label="Entries" value={String(entries.data?.length ?? 0)} /><Metric label="Approved" value={String(approved)} /><Metric label="Winner positions" value={`${item.numberOfWinners + item.numberOfReserveWinners}`} />
    </section>
    <section className="mt-5 panel p-5"><h2 className="font-semibold">Schedule and controls</h2><dl className="mt-4 grid gap-4 text-sm sm:grid-cols-2 lg:grid-cols-4"><Detail label="Opens" value={formatDate(item.startsAt)} /><Detail label="Closes" value={formatDate(item.endsAt)} /><Detail label="Per participant" value={String(item.perParticipantEntryLimit)} /><Detail label="Approval" value={item.requiresManualApproval ? "Manual" : "Automatic"} /></dl></section>
    <section className="mt-5 panel p-5"><h2 className="font-semibold">Participant login methods</h2><div className="mt-4 grid gap-3 sm:grid-cols-3">{["Email", "Google", "Apple", "Facebook", "X", "TikTok"].map(provider => <label className="flex items-center gap-2 text-sm" key={provider}><input type="checkbox" checked={authProviders.includes(provider)} onChange={event => setAuthProviders(current => event.target.checked ? [...current, provider] : current.filter(value => value !== provider))} />{provider}</label>)}</div>{saveAuth.error && <p className="mt-3 text-sm text-red-700">{saveAuth.error.message}</p>}<button className="command-button mt-4" disabled={saveAuth.isPending || authProviders.length === 0} onClick={() => saveAuth.mutate()}><Save size={16} />Save login methods</button></section>
    <section className="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {[['Entries', 'entries'], ['Analytics', 'analytics'], ['Draw', 'draw'], ['Winners', 'winners']].map(([label, route]) => <Link className="panel p-4 font-semibold transition hover:border-emerald-700" href={`/dashboard/competitions/${id}/${route}`} key={route}>{label}<span className="mt-2 block text-sm font-normal text-muted">Open operational workspace</span></Link>)}
    </section>
  </main>;
}

function Metric({ label, value }: { label: string; value: React.ReactNode }) { return <div className="panel p-4"><div className="text-xs font-semibold uppercase text-muted">{label}</div><div className="mt-2 text-xl font-semibold">{value}</div></div>; }
function Detail({ label, value }: { label: string; value: string }) { return <div><dt className="text-xs font-semibold uppercase text-muted">{label}</dt><dd className="mt-1">{value}</dd></div>; }
