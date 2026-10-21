# Brands and Competition Media

Administrators select a client workspace in Dashboard > Brands. Brand profiles remain tenant-specific; customers can manage brands only within their own workspace. Each profile supports a JPG, PNG or WebP logo, colours, fonts, support details and a public competition directory.

Create a brand, upload its logo, and save it. The public brand page is available at `/c/{tenantSlug}/brands/{brandId}`, or `/brands/{brandId}` on a resolved tenant domain. It lists published competitions in pages of 24 and excludes unpublished, cancelled and archived competitions. The dashboard also lists draft competitions assigned to the selected brand.

Competition > Content and media supports brand assignment, English/Greek content, text, headline/image blocks, image uploads, YouTube URLs and uploaded MP4 videos. Blocks can be reordered, hidden and removed. The editor provides mobile/desktop previews and separate Save draft and Publish actions.

Uploads use the existing storage provider, MIME/extension checks, content-signature validation and scanner interface. Files are limited to 25 MB. Logos must be clean shared image assets within the same tenant. Competition media must be clean assets of the same tenant and competition; participant uploads cannot be published through these endpoints. Real malware scanning still requires a production scanner configuration.

`PublishedContentJson` stores the published page independently of edits. Saving a draft retains the previous public page. Publishing snapshots the edited content and records it in competition configuration history. Republishing a closed competition does not reopen it. Brand reassignment is separately audited.

Public media is served only when referenced by an active brand logo or a published competition page. Removing a media reference from the next published version withdraws that public access. Browser/proxy caches can retain a previous response for up to 60 seconds. MP4 playback supports HTTP range requests. YouTube embeds use the privacy-enhanced player; third-party video availability and playback restrictions are controlled by YouTube.

API additions:

- `GET /api/competitions/{id}/page?language=en`: authenticated draft page.
- `PUT /api/competitions/{id}/brand`: assign or clear a same-tenant brand.
- Brand create/update accepts optional `logoAssetId`.
- `GET /c/{tenantSlug}/api/public/brands/{brandId}?page=1`: public brand identity and competitions.
- `GET /c/{tenantSlug}/api/public/media/{assetId}`: published media stream.
- Equivalent public APIs without the `/c/{tenantSlug}` prefix work on resolved tenant hosts.

The database migration `AddPublishedCompetitionContent` adds a nullable snapshot column. Existing published pages remain readable, and the first subsequent edit preserves their current content as the published snapshot.

Retention cleanup preserves active brand logos and media referenced by either draft or published competition pages. Unreferenced expired uploads remain eligible for cleanup.
