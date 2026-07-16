import { headers } from "next/headers";
import Link from "next/link";
import type { PublicCompetition } from "@/lib/contracts";

export async function PublicCampaign({ competitionSlug, tenantSlug }: { competitionSlug: string; tenantSlug?: string }) {
  const prefix = tenantSlug ? `c/${encodeURIComponent(tenantSlug)}/api/public` : "api/public";
  const requestHeaders = await headers();
  const host = requestHeaders.get("host");
  const response = await fetch(`${process.env.API_BASE_URL ?? "http://127.0.0.1:5050"}/${prefix}/competitions/${encodeURIComponent(competitionSlug)}`, {
    cache: "no-store",
    headers: host ? { Host: host, "X-Forwarded-Proto": requestHeaders.get("x-forwarded-proto") ?? "http" } : undefined
  });
  if (!response.ok) return <main className="mx-auto max-w-3xl px-6 py-12"><h1 className="text-3xl font-semibold">Competition unavailable</h1></main>;
  const data = await response.json() as PublicCompetition;
  const href = tenantSlug ? `/c/${tenantSlug}/${competitionSlug}/enter` : `/${competitionSlug}/enter`;
  return <main className="min-h-screen bg-white"><section className="mx-auto max-w-4xl px-6 py-12"><div className="border-b border-line pb-8"><div className="text-sm font-semibold text-emerald-800">ONE. Competitions</div><h1 className="mt-4 text-4xl font-semibold">{data.name}</h1>{data.page?.seoDescription && <p className="mt-4 max-w-2xl leading-7 text-neutral-700">{data.page.seoDescription}</p>}<div className="mt-5 flex flex-wrap gap-4 text-sm text-neutral-700"><span>Status: <strong>{data.status}</strong></span><span>Closes: <strong>{new Date(data.endsAt).toLocaleString()}</strong></span>{data.minimumAge != null && <span>Minimum age: <strong>{data.minimumAge}</strong></span>}</div></div><Link className="command-button mt-7" href={href}>Enter competition</Link></section></main>;
}
