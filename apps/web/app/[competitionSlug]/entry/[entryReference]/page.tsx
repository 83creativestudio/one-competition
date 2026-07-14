export default async function EntryStatusPage({ params }: { params: Promise<{ entryReference: string }> }) {
  const { entryReference } = await params;
  return <main className="mx-auto max-w-2xl px-6 py-10"><h1 className="text-2xl font-semibold text-ink">Entry {entryReference}</h1><p className="mt-3 text-sm text-neutral-700">Current entry status.</p></main>;
}
