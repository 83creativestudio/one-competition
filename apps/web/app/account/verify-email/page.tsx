"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { MailCheck } from "lucide-react";
import { apiFetch } from "@/lib/api";

export default function VerifyEmailPage() {
  const [email, setEmail] = useState(""); const [token, setToken] = useState("");
  const [error, setError] = useState(""); const [pending, setPending] = useState(false); const [complete, setComplete] = useState(false);
  const [notice, setNotice] = useState("");
  useEffect(() => { const query = new URLSearchParams(window.location.search); setEmail(query.get("email") ?? ""); setToken(query.get("token") ?? ""); }, []);
  async function verify() {
    setPending(true); setError("");
    try { await apiFetch("api/auth/verify-email", { method: "POST", body: JSON.stringify({ email, token }) }); setComplete(true); window.history.replaceState(null, "", "/account/verify-email"); }
    catch (failure) { setError(failure instanceof Error ? failure.message : "Email verification failed."); }
    finally { setPending(false); }
  }
  async function resend() {
    setPending(true); setError(""); setNotice("");
    try { await apiFetch("api/auth/resend-verification", { method: "POST", body: JSON.stringify({ email }) }); setNotice("If this account needs verification, a new link will be emailed to you."); }
    catch (failure) { setError(failure instanceof Error ? failure.message : "Unable to request verification."); }
    finally { setPending(false); }
  }
  return <main className="login-shell"><section className="login-panel"><div className="login-symbol"><MailCheck size={30} /></div><p className="login-eyebrow">ONE. Competitions</p><h1 className="login-title">Verify your email</h1>
    {complete ? <div role="status"><p className="my-5">Your email is verified. You can now sign in.</p><Link className="command-button" href="/login">Sign in</Link></div> : <form className="login-form" onSubmit={e => { e.preventDefault(); void verify(); }}>
      <label className="field"><span>Email</span><input required type="email" autoComplete="email" value={email} onChange={e => setEmail(e.target.value)} /></label>
      <label className="field"><span>Verification token</span><input required value={token} onChange={e => setToken(e.target.value)} /></label>
      {error && <p className="text-sm text-red-700" role="alert">{error}</p>}<button className="command-button login-submit" disabled={pending}>{pending ? "Verifying..." : "Verify email"}</button>
      {notice && <p role="status" className="text-sm text-muted">{notice}</p>}
      <button className="text-left text-sm font-semibold text-brand" type="button" disabled={pending || !email.trim()} onClick={() => void resend()}>Resend verification email</button>
      <Link className="text-sm text-muted" href="/login">Back to sign in</Link>
    </form>}
  </section></main>;
}
