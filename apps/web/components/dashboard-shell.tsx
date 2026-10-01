"use client";

import {
  Building2, ChevronDown, CreditCard, FileClock, Globe2, HeartPulse,
  LayoutDashboard, LogOut, Megaphone, Menu, PanelLeftClose, PanelLeftOpen, Palette,
  Search, Settings, ShieldCheck, SlidersHorizontal, Users, Webhook, X
} from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";

const workspaceGroups = [
  { label: "WORKSPACE", items: [
    { label: "Overview", href: "/dashboard", icon: LayoutDashboard },
    { label: "Competitions", href: "/dashboard/competitions", icon: Megaphone },
    { label: "Domains", href: "/dashboard/domains", icon: Globe2 },
    { label: "Brands", href: "/dashboard/brands", icon: Palette }
  ] },
  { label: "MANAGEMENT", items: [
    { label: "Team", href: "/dashboard/team", icon: Users },
    { label: "Integrations", href: "/dashboard/integrations", icon: Webhook },
    { label: "Billing", href: "/dashboard/billing", icon: CreditCard },
    { label: "Audit", href: "/dashboard/audit", icon: FileClock },
    { label: "Settings", href: "/dashboard/settings", icon: Settings }
  ] }
];

const platformGroups = [
  { label: "PLATFORM", items: [
    { label: "Overview", href: "/platform", icon: LayoutDashboard },
    { label: "Tenants", href: "/platform/tenants", icon: Building2 },
    { label: "Domains", href: "/platform/domains", icon: Globe2 },
    { label: "Plans", href: "/platform/plans", icon: CreditCard },
    { label: "Feature flags", href: "/platform/features", icon: SlidersHorizontal }
  ] },
  { label: "OPERATIONS", items: [
    { label: "Resellers", href: "/platform/resellers", icon: Users },
    { label: "System health", href: "/platform/system-health", icon: HeartPulse },
    { label: "Audit", href: "/platform/audit", icon: FileClock }
  ] }
];

export function DashboardShell({ children, email, platform, canAccessPlatform }: {
  children: React.ReactNode;
  email: string;
  platform?: boolean;
  canAccessPlatform?: boolean;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [accountOpen, setAccountOpen] = useState(false);
  const groups = platform ? platformGroups : workspaceGroups;
  const items = groups.flatMap(group => group.items);
  const current = [...items].reverse().find(item => pathname === item.href || pathname.startsWith(`${item.href}/`));
  const matches = items.filter(item => item.label.toLowerCase().includes(search.toLowerCase()));

  useEffect(() => {
    setCollapsed(window.localStorage.getItem("one-sidebar-collapsed") === "true");
  }, []);

  async function logout() {
    await fetch("/api/session/logout", { method: "POST" });
    router.replace("/login");
    router.refresh();
  }

  function toggleSidebar() {
    if (window.matchMedia("(max-width: 1023px)").matches) {
      setMobileOpen(value => !value);
      return;
    }
    setCollapsed(value => {
      window.localStorage.setItem("one-sidebar-collapsed", String(!value));
      return !value;
    });
  }

  return (
    <div className={`app-shell${collapsed ? " app-shell-collapsed" : ""}`}>
      {mobileOpen && <button className="sidebar-backdrop" aria-label="Close navigation" onClick={() => setMobileOpen(false)} />}
      <aside className={`app-sidebar${mobileOpen ? " app-sidebar-open" : ""}`} aria-label="Main navigation">
        <div className="sidebar-brand">
          <Link href={platform ? "/platform" : "/dashboard"} className="brand-link" onClick={() => setMobileOpen(false)} aria-label="ONE. Competitions home">
            <span className="brand-mark" aria-hidden="true">1</span>
            <span className="brand-wordmark"><strong>ONE.</strong><small>Competitions</small></span>
          </Link>
          <button className="sidebar-mobile-close" type="button" aria-label="Close navigation" onClick={() => setMobileOpen(false)}><X size={20} /></button>
        </div>
        <div className="sidebar-nav-scroll">
          {groups.map(group => <div className="sidebar-group" key={group.label}>
            <div className="sidebar-group-label">{group.label}</div>
            <nav aria-label={group.label}>
              {group.items.map(item => {
                const Icon = item.icon;
                const active = pathname === item.href || (item.href !== "/dashboard" && item.href !== "/platform" && pathname.startsWith(`${item.href}/`));
                return <Link key={item.href} href={item.href} title={collapsed ? item.label : undefined} aria-current={active ? "page" : undefined}
                  className={`nav-item${active ? " nav-item-active" : ""}`} onClick={() => setMobileOpen(false)}>
                  <Icon size={19} strokeWidth={1.8} /><span>{item.label}</span>
                </Link>;
              })}
            </nav>
          </div>)}
        </div>
        <div className="sidebar-footer">
          {platform ? <Link className="sidebar-switch" href="/dashboard" title="Customer dashboard"><Megaphone size={17} /><span>Customer dashboard</span></Link>
            : canAccessPlatform && <Link className="sidebar-switch" href="/platform" title="Platform administration"><ShieldCheck size={17} /><span>Platform administration</span></Link>}
        </div>
      </aside>

      <div className="app-main">
        <header className="app-topbar">
          <div className="topbar-leading">
            <button className="topbar-icon" type="button" onClick={toggleSidebar} aria-label={collapsed ? "Expand navigation" : "Toggle navigation"} title="Toggle navigation">
              {collapsed ? <PanelLeftOpen size={21} /> : <PanelLeftClose className="hidden lg:block" size={21} />}
              {!collapsed && <Menu className="lg:hidden" size={21} />}
            </button>
            <span className="topbar-divider" />
            <div className="topbar-location"><span>{platform ? "Platform" : "Workspace"}</span><span className="topbar-slash">/</span><strong>{current?.label ?? "Overview"}</strong></div>
          </div>
          <div className="topbar-actions">
            <div className="topbar-search-wrap">
              <button className="topbar-search-button" type="button" onClick={() => { setSearchOpen(value => !value); setAccountOpen(false); }} aria-label="Search navigation" aria-expanded={searchOpen}>
                <Search size={18} /><span>Search</span>
              </button>
              {searchOpen && <div className="topbar-popover search-popover">
                <label className="search-field"><Search size={18} /><input autoFocus value={search} onChange={event => setSearch(event.target.value)} placeholder="Find a page" aria-label="Find a page" /></label>
                <div className="search-results">{matches.map(item => {
                  const Icon = item.icon;
                  return <Link href={item.href} key={item.href} onClick={() => { setSearchOpen(false); setSearch(""); }}><Icon size={17} />{item.label}</Link>;
                })}{matches.length === 0 && <span className="search-empty">No matching pages</span>}</div>
              </div>}
            </div>
            <div className="topbar-account-wrap">
              <button className="account-button" type="button" aria-label="Account menu" aria-expanded={accountOpen} onClick={() => { setAccountOpen(value => !value); setSearchOpen(false); }}>
                <span className="account-avatar">{email.slice(0, 2).toUpperCase()}</span><span className="account-name">{email}</span><ChevronDown size={15} />
              </button>
              {accountOpen && <div className="topbar-popover account-popover">
                <div className="account-popover-heading"><strong>Signed in</strong><span>{email}</span></div>
                <Link href={platform ? "/platform" : "/dashboard/settings"} onClick={() => setAccountOpen(false)}><Settings size={17} />{platform ? "Platform overview" : "Settings"}</Link>
                <button type="button" onClick={logout}><LogOut size={17} />Sign out</button>
              </div>}
            </div>
          </div>
        </header>
        <div className="app-page">{children}</div>
      </div>
    </div>
  );
}
