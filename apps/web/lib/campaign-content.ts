export type CampaignBlock = { id: string; type: string; hidden?: boolean; settings: {
  headline?: string; subheading?: string; text?: string; assetId?: string; backgroundAssetId?: string;
  youtubeUrl?: string; alt?: string; caption?: string;
} };

export function youtubeVideoId(value: string): string | null {
  try {
    const url = new URL(value);
    if (url.protocol !== "https:" || !["youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be"].includes(url.hostname) || url.username || url.password) return null;
    const id = url.hostname === "youtu.be" ? url.pathname.slice(1) :
      /^\/(embed|shorts)\//.test(url.pathname) ? url.pathname.split("/")[2] : url.searchParams.get("v");
    return id && /^[a-zA-Z0-9_-]{11}$/.test(id) ? id : null;
  } catch { return null; }
}

export function readCampaignBlocks(layout?: string): CampaignBlock[] {
  try { const data = JSON.parse(layout ?? "{}"); return Array.isArray(data.blocks) ? data.blocks : []; }
  catch { return []; }
}
