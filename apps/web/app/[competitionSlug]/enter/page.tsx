import { PublicEntryForm } from "@/components/public-entry-form";
export default async function PublicEnterPage({ params }: { params: Promise<{ competitionSlug: string }> }) { const { competitionSlug } = await params; return <PublicEntryForm competitionSlug={competitionSlug} />; }
