"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { Check, CheckCircle2, ExternalLink, LoaderCircle, LogIn, ShieldCheck } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { apiFetch } from "@/lib/api";
import type { Entry, ParticipantSocialSession, PublicCompetition, SocialActionRequirement } from "@/lib/contracts";

declare global {
  interface Window {
    turnstile?: { render(element: HTMLElement, options: { sitekey: string; callback: (token: string) => void; "expired-callback": () => void }): string };
  }
}

function Captcha({ onToken }: { onToken: (token: string) => void }) {
  const container = useRef<HTMLDivElement>(null);
  const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;
  useEffect(() => {
    if (!siteKey) { onToken("development-pass"); return; }
    const render = () => {
      if (container.current && window.turnstile)
        window.turnstile.render(container.current, { sitekey: siteKey, callback: onToken, "expired-callback": () => onToken("") });
    };
    const existing = document.getElementById("turnstile-script") as HTMLScriptElement | null;
    if (existing) { if (window.turnstile) render(); else existing.addEventListener("load", render, { once: true }); return; }
    const script = document.createElement("script");
    script.id = "turnstile-script"; script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
    script.async = true; script.defer = true; script.addEventListener("load", render, { once: true }); document.head.appendChild(script);
  }, [onToken, siteKey]);
  return <div ref={container} />;
}

