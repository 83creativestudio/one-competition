import Image from "next/image";
import Link from "next/link";
import { Gift } from "lucide-react";
import type { PublicBrandIdentity } from "@/lib/contracts";

export function PublicBrandHeader({ brand, tenantSlug, href }: { brand: PublicBrandIdentity; tenantSlug: string; href: string }) {
  return <header className="brand-public-masthead" style={{ borderBottom: `4px solid ${brand.primaryColor}` }}>
    <div className="brand-public-container brand-public-header">
      <Link href={href} className="brand-public-logo-link" aria-label={`${brand.name} competitions home`}>
        {brand.logoAssetId ? <Image priority unoptimized src={`/media/${encodeURIComponent(tenantSlug)}/${brand.logoAssetId}`} alt={brand.name} width={350} height={100} className="brand-public-logo" />
          : <span className="brand-public-wordmark">{brand.name}</span>}
      </Link>
      <Link href={href} className="brand-public-header-link" aria-label={`All ${brand.name} competitions`}><Gift size={17} strokeWidth={1.7} /><span>Competitions &amp; giveaways</span></Link>
    </div>
  </header>;
}
