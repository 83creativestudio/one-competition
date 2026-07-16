import { PublicCampaign } from "@/components/public-campaign";
export default async function PublicCompetitionPage({ params }: { params: Promise<{ competitionSlug: string }> }) { const { competitionSlug } = await params; return <PublicCampaign competitionSlug={competitionSlug} />; }
