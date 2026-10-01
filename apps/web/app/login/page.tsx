"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowRight, LockKeyhole } from "lucide-react";
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
    const response = await fetch("/api/session/login", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(values) });
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string } | null;
      setError(problem?.detail ?? "Login failed.");
      return;
    }
    const returnTo = new URLSearchParams(window.location.search).get("returnTo");
    router.replace(returnTo?.startsWith("/") ? returnTo : "/dashboard");
    router.refresh();
  }

  return <main className="login-shell">
    <section className="login-panel">
      <div className="login-brand"><span className="brand-mark" aria-hidden="true">1</span><span className="brand-wordmark"><strong>ONE.</strong><small>Competitions</small></span></div>
      <div className="login-heading"><span className="login-lock"><LockKeyhole size={18} /></span><h1>Sign in</h1></div>
      <form className="mt-7 grid gap-5" onSubmit={handleSubmit(submit)}>
        <div><label className="field"><span>Email address</span><input autoComplete="email" type="email" {...register("email")} /></label>{errors.email && <p className="field-error mt-2">Enter a valid email.</p>}</div>
        <div><label className="field"><span>Password</span><input autoComplete="current-password" type="password" {...register("password")} /></label>{errors.password && <p className="field-error mt-2">Password must contain at least 12 characters.</p>}</div>
        <div><label className="field"><span>Authenticator code <small className="text-muted">(if enabled)</small></span><input autoComplete="one-time-code" inputMode="numeric" maxLength={8} {...register("twoFactorCode")} /></label></div>
        {error && <p className="border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">{error}</p>}
        <button className="command-button mt-1 w-full justify-center" disabled={isSubmitting} type="submit">{isSubmitting ? "Signing in" : "Sign in"}<ArrowRight size={17} /></button>
      </form>
      <div className="login-links"><Link href="/account/password">Reset password</Link><Link href="/account/mfa">Set up MFA</Link></div>
    </section>
  </main>;
}
