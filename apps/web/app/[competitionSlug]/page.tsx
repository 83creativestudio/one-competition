export default async function PublicCompetitionPage({ params }: { params: Promise<{ competitionSlug: string }> }) {
  const { competitionSlug } = await params;
  return <PublicCampaignShell slug={competitionSlug} state="Open" />;
}

function PublicCampaignShell({ slug, state }: { slug: string; state: string }) {
  return (
    <main className="min-h-screen bg-white">
      <section className="mx-auto max-w-3xl px-6 py-12">
        <div className="text-sm font-semibold text-brand">ONE. Competitions</div>
        <h1 className="mt-3 text-4xl font-semibold text-ink">{slug.replaceAll("-", " ")}</h1>
        <p className="mt-3 text-sm text-neutral-700">Status: {state}</p>
        <a className="mt-6 inline-flex rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white" href={`/${slug}/enter`}>Enter</a>
      </section>
    </main>
  );
}