export function PublicEntryForm({ competitionSlug, tenantSlug }: { competitionSlug: string; tenantSlug?: string }) {
  const prefix = tenantSlug ? `c/${tenantSlug}/api/public` : "api/public";
  const startedAt = useRef(new Date().toISOString());
  const competition = useQuery({ queryKey: ["public-competition", tenantSlug, competitionSlug], queryFn: () => apiFetch<PublicCompetition>(`${prefix}/competitions/${competitionSlug}`) });
  const [values, setValues] = useState<Record<string, string>>({});
  const [consents, setConsents] = useState<Record<string, boolean>>({});
  const [captchaToken, setCaptchaToken] = useState("");
  const [entry, setEntry] = useState<Entry>();
  const [code, setCode] = useState("");
  const [socialSession, setSocialSession] = useState<ParticipantSocialSession>();
  const [socialError, setSocialError] = useState("");

  useEffect(() => {
    const query = new URLSearchParams(window.location.search);
    const completionCode = query.get("social_code");
    const callbackError = query.get("social_error");
    if (callbackError) setSocialError("The social account was not connected.");
    if (!completionCode) return;
    fetch("/api/participant-session", {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ code: completionCode, tenantSlug })
    }).then(async response => {
      const payload = await response.json();
      if (!response.ok) throw new Error(payload.detail ?? payload.title ?? "Social login failed.");
      setSocialSession(payload as ParticipantSocialSession);
      setValues(current => ({ ...current, ...(payload.email ? { email: payload.email } : {}) }));
    }).catch(error => setSocialError(error instanceof Error ? error.message : "Social login failed."));
    query.delete("social_code"); query.delete("social_provider"); query.delete("social_error");
    window.history.replaceState({}, "", `${window.location.pathname}${query.size ? `?${query}` : ""}${window.location.hash}`);
  }, [tenantSlug]);

  const submit = useMutation({
    mutationFn: () => apiFetch<Entry>(`${prefix}/competitions/${competitionSlug}/entries`, { method: "POST", body: JSON.stringify({
      email: values.email, phone: values.phone, firstName: values.first_name, lastName: values.last_name, preferredLanguage: "en",
      idempotencyKey: crypto.randomUUID(), deviceFingerprint: `${navigator.language}:${screen.width}x${screen.height}:${navigator.platform}`,
      formStartedAt: startedAt.current, captchaToken, dateOfBirth: values.date_of_birth || undefined, countryCode: values.country || undefined,
      answers: competition.data?.fields.map(field => ({ fieldKey: field.fieldKey, value: values[field.fieldKey] ?? "" })) ?? [],
      consents: competition.data?.consents.map(consent => ({ consentDefinitionId: consent.id, accepted: !!consents[consent.id] })) ?? []
    }) }), onSuccess: setEntry
  });
  const verify = useMutation({ mutationFn: (channel: "email" | "phone") => apiFetch<Entry>(`${prefix}/entries/${entry?.entryReference}/verify-${channel}`, { method: "POST", body: JSON.stringify({ code }) }), onSuccess: setEntry });
  const verifyAction = useMutation({
    mutationFn: (requirementId: string) => apiFetch<{ requirementId: string; status: string }>(`${prefix}/social-actions/${requirementId}/verify`, { method: "POST" }),
    onSuccess: result => setSocialSession(current => current ? { ...current, actions: current.actions.map(action => action.id === result.requirementId ? { ...action, status: result.status } : action) } : current)
  });
  const onCaptcha = useCallback((token: string) => setCaptchaToken(token), []);

  if (competition.isLoading) return <main className="grid min-h-[60vh] place-items-center"><LoaderCircle className="animate-spin" /></main>;
  if (competition.error || !competition.data) return <main className="mx-auto max-w-2xl px-6 py-12"><h1 className="text-2xl font-semibold">Competition unavailable</h1><p className="mt-3 text-sm text-red-700">{competition.error instanceof Error ? competition.error.message : "The campaign could not be loaded."}</p></main>;
  const data = competition.data;

  if (entry && !entry.status.includes("VerificationPending"))
    return <main className="mx-auto max-w-2xl px-6 py-12"><CheckCircle2 className="text-emerald-700" size={34} /><h1 className="mt-5 text-3xl font-semibold">Entry received</h1><p className="mt-3">Reference <strong>{entry.entryReference}</strong></p><p className="mt-2 text-sm text-neutral-700">Status: {entry.status}</p></main>;
  if (entry) {
    const channel = entry.status.startsWith("Email") ? "email" : "phone";
    return <main className="mx-auto max-w-lg px-6 py-12"><h1 className="text-3xl font-semibold">Verify your {channel}</h1><p className="mt-3 text-sm text-neutral-700">Enter the six-digit code sent to you. Entry reference: {entry.entryReference}</p><div className="mt-6 grid gap-4"><label className="field"><span>Verification code</span><input inputMode="numeric" maxLength={6} value={code} onChange={e => setCode(e.target.value)} /></label><button className="command-button w-fit" disabled={code.length !== 6 || verify.isPending} onClick={() => verify.mutate(channel)}>Verify entry</button>{verify.error && <p className="text-sm text-red-700">{verify.error.message}</p>}</div></main>;
  }

  const actions = socialSession?.actions ?? data.socialActions;
  const missingRequiredAction = actions.some(action => action.isRequired && action.status !== "Verified");
  return <main className="mx-auto max-w-2xl px-5 py-10">
    <header className="border-b border-line pb-6"><div className="text-sm font-semibold text-emerald-800">ONE. Competitions</div><h1 className="mt-3 text-3xl font-semibold">{data.name}</h1><p className="mt-2 text-sm text-neutral-700">Entries close {new Date(data.endsAt).toLocaleString()}.</p></header>

    {data.socialAuthProviders.length > 0 && <section className="border-b border-line py-6" aria-labelledby="social-login-title">
      <h2 id="social-login-title" className="text-base font-semibold">Enter with an account</h2>
      {socialSession ? <div className="mt-3 flex items-center gap-3 border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-900"><ShieldCheck size={20} /><div><strong>{socialSession.provider} connected</strong><div>{socialSession.email ?? socialSession.userName ?? socialSession.displayName ?? "Verified account"}</div></div></div>
        : <div className="mt-3 flex flex-wrap gap-2">{data.socialAuthProviders.map(provider => <button key={provider.provider} type="button" className="secondary-button" onClick={() => {
          const returnUrl = window.location.href.split("?")[0];
          window.location.assign(`/api/backend/${prefix}/competitions/${encodeURIComponent(competitionSlug)}/social-auth/${encodeURIComponent(provider.provider)}/start?returnUrl=${encodeURIComponent(returnUrl)}`);
        }}><LogIn size={16} />Continue with {provider.displayName}</button>)}</div>}
      {socialError && <p className="mt-3 text-sm text-red-700">{socialError}</p>}
    </section>}

    {actions.length > 0 && <SocialActions actions={actions} connectedProvider={socialSession?.provider} pending={verifyAction.isPending} error={verifyAction.error} onVerify={id => verifyAction.mutate(id)} />}

    {(data.allowEmailEntry || socialSession) ? <form className="mt-6 grid gap-5" onSubmit={event => { event.preventDefault(); submit.mutate(); }}>
      {data.fields.filter(field => field.fieldType !== "Hidden" && field.fieldType !== "Consent").map(field => <label className="field" key={field.id}><span>{field.label}{field.isRequired ? " *" : ""}</span>{field.fieldType === "Textarea" ? <textarea required={field.isRequired} value={values[field.fieldKey] ?? ""} onChange={event => setValues({ ...values, [field.fieldKey]: event.target.value })} /> : <input required={field.isRequired} type={field.fieldType === "Email" ? "email" : field.fieldType === "Phone" ? "tel" : field.fieldType === "Date" || field.fieldType === "DateOfBirth" ? "date" : "text"} value={values[field.fieldKey] ?? ""} onChange={event => setValues({ ...values, [field.fieldKey]: event.target.value })} />}{field.helpText && <small className="mt-1 block font-normal text-neutral-600">{field.helpText}</small>}</label>)}
      <fieldset className="grid gap-3"><legend className="mb-2 font-semibold">Consent</legend>{data.consents.map(consent => <label className="flex items-start gap-3 text-sm" key={consent.id}><input className="mt-1" type="checkbox" required={consent.isRequired} checked={!!consents[consent.id]} onChange={event => setConsents({ ...consents, [consent.id]: event.target.checked })} /><span>{consent.text}{consent.isRequired ? " *" : ""}</span></label>)}</fieldset>
      <Captcha onToken={onCaptcha} />
      {submit.error && <p className="border border-red-200 bg-red-50 p-3 text-sm text-red-700">{submit.error.message}</p>}
      <button className="command-button w-fit" disabled={!captchaToken || submit.isPending || missingRequiredAction}>{submit.isPending ? "Submitting" : "Submit entry"}</button>
    </form> : <p className="mt-6 border border-neutral-200 bg-neutral-50 p-4 text-sm">Connect one of the enabled accounts above to enter.</p>}
  </main>;
}

