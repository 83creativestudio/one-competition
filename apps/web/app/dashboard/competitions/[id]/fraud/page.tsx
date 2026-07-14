import { SectionPage } from "../section-page";

export default function FraudPage() {
  return <SectionPage title="Fraud Review" summary="Rules-based risk signals and manual review decisions." items={["Duplicate email", "Duplicate phone", "High risk"]} />;
}
