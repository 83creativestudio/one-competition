const swatches = [
  ["Primary", "#0f766e"],
  ["Secondary", "#111827"],
  ["Accent", "#c2410c"],
  ["Background", "#ffffff"],
  ["Text", "#111827"]
];

export default function BrandsPage() {
  return (
    <main className="mx-auto max-w-6xl px-6 py-8">
      <div className="flex flex-col gap-4 border-b border-line pb-6 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-ink">Brand Profiles</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-neutral-700">
            Control the tenant theme used by public campaign pages, confirmation screens, and future notifications.
          </p>
        </div>
        <button className="w-fit rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white">New brand</button>
      </div>

      <section className="mt-6 grid gap-4 lg:grid-cols-[0.9fr_1.1fr]">
        <form className="rounded-md border border-line bg-white p-5">
          <h2 className="text-base font-semibold text-ink">Tenant default</h2>
          <div className="mt-5 grid gap-4">
            <label className="grid gap-1 text-sm font-medium text-neutral-800">
              Brand name
              <input className="rounded-md border border-line px-3 py-2 text-sm" defaultValue="ONE. Digital" />
            </label>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="grid gap-1 text-sm font-medium text-neutral-800">
                Heading font
                <input className="rounded-md border border-line px-3 py-2 text-sm" defaultValue="Inter" />
              </label>
              <label className="grid gap-1 text-sm font-medium text-neutral-800">
                Body font
                <input className="rounded-md border border-line px-3 py-2 text-sm" defaultValue="Inter" />
              </label>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="grid gap-1 text-sm font-medium text-neutral-800">
                Support email
                <input className="rounded-md border border-line px-3 py-2 text-sm" defaultValue="support@onecompetitions.local" />
              </label>
              <label className="grid gap-1 text-sm font-medium text-neutral-800">
                Border radius
                <input className="rounded-md border border-line px-3 py-2 text-sm" defaultValue="6" />
              </label>
            </div>
          </div>
          <div className="mt-5 flex gap-2">
            <button className="rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white" type="button">
              Save profile
            </button>
            <button className="rounded-md border border-line px-4 py-2 text-sm font-semibold" type="button">
              Preview
            </button>
          </div>
        </form>

        <div className="space-y-4">
          <section className="rounded-md border border-line bg-white p-5">
            <h2 className="text-base font-semibold text-ink">Colours</h2>
            <div className="mt-4 grid gap-3 sm:grid-cols-5">
              {swatches.map(([label, color]) => (
                <div className="rounded-md border border-line p-3" key={label}>
                  <div className="h-10 rounded" style={{ backgroundColor: color }} />
                  <div className="mt-3 text-sm font-medium">{label}</div>
                  <div className="text-xs text-neutral-600">{color}</div>
                </div>
              ))}
            </div>
          </section>

          <section className="rounded-md border border-line bg-white p-5">
            <h2 className="text-base font-semibold text-ink">Public theme preview</h2>
            <div className="mt-4 rounded-md border border-line bg-[#ffffff] p-5 text-[#111827]">
              <div className="text-sm font-semibold text-[#0f766e]">ONE. Digital</div>
              <h3 className="mt-2 text-2xl font-semibold">Win the summer prize draw</h3>
              <p className="mt-2 max-w-xl text-sm leading-6 text-neutral-700">
                Public pages inherit this tenant profile until a competition-specific override is introduced.
              </p>
              <button className="mt-4 rounded-md bg-[#0f766e] px-4 py-2 text-sm font-semibold text-white">Enter now</button>
            </div>
          </section>
        </div>
      </section>
    </main>
  );
}
