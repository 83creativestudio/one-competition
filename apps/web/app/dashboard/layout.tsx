import { redirect } from "next/navigation";
import { DashboardShell } from "@/components/dashboard-shell";
import { getSessionProfile } from "@/lib/server-auth";

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  const profile = await getSessionProfile();
  if (!profile) redirect("/login");
  const canAccessPlatform = profile.roles.some(role => role === "PlatformOwner" || role === "PlatformAdministrator");
  return <DashboardShell email={profile.email} canAccessPlatform={canAccessPlatform}>{children}</DashboardShell>;
}
