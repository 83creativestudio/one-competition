"use client";

import { useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";

export default function InvitationPage() {
  return <Suspense fallback={<main className="grid min-h-screen place-items-center bg-canvas px-5"><p>Loading invitation...</p></main>}><InvitationForm /></Suspense>;
}

function InvitationForm() {
  const params = useSearchParams(); const [form, setForm] = useState({ invitationId: params.get("id") ?? "", token: params.get("token") ?? "", password: "", displayName: "" }); const [message, setMessage] = useState("");
  async function accept() { const response = await fetch("/api/backend/api/auth/invitations/accept", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(form) }); setMessage(response.ok ? "Invitation accepted. You can now sign in." : "The invitation is invalid or expired."); }
  return <main className="grid min-h-screen place-items-center bg-canvas px-5"><section className="w-full max-w-md border border-line bg-white p-7"><h1 className="text-2xl font-semibold">Accept invitation</h1><div className="mt-6 grid gap-4"><label className="field"><span>Invitation ID</span><input value={form.invitationId} onChange={e => setForm({ ...form, invitationId: e.target.value })} /></label><label className="field"><span>One-time token</span><textarea value={form.token} onChange={e => setForm({ ...form, token: e.target.value })} /></label><label className="field"><span>Display name</span><input value={form.displayName} onChange={e => setForm({ ...form, displayName: e.target.value })} /></label><label className="field"><span>Password</span><input type="password" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} /></label><button className="command-button w-fit" onClick={accept}>Accept invitation</button>{message && <p className="text-sm">{message}</p>}</div></section></main>;
}
