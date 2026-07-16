import { PublicCampaign } from "@/components/public-campaign";
export default async function PublicCompetitionPage({ params }: { params: Promise<{ tenantSlug: string; competitionSlug: string }> }) { const { tenantSlug, competitionSlug } = await params; return <PublicCampaign tenantSlug={tenantSlug} competitionSlug={competitionSlug} />; }
