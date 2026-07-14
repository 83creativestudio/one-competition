import { SectionPage } from "../section-page";

export default function EntriesPage() {
  return <SectionPage title="Entries" summary="Participant registrations, duplicate flags, review status, and public references." items={["Approved", "Under review", "Rejected"]} />;
}
