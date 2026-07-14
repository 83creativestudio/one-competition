import { SectionPage } from "../section-page";

export default function ExportsPage() {
  return <SectionPage title="Exports" summary="CSV export jobs with expiry and audit records." items={["Entries CSV", "Winners CSV", "Audit CSV"]} />;
}
