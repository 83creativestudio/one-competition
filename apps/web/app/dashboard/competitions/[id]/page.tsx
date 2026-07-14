import { SectionPage } from "./section-page";

export default function CompetitionDetailPage() {
  return <SectionPage title="Competition Overview" summary="Status, entry counts, publication state, and draw readiness." items={["Lifecycle", "Entries", "Draw readiness"]} />;
}
