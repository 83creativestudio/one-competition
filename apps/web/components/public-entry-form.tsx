"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { CheckCircle2, LoaderCircle } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { apiFetch } from "@/lib/api";
import type { Entry, PublicCompetition } from "@/lib/contracts";

declare global { interface Window { turnstile?: { render(element: HTMLElement, options: { sitekey: string; callback: (token: string) => void; "expired-callback": () => void }): string } } }

function Captcha({ onToken }: { onToken: (token: string) => void }) {
  const container = useRef<HTMLDivElement>(null); const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;
  useEffect(() => {
    if (!siteKey) { onToken("development-pass"); return; }
    const render = () => { if (container.current && window.turnstile) window.turnstile.render(container.current, { sitekey: siteKey, callback: onToken, "expired-callback": () => onToken("") }); };
    const existing = document.getElementById("turnstile-script") as HTMLScriptElement | null;
    if (existing) { if (window.turnstile) render(); else existing.addEventListener("load", render, { once: true }); return; }
    const script = document.createElement("script"); script.id = "turnstile-script"; script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit"; script.async = true; script.defer = true; script.addEventListener("load", render, { once: true }); document.head.appendChild(script);
  }, [onToken, siteKey]);
  return <div ref={container} />;
}

export function PublicEntryForm({ competitionSlug, tenantSlug }: { competitionSlug: string; tenantSlug?: string }) {
  const prefix = tenantSlug ? `c/${tenantSlug}/api/public` : "api/public"; const startedAt = useRef(new Date().toISOString());
  const competition = useQuery({ queryKey: ["public-competition", tenantSlug, competitionSlug], queryFn: () => apiFetch<PublicCompetition>(`${prefix}/competitions/${competitionSlug}`) });
  const [values, setValues] = useState<Record<string, string>>({}); const [consents, setConsents] = useState<Record<string, boolean>>({}); const [captchaToken, setCaptchaToken] = useState(""); const [entry, setEntry] = useState<Entry>(); const [code, setCode] = useState("");
  const submit = useMutation({ mutationFn: () => apiFetch<Entry>(`${prefix}/competitions/${competitionSlug}/entries`, { method: "POST", body: JSON.stringify({
    email: values.email, phone: values.phone, firstName: values.first_name, lastName: values.last_name, preferredLanguage: "en",
    idempotencyKey: crypto.randomUUID(), deviceFingerprint: `${navigator.language}:${screen.width}x${screen.height}:${navigator.platform}`,
    formStartedAt: startedAt.current, captchaToken, dateOfBirth: values.date_of_birth || undefined, countryCode: values.country || undefined,
    answers: competition.data?.fields.map(field => ({ fieldKey: field.fieldKey, value: values[field.fieldKey] ?? "" })) ?? [],
    consents: competition.data?.consents.map(consent => ({ consentDefinitionId: consent.id, accepted: !!consents[consent.id] })) ?? []
  }) }), onSuccess: setEntry });
  const verify = useMutation({ mutationFn: (channel: "email" | "phone") => apiFetch<Entry>(`${prefix}/entries/${entry?.entryReference}/verify-${channel}`, { method: "POST", body: JSON.stringify({ code }) }), onSuccess: setEntry });
  if (competition.isLoading) return <main className="grid min-h-[60vh] place-items-center"><LoaderCircle className="animate-spin" /></main>;
  if (competition.error || !competition.data) return <main className="mx-auto max-w-2xl px-6 py-12"><h1 className="text-2xl font-semibold">Competition unavailable</h1><p className="mt-3 text-sm text-red-700">{competition.error instanceof Error ? competition.error.message : "The campaign could not be loaded."}</p></main>;
  const data = competition.data;
  if (entry && !entry.status.includes("VerificationPending")) return <main className="mx-auto max-w-2xl px-6 py-12"><CheckCircle2 className="text-emerald-700" size={34} /><h1 className="mt-5 text-3xl font-semibold">Entry received</h1><p className="mt-3">Reference <strong>{entry.entryReference}</strong></p><p className="mt-2 text-sm text-neutral-700">Status: {entry.status}</p></main>;
  if (entry) { const channel = entry.status.startsWith("Email") ? "email" : "phone"; return <main className="mx-auto max-w-lg px-6 py-12"><h1 className="text-3xl font-semibold">Verify your {channel}</h1><p className="mt-3 text-sm text-neutral-700">Enter the six-digit code sent to you. Entry reference: {entry.entryReference}</p><div className="mt-6 grid gap-4"><label className="field"><span>Verification code</span><input inputMode="numeric" maxLength={6} value={code} onChange={e => setCode(e.target.value)} /></label><button className="command-button w-fit" disabled={code.length !== 6 || verify.isPending} onClick={() => verify.mutate(channel)}>Verify entry</button>{verify.error && <p className="text-sm text-red-700">{verify.error.message}</p>}</div></main>; }
  return <main className="mx-auto max-w-2xl px-5 py-10"><header className="border-b border-line pb-6"><div className="text-sm font-semibold text-emerald-800">ONE. Competitions</div><h1 className="mt-3 text-3xl font-semibold">{data.name}</h1><p className="mt-2 text-sm text-neutral-700">Entries close {new Date(data.endsAt).toLocaleString()}.</p></header><form className="mt-6 grid gap-5" onSubmit={e => { e.preventDefault(); submit.mutate(); }}>{data.fields.filter(x => x.fieldType !== "Hidden" && x.fieldType !== "Consent").map(field => <label className="field" key={field.id}><span>{field.label}{field.isRequired ? " *" : ""}</span>{field.fieldType === "Textarea" ? <textarea required={field.isRequired} value={values[field.fieldKey] ?? ""} onChange={e => setValues({ ...values, [field.fieldKey]: e.target.value })} /> : <input required={field.isRequired} type={field.fieldType === "Email" ? "email" : field.fieldType === "Phone" ? "tel" : field.fieldType === "Date" || field.fieldType === "DateOfBirth" ? "date" : "text"} value={values[field.fieldKey] ?? ""} onChange={e => setValues({ ...values, [field.fieldKey]: e.target.value })} />}{field.helpText && <small className="mt-1 block font-normal text-neutral-600">{field.helpText}</small>}</label>)}<fieldset className="grid gap-3"><legend className="mb-2 font-semibold">Consent</legend>{data.consents.map(consent => <label className="flex items-start gap-3 text-sm" key={consent.id}><input className="mt-1" type="checkbox" required={consent.isRequired} checked={!!consents[consent.id]} onChange={e => setConsents({ ...consents, [consent.id]: e.target.checked })} /><span>{consent.text}{consent.isRequired ? " *" : ""}</span></label>)}</fieldset><Captcha onToken={setCaptchaToken} />{submit.error && <p className="border border-red-200 bg-red-50 p-3 text-sm text-red-700">{submit.error.message}</p>}<button className="command-button w-fit" disabled={!captchaToken || submit.isPending}>{submit.isPending ? "Submitting" : "Submit entry"}</button></form></main>;
}
