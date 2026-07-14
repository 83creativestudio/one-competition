import { SectionPage } from "../section-page";

export default function FieldsPage() {
  return <SectionPage title="Entry Fields" summary="Dynamic form fields with ordering, validation, sensitivity, and export controls." items={["Email", "Phone", "Consent"]} />;
}
