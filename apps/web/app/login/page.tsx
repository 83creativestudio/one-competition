"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { KeyRound, ShieldCheck } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import Link from "next/link";
import { z } from "zod";

const schema = z.object({ email: z.string().email(), password: z.string().min(12), twoFactorCode: z.string().optional() });
type Values = z.infer<typeof schema>;

export default function LoginPage() {
  const router = useRouter();
  const [error, setError] = useState("");
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<Values>({ resolver: zodResolver(schema) });

  async function submit(values: Values) {
    setError("");
    try {
      const response = await fetch("/api/session/login", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(values) });
      if (!response.ok) {
        const problem = await response.json().catch(() => null) as { detail?: string } | null;
        setError(problem?.detail ?? "Login failed.");
        return;
      }
      const returnTo = new URLSearchParams(window.location.search).get("returnTo");
      const destination = returnTo ? new URL(returnTo, window.location.origin) : null;
      router.replace(destination?.origin === window.location.origin ? `${destination.pathname}${destination.search}${destination.hash}` : "/dashboard");
      router.refresh();
    } catch {
      setError("Unable to connect. Please try again.");
    }
  }

  return <main className="login-shell">
    <section className="login-panel" aria-labelledby="login-title">
      <div className="login-symbol" aria-hidden="true"><ShieldCheck size={30} strokeWidth={1.8} /></div>
      <p className="login-eyebrow">Secure workspace</p>
      <h1 id="login-title" className="login-title">ONE. Competitions</h1>
      <p className="login-description">Sign in to your account.</p>
      <form className="login-form" onSubmit={handleSubmit(submit)} noValidate>
        <div><label className="field"><span>Email</span><input autoComplete="username" type="email" aria-invalid={!!errors.email} aria-describedby={errors.email ? "email-error" : undefined} {...register("email")} /></label>{errors.email && <p id="email-error" className="field-error mt-2">Enter a valid email.</p>}</div>
        <div><label className="field"><span>Password</span><input autoComplete="current-password" type="password" aria-invalid={!!errors.password} aria-describedby={errors.password ? "password-error" : undefined} {...register("password")} /></label>{errors.password && <p id="password-error" className="field-error mt-2">Password must contain at least 12 characters.</p>}</div>
        <details className="login-mfa"><summary>Use an authenticator code</summary><label className="field"><span>Authenticator code</span><input autoComplete="one-time-code" inputMode="numeric" maxLength={8} {...register("twoFactorCode")} /></label></details>
        {error && <p className="border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">{error}</p>}
        <button className="command-button login-submit" disabled={isSubmitting} type="submit"><KeyRound size={21} aria-hidden="true" />{isSubmitting ? "Signing in..." : "Sign in"}</button>
      </form>
      <div className="login-links"><Link href="/account/password">Reset password</Link><Link href="/account/mfa">Set up MFA</Link></div>
    </section>
  </main>;
}
