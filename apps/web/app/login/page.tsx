"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowRight, LockKeyhole } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";

const schema = z.object({ email: z.string().email(), password: z.string().min(12) });
type Values = z.infer<typeof schema>;

export default function LoginPage() {
  const router = useRouter();
  const [error, setError] = useState("");
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { email: "admin@onecompetitions.local", password: "" } });

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

  return (
    <main className="grid min-h-screen place-items-center bg-canvas px-5 py-10">
      <section className="w-full max-w-sm border border-line bg-white p-7 shadow-sm">
        <div className="flex items-center gap-3 border-b border-line pb-5">
          <span className="grid h-10 w-10 place-items-center bg-ink text-white"><LockKeyhole size={18} /></span>
          <div><div className="text-lg font-semibold">ONE. Competitions</div><div className="text-xs text-muted">Operations console</div></div>
        </div>
        <form className="mt-6 space-y-4" onSubmit={handleSubmit(submit)}>
          <label className="field"><span>Email</span><input autoComplete="email" {...register("email")} /></label>
          {errors.email && <p className="field-error">Enter a valid email.</p>}
          <label className="field"><span>Password</span><input autoComplete="current-password" type="password" {...register("password")} /></label>
          {errors.password && <p className="field-error">Password must contain at least 12 characters.</p>}
          {error && <p className="border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">{error}</p>}
          <button className="command-button w-full justify-center" disabled={isSubmitting} type="submit">
            {isSubmitting ? "Signing in" : "Sign in"}<ArrowRight size={16} />
          </button>
        </form>
      </section>
    </main>
  );
}
