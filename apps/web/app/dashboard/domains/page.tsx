"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, Globe2, Plus, Star, Trash2 } from "lucide-react";
import { useState } from "react";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Domain } from "@/lib/contracts";

export default function DomainsPage() {
  const client = useQueryClient();
  const [hostname, setHostname] = useState("");
  const [domainType, setDomainType] = useState("CustomSubdomain");
  const query = useQuery({ queryKey: ["domains"], queryFn: () => apiFetch<Domain[]>("api/domains") });
  const refresh = () => client.invalidateQueries({ queryKey: ["domains"] });
  const create = useMutation({ mutationFn: () => apiFetch<Domain>("api/domains", { method: "POST", body: JSON.stringify({ hostname, domainType }) }), onSuccess: () => { setHostname(""); refresh(); } });
  const mutate = useMutation({ mutationFn: ({ id, action }: { id: string; action: string }) => apiFetch<unknown>(`api/domains/${id}${action === "delete" ? "" : `/${action}`}`, { method: action === "delete" ? "DELETE" : "POST" }), onSuccess: refresh });
  return <main className="page">
    <PageHeading title="Domains" description="Connect campaign hosts, verify ownership, and select the tenant's primary public address." />
    <form className="panel mb-5 grid gap-4 p-4 sm:grid-cols-[1fr_220px_auto] sm:items-end" onSubmit={event => { event.preventDefault(); create.mutate(); }}>
      <label className="field"><span>Hostname</span><input required placeholder="win.example.com" value={hostname} onChange={event => setHostname(event.target.value)} /></label>
      <label className="field"><span>Domain type</span><select value={domainType} onChange={event => setDomainType(event.target.value)}><option>CustomSubdomain</option><option>CustomRootDomain</option><option>CustomWwwDomain</option></select></label>
      <button className="command-button" disabled={create.isPending}><Plus size={17} />Add domain</button>
      {create.error && <p className="text-sm text-red-700 sm:col-span-3">{create.error.message}</p>}
    </form>
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="grid gap-4 lg:grid-cols-2">{query.data?.map(domain => <article className="panel p-5" key={domain.id}>
      <div className="flex items-start justify-between gap-3"><div className="min-w-0"><div className="flex items-center gap-2"><Globe2 size={18} /><h2 className="truncate font-semibold">{domain.hostname}</h2></div><div className="mt-2 flex flex-wrap gap-2"><StatusBadge value={domain.status} />{domain.sslStatus && <StatusBadge value={`SSL ${domain.sslStatus}`} />}{domain.isPrimary && <span className="status status-active">Primary</span>}</div></div><button className="icon-button text-red-700" title="Disconnect domain" onClick={() => mutate.mutate({ id: domain.id, action: "delete" })}><Trash2 size={16} /></button></div>
      <dl className="mt-5 grid gap-3 text-sm sm:grid-cols-2"><Info label="Type" value={domain.domainType} /><Info label="Last checked" value={formatDate(domain.lastCheckedAt)} /><Info label="Verification" value={domain.verificationMethod ?? "Platform managed"} /><Info label="Certificate expires" value={formatDate(domain.certificateExpiresAt)} /></dl>
      {domain.verificationToken && <div className="mt-4 border border-line bg-neutral-50 p-3 text-xs"><div className="font-semibold uppercase text-muted">Required TXT value</div><code className="mt-2 block break-all">one-competitions-verify={domain.verificationToken}</code>{domain.expectedDnsTarget && <div className="mt-2 text-muted">Target: {domain.expectedDnsTarget}</div>}</div>}
      {domain.failureReason && <p className="mt-3 text-sm text-red-700">{domain.failureReason}</p>}
      <div className="mt-4 flex flex-wrap gap-2"><button className="secondary-button" disabled={mutate.isPending} onClick={() => mutate.mutate({ id: domain.id, action: "verify" })}><CheckCircle2 size={16} />Verify now</button>{!domain.isPrimary && <button className="secondary-button" disabled={mutate.isPending} onClick={() => mutate.mutate({ id: domain.id, action: "set-primary" })}><Star size={16} />Set primary</button>}</div>
    </article>)}{!query.data?.length && <div className="panel empty-state lg:col-span-2">No domains configured.</div>}</div>}
  </main>;
}
function Info({ label, value }: { label: string; value: string }) { return <div><dt className="text-xs font-semibold uppercase text-muted">{label}</dt><dd className="mt-1">{value}</dd></div>; }
