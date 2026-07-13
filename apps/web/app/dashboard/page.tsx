export default function DashboardPage() {
  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <h1 className="text-3xl font-semibold">Customer Dashboard</h1>
      <div className="mt-6 grid gap-4 md:grid-cols-3">
        {["Current tenant", "Team access", "Audit readiness"].map((label) => (
          <section className="rounded-md border border-line bg-white p-5" key={label}>
            <h2 className="text-base font-semibold">{label}</h2>
            <p className="mt-2 text-sm leading-6 text-neutral-700">Stage 1 shell connected to the foundation API contract.</p>
          </section>
        ))}
      </div>
    </main>
  );
}
