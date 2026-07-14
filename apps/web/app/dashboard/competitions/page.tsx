import Link from "next/link";

const competitions = [
  ["Win an iPhone", "Live", "3 approved entries"],
  ["Summer Restaurant Giveaway", "Draft", "0 entries"],
  ["Live TV Prize Draw", "Closed", "Draw ready"]
];

export default function CompetitionsPage() {
  return (
    <main className="mx-auto max-w-6xl px-6 py-8">
      <div className="flex items-end justify-between border-b border-line pb-6">
        <div>
          <h1 className="text-2xl font-semibold text-ink">Competitions</h1>
          <p className="mt-2 text-sm text-neutral-700">Create, publish, close, and prepare standard draw campaigns.</p>
        </div>
        <Link className="rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white" href="/dashboard/competitions/new">New competition</Link>
      </div>
      <div className="mt-6 overflow-hidden rounded-md border border-line bg-white">
        {competitions.map(([name, status, entries]) => (
          <Link className="grid grid-cols-3 border-b border-line px-4 py-4 text-sm last:border-b-0" href="/dashboard/competitions/demo" key={name}>
            <span className="font-medium">{name}</span>
            <span>{status}</span>
            <span>{entries}</span>
          </Link>
        ))}
      </div>
    </main>
  );
}
