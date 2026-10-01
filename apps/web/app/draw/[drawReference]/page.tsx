import { CheckCircle2, ShieldCheck, XCircle } from "lucide-react";
import { headers } from "next/headers";
import { apiBaseUrl } from "@/lib/server-auth";

type Verification = { drawReference: string; status: string; competitionName: string; organiser: string; drawnAt?: string; eligibleEntryCount: number; excludedEntryCount: number; entryPoolHash: string; configurationHash: string; algorithmVersion: string; certificateValid: boolean; certificateSha256Hash?: string };

export default async function DrawVerificationPage({ params }: { params: Promise<{ drawReference: string }> }) {
  const { drawReference } = await params;
  const host = (await headers()).get("host");
  const response = await fetch(`${apiBaseUrl()}/api/public/draws/${encodeURIComponent(drawReference)}/verification`, {
    headers: host ? { Host: host } : {},
    cache: "no-store"
  });
  if (!response.ok) return <main className="mx-auto max-w-3xl px-5 py-16"><h1 className="text-2xl font-semibold">Draw record not found</h1><p className="mt-3 text-muted">The supplied draw reference could not be verified.</p></main>;
  const data = await response.json() as Verification;
  return <main className="mx-auto max-w-3xl px-5 py-12"><div className="mb-8 flex items-center gap-3 border-b border-line pb-6"><ShieldCheck className="text-emerald-700" size={28} /><div><div className="font-semibold">ONE. Competitions</div><div className="text-sm text-muted">Public draw verification</div></div></div><section className="panel p-6"><div className="flex items-start justify-between gap-4"><div><h1 className="text-2xl font-semibold">{data.competitionName}</h1><p className="mt-2 text-sm text-muted">Organised by {data.organiser}</p></div>{data.certificateValid ? <CheckCircle2 className="text-emerald-700" size={30} /> : <XCircle className="text-red-700" size={30} />}</div><div className={`mt-6 border p-4 text-sm font-semibold ${data.certificateValid ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-red-200 bg-red-50 text-red-800"}`}>{data.certificateValid ? "Certificate integrity verified" : "Certificate integrity could not be verified"}</div><dl className="mt-6 grid gap-5 text-sm sm:grid-cols-2"><Item label="Draw reference" value={data.drawReference} /><Item label="Status" value={data.status} /><Item label="Draw date" value={data.drawnAt ? new Date(data.drawnAt).toLocaleString() : "Not completed"} /><Item label="Algorithm" value={data.algorithmVersion} /><Item label="Eligible entries" value={String(data.eligibleEntryCount)} /><Item label="Excluded entries" value={String(data.excludedEntryCount)} /><Item label="Entry pool hash" value={data.entryPoolHash} mono /><Item label="Configuration hash" value={data.configurationHash} mono /></dl></section></main>;
}
function Item({ label, value, mono }: { label: string; value: string; mono?: boolean }) { return <div><dt className="text-xs font-semibold uppercase text-muted">{label}</dt><dd className={`mt-1 break-all ${mono ? "font-mono text-xs" : ""}`}>{value}</dd></div>; }
