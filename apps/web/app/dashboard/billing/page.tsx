"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { Check, CreditCard } from "lucide-react";
import { ErrorState, LoadingState, PageHeading, StatusBadge, humanize } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { BillingOverview, Plan } from "@/lib/contracts";

export default function BillingPage() {
  const query = useQuery({ queryKey: ["billing"], queryFn: () => apiFetch<BillingOverview>("api/billing") });
  const checkout = useMutation({ mutationFn: (plan: Plan) => apiFetch<{ checkoutUrl: string }>("api/billing/checkout", { method: "POST", body: JSON.stringify({ planId: plan.id, billingPeriod: "monthly", successUrl: `${location.origin}/dashboard/billing?checkout=success`, cancelUrl: `${location.origin}/dashboard/billing?checkout=cancelled` }) }), onSuccess: data => location.assign(data.checkoutUrl) });
  return <main className="page"><PageHeading title="Billing" description="Subscription state and server-enforced plan limits." />{query.isLoading ? <LoadingState /> : query.error ? <ErrorState error={query.error} /> : <><section className="panel mb-5 p-5"><div className="text-xs font-semibold uppercase text-muted">Current subscription</div><div className="mt-3 flex items-center gap-3 text-xl font-semibold">{query.data?.subscription ? <><StatusBadge value={query.data.subscription.status} /><span>Renews {new Date(query.data.subscription.currentPeriodEndsAt).toLocaleDateString()}</span></> : "No paid subscription"}</div></section><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{query.data?.plans.map(plan => <article className="panel p-5" key={plan.id}><h2 className="text-lg font-semibold">{plan.name}</h2><div className="mt-3 text-3xl font-semibold">€{plan.monthlyPrice}<span className="text-sm font-normal text-muted"> / month</span></div><ul className="mt-5 space-y-2 text-sm">{Object.entries(plan.features).slice(0, 8).map(([key, value]) => <li className="flex gap-2" key={key}><Check className="mt-0.5 text-emerald-700" size={15} /><span>{humanize(key)}: {String(value)}</span></li>)}</ul><button className="command-button mt-5" disabled={checkout.isPending || query.data?.subscription?.planId === plan.id} onClick={() => checkout.mutate(plan)}><CreditCard size={17} />{query.data?.subscription?.planId === plan.id ? "Current plan" : "Choose plan"}</button></article>)}</div></>}</main>;
}
