export default function TeamPage() {
  return <DashboardSimplePage title="Team" items={["Tenant owner", "Administrator", "Viewer"]} />;
}

function DashboardSimplePage({ title, items }: { title: string; items: string[] }) {
  return <main className="mx-auto max-w-6xl px-6 py-8"><h1 className="text-2xl font-semibold text-ink">{title}</h1><div className="mt-6 grid gap-3 md:grid-cols-3">{items.map((item) => <section className="rounded-md border border-line bg-white p-5" key={item}><h2 className="font-semibold">{item}</h2></section>)}</div></main>;
}
