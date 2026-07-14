export default async function PlatformPathCompetitionPage({ params }: { params: Promise<{ tenantSlug: string; competitionSlug: string }> }) {
  const { tenantSlug, competitionSlug } = await params;
  return <main className="mx-auto max-w-3xl px-6 py-12"><div className="text-sm font-semibold text-brand">{tenantSlug}</div><h1 className="mt-3 text-4xl font-semibold text-ink">{competitionSlug.replaceAll("-", " ")}</h1><a className="mt-6 inline-flex rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white" href={`/c/${tenantSlug}/${competitionSlug}/enter`}>Enter</a></main>;
}
