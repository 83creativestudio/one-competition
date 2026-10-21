"use client";

import { Upload } from "lucide-react";
import { useRef, useState } from "react";
import { apiFetch } from "@/lib/api";

export const privateMediaUrl = (id: string) => `/api/backend/api/assets/${id}/download`;
export const publicMediaUrl = (tenant: string, id: string) => `/media/${encodeURIComponent(tenant)}/${encodeURIComponent(id)}`;

export function MediaUpload({ competitionId, video = false, onUploaded }: {
  competitionId?: string; video?: boolean; onUploaded: (assetId: string) => void;
}) {
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function upload(file?: File) {
    if (!file) return;
    setError("");
    if (file.size > 25 * 1024 * 1024) { setError("Choose a file smaller than 25 MB."); return; }
    setBusy(true);
    try {
      const asset = await apiFetch<{ assetId: string; uploadUrl: string }>("api/assets/uploads", {
        method: "POST", body: JSON.stringify({ competitionId: competitionId ?? null, entryId: null, assetType: competitionId ? "CompetitionMedia" : "BrandLogo", filename: file.name, mimeType: file.type, sizeBytes: file.size })
      });
      const target = asset.uploadUrl.startsWith("/api/") ? `/api/backend${asset.uploadUrl}` : asset.uploadUrl;
      const response = await fetch(target, { method: "PUT", headers: { "Content-Type": file.type }, body: file });
      if (!response.ok) throw new Error("Upload failed. Please try again.");
      await apiFetch(`api/assets/${asset.assetId}/complete`, { method: "POST" });
      onUploaded(asset.assetId);
    } catch (failure) { setError(failure instanceof Error ? failure.message : "Upload failed."); }
    finally { setBusy(false); if (input.current) input.current.value = ""; }
  }
  return <div className="grid gap-2"><input ref={input} className="sr-only" type="file" accept={video ? "video/mp4" : "image/png,image/jpeg,image/webp"} onChange={e => void upload(e.target.files?.[0])} />
    <button className="secondary-button w-fit" type="button" disabled={busy} onClick={() => input.current?.click()}><Upload size={16} />{busy ? "Uploading..." : video ? "Upload MP4" : "Upload image"}</button>
    {error && <p className="text-sm text-red-700" role="alert">{error}</p>}</div>;
}
