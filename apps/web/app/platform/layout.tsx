import { redirect } from "next/navigation";
import { DashboardShell } from "@/components/dashboard-shell";
import { getSessionProfile } from "@/lib/server-auth";

export default async function PlatformLayout({ children }: { children: React.ReactNode }) {
  const profile = await getSessionProfile();
  if (!profile) redirect("/login");
  return <DashboardShell email={profile.email} platform>{children}</DashboardShell>;
}
