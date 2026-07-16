import { PrivacyRequestForm } from "@/components/privacy-request-form";
export default async function PrivacyPage({ params }: { params: Promise<{ tenantSlug: string }> }) { return <PrivacyRequestForm tenantSlug={(await params).tenantSlug} />; }
