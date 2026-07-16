"use client";

import { BarChart3, Building2, CreditCard, FileClock, Globe2, LayoutDashboard, LogOut, Megaphone, Palette, Settings, ShieldCheck, Users, Webhook } from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";

const nav = [
  ["Overview", "/dashboard", LayoutDashboard], ["Competitions", "/dashboard/competitions", Megaphone],
  ["Domains", "/dashboard/domains", Globe2], ["Brands", "/dashboard/brands", Palette],
  ["Team", "/dashboard/team", Users], ["Integrations", "/dashboard/integrations", Webhook],
  ["Billing", "/dashboard/billing", CreditCard], ["Audit", "/dashboard/audit", FileClock],
  ["Settings", "/dashboard/settings", Settings]
] as const;

export function DashboardShell({ children, email, platform }: { children: React.ReactNode; email: string; platform?: boolean }) {
  const pathname = usePathname();
  const router = useRouter();
  async function logout() { await fetch("/api/session/logout", { method: "POST" }); router.replace("/login"); router.refresh(); }
  const items = platform ? [["Platform", "/platform", ShieldCheck], ["Tenants", "/platform/tenants", Building2], ["Plans", "/platform/plans", CreditCard], ["System health", "/platform/system-health", BarChart3]] as const : nav;
  return (
    <div className="min-h-screen bg-canvas lg:grid lg:grid-cols-[236px_1fr]">
      <aside className="border-b border-line bg-ink text-white lg:min-h-screen lg:border-b-0 lg:border-r lg:border-neutral-800">
        <div className="flex h-16 items-center justify-between px-5 lg:border-b lg:border-neutral-800"><Link className="font-semibold" href={platform ? "/platform" : "/dashboard"}>ONE. Competitions</Link></div>
        <nav className="flex gap-1 overflow-x-auto px-3 pb-3 lg:block lg:space-y-1 lg:py-4" aria-label="Primary">
          {items.map(([label, href, Icon]) => {
            const active = pathname === href || (href !== "/dashboard" && href !== "/platform" && pathname.startsWith(`${href}/`));
            return <Link className={`nav-item ${active ? "nav-item-active" : ""}`} href={href} key={href}><Icon size={17} /><span>{label}</span></Link>;
          })}
        </nav>
        <div className="hidden px-4 lg:fixed lg:bottom-5 lg:block lg:w-[236px]">
          <div className="truncate text-xs text-neutral-400">{email}</div>
          <button className="mt-3 flex items-center gap-2 text-sm text-neutral-300 hover:text-white" onClick={logout}><LogOut size={15} />Sign out</button>
        </div>
      </aside>
      <div className="min-w-0">{children}</div>
    </div>
  );
}
