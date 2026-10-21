"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, Eye, EyeOff, Monitor, Plus, Radio, Save, Smartphone, Trash2 } from "lucide-react";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ErrorState, PageHeading } from "@/components/operations-ui";
import { MediaUpload, privateMediaUrl } from "@/components/media-upload";
import { CampaignBlocks } from "@/components/campaign-blocks";
import { apiFetch } from "@/lib/api";
import { readCampaignBlocks, youtubeVideoId, type CampaignBlock } from "@/lib/campaign-content";
import type { Brand, Competition } from "@/lib/contracts";

type PageContent = { title: string; seoDescription?: string; layoutJson: string };
export default function BuilderPage() {
  const id = useParams<{ id: string }>().id;
  const cache = useQueryClient();
  const [language, setLanguage] = useState("en");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [brandId, setBrandId] = useState("");
  const [blocks, setBlocks] = useState<CampaignBlock[]>([]);
  const [kind, setKind] = useState("text");
  const [mobile, setMobile] = useState(false);
  const [notice, setNotice] = useState("");
  const competition = useQuery({ queryKey: ["competition", id], queryFn: () => apiFetch<Competition>(`api/competitions/${id}`) });
  const brands = useQuery({ queryKey: ["brands"], queryFn: () => apiFetch<Brand[]>("api/brands") });
  const page = useQuery({ queryKey: ["page", id, language], queryFn: async () => {
    const response = await fetch(`/api/backend/api/competitions/${id}/page?language=${language}`);
    if (response.status === 404) return null;
    if (!response.ok) throw new Error("Unable to load competition content.");
    return response.json() as Promise<PageContent>;
  } });
  useEffect(() => { if (competition.data) setBrandId(competition.data.brandProfileId ?? ""); }, [competition.data]);
  useEffect(() => {
    if (page.isSuccess) { setTitle(page.data?.title ?? "Competition page"); setDescription(page.data?.seoDescription ?? "");
      setBlocks(page.data ? readCampaignBlocks(page.data.layoutJson) : [{ id: crypto.randomUUID(), type: "hero", settings: { headline: competition.data?.name ?? "" } }, { id: crypto.randomUUID(), type: "entryForm", settings: {} }]); }
  }, [page.data, page.isSuccess, competition.data?.name]);
  const save = useMutation({ mutationFn: async (publish: boolean) => {
    for (const block of blocks) {
      if (block.type === "video" && block.settings.youtubeUrl && !youtubeVideoId(block.settings.youtubeUrl)) throw new Error("Enter a valid HTTPS YouTube video URL.");
    }
    await apiFetch(`api/competitions/${id}/page`, { method: "PUT", body: JSON.stringify({ languageCode: language, title, seoTitle: title, seoDescription: description, layoutJson: JSON.stringify({ schemaVersion: 1, blocks }) }) });
    await apiFetch(`api/competitions/${id}/brand`, { method: "PUT", body: JSON.stringify({ brandProfileId: brandId || null }) });
    if (publish) await apiFetch(`api/competitions/${id}/publish`, { method: "POST" });
  }, onSuccess: (_, publish) => { setNotice(publish ? "Competition content published." : "Draft saved."); void cache.invalidateQueries({ queryKey: ["competition", id] }); } });
  function settings(blockId: string, patch: Partial<CampaignBlock["settings"]>) { setBlocks(items => items.map(block => block.id === blockId ? { ...block, settings: { ...block.settings, ...patch } } : block)); setNotice(""); }
  function move(index: number, offset: number) { setBlocks(items => { const next = [...items]; [next[index], next[index + offset]] = [next[index + offset], next[index]]; return next; }); }
  const layout = JSON.stringify({ schemaVersion: 1, blocks });
  return <main className="page">
    <PageHeading title="Competition content" description="" action={<div className="flex flex-wrap gap-2"><button className="secondary-button" disabled={save.isPending || page.isLoading} onClick={() => save.mutate(false)}><Save size={16} />Save draft</button><button className="command-button" disabled={save.isPending || page.isLoading} onClick={() => save.mutate(true)}><Radio size={16} />Publish</button></div>} />
    {notice && <p className="mb-4 text-sm text-emerald-800" role="status">{notice}</p>}{save.error && <ErrorState error={save.error} />}{page.error && <ErrorState error={page.error} />}
    <div className="grid gap-8 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
      <div className="min-w-0"><div className="grid gap-4 border-b border-line pb-6 sm:grid-cols-2">
        <label className="field"><span>Client brand</span><select value={brandId} onChange={e => setBrandId(e.target.value)}><option value="">No brand</option>{brands.data?.map(brand => <option key={brand.id} value={brand.id}>{brand.name}</option>)}</select></label>
        <label className="field"><span>Language</span><select value={language} onChange={e => setLanguage(e.target.value)}><option value="en">English</option><option value="el">Greek</option></select></label>
        <label className="field sm:col-span-2"><span>Page title</span><input required maxLength={220} value={title} onChange={e => setTitle(e.target.value)} /></label>
        <label className="field sm:col-span-2"><span>Summary</span><textarea maxLength={500} value={description} onChange={e => setDescription(e.target.value)} /></label>
      </div>
      <div className="mt-5 flex items-center gap-2"><select className="min-w-0 rounded border border-line bg-white p-2 text-sm" aria-label="Block type" value={kind} onChange={e => setKind(e.target.value)}>{[["text", "Text"], ["hero", "Headline and image"], ["image", "Image"], ["video", "Video"], ["entryForm", "Entry button"]].map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select><button className="secondary-button" disabled={blocks.length >= 40} onClick={() => setBlocks(items => [...items, { id: crypto.randomUUID(), type: kind, settings: {} }])}><Plus size={16} />Add block</button></div>
      <div className="mt-4 divide-y divide-line">{blocks.map((block, index) => <section key={block.id} className="py-5">
        <div className="mb-4 flex items-center justify-between gap-2"><h2 className="text-sm font-semibold">{index + 1}. {block.type.toLowerCase() === "entryform" ? "Entry button" : block.type}</h2><div className="flex shrink-0">
          <button className="icon-button" title="Move up" aria-label="Move block up" disabled={!index} onClick={() => move(index, -1)}><ArrowUp size={16} /></button><button className="icon-button" title="Move down" aria-label="Move block down" disabled={index === blocks.length - 1} onClick={() => move(index, 1)}><ArrowDown size={16} /></button>
          <button className="icon-button" title={block.hidden ? "Show block" : "Hide block"} aria-label={block.hidden ? "Show block" : "Hide block"} onClick={() => setBlocks(items => items.map(item => item.id === block.id ? { ...item, hidden: !item.hidden } : item))}>{block.hidden ? <EyeOff size={16} /> : <Eye size={16} />}</button>
          <button className="icon-button text-red-700" title="Remove block" aria-label="Remove block" onClick={() => setBlocks(items => items.filter(item => item.id !== block.id))}><Trash2 size={16} /></button>
        </div></div>
        {block.type.toLowerCase() !== "entryform" && <div className="grid gap-3">
          <label className="field"><span>Heading</span><input value={block.settings.headline ?? ""} maxLength={300} onChange={e => settings(block.id, { headline: e.target.value })} /></label>
          {["text", "hero", "description", "prize", "footer"].includes(block.type.toLowerCase()) && <label className="field"><span>Text</span><textarea className="!min-h-32" value={block.settings.text ?? block.settings.subheading ?? ""} onChange={e => settings(block.id, { text: e.target.value, subheading: "" })} /></label>}
          {["image", "hero", "video"].includes(block.type.toLowerCase()) && <><MediaUpload competitionId={id} video={block.type === "video"} onUploaded={assetId => settings(block.id, { assetId, backgroundAssetId: "", youtubeUrl: "" })} />
            {(block.settings.assetId || block.settings.backgroundAssetId) && <button className="w-fit text-sm text-red-700" onClick={() => settings(block.id, { assetId: "", backgroundAssetId: "" })}>Remove uploaded media</button>}
            {block.type !== "video" && <label className="field"><span>Image description</span><input value={block.settings.alt ?? ""} onChange={e => settings(block.id, { alt: e.target.value })} /></label>}
          </>}
          {block.type === "video" && <label className="field"><span>YouTube URL</span><input type="url" placeholder="https://www.youtube.com/watch?v=..." value={block.settings.youtubeUrl ?? ""} onChange={e => settings(block.id, { youtubeUrl: e.target.value, assetId: "" })} /></label>}
        </div>}
      </section>)}</div></div>
      <section className="min-w-0 self-start xl:sticky xl:top-24"><div className="mb-4 flex items-center justify-between"><h2 className="text-sm font-semibold">Preview</h2><div className="flex"><button className="icon-button" title="Desktop preview" aria-label="Desktop preview" onClick={() => setMobile(false)}><Monitor size={17} /></button><button className="icon-button" title="Mobile preview" aria-label="Mobile preview" onClick={() => setMobile(true)}><Smartphone size={17} /></button></div></div>
        <div className={`mx-auto w-full overflow-hidden border border-line bg-white p-5 ${mobile ? "max-w-[390px]" : ""}`}><CampaignBlocks layout={layout} mediaUrl={privateMediaUrl} primaryColor={brands.data?.find(brand => brand.id === brandId)?.primaryColor} /></div>
      </section>
    </div>
  </main>;
}
