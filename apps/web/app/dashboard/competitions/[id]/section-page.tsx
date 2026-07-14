type SectionPageProps = {
  title: string;
  summary: string;
  items: string[];
};

export function SectionPage({ title, summary, items }: SectionPageProps) {
  return (
    <main className="mx-auto max-w-6xl px-6 py-8">
      <div className="border-b border-line pb-6">
        <h1 className="text-2xl font-semibold text-ink">{title}</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-neutral-700">{summary}</p>
      </div>
      <div className="mt-6 grid gap-3 md:grid-cols-3">
        {items.map((item) => (
          <section className="rounded-md border border-line bg-white p-5" key={item}>
            <h2 className="text-base font-semibold text-ink">{item}</h2>
            <p className="mt-2 text-sm leading-6 text-neutral-700">Ready for API-backed dashboard data.</p>
          </section>
        ))}
      </div>
    </main>
  );
}
