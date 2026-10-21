import Image from "next/image";
import Link from "next/link";
import { readCampaignBlocks, youtubeVideoId } from "@/lib/campaign-content";

export function CampaignBlocks({ layout, mediaUrl, entryHref, primaryColor = "#006c5b" }: {
  layout?: string; mediaUrl: (id: string) => string; entryHref?: string; primaryColor?: string;
}) {
  return <div className="grid gap-8">{readCampaignBlocks(layout).filter(block => !block.hidden).map(block => {
    const settings = block.settings ?? {};
    const kind = block.type.toLowerCase();
    const asset = settings.assetId || settings.backgroundAssetId;
    const youtube = settings.youtubeUrl ? youtubeVideoId(settings.youtubeUrl) : null;
    if (kind === "entryform") return entryHref ? <Link key={block.id} className="command-button w-fit" style={{ background: primaryColor }} href={entryHref}>Enter competition</Link> : <button type="button" key={block.id} className="command-button w-fit" style={{ background: primaryColor }}>Enter competition</button>;
    return <section key={block.id} className="min-w-0">
      {settings.headline && <h2 className={kind === "hero" ? "text-3xl font-semibold sm:text-4xl" : "text-2xl font-semibold"}>{settings.headline}</h2>}
      {settings.subheading && <p className="mt-3 whitespace-pre-wrap leading-7">{settings.subheading}</p>}
      {settings.text && <p className="mt-3 whitespace-pre-wrap leading-7">{settings.text}</p>}
      {asset && kind !== "video" && <figure className="mt-4"><Image unoptimized src={mediaUrl(asset)} alt={settings.alt || settings.headline || "Competition image"} width={1200} height={800} className="max-h-[640px] w-full object-contain" />{settings.caption && <figcaption className="mt-2 text-sm">{settings.caption}</figcaption>}</figure>}
      {kind === "video" && (youtube ? <iframe className="mt-4 aspect-video w-full border-0" src={`https://www.youtube-nocookie.com/embed/${youtube}`} title={settings.headline || "Competition video"} loading="lazy" referrerPolicy="strict-origin-when-cross-origin" allow="encrypted-media; picture-in-picture; fullscreen" allowFullScreen /> :
        asset ? <video className="mt-4 aspect-video w-full bg-black" src={mediaUrl(asset)} controls playsInline preload="metadata" /> : null)}
    </section>;
  })}</div>;
}
