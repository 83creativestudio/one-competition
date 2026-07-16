"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { WebhookEndpoint } from "@/lib/contracts";

const events = ["competition.published", "competition.closed", "entry.submitted", "draw.completed", "winner.accepted", "winner.disqualified"];
export default function IntegrationsPage() {
  const cache = useQueryClient(); const [url, setUrl] = useState(""); const [selected, setSelected] = useState<string[]>(["entry.submitted"]); const [secret, setSecret] = useState<string>();
  const query = useQuery({ queryKey: ["webhooks"], queryFn: () => apiFetch<WebhookEndpoint[]>("api/webhooks") });
  const create = useMutation({ mutationFn: () => apiFetch<{ endpoint: WebhookEndpoint; signingSecret: string }>("api/webhooks", { method: "POST", body: JSON.stringify({ url, eventTypes: selected }) }), onSuccess: data => { setSecret(data.signingSecret); setUrl(""); cache.invalidateQueries({ queryKey: ["webhooks"] }); } });
  const remove = useMutation({ mutationFn: (id: string) => apiFetch<void>(`api/webhooks/${id}`, { method: "DELETE" }), onSuccess: () => cache.invalidateQueries({ queryKey: ["webhooks"] }) });
  return <main className="page"><PageHeading title="Integrations" description="Register signed webhook destinations. Delivery is queued, retried, and disabled after repeated failure." />
    {secret && <div className="mb-5 border border-amber-300 bg-amber-50 p-4 text-sm"><strong>Signing secret, shown once:</strong><code className="mt-2 block break-all">{secret}</code></div>}
    <form className="panel mb-5 grid gap-4 p-5" onSubmit={e => { e.preventDefault(); create.mutate(); }}><label className="field"><span>HTTPS endpoint URL</span><input required type="url" placeholder="https://hooks.example.com/one-competitions" value={url} onChange={e => setUrl(e.target.value)} /></label><fieldset><legend className="mb-2 text-sm font-medium">Events</legend><div className="flex flex-wrap gap-3">{events.map(event => <label className="flex items-center gap-2 text-sm" key={event}><input type="checkbox" checked={selected.includes(event)} onChange={e => setSelected(current => e.target.checked ? [...current, event] : current.filter(x => x !== event))} />{event}</label>)}</div></fieldset>{create.error && <p className="text-sm text-red-700">{create.error.message}</p>}<button className="command-button w-fit" disabled={!selected.length || create.isPending}><Plus size={17} />Add endpoint</button></form>
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="table-wrap"><table className="data-table"><thead><tr><th>Destination</th><th>Events</th><th>Status</th><th>Created</th><th></th></tr></thead><tbody>{query.data?.map(item => <tr key={item.id}><td className="max-w-xs break-all">{item.url}</td><td>{item.eventTypes.length}</td><td><StatusBadge value={item.isActive ? "Active" : "Disabled"} />{item.consecutiveFailureCount > 0 && <div className="mt-1 text-xs text-red-700">{item.consecutiveFailureCount} failures</div>}</td><td>{formatDate(item.createdAt)}</td><td><button className="icon-button text-red-700" title="Delete endpoint" onClick={() => remove.mutate(item.id)}><Trash2 size={16} /></button></td></tr>)}{!query.data?.length && <tr><td colSpan={5}><div className="empty-state">No webhook endpoints registered.</div></td></tr>}</tbody></table></div>}
  </main>;
}
