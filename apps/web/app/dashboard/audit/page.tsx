import { AuditTable } from "@/components/audit-table";
import { PageHeading } from "@/components/operations-ui";
export default function AuditPage() { return <main className="page"><PageHeading title="Audit" description="Append-only tenant activity and security events." /><AuditTable endpoint="api/audit" queryKey="tenant-audit" /></main>; }
