const domains = [
  {
    host: "one-digital.competitions.local",
    type: "Platform subdomain",
    status: "Active",
    primary: true,
    dns: "System managed",
    checked: "Continuous"
  },
  {
    host: "competitions.one-digital.local",
    type: "Custom subdomain",
    status: "Awaiting DNS",
    primary: false,
    dns: "TXT one-competitions-verify=dev-token",
    checked: "Not checked"
  },
  {
    host: "/c/one-digital",
    type: "Platform path",
    status: "Active",
    primary: false,
    dns: "System managed",
    checked: "Continuous"
  }
];

const statusStyles: Record<string, string> = {
  Active: "bg-emerald-50 text-emerald-700 ring-emerald-200",
  "Awaiting DNS": "bg-amber-50 text-amber-800 ring-amber-200"
};

export default function DomainsPage() {
  return (
    <main className="mx-auto max-w-6xl px-6 py-8">
      <div className="flex flex-col gap-4 border-b border-line pb-6 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-ink">Domains</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-neutral-700">
            Manage tenant campaign hosts, DNS verification, and the primary public URL for this organisation.
          </p>
        </div>
        <button className="w-fit rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white">Add domain</button>
      </div>

      <section className="mt-6 grid gap-4 md:grid-cols-[1.2fr_0.8fr]">
        <div className="overflow-hidden rounded-md border border-line bg-white">
          <div className="grid grid-cols-[1.4fr_1fr_0.8fr_0.6fr] border-b border-line bg-neutral-50 px-4 py-3 text-xs font-semibold uppercase text-neutral-600">
            <span>Hostname</span>
            <span>Type</span>
            <span>Status</span>
            <span>Primary</span>
          </div>
          {domains.map((domain) => (
            <div className="grid grid-cols-[1.4fr_1fr_0.8fr_0.6fr] items-center border-b border-line px-4 py-4 text-sm last:border-b-0" key={domain.host}>
              <div>
                <div className="font-medium text-ink">{domain.host}</div>
                <div className="mt-1 text-xs text-neutral-600">{domain.checked}</div>
              </div>
              <span className="text-neutral-700">{domain.type}</span>
              <span>
                <span className={`inline-flex rounded-full px-2 py-1 text-xs font-semibold ring-1 ${statusStyles[domain.status]}`}>
                  {domain.status}
                </span>
              </span>
              <span className="text-neutral-700">{domain.primary ? "Yes" : "No"}</span>
            </div>
          ))}
        </div>

        <aside className="rounded-md border border-line bg-white p-5">
          <h2 className="text-base font-semibold text-ink">DNS verification</h2>
          <div className="mt-4 space-y-4 text-sm">
            <div>
              <div className="text-xs font-semibold uppercase text-neutral-500">Record type</div>
              <div className="mt-1 font-medium text-ink">TXT</div>
            </div>
            <div>
              <div className="text-xs font-semibold uppercase text-neutral-500">Expected value</div>
              <code className="mt-1 block rounded-md bg-neutral-100 px-3 py-2 text-xs text-neutral-800">
                one-competitions-verify=dev-token
              </code>
            </div>
            <div>
              <div className="text-xs font-semibold uppercase text-neutral-500">Verification provider</div>
              <div className="mt-1 text-neutral-700">Development DNS provider</div>
            </div>
          </div>
          <div className="mt-5 flex gap-2">
            <button className="rounded-md border border-line px-3 py-2 text-sm font-semibold">Verify now</button>
            <button className="rounded-md border border-line px-3 py-2 text-sm font-semibold">Set primary</button>
          </div>
        </aside>
      </section>
    </main>
  );
}
