import { AuditTable } from "@/components/audit-table";
import { PageHeading } from "@/components/operations-ui";
export default function PlatformAuditPage() { return <main className="page"><PageHeading title="Platform audit" description="Append-only activity across every tenant." /><AuditTable endpoint="api/platform/audit" queryKey="platform-audit" /></main>; }
