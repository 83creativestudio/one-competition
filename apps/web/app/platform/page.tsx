export default function PlatformPage() {
  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <h1 className="text-3xl font-semibold">Platform Administration</h1>
      <div className="mt-6 overflow-hidden rounded-md border border-line bg-white">
        <div className="grid grid-cols-3 border-b border-line px-4 py-3 text-sm font-semibold">
          <span>Area</span>
          <span>Status</span>
          <span>Scope</span>
        </div>
        {[
          ["Tenants", "Ready", "Platform administrators"],
          ["Health", "Ready", "Operations"],
          ["Audit", "Foundation", "Append-only records"]
        ].map(([area, status, scope]) => (
          <div className="grid grid-cols-3 px-4 py-3 text-sm" key={area}>
            <span>{area}</span>
            <span>{status}</span>
            <span>{scope}</span>
          </div>
        ))}
      </div>
    </main>
  );
}
