export default async function PublicEnterPage({ params }: { params: Promise<{ competitionSlug: string }> }) {
  const { competitionSlug } = await params;
  return (
    <main className="mx-auto max-w-2xl px-6 py-10">
      <h1 className="text-2xl font-semibold text-ink">Enter {competitionSlug.replaceAll("-", " ")}</h1>
      <form className="mt-6 grid gap-4 rounded-md border border-line bg-white p-5">
        <label className="grid gap-1 text-sm font-medium">Email<input className="rounded-md border border-line px-3 py-2" type="email" /></label>
        <label className="flex gap-2 text-sm"><input type="checkbox" /> I accept the competition terms</label>
        <label className="flex gap-2 text-sm"><input type="checkbox" /> I agree to marketing email</label>
        <button className="w-fit rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white" type="button">Submit entry</button>
      </form>
    </main>
  );
}
