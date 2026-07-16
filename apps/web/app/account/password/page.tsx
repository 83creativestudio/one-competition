"use client";

import Link from "next/link";
import { useState } from "react";

export default function PasswordPage() {
  const [form, setForm] = useState({ email: "", token: "", newPassword: "" }); const [message, setMessage] = useState("");
  async function send() { const response = await fetch("/api/backend/api/auth/forgot-password", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email: form.email }) }); setMessage(response.ok ? "If the account exists, a reset message has been queued." : "The request could not be completed."); }
  async function reset() { const response = await fetch("/api/backend/api/auth/reset-password", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(form) }); setMessage(response.ok ? "Password reset. You can now sign in." : "The token is invalid or expired."); }
  return <main className="grid min-h-screen place-items-center bg-canvas px-5"><section className="w-full max-w-md border border-line bg-white p-7"><h1 className="text-2xl font-semibold">Password recovery</h1><div className="mt-6 grid gap-4"><label className="field"><span>Email</span><input type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} /></label><button className="secondary-button w-fit" onClick={send}>Send reset token</button><label className="field"><span>Reset token</span><textarea value={form.token} onChange={e => setForm({ ...form, token: e.target.value })} /></label><label className="field"><span>New password</span><input type="password" value={form.newPassword} onChange={e => setForm({ ...form, newPassword: e.target.value })} /></label><button className="command-button w-fit" onClick={reset}>Reset password</button>{message && <p className="text-sm">{message}</p>}<Link className="text-sm text-emerald-800 underline" href="/login">Return to sign in</Link></div></section></main>;
}
