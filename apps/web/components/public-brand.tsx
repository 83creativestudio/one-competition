import { headers } from "next/headers";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import type { PublicBrand } from "@/lib/contracts";

export async function PublicBrandPage({ brandId, tenantSlug, page = 1 }: { brandId: string; tenantSlug?: string; page?: number }) {
  if (!/^[a-f0-9-]{36}$/i.test(brandId)) notFound();
  const requestHeaders = await headers();
  const prefix = tenantSlug ? `c/${encodeURIComponent(tenantSlug)}/api/public` : "api/public";
  const response = await fetch(`${process.env.API_BASE_URL ?? "http://127.0.0.1:5050"}/${prefix}/brands/${brandId}?page=${Math.max(1, page)}`, {
    cache: "no-store", headers: { Host: requestHeaders.get("host") ?? "localhost" }
  });
  if (!response.ok) notFound();
  const data = await response.json() as PublicBrand;
  const media = (id: string) => `/media/${encodeURIComponent(data.tenantSlug)}/${id}`;
  const competitionHref = (slug: string) => tenantSlug ? `/c/${tenantSlug}/${slug}` : `/${slug}`;
  const brandPath = tenantSlug ? `/c/${tenantSlug}/brands/${brandId}` : `/brands/${brandId}`;
  return <main className="min-h-screen" style={{ background: data.brand.backgroundColor, color: data.brand.textColor }}>
    <header className="border-b border-black/10"><div className="mx-auto flex max-w-6xl flex-wrap items-center gap-6 px-5 py-10 sm:px-8">
      {data.brand.logoAssetId && <Image unoptimized src={media(data.brand.logoAssetId)} alt={data.brand.name} width={240} height={120} className="max-h-28 w-48 object-contain" />}
      <div className="min-w-0"><h1 className="break-words text-3xl font-semibold sm:text-4xl">{data.brand.name}</h1><p className="mt-3 text-sm opacity-75">{data.total} {data.total === 1 ? "competition" : "competitions"}</p></div>
    </div></header>
    <section className="mx-auto max-w-6xl px-5 py-10 sm:px-8"><h2 className="mb-6 text-xl font-semibold">Competitions</h2>
      <div className="grid gap-x-8 gap-y-10 sm:grid-cols-2 lg:grid-cols-3">{data.competitions.map(item => <article key={item.slug} className="min-w-0 border-b border-black/10 pb-6">
        {(item.coverAssetId || data.brand.logoAssetId) && <Link href={competitionHref(item.slug)}><Image unoptimized src={media(item.coverAssetId || data.brand.logoAssetId!)} alt={item.name} width={720} height={480} className="mb-4 aspect-[3/2] w-full object-contain" /></Link>}
        <div className="text-xs font-semibold uppercase opacity-70">{item.status}</div><h3 className="mt-2 break-words text-xl font-semibold"><Link href={competitionHref(item.slug)}>{item.name}</Link></h3>
        {item.description && <p className="mt-3 whitespace-pre-wrap text-sm leading-6">{item.description}</p>}
        <p className="mt-4 text-sm opacity-70">Closes {new Date(item.endsAt).toLocaleDateString("en-GB", { timeZone: "UTC" })}</p>
        <Link className="mt-4 inline-block text-sm font-semibold underline underline-offset-4" style={{ color: data.brand.primaryColor }} href={competitionHref(item.slug)}>View competition</Link>
      </article>)}</div>
      {!data.competitions.length && <p className="py-10 text-sm">No published competitions yet.</p>}
      {data.total > data.pageSize && <nav aria-label="Competition pages" className="mt-8 flex justify-between text-sm">{data.page > 1 ? <Link href={`${brandPath}?page=${data.page - 1}`}>Previous</Link> : <span />}{data.page * data.pageSize < data.total && <Link href={`${brandPath}?page=${data.page + 1}`}>Next</Link>}</nav>}
    </section>
    {data.footerText && <footer className="mx-auto max-w-6xl border-t border-black/10 px-5 py-6 text-sm">{data.footerText}</footer>}
  </main>;
}
