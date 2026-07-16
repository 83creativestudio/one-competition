"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import { ErrorState, formatDate, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { TenantUser } from "@/lib/contracts";

const roles = ["TenantOwner", "TenantAdministrator", "CompetitionManager", "EntryReviewer", "DrawOperator", "Auditor", "Viewer"];
export default function TeamPage() {
  const cache = useQueryClient(); const query = useQuery({ queryKey: ["team"], queryFn: () => apiFetch<TenantUser[]>("api/tenants/current/users") });
  const [invite, setInvite] = useState({ email: "", role: "Viewer" }); const refresh = () => cache.invalidateQueries({ queryKey: ["team"] });
  const create = useMutation({ mutationFn: () => apiFetch<TenantUser>("api/tenants/current/users/invite", { method: "POST", body: JSON.stringify(invite) }), onSuccess: () => { setInvite({ email: "", role: "Viewer" }); refresh(); } });
  const update = useMutation({ mutationFn: (value: { user: TenantUser; role?: string; status?: string }) => apiFetch<TenantUser>(`api/tenants/current/users/${value.user.id}`, { method: "PATCH", body: JSON.stringify({ role: value.role ?? value.user.role, status: value.status ?? value.user.status }) }), onSuccess: refresh });
  const remove = useMutation({ mutationFn: (id: string) => apiFetch<void>(`api/tenants/current/users/${id}`, { method: "DELETE" }), onSuccess: refresh });
  return <main className="page"><PageHeading title="Team" description="Invite users, assign least-privilege tenant roles, and revoke access." /><form className="panel mb-5 grid gap-4 p-5 sm:grid-cols-[1fr_240px_auto]" onSubmit={e => { e.preventDefault(); create.mutate(); }}><label className="field"><span>Email</span><input required type="email" value={invite.email} onChange={e => setInvite({ ...invite, email: e.target.value })} /></label><label className="field"><span>Role</span><select value={invite.role} onChange={e => setInvite({ ...invite, role: e.target.value })}>{roles.map(x => <option key={x}>{x}</option>)}</select></label><button className="command-button self-end" disabled={create.isPending}><Plus size={16} />Send invitation</button>{create.error && <p className="text-sm text-red-700 sm:col-span-3">{create.error.message}</p>}</form>{query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <div className="table-wrap"><table className="data-table"><thead><tr><th>User</th><th>Role</th><th>Status</th><th>Joined</th><th></th></tr></thead><tbody>{query.data?.map(user => <tr key={user.id}><td className="font-semibold">{user.email}</td><td><select className="border border-line bg-white px-2 py-1" value={user.role} onChange={e => update.mutate({ user, role: e.target.value })}>{roles.map(x => <option key={x}>{x}</option>)}</select></td><td><button onClick={() => update.mutate({ user, status: user.status === "Active" ? "Suspended" : "Active" })}><StatusBadge value={user.status} /></button></td><td>{formatDate(user.createdAt)}</td><td><button className="icon-button text-red-700" title="Remove user" onClick={() => remove.mutate(user.id)}><Trash2 size={16} /></button></td></tr>)}</tbody></table></div>}</main>;
}
