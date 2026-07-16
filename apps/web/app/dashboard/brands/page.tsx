"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Palette, Plus, Save, Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { ErrorState, LoadingState, PageHeading } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Brand } from "@/lib/contracts";

const blank = { name: "Campaign brand", primaryColor: "#006c5b", secondaryColor: "#171918", accentColor: "#d14b30", backgroundColor: "#ffffff", textColor: "#171918", headingFont: "Inter", bodyFont: "Inter", buttonStyle: "solid", borderRadius: 6, footerText: "", supportEmail: "", supportPhone: "", showPoweredBy: true, customCss: "" };

export default function BrandsPage() {
  const cache = useQueryClient();
  const query = useQuery({ queryKey: ["brands"], queryFn: () => apiFetch<Brand[]>("api/brands") });
  const [selectedId, setSelectedId] = useState<string>();
  const [form, setForm] = useState(blank);
  useEffect(() => { const selected = query.data?.find(x => x.id === selectedId) ?? query.data?.[0]; if (selected) { setSelectedId(selected.id); setForm({ ...blank, ...selected }); } }, [query.data, selectedId]);
  const refresh = () => cache.invalidateQueries({ queryKey: ["brands"] });
  const save = useMutation({ mutationFn: () => apiFetch<Brand>(selectedId ? `api/brands/${selectedId}` : "api/brands", { method: selectedId ? "PATCH" : "POST", body: JSON.stringify(form) }), onSuccess: item => { setSelectedId(item.id); refresh(); } });
  const remove = useMutation({ mutationFn: (id: string) => apiFetch<void>(`api/brands/${id}`, { method: "DELETE" }), onSuccess: () => { setSelectedId(undefined); setForm(blank); refresh(); } });
  const set = (key: keyof typeof blank, value: string | number | boolean) => setForm(current => ({ ...current, [key]: value }));
  return <main className="page"><PageHeading title="Brand profiles" description="Define tenant themes used by public campaigns and notification content." action={<button className="secondary-button" onClick={() => { setSelectedId(undefined); setForm(blank); }}><Plus size={17} />New profile</button>} />
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="grid gap-5 lg:grid-cols-[250px_1fr]">
      <aside className="panel self-start p-2">{query.data?.map(brand => <button className={`flex w-full items-center gap-3 px-3 py-3 text-left text-sm ${selectedId === brand.id ? "bg-neutral-100 font-semibold" : "hover:bg-neutral-50"}`} key={brand.id} onClick={() => setSelectedId(brand.id)}><span className="h-5 w-5 border border-line" style={{ background: brand.primaryColor }} />{brand.name}</button>)}{!query.data?.length && <div className="p-4 text-sm text-muted">No profiles yet.</div>}</aside>
      <div className="grid gap-5 xl:grid-cols-[1fr_0.9fr]"><form className="panel grid gap-4 p-5 sm:grid-cols-2" onSubmit={event => { event.preventDefault(); save.mutate(); }}>
        <label className="field sm:col-span-2"><span>Name</span><input required value={form.name} onChange={e => set("name", e.target.value)} /></label>
        {(["primaryColor", "secondaryColor", "accentColor", "backgroundColor", "textColor"] as const).map(key => <label className="field" key={key}><span>{key.replace("Color", " colour")}</span><div className="flex gap-2"><input className="!w-12 !px-1" type="color" value={form[key]} onChange={e => set(key, e.target.value)} /><input value={form[key]} onChange={e => set(key, e.target.value)} /></div></label>)}
        <label className="field"><span>Heading font</span><input value={form.headingFont} onChange={e => set("headingFont", e.target.value)} /></label><label className="field"><span>Body font</span><input value={form.bodyFont} onChange={e => set("bodyFont", e.target.value)} /></label>
        <label className="field"><span>Support email</span><input type="email" value={form.supportEmail} onChange={e => set("supportEmail", e.target.value)} /></label><label className="field"><span>Border radius</span><input max={24} min={0} type="number" value={form.borderRadius} onChange={e => set("borderRadius", Number(e.target.value))} /></label>
        <label className="flex items-center gap-3 text-sm font-medium sm:col-span-2"><input type="checkbox" checked={form.showPoweredBy} onChange={e => set("showPoweredBy", e.target.checked)} />Show powered by ONE. Competitions</label>
        {save.error && <p className="text-sm text-red-700 sm:col-span-2">{save.error.message}</p>}
        <div className="flex gap-2 sm:col-span-2"><button className="command-button" disabled={save.isPending}><Save size={17} />Save profile</button>{selectedId && <button className="danger-button" type="button" onClick={() => remove.mutate(selectedId)}><Trash2 size={16} />Delete</button>}</div>
      </form>
      <section className="panel self-start overflow-hidden"><div className="panel-header"><h2 className="font-semibold">Theme preview</h2><Palette size={18} /></div><div className="p-5" style={{ background: form.backgroundColor, color: form.textColor, fontFamily: form.bodyFont }}><div className="text-sm font-semibold" style={{ color: form.primaryColor }}>{form.name}</div><h3 className="mt-5 text-2xl font-semibold" style={{ fontFamily: form.headingFont }}>Win the featured prize</h3><p className="mt-3 text-sm leading-6">This preview resolves the current colours, type and control styling without executing custom CSS.</p><button className="mt-5 px-4 py-2 text-sm font-semibold text-white" style={{ background: form.primaryColor, borderRadius: form.borderRadius }}>Enter competition</button><div className="mt-6 border-t pt-3 text-xs" style={{ borderColor: form.secondaryColor }}>{form.showPoweredBy ? "Powered by ONE. Competitions" : form.footerText}</div></div></section></div>
    </div>}
  </main>;
}
