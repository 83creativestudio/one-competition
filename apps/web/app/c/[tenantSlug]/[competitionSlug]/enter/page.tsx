import { PublicEntryForm } from "@/components/public-entry-form";
export default async function PlatformPathEnterPage({ params }: { params: Promise<{ tenantSlug: string; competitionSlug: string }> }) { const { tenantSlug, competitionSlug } = await params; return <PublicEntryForm tenantSlug={tenantSlug} competitionSlug={competitionSlug} />; }
