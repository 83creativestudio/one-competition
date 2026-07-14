export default function NewCompetitionPage() {
  return (
    <main className="mx-auto max-w-3xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-ink">New Competition</h1>
      <form className="mt-6 grid gap-4 rounded-md border border-line bg-white p-5">
        <label className="grid gap-1 text-sm font-medium">Name<input className="rounded-md border border-line px-3 py-2" defaultValue="Win an iPhone" /></label>
        <label className="grid gap-1 text-sm font-medium">Slug<input className="rounded-md border border-line px-3 py-2" defaultValue="win-an-iphone" /></label>
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="grid gap-1 text-sm font-medium">Start<input className="rounded-md border border-line px-3 py-2" type="datetime-local" /></label>
          <label className="grid gap-1 text-sm font-medium">End<input className="rounded-md border border-line px-3 py-2" type="datetime-local" /></label>
        </div>
        <button className="w-fit rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white" type="button">Create draft</button>
      </form>
    </main>
  );
}
