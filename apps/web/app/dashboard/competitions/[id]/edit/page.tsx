"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Save } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { ErrorState, LoadingState, PageHeading } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import { competitionSettings, competitionSettingsRequest, competitionSettingsSchema, participantProviders, type CompetitionSettings } from "@/lib/competition-settings";
import type { Competition } from "@/lib/contracts";

export default function EditCompetitionPage() {
  const id = useParams<{ id: string }>().id;
  const query = useQuery({ queryKey: ["competition", id], queryFn: () => apiFetch<Competition>(`api/competitions/${id}`) });
  if (query.isLoading) return <main className="page"><LoadingState /></main>;
  if (query.error || !query.data) return <main className="page"><ErrorState error={query.error} /></main>;
  return <CompetitionEditor key={id} item={query.data} />;
}

function CompetitionEditor({ item }: { item: Competition }) {
  const cache = useQueryClient();
  const [saved, setSaved] = useState(false);
  const [confirmed, setConfirmed] = useState(false);
  const form = useForm<CompetitionSettings>({ resolver: zodResolver(competitionSettingsSchema), defaultValues: competitionSettings(item) });
  const save = useMutation({
    mutationFn: async (values: CompetitionSettings) => {
      const current = await apiFetch<Competition>(`api/competitions/${item.id}`);
      return apiFetch<Competition>(`api/competitions/${item.id}`, {
        method: "PATCH", body: JSON.stringify({ ...competitionSettingsRequest(values), requiresEmailVerification: current.requiresEmailVerification })
      });
    },
    onSuccess: value => {
      cache.setQueryData(["competition", item.id], value);
      void cache.invalidateQueries({ queryKey: ["competitions"] });
      void cache.invalidateQueries({ queryKey: ["versions", item.id] });
      form.reset(competitionSettings(value));
      setSaved(true); setConfirmed(false);
    }
  });
  const locked = ["DrawPreparation", "DrawApproved", "WinnerVerification", "Completed", "Archived", "Cancelled"].includes(item.status);
  const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const error = (name: keyof CompetitionSettings) => form.formState.errors[name]
    ? <p id={`error-${name}`} className="field-error" role="alert">{form.formState.errors[name]?.message}</p> : null;
  return <main className="page max-w-4xl">
    <PageHeading title="Edit competition" description={item.name} action={<Link className="secondary-button" href={`/dashboard/competitions/${item.id}`}><ArrowLeft size={17} />Back</Link>} />
    <nav className="mb-6 flex flex-wrap gap-4 text-sm" aria-label="Competition editors">
      {[["Content and media", "builder"], ["Entry fields", "fields"], ["Rules", "rules"]].map(([label, route]) => <Link key={route} className="font-semibold text-brand hover:underline" href={`/dashboard/competitions/${item.id}/${route}`}>{label}</Link>)}
    </nav>
    {locked && <p className="mb-5 text-sm text-amber-800" role="status">Competition settings are locked for this stage.</p>}
    <form onSubmit={form.handleSubmit(values => { setSaved(false); save.mutate(values); })}>
      <fieldset disabled={locked || save.isPending} className="grid min-w-0 gap-5 border-t border-line pt-5 md:grid-cols-2">
        <label className="field md:col-span-2"><span>Name</span><input {...form.register("name")} maxLength={200} aria-invalid={!!form.formState.errors.name} />{error("name")}</label>
        <label className="field md:col-span-2"><span>Description</span><textarea {...form.register("description")} />{error("description")}</label>
        <div className="text-sm text-muted md:col-span-2">URL: /{item.slug}</div>
        {([ ["startsAt", "Opens"], ["endsAt", "Closes"] ] as const).map(([name, label]) => <label key={name} className="field"><span>{label} ({timezone})</span><input type="datetime-local" step="0.001" {...form.register(name)} aria-invalid={!!form.formState.errors[name]} />{error(name)}</label>)}
        {([ ["entryLimit", "Total entry limit", 1], ["perParticipantEntryLimit", "Entries per participant", 1], ["numberOfWinners", "Winners", 1], ["numberOfReserveWinners", "Reserve winners", 0], ["minimumAge", "Minimum age", 0] ] as const).map(([name, label, minimum]) => <label key={name} className="field"><span>{label}{["entryLimit", "minimumAge"].includes(name) ? " (optional)" : ""}</span><input type="number" min={minimum} step="1" {...form.register(name, { setValueAs: value => value === "" ? undefined : Number(value) })} aria-invalid={!!form.formState.errors[name]} />{error(name)}</label>)}
        <label className="field md:col-span-2"><span>Allowed countries</span><input placeholder="CY, GR" {...form.register("allowedCountryCodes")} aria-invalid={!!form.formState.errors.allowedCountryCodes} />{error("allowedCountryCodes")}</label>
        {([ ["requiresManualApproval", "Require manual entry approval"], ["requiresPhoneVerification", "Require phone verification"] ] as const).map(([name, label]) => <label key={name} className="flex items-center gap-3 text-sm"><input type="checkbox" {...form.register(name)} />{label}</label>)}
        <div className="text-sm md:col-span-2">Email verification: {item.requiresEmailVerification ? "Enabled" : "Off"}<Link className="ml-3 font-semibold text-brand" href="/dashboard/settings">Manage in Settings</Link></div>
        <fieldset className="min-w-0 md:col-span-2"><legend className="text-sm font-semibold">Participant login methods</legend><div className="mt-3 grid gap-3 sm:grid-cols-3">{participantProviders.map(provider => <label key={provider} className="flex items-center gap-2 text-sm"><input type="checkbox" value={provider} {...form.register("allowedParticipantAuthProviders")} />{provider}</label>)}</div>{error("allowedParticipantAuthProviders")}</fieldset>
        {item.publishedAt && <label className="flex items-start gap-3 border-t border-line pt-4 text-sm md:col-span-2"><input type="checkbox" className="mt-1" checked={confirmed} onChange={event => setConfirmed(event.target.checked)} />Apply these settings to the published competition.</label>}
        {save.error && <div className="md:col-span-2"><ErrorState error={save.error} /></div>}
        {saved && !form.formState.isDirty && <p className="text-sm text-emerald-800 md:col-span-2" role="status">Competition changes saved.</p>}
        <div className="flex flex-wrap gap-3 md:col-span-2"><button type="submit" className="command-button" disabled={!form.formState.isDirty || (!!item.publishedAt && !confirmed)}><Save size={17} />{save.isPending ? "Saving…" : "Save changes"}</button><Link className="secondary-button" href={`/dashboard/competitions/${item.id}`}>Cancel</Link></div>
      </fieldset>
    </form>
  </main>;
}
