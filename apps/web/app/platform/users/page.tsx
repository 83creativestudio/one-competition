"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Building2, ChevronLeft, ChevronRight, Pencil, Plus, Search, Trash2, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { apiFetch } from "@/lib/api";
import { ErrorState, LoadingState, PageHeading, StatusBadge } from "@/components/operations-ui";

type Account = { id: string; displayName: string; email: string; role: "Admin" | "Customer"; isActive: boolean; emailConfirmed: boolean; mfaEnabled: boolean; isLockedOut: boolean; workspaces: { id: string; name: string; slug: string }[] };
type AccountPage = { items: Account[]; total: number; page: number; pageSize: number };
type AccountForm = { displayName: string; email: string; password: string; newPassword: string; unlockLogin: boolean; organisationName: string; role: "Admin" | "Customer"; isActive: boolean };
const blank: AccountForm = { displayName: "", email: "", password: "", newPassword: "", unlockLogin: false, organisationName: "", role: "Customer", isActive: true };

export default function UsersPage() {
  const cache = useQueryClient();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [editor, setEditor] = useState<Account | "new" | null>(null);
  const [deleting, setDeleting] = useState<Account | null>(null);
  const [form, setForm] = useState<AccountForm>(blank);
  const [notice, setNotice] = useState("");
  const dialog = useRef<HTMLDialogElement>(null);
  const deleteDialog = useRef<HTMLDialogElement>(null);
  const query = useQuery({ queryKey: ["platform-users", search, page], queryFn: () => apiFetch<AccountPage>(`api/platform/users?search=${encodeURIComponent(search)}&page=${page}&pageSize=20`) });
  const refresh = () => cache.invalidateQueries({ queryKey: ["platform-users"] });
  const save = useMutation({
    mutationFn: () => apiFetch<Account>(editor === "new" ? "api/platform/users" : `api/platform/users/${editor?.id}`, {
      method: editor === "new" ? "POST" : "PATCH", body: JSON.stringify({ ...form, newPassword: form.newPassword || null })
    }),
    onSuccess: () => { setNotice(editor === "new" ? "User created." : "User updated. They will need to sign in again."); setEditor(null); void refresh(); }
  });
  const remove = useMutation({ mutationFn: () => apiFetch<void>(`api/platform/users/${deleting?.id}`, { method: "DELETE" }),
    onSuccess: () => { setDeleting(null); setNotice("User deleted. Account access has been revoked."); void refresh(); } });
  const workspace = useMutation({ mutationFn: async (slug: string) => {
    const response = await fetch("/api/session/workspace", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ slug }) });
    if (!response.ok) { const problem = await response.json(); throw new Error(problem.detail ?? "Unable to open workspace."); }
  }, onSuccess: () => window.location.assign("/dashboard/competitions") });

  useEffect(() => { if (editor) dialog.current?.showModal(); else dialog.current?.close(); }, [editor]);
  useEffect(() => { if (deleting) deleteDialog.current?.showModal(); else deleteDialog.current?.close(); }, [deleting]);

  function open(account: Account | "new") {
    save.reset(); setNotice("");
    setForm(account === "new" ? { ...blank } : { ...blank, displayName: account.displayName, email: account.email, role: account.role, isActive: account.isActive });
    setEditor(account);
  }

  return <main className="page">
    <div className="flex flex-wrap items-start justify-between gap-4"><PageHeading title="Users" description="" /><button className="command-button" onClick={() => open("new")}><Plus size={18} />Create user</button></div>
    <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
      <label className="search-field w-full sm:w-80"><Search size={18} /><input aria-label="Search users" placeholder="Search by name or email" value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} /></label>
      <span className="text-sm text-muted">{query.data?.total ?? 0} users</span>
    </div>
    {notice && <p className="mb-4 text-sm text-emerald-800" role="status">{notice}</p>}
    {workspace.error && <p className="mb-4 text-sm text-red-700" role="alert">{workspace.error.message}</p>}
    {query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <>
      <div className="table-wrap"><table className="data-table"><thead><tr><th>Name</th><th>Email</th><th>Account role</th><th>Workspace</th><th>Status</th><th>Security</th><th><span className="sr-only">Actions</span></th></tr></thead><tbody>
        {query.data?.items.map(account => <tr key={account.id}>
          <td className="font-semibold">{account.displayName}</td><td>{account.email}</td><td><StatusBadge value={account.role} /></td>
          <td>{account.workspaces.length ? account.workspaces.map(item => <button className="flex items-center gap-2 text-left text-brand" key={item.id} title={`Manage ${item.name}`} disabled={workspace.isPending} onClick={() => workspace.mutate(item.slug)}><Building2 size={15} />{item.name}</button>) : "No workspace"}</td>
          <td><StatusBadge value={!account.isActive ? "Suspended" : account.isLockedOut ? "Locked" : account.emailConfirmed ? "Active" : "Pending verification"} /></td>
          <td>{account.mfaEnabled ? "MFA enabled" : "Password"}</td>
          <td><div className="flex gap-1"><button className="icon-button" title={`Edit ${account.displayName}`} aria-label={`Edit ${account.displayName}`} onClick={() => open(account)}><Pencil size={17} /></button><button className="icon-button text-red-700" title={`Delete ${account.displayName}`} aria-label={`Delete ${account.displayName}`} onClick={() => { remove.reset(); setDeleting(account); }}><Trash2 size={17} /></button></div></td>
        </tr>)}
        {query.data?.items.length === 0 && <tr><td colSpan={7} className="py-10 text-center text-muted">No users found.</td></tr>}
      </tbody></table></div>
      <div className="mt-4 flex items-center justify-end gap-3"><button className="icon-button" aria-label="Previous page" disabled={page === 1} onClick={() => setPage(page - 1)}><ChevronLeft size={18} /></button><span className="text-sm">Page {page} of {Math.max(1, Math.ceil((query.data?.total ?? 0) / 20))}</span><button className="icon-button" aria-label="Next page" disabled={page * 20 >= (query.data?.total ?? 0)} onClick={() => setPage(page + 1)}><ChevronRight size={18} /></button></div>
    </>}
    <dialog ref={dialog} className="account-dialog" onCancel={event => { if (save.isPending) event.preventDefault(); else setEditor(null); }} onClose={() => setEditor(null)} aria-labelledby="user-dialog-title">
      <div className="mb-6 flex items-center justify-between gap-4"><h2 id="user-dialog-title" className="text-xl font-semibold">{editor === "new" ? "Create user" : "Edit user"}</h2><button className="icon-button" aria-label="Close user editor" disabled={save.isPending} onClick={() => setEditor(null)}><X size={20} /></button></div>
      <form className="grid gap-4" onSubmit={e => { e.preventDefault(); save.mutate(); }}>
        <label className="field"><span>Full name</span><input autoFocus required minLength={2} maxLength={160} value={form.displayName} onChange={e => setForm({ ...form, displayName: e.target.value })} /></label>
        <label className="field"><span>Login email</span><input required type="email" autoComplete="off" maxLength={254} value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} /></label>
        {editor === "new" && <><label className="field"><span>Organisation</span><input required minLength={2} maxLength={200} value={form.organisationName} onChange={e => setForm({ ...form, organisationName: e.target.value })} /></label><label className="field"><span>Initial password</span><input required autoComplete="new-password" type="password" minLength={12} maxLength={128} value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} /><small className="mt-1 block text-muted">At least 12 characters, with uppercase, lowercase, a number and a symbol.</small></label></>}
        <label className="field"><span>Account role</span><select value={form.role} onChange={e => setForm({ ...form, role: e.target.value as AccountForm["role"] })}><option value="Customer">Customer</option><option value="Admin">Admin</option></select></label>
        {editor !== "new" && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isActive} onChange={e => setForm({ ...form, isActive: e.target.checked })} />Account active</label>}
        {editor && editor !== "new" && <><label className="field"><span>New password (optional)</span><input type="password" autoComplete="new-password" minLength={12} maxLength={128} value={form.newPassword} onChange={e => setForm({ ...form, newPassword: e.target.value })} /></label>{editor.isLockedOut && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.unlockLogin} onChange={e => setForm({ ...form, unlockLogin: e.target.checked })} />Unlock login</label>}</>}
        {save.error && <p className="text-sm text-red-700" role="alert">{save.error.message}</p>}
        <div className="mt-2 flex justify-end gap-3"><button type="button" className="secondary-button" disabled={save.isPending} onClick={() => setEditor(null)}>Cancel</button><button className="command-button" disabled={save.isPending}>{save.isPending ? "Saving..." : editor === "new" ? "Create user" : "Save changes"}</button></div>
      </form>
    </dialog>
    <dialog ref={deleteDialog} className="account-dialog" onCancel={e => { if (remove.isPending) e.preventDefault(); else setDeleting(null); }} onClose={() => setDeleting(null)} aria-labelledby="delete-dialog-title">
      <h2 id="delete-dialog-title" className="text-xl font-semibold">Delete user?</h2><p className="my-4 text-sm">{deleting?.displayName} ({deleting?.email}) will lose access immediately. Competition records and audit history will be retained.</p>
      {remove.error && <p className="mb-4 text-sm text-red-700" role="alert">{remove.error.message}</p>}
      <div className="flex justify-end gap-3"><button className="secondary-button" disabled={remove.isPending} onClick={() => setDeleting(null)}>Cancel</button><button className="command-button !bg-red-700" disabled={remove.isPending} onClick={() => remove.mutate()}><Trash2 size={17} />{remove.isPending ? "Deleting..." : "Delete user"}</button></div>
    </dialog>
  </main>;
}
