"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, FilePlus2 } from "lucide-react";
import { useParams } from "next/navigation";
import { useState } from "react";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { ExportJob } from "@/lib/contracts";

export default function ExportsPage() {
  const id = useParams<{ id: string }>().id; const cache = useQueryClient(); const [type, setType] = useState("Entries");
  const query = useQuery({ queryKey: ["exports", id], queryFn: () => apiFetch<ExportJob[]>(`api/competitions/${id}/exports`), refetchInterval: q => q.state.data?.some(x => ["Pending", "Processing"].includes(x.status)) ? 4000 : false });
  const create = useMutation({ mutationFn: () => apiFetch<ExportJob>(`api/competitions/${id}/exports`, { method: "POST", body: JSON.stringify({ exportType: type, format: "Csv" }) }), onSuccess: () => cache.invalidateQueries({ queryKey: ["exports", id] }) });
  return <main className="page"><PageHeading title="Exports" description="Generate tenant-scoped CSV files asynchronously. Completed files expire automatically." action={<div className="flex gap-2"><select className="border border-line bg-white px-3 text-sm" value={type} onChange={e => setType(e.target.value)}><option>Entries</option><option>Winners</option><option>Audit</option></select><button className="command-button" disabled={create.isPending} onClick={() => create.mutate()}><FilePlus2 size={17} />Generate</button></div>} />{create.error && <p className="mb-4 text-sm text-red-700">{create.error.message}</p>}{query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="table-wrap"><table className="data-table"><thead><tr><th>Type</th><th>Format</th><th>Status</th><th>Requested</th><th>Expires</th><th>File</th></tr></thead><tbody>{query.data?.map(job => <tr key={job.id}><td>{job.exportType}</td><td>{job.format}</td><td><StatusBadge value={job.status} /></td><td>{formatDate(job.createdAt)}</td><td>{formatDate(job.expiresAt)}</td><td>{job.status === "Completed" ? <a className="icon-button" title="Download export" href={`/api/backend/api/competitions/${id}/exports/${job.id}/download`}><Download size={16} /></a> : "-"}</td></tr>)}{!query.data?.length && <tr><td colSpan={6}><div className="empty-state">No exports requested.</div></td></tr>}</tbody></table></div>}</main>;
}