function SocialActions({ actions, connectedProvider, pending, error, onVerify }: {
  actions: SocialActionRequirement[]; connectedProvider?: string; pending: boolean; error: Error | null; onVerify: (id: string) => void;
}) {
  return <section className="border-b border-line py-6" aria-labelledby="social-actions-title">
    <h2 id="social-actions-title" className="text-base font-semibold">Social actions</h2>
    <div className="mt-3 grid gap-3">{actions.map(action => <div className="flex items-start justify-between gap-4 border border-line p-3" key={action.id}><div><div className="text-sm font-semibold">{action.description ?? `${action.actionType} on ${action.provider}`}{action.isRequired ? " *" : ""}</div><div className="mt-1 text-xs text-neutral-600">{action.provider} · {action.actionType}</div></div>{action.status === "Verified" ? <span className="flex items-center gap-1 text-sm font-semibold text-emerald-800"><Check size={16} />Verified</span> : action.supportsAutomatedVerification && connectedProvider === action.provider ? <button type="button" className="secondary-button" disabled={pending} onClick={() => onVerify(action.id)}>Verify</button> : <a className="icon-button" href={safeExternalUrl(action.targetReference)} target="_blank" rel="noreferrer" title="Open social destination"><ExternalLink size={17} /></a>}</div>)}</div>
    {error && <p className="mt-3 text-sm text-red-700">{error.message}</p>}
  </section>;
}

function safeExternalUrl(value: string) {
  try { const uri = new URL(value); return uri.protocol === "https:" ? uri.toString() : "#"; }
  catch { return "#"; }
}
