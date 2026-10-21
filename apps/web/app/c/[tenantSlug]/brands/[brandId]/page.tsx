import { PublicBrandPage } from "@/components/public-brand";

export default async function Page({ params, searchParams }: { params: Promise<{ tenantSlug: string; brandId: string }>; searchParams: Promise<{ page?: string }> }) {
  const route = await params;
  const query = await searchParams;
  return <PublicBrandPage {...route} page={Number(query.page) || 1} />;
}
