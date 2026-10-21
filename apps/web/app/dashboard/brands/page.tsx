"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ExternalLink, Palette, Plus, Save, Trash2 } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { ErrorState, LoadingState, PageHeading } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Brand, Competition, SessionUser, Tenant, TenantSummary } from "@/lib/contracts";
import { MediaUpload, privateMediaUrl } from "@/components/media-upload";

const blank = { name: "", logoAssetId: "", primaryColor: "#006c5b", secondaryColor: "#171918", accentColor: "#d14b30", backgroundColor: "#ffffff", textColor: "#171918", headingFont: "Inter", bodyFont: "Inter", buttonStyle: "solid", borderRadius: 6, footerText: "", supportEmail: "", supportPhone: "", showPoweredBy: true, customCss: "" };

export default function BrandsPage() {
  const cache = useQueryClient();
  const profile = useQuery({ queryKey: ["session"], queryFn: async () => { const response = await fetch("/api/session/me"); return response.json() as Promise<SessionUser>; } });
  const isAdmin = profile.data?.roles?.some(role => role === "PlatformOwner" || role === "PlatformAdministrator");
  const clients = useQuery({ queryKey: ["platform-tenants"], queryFn: () => apiFetch<TenantSummary[]>("api/platform/tenants"), enabled: !!isAdmin });
  const tenant = useQuery({ queryKey: ["current-tenant"], queryFn: () => apiFetch<Tenant>("api/tenants/current") });
  const competitions = useQuery({ queryKey: ["competitions"], queryFn: () => apiFetch<Competition[]>("api/competitions") });
  const workspace = useMutation({ mutationFn: async (slug: string) => {
    const response = await fetch("/api/session/workspace", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ slug }) });
    if (!response.ok) throw new Error("Unable to switch client workspace.");
  }, onSuccess: () => window.location.assign("/dashboard/brands") });
  const query = useQuery({ queryKey: ["brands"], queryFn: () => apiFetch<Brand[]>("api/brands") });
  const [selectedId, setSelectedId] = useState<string | null>();
  const [form, setForm] = useState(blank);
  useEffect(() => { if (selectedId === null) return; const selected = selectedId === undefined ? query.data?.[0] : query.data?.find(x => x.id === selectedId); if (selected) { setSelectedId(selected.id); setForm({ ...blank, ...selected, logoAssetId: selected.logoAssetId ?? "", footerText: selected.footerText ?? "", supportEmail: selected.supportEmail ?? "", supportPhone: selected.supportPhone ?? "", customCss: selected.customCss ?? "" }); } }, [query.data, selectedId]);
  const refresh = () => cache.invalidateQueries({ queryKey: ["brands"] });
  const save = useMutation({ mutationFn: () => apiFetch<Brand>(selectedId ? `api/brands/${selectedId}` : "api/brands", { method: selectedId ? "PATCH" : "POST", body: JSON.stringify({ ...form, logoAssetId: form.logoAssetId || null }) }), onSuccess: item => { setSelectedId(item.id); refresh(); } });
  const remove = useMutation({ mutationFn: (id: string) => apiFetch<void>(`api/brands/${id}`, { method: "DELETE" }), onSuccess: () => { setSelectedId(undefined); setForm(blank); refresh(); } });
  const set = (key: keyof typeof blank, value: string | number | boolean) => setForm(current => ({ ...current, [key]: value }));
  return <main className="page"><PageHeading title="Client brands" description="" action={<button className="secondary-button" onClick={() => { setSelectedId(null); setForm({ ...blank }); }}><Plus size={17} />Create brand</button>} />
    {isAdmin && <label className="field mb-5 max-w-sm"><span>Client workspace</span><select disabled={workspace.isPending} value={tenant.data?.slug ?? ""} onChange={e => workspace.mutate(e.target.value)}><option value="" disabled>Select client</option>{clients.data?.map(client => <option key={client.id} value={client.slug}>{client.name}</option>)}</select></label>}
    {workspace.error && <ErrorState error={workspace.error} />}
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="grid gap-5 lg:grid-cols-[250px_1fr]">
      <aside className="panel self-start p-2">{query.data?.map(brand => <button className={`flex w-full items-center gap-3 px-3 py-3 text-left text-sm ${selectedId === brand.id ? "bg-neutral-100 font-semibold" : "hover:bg-neutral-50"}`} key={brand.id} onClick={() => setSelectedId(brand.id)}><span className="h-5 w-5 border border-line" style={{ background: brand.primaryColor }} />{brand.name}</button>)}{!query.data?.length && <div className="p-4 text-sm text-muted">No profiles yet.</div>}</aside>
      <div className="grid gap-5 xl:grid-cols-[1fr_0.9fr]"><form className="panel grid gap-4 p-5 sm:grid-cols-2" onSubmit={event => { event.preventDefault(); save.mutate(); }}>
        <label className="field sm:col-span-2"><span>Name</span><input required value={form.name} onChange={e => set("name", e.target.value)} /></label>
        <div className="grid gap-3 sm:col-span-2"><span className="text-sm font-semibold">Brand logo</span>{form.logoAssetId && <div className="flex items-center gap-4"><Image unoptimized src={privateMediaUrl(form.logoAssetId)} alt={form.name || "Brand logo"} width={160} height={80} className="h-20 w-40 object-contain" /><button type="button" className="icon-button text-red-700" title="Remove logo" aria-label="Remove logo" onClick={() => set("logoAssetId", "")}><Trash2 size={17} /></button></div>}<MediaUpload onUploaded={id => set("logoAssetId", id)} /></div>
        {(["primaryColor", "secondaryColor", "accentColor", "backgroundColor", "textColor"] as const).map(key => <label className="field" key={key}><span>{key.replace("Color", " colour")}</span><div className="flex gap-2"><input className="!w-12 !px-1" type="color" value={form[key]} onChange={e => set(key, e.target.value)} /><input value={form[key]} onChange={e => set(key, e.target.value)} /></div></label>)}
        <label className="field"><span>Heading font</span><input value={form.headingFont} onChange={e => set("headingFont", e.target.value)} /></label><label className="field"><span>Body font</span><input value={form.bodyFont} onChange={e => set("bodyFont", e.target.value)} /></label>
        <label className="field"><span>Support email</span><input type="email" value={form.supportEmail} onChange={e => set("supportEmail", e.target.value)} /></label><label className="field"><span>Border radius</span><input max={24} min={0} type="number" value={form.borderRadius} onChange={e => set("borderRadius", Number(e.target.value))} /></label>
        <label className="flex items-center gap-3 text-sm font-medium sm:col-span-2"><input type="checkbox" checked={form.showPoweredBy} onChange={e => set("showPoweredBy", e.target.checked)} />Show powered by ONE. Competitions</label>
        {save.error && <p className="text-sm text-red-700 sm:col-span-2">{save.error.message}</p>}
        {remove.error && <p className="text-sm text-red-700 sm:col-span-2">{remove.error.message}</p>}
        {save.isSuccess && <p className="text-sm text-emerald-800 sm:col-span-2" role="status">Brand saved.</p>}
        <div className="flex gap-2 sm:col-span-2"><button className="command-button" disabled={save.isPending}><Save size={17} />Save profile</button>{selectedId && <button className="danger-button" type="button" onClick={() => remove.mutate(selectedId)}><Trash2 size={16} />Delete</button>}</div>
      </form>
      <section className="self-start overflow-hidden"><div className="panel-header"><h2 className="font-semibold">Brand preview</h2><Palette size={18} /></div><div className="p-5" style={{ background: form.backgroundColor, color: form.textColor, fontFamily: form.bodyFont }}>{form.logoAssetId && <Image unoptimized src={privateMediaUrl(form.logoAssetId)} alt={form.name || "Brand logo"} width={180} height={90} className="mb-5 h-24 w-44 object-contain" />}<h3 className="text-2xl font-semibold" style={{ color: form.primaryColor }}>{form.name || "New brand"}</h3><div className="mt-6 border-t pt-3 text-xs" style={{ borderColor: form.secondaryColor }}>{form.showPoweredBy ? "Powered by ONE. Competitions" : form.footerText}</div></div>
        {selectedId && tenant.data && <Link className="secondary-button mt-4" href={`/c/${tenant.data.slug}/brands/${selectedId}`} target="_blank"><ExternalLink size={16} />Open public brand page</Link>}
        {selectedId && <div className="mt-6"><h3 className="font-semibold">Brand competitions</h3><div className="mt-3 divide-y divide-line">{competitions.data?.filter(item => item.brandProfileId === selectedId).map(item => <Link className="flex items-center justify-between gap-3 py-3 text-sm" key={item.id} href={`/dashboard/competitions/${item.id}/builder`}><span>{item.name}</span><span className="text-muted">{item.status}</span></Link>)}</div>{!competitions.data?.some(item => item.brandProfileId === selectedId) && <p className="mt-3 text-sm text-muted">No competitions assigned.</p>}</div>}
      </section></div>
    </div>}
  </main>;
}
