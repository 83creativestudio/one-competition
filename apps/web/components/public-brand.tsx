import { headers } from "next/headers";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, ArrowRight, CalendarDays, Gift, ShieldCheck } from "lucide-react";
import { cache, type CSSProperties } from "react";
import type { PublicBrand } from "@/lib/contracts";

export const loadPublicBrand = cache(async (brandId: string, tenantSlug?: string, page = 1) => {
  if (!/^[a-f0-9-]{36}$/i.test(brandId)) notFound();
  const requestHeaders = await headers();
  const prefix = tenantSlug ? `c/${encodeURIComponent(tenantSlug)}/api/public` : "api/public";
  const response = await fetch(`${process.env.API_BASE_URL ?? "http://127.0.0.1:5050"}/${prefix}/brands/${brandId}?page=${Math.max(1, page)}`, {
    cache: "no-store", headers: { Host: requestHeaders.get("host") ?? "localhost" }
  });
  if (!response.ok) notFound();
  return await response.json() as PublicBrand;
});

export async function PublicBrandPage({ brandId, tenantSlug, page = 1 }: { brandId: string; tenantSlug?: string; page?: number }) {
  const data = await loadPublicBrand(brandId, tenantSlug, page);
  const media = (id: string) => `/media/${encodeURIComponent(data.tenantSlug)}/${id}`;
  const competitionHref = (slug: string) => tenantSlug ? `/c/${tenantSlug}/${slug}` : `/${slug}`;
  const brandPath = tenantSlug ? `/c/${tenantSlug}/brands/${brandId}` : `/brands/${brandId}`;
  const now = Date.now();
  const date = (value: string) => new Date(value).toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric", timeZone: "UTC" });
  return <main className="brand-public" style={{ "--public-brand": data.brand.primaryColor,
    background: data.brand.backgroundColor, color: data.brand.textColor } as CSSProperties}>
    <a className="brand-public-skip" href="#brand-competitions">Skip to competitions</a>
    <header className="brand-public-masthead"><div className="brand-public-container brand-public-header">
      <Link href={brandPath} className="brand-public-logo-link" aria-label={`${data.brand.name} competitions home`}>
        {data.brand.logoAssetId ? <Image priority unoptimized src={media(data.brand.logoAssetId)} alt={data.brand.name}
          width={350} height={100} className="brand-public-logo" /> : <span className="brand-public-wordmark">{data.brand.name}</span>}
      </Link>
      <a className="brand-public-header-link" href="#brand-competitions" aria-label="Competitions and giveaways"><Gift size={17} strokeWidth={1.7} /><span>Competitions & giveaways</span></a>
    </div></header>
    <div className="brand-public-container brand-public-body">
      <section className="brand-public-intro" aria-labelledby="brand-title">
        <p className="brand-public-eyebrow"><span />A little more to look forward to</p>
        <h1 id="brand-title">{data.brand.name}</h1>
        <p className="brand-public-intro-copy">Prizes, experiences and giveaways. Discover what&apos;s next from {data.brand.name}.</p>
      </section>
      <section id="brand-competitions" className="brand-public-directory" aria-labelledby="directory-title">
        <div className="brand-public-section-heading"><h2 id="directory-title">Competitions</h2><span>{data.total} {data.total === 1 ? "competition" : "competitions"}</span></div>
        {data.competitions.length > 0 ? <div className="brand-public-grid">{data.competitions.map(item => {
          const upcoming = new Date(item.startsAt).getTime() > now;
          const closed = new Date(item.endsAt).getTime() <= now || !["Live", "Scheduled", "Paused"].includes(item.status);
          const status = closed ? "Entries closed" : upcoming ? "Coming soon" : item.status === "Paused" ? "Paused" : "Open for entries";
          return <article key={item.slug} className="brand-public-competition">
            <Link className={`brand-public-competition-image ${!item.coverAssetId ? "brand-public-competition-image-logo" : ""}`} href={competitionHref(item.slug)} aria-label={item.name}>
              {(item.coverAssetId || data.brand.logoAssetId) ? <Image unoptimized src={media(item.coverAssetId || data.brand.logoAssetId!)} alt={item.coverAssetId ? item.name : data.brand.name}
                width={720} height={450} className="brand-public-cover" /> : <Gift size={56} strokeWidth={1} />}
            </Link>
            <div className="brand-public-competition-copy">
              <p className={`brand-public-status ${!closed && !upcoming && item.status !== "Paused" ? "brand-public-status-open" : ""}`}><span />{status}</p>
              <h3><Link href={competitionHref(item.slug)}>{item.name}</Link></h3>
              {item.description && <p className="brand-public-description">{item.description}</p>}
              <p className="brand-public-date"><CalendarDays size={16} />{upcoming && !closed ? `Opens ${date(item.startsAt)}` : `${closed ? "Closed" : "Closes"} ${date(item.endsAt)}`}</p>
              <Link className="brand-public-competition-cta" href={competitionHref(item.slug)}><span>View competition</span><ArrowRight size={18} /></Link>
            </div>
          </article>;
        })}</div> : <div className="brand-public-empty">
          <div className="brand-public-empty-icon"><Gift size={34} strokeWidth={1.4} /></div>
          <h3>{data.total ? "No competitions on this page" : "Good things are on their way"}</h3>
          <p>{data.total ? "Explore our other competitions below." : `Our next giveaway is coming soon. Check back for new chances to win with ${data.brand.name}.`}</p>
          {data.total > 0 && <Link className="brand-public-back" href={brandPath}>View all competitions<ArrowRight size={17} /></Link>}
        </div>}
        {data.total > data.pageSize && <nav aria-label="Competition pages" className="brand-public-pagination">
          {data.page > 1 ? <Link href={`${brandPath}?page=${data.page - 1}`}><ArrowLeft size={16} />Previous</Link> : <span />}
          <span>Page {data.page} of {Math.ceil(data.total / data.pageSize)}</span>
          {data.page * data.pageSize < data.total ? <Link href={`${brandPath}?page=${data.page + 1}`}>Next<ArrowRight size={16} /></Link> : <span />}
        </nav>}
      </section>
      <div className="brand-public-organiser"><ShieldCheck size={18} strokeWidth={1.6} /><span>Competitions organised by <strong>{data.brand.name}</strong></span></div>
    </div>
    <footer className="brand-public-footer"><div className="brand-public-container"><span>{data.footerText || data.brand.name}</span><span>Competitions & giveaways</span></div></footer>
  </main>;
}
