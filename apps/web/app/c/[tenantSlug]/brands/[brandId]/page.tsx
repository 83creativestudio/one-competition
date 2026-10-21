import { loadPublicBrand, PublicBrandPage } from "@/components/public-brand";

export async function generateMetadata({ params }: { params: Promise<{ tenantSlug: string; brandId: string }> }) {
  const { tenantSlug, brandId } = await params;
  const data = await loadPublicBrand(brandId, tenantSlug);
  return { title: `${data.brand.name} | Competitions & Giveaways`,
    description: `Discover competitions, giveaways and prizes from ${data.brand.name}.` };
}

export default async function Page({ params, searchParams }: { params: Promise<{ tenantSlug: string; brandId: string }>; searchParams: Promise<{ page?: string }> }) {
  const route = await params;
  const query = await searchParams;
  return <PublicBrandPage {...route} page={Number(query.page) || 1} />;
}
