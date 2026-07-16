"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { ArrowLeft, Save } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { PageHeading } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Competition } from "@/lib/contracts";

const schema = z.object({
  name: z.string().min(3), slug: z.string().regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, "Use lowercase words separated by hyphens."),
  description: z.string().optional(), startsAt: z.string().min(1), endsAt: z.string().min(1), entryLimit: z.coerce.number().int().positive().optional(),
  perParticipantEntryLimit: z.coerce.number().int().min(1), numberOfWinners: z.coerce.number().int().min(1), numberOfReserveWinners: z.coerce.number().int().min(0), requiresManualApproval: z.boolean(),
  minimumAge: z.coerce.number().int().min(0).optional(), allowedCountryCodes: z.string().optional(), requiresEmailVerification: z.boolean(), requiresPhoneVerification: z.boolean()
}).refine(data => new Date(data.endsAt) > new Date(data.startsAt), { path: ["endsAt"], message: "Closing time must be after opening time." });
type FormData = z.infer<typeof schema>;

export default function NewCompetitionPage() {
  const router = useRouter();
  const form = useForm<FormData>({ resolver: zodResolver(schema), defaultValues: { perParticipantEntryLimit: 1, numberOfWinners: 1, numberOfReserveWinners: 1, requiresManualApproval: true, requiresEmailVerification: true, requiresPhoneVerification: false } });
  const create = useMutation({ mutationFn: (values: FormData) => apiFetch<Competition>("api/competitions", { method: "POST", body: JSON.stringify({ ...values, entryLimit: values.entryLimit || null, minimumAge: values.minimumAge || null, allowedCountryCodes: values.allowedCountryCodes?.split(",").map(x => x.trim()).filter(Boolean) ?? [], startsAt: new Date(values.startsAt).toISOString(), endsAt: new Date(values.endsAt).toISOString() }) }), onSuccess: item => router.push(`/dashboard/competitions/${item.id}`) });
  return <main className="page max-w-4xl">
    <PageHeading title="New competition" description="Create the operational draft. Rules, fields and public content are configured after creation." action={<Link className="secondary-button" href="/dashboard/competitions"><ArrowLeft size={17} />Back</Link>} />
    <form className="panel grid gap-5 p-5 md:grid-cols-2" onSubmit={form.handleSubmit(values => create.mutate(values))}>
      <label className="field md:col-span-2"><span>Name</span><input {...form.register("name")} autoFocus /></label>{form.formState.errors.name && <p className="field-error md:col-span-2">{form.formState.errors.name.message}</p>}
      <label className="field"><span>URL slug</span><input {...form.register("slug")} /></label>
      <label className="field"><span>Total entry limit (optional)</span><input type="number" {...form.register("entryLimit", { setValueAs: v => v === "" ? undefined : Number(v) })} /></label>
      <label className="field md:col-span-2"><span>Description</span><textarea {...form.register("description")} /></label>
      <label className="field"><span>Opens</span><input type="datetime-local" {...form.register("startsAt")} /></label>
      <label className="field"><span>Closes</span><input type="datetime-local" {...form.register("endsAt")} /></label>{form.formState.errors.endsAt && <p className="field-error md:col-span-2">{form.formState.errors.endsAt.message}</p>}
      <label className="field"><span>Entries per participant</span><input type="number" {...form.register("perParticipantEntryLimit")} /></label>
      <label className="field"><span>Winners</span><input type="number" {...form.register("numberOfWinners")} /></label>
      <label className="field"><span>Reserve winners</span><input type="number" {...form.register("numberOfReserveWinners")} /></label>
      <label className="field"><span>Minimum age (optional)</span><input type="number" {...form.register("minimumAge", { setValueAs: v => v === "" ? undefined : Number(v) })} /></label>
      <label className="field md:col-span-2"><span>Allowed country codes (comma separated, blank for all)</span><input placeholder="CY, GR" {...form.register("allowedCountryCodes")} /></label>
      <label className="flex items-center gap-3 self-end pb-2 text-sm font-medium"><input className="h-4 w-4" type="checkbox" {...form.register("requiresManualApproval")} />Require manual entry approval</label>
      <label className="flex items-center gap-3 self-end pb-2 text-sm font-medium"><input className="h-4 w-4" type="checkbox" {...form.register("requiresEmailVerification")} />Require email verification</label>
      <label className="flex items-center gap-3 self-end pb-2 text-sm font-medium"><input className="h-4 w-4" type="checkbox" {...form.register("requiresPhoneVerification")} />Require phone verification</label>
      {create.error && <p className="text-sm text-red-700 md:col-span-2">{create.error.message}</p>}
      <div className="md:col-span-2"><button className="command-button" disabled={create.isPending} type="submit"><Save size={17} />{create.isPending ? "Creating" : "Create draft"}</button></div>
    </form>
  </main>;
}
