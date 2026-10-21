"use client";

import Link from "next/link";
import { ShieldCheck, UserPlus } from "lucide-react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useState } from "react";
import { z } from "zod";
import { apiFetch } from "@/lib/api";

const schema = z.object({ displayName: z.string().trim().min(2).max(160), organisationName: z.string().trim().min(2).max(200),
  email: z.string().email().max(254), password: z.string().min(12).max(128).regex(/[A-Z]/).regex(/[a-z]/).regex(/[0-9]/).regex(/[^A-Za-z0-9]/) });
type Values = z.infer<typeof schema>;

export default function RegisterPage() {
  const [complete, setComplete] = useState(false);
  const [error, setError] = useState("");
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<Values>({ resolver: zodResolver(schema) });
  async function submit(values: Values) {
    setError("");
    try { await apiFetch("api/auth/register", { method: "POST", body: JSON.stringify(values) }); setComplete(true); }
    catch (failure) { setError(failure instanceof Error ? failure.message : "Registration failed. Please try again."); }
  }
  return <main className="login-shell"><section className="login-panel" aria-labelledby="register-title">
    <div className="login-symbol" aria-hidden="true"><ShieldCheck size={30} /></div><p className="login-eyebrow">Customer account</p>
    <h1 id="register-title" className="login-title">Create your account</h1>
    {complete ? <div role="status"><p className="mt-5">Check your email for a verification link. If you already have an account, sign in or reset your password.</p><Link className="command-button mt-6" href="/login">Back to sign in</Link><Link className="mt-4 block text-sm text-brand" href="/account/verify-email">Verify email or request a new link</Link></div> : <>
      <form className="login-form" onSubmit={handleSubmit(submit)} noValidate>
        <label className="field"><span>Full name</span><input autoComplete="name" aria-invalid={!!errors.displayName} {...register("displayName")} />{errors.displayName && <small className="text-red-700">Enter your full name (2-160 characters).</small>}</label>
        <label className="field"><span>Organisation</span><input autoComplete="organization" aria-invalid={!!errors.organisationName} {...register("organisationName")} />{errors.organisationName && <small className="text-red-700">Enter an organisation name (2-200 characters).</small>}</label>
        <label className="field"><span>Email</span><input autoComplete="email" type="email" aria-invalid={!!errors.email} {...register("email")} />{errors.email && <small className="text-red-700">Enter a valid email.</small>}</label>
        <label className="field"><span>Password</span><input autoComplete="new-password" type="password" aria-invalid={!!errors.password} {...register("password")} /><small className={errors.password ? "text-red-700" : "text-muted"}>At least 12 characters, with uppercase, lowercase, a number and a symbol.</small></label>
        {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
        <button className="command-button login-submit" disabled={isSubmitting}><UserPlus size={20} />{isSubmitting ? "Creating account..." : "Create account"}</button>
      </form><div className="login-links"><Link href="/login">Already registered? Sign in</Link></div>
    </>}
  </section></main>;
}
