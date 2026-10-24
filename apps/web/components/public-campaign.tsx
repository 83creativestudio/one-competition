import { headers } from "next/headers";
import Link from "next/link";
import { PublicBrandHeader } from "@/components/public-brand-header";
import { CampaignBlocks } from "@/components/campaign-blocks";
import type { PublicCompetition } from "@/lib/contracts";
import { publicRequestHeaders } from "@/lib/public-request-headers";

export async function PublicCampaign({ competitionSlug, tenantSlug }: { competitionSlug: string; tenantSlug?: string }) {
  const prefix = tenantSlug ? `c/${encodeURIComponent(tenantSlug)}/api/public` : "api/public";
  const requestHeaders = await headers();
  const response = await fetch(`${process.env.API_BASE_URL ?? "http://127.0.0.1:5050"}/${prefix}/competitions/${encodeURIComponent(competitionSlug)}`, {
    cache: "no-store",
    headers: publicRequestHeaders(requestHeaders)
  });
  if (!response.ok) return <main className="mx-auto max-w-3xl px-6 py-12"><h1 className="text-3xl font-semibold">Competition unavailable</h1></main>;
  const data = await response.json() as PublicCompetition;
  const href = tenantSlug ? `/c/${tenantSlug}/${competitionSlug}/enter` : `/${competitionSlug}/enter`;
  const media = (id: string) => `/media/${encodeURIComponent(data.tenantSlug)}/${id}`;
  const brandHref = data.brand ? `${tenantSlug ? `/c/${tenantSlug}` : ""}/brands/${data.brand.id}` : null;
  return <main className="min-h-screen" style={{ background: data.brand?.backgroundColor ?? "#ffffff", color: data.brand?.textColor ?? "#171918" }}>
    {data.brand && brandHref && <PublicBrandHeader brand={data.brand} tenantSlug={data.tenantSlug} href={brandHref} />}
    <section className="mx-auto max-w-4xl px-5 py-8 sm:px-8 sm:py-12">
      <header className="mb-8 border-b border-black/10 pb-6">{!data.brand && <span className="text-sm font-semibold">ONE. Competitions</span>}
      <h1 className="mt-6 break-words text-3xl font-semibold sm:text-4xl">{data.name}</h1>
      {data.page?.seoDescription && <p className="mt-4 whitespace-pre-wrap leading-7">{data.page.seoDescription}</p>}
      <div className="mt-5 flex flex-wrap gap-4 text-sm opacity-75"><span>{data.status}</span><span>Closes {new Date(data.endsAt).toLocaleDateString("en-GB", { timeZone: "UTC" })}</span>{data.minimumAge != null && <span>Minimum age {data.minimumAge}</span>}</div></header>
      <CampaignBlocks layout={data.page?.layoutJson} mediaUrl={media} entryHref={href} primaryColor={data.brand?.primaryColor} />
      {!data.page?.layoutJson && <Link className="command-button mt-7" href={href}>Enter competition</Link>}
      {brandHref && <footer className="mt-12 border-t border-black/10 pt-6"><Link className="text-sm underline" href={brandHref}>All {data.brand?.name} competitions</Link></footer>}
    </section></main>;
}
