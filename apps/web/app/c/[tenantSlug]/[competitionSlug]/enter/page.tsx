export default async function PlatformPathEnterPage({ params }: { params: Promise<{ competitionSlug: string }> }) {
  const { competitionSlug } = await params;
  return <main className="mx-auto max-w-2xl px-6 py-10"><h1 className="text-2xl font-semibold text-ink">Enter {competitionSlug.replaceAll("-", " ")}</h1></main>;
}
