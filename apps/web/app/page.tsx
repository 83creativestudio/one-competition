import Link from "next/link";

const foundationItems = [
  "Tenant-aware API",
  "Secure dashboard authentication",
  "Role and policy foundation",
  "Audit-event baseline",
  "Health and deployment scaffolding"
];

export default function HomePage() {
  return (
    <main className="min-h-screen">
      <section className="border-b border-line bg-white">
        <div className="mx-auto flex min-h-[72vh] max-w-6xl flex-col justify-center px-6 py-12">
          <p className="text-sm font-semibold uppercase tracking-wide text-brand">ONE. Competitions</p>
          <h1 className="mt-4 max-w-3xl text-4xl font-semibold leading-tight text-ink sm:text-6xl">
            Multi-tenant competition platform foundation
          </h1>
          <p className="mt-5 max-w-2xl text-lg leading-8 text-neutral-700">
            Stage 1 establishes the deployable SaaS base: tenant isolation, authentication,
            platform administration, audit logging, health checks, and workspace structure.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link className="rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white" href="/dashboard">
              Dashboard
            </Link>
            <Link className="rounded-md border border-line px-4 py-2 text-sm font-semibold" href="/platform">
              Platform admin
            </Link>
          </div>
        </div>
      </section>
      <section className="mx-auto max-w-6xl px-6 py-10">
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
          {foundationItems.map((item) => (
            <div className="rounded-md border border-line bg-white p-4 text-sm font-medium" key={item}>
              {item}
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
