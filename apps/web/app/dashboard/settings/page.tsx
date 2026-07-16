"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Save } from "lucide-react";
import { useEffect, useState } from "react";
import { ErrorState, LoadingState, PageHeading } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Tenant } from "@/lib/contracts";

export default function SettingsPage() {
  const cache = useQueryClient(); const query = useQuery({ queryKey: ["tenant"], queryFn: () => apiFetch<Tenant>("api/tenants/current") });
  const [form, setForm] = useState({ name: "", legalName: "", defaultLanguage: "en", timeZone: "UTC", countryCode: "", currency: "EUR" });
  useEffect(() => { if (query.data) setForm({ name: query.data.name, legalName: query.data.legalName, defaultLanguage: query.data.defaultLanguage, timeZone: query.data.timeZone, countryCode: query.data.countryCode, currency: query.data.currency }); }, [query.data]);
  const save = useMutation({ mutationFn: () => apiFetch<Tenant>("api/tenants/current", { method: "PATCH", body: JSON.stringify(form) }), onSuccess: () => cache.invalidateQueries({ queryKey: ["tenant"] }) });
  if (query.isLoading) return <main className="page"><LoadingState /></main>; if (query.error) return <main className="page"><ErrorState error={query.error} /></main>;
  const set = (key: keyof typeof form, value: string) => setForm(current => ({ ...current, [key]: value }));
  return <main className="page"><PageHeading title="Organisation settings" description={`Tenant slug: ${query.data?.slug}`} /><form className="panel grid max-w-3xl gap-4 p-5 sm:grid-cols-2" onSubmit={e => { e.preventDefault(); save.mutate(); }}><label className="field"><span>Display name</span><input required value={form.name} onChange={e => set("name", e.target.value)} /></label><label className="field"><span>Legal name</span><input required value={form.legalName} onChange={e => set("legalName", e.target.value)} /></label><label className="field"><span>Default language</span><select value={form.defaultLanguage} onChange={e => set("defaultLanguage", e.target.value)}><option value="en">English</option><option value="el">Greek</option></select></label><label className="field"><span>Time zone</span><input required value={form.timeZone} onChange={e => set("timeZone", e.target.value)} /></label><label className="field"><span>Country code</span><input maxLength={2} required value={form.countryCode} onChange={e => set("countryCode", e.target.value)} /></label><label className="field"><span>Currency</span><input maxLength={3} required value={form.currency} onChange={e => set("currency", e.target.value)} /></label>{save.error && <p className="text-sm text-red-700 sm:col-span-2">{save.error.message}</p>}<button className="command-button w-fit sm:col-span-2" disabled={save.isPending}><Save size={16} />Save settings</button></form></main>;
}
