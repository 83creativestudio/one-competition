"use client";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Monitor, Save, Smartphone } from "lucide-react";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { PageHeading } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Competition } from "@/lib/contracts";
type PublicCampaign = {
  page?: {
    languageCode: string;
    title: string;
    seoTitle?: string;
    seoDescription?: string;
    layoutJson: string;
    publishedVersion: number;
  };
};
const initial = JSON.stringify(
  {
    schemaVersion: 1,
    blocks: [
      {
        id: "hero-1",
        type: "hero",
        settings: {
          headline: "Win the featured prize",
          subheading: "Enter before the competition closes",
        },
      },
      { id: "entry-1", type: "entryForm", settings: {} },
    ],
  },
  null,
  2,
);
export default function BuilderPage() {
  const id = useParams<{ id: string }>().id;
  const [language, setLanguage] = useState("en");
  const [title, setTitle] = useState("Competition page");
  const [layout, setLayout] = useState(initial);
  const [mobile, setMobile] = useState(false);
  const competition = useQuery({
    queryKey: ["competition", id],
    queryFn: () => apiFetch<Competition>(`api/competitions/${id}`),
  });
  const campaign = useQuery({
    queryKey: ["public-campaign", competition.data?.slug],
    enabled: !!competition.data?.slug,
    queryFn: () =>
      apiFetch<PublicCampaign>(
        `api/public/competitions/${competition.data!.slug}`,
      ),
  });
  useEffect(() => {
    if (campaign.data?.page) {
      setLanguage(campaign.data.page.languageCode);
      setTitle(campaign.data.page.title);
      setLayout(campaign.data.page.layoutJson);
    }
  }, [campaign.data]);
  const save = useMutation({
    mutationFn: () => {
      JSON.parse(layout);
      return apiFetch(`api/competitions/${id}/page`, {
        method: "PUT",
        body: JSON.stringify({
          languageCode: language,
          title,
          seoTitle: title,
          seoDescription: null,
          layoutJson: layout,
        }),
      });
    },
  });
  let preview: unknown = {};
  try {
    preview = JSON.parse(layout);
  } catch {
    preview = {};
  }
  const blocks =
    typeof preview === "object" &&
    preview &&
    "blocks" in preview &&
    Array.isArray((preview as { blocks: unknown[] }).blocks)
      ? (
          preview as {
            blocks: Array<{
              id?: string;
              type?: string;
              settings?: Record<string, string>;
            }>;
          }
        ).blocks
      : [];
  return (
    <main className="page">
      <PageHeading
        title="Page builder"
        description="Edit the versioned block schema and preview its content before publication."
        action={
          <div className="flex gap-1">
            <button
              className={`icon-button ${!mobile ? "bg-neutral-100" : ""}`}
              title="Desktop preview"
              onClick={() => setMobile(false)}
            >
              <Monitor size={16} />
            </button>
            <button
              className={`icon-button ${mobile ? "bg-neutral-100" : ""}`}
              title="Mobile preview"
              onClick={() => setMobile(true)}
            >
              <Smartphone size={16} />
            </button>
          </div>
        }
      />
      <div className="grid gap-5 lg:grid-cols-2">
        <form
          className="panel grid gap-4 p-5"
          onSubmit={(e) => {
            e.preventDefault();
            save.mutate();
          }}
        >
          <div className="grid grid-cols-[120px_1fr] gap-3">
            <label className="field">
              <span>Language</span>
              <select
                value={language}
                onChange={(e) => setLanguage(e.target.value)}
              >
                <option value="en">English</option>
                <option value="el">Greek</option>
              </select>
            </label>
            <label className="field">
              <span>Page title</span>
              <input value={title} onChange={(e) => setTitle(e.target.value)} />
            </label>
          </div>
          <label className="field">
            <span>Layout JSON</span>
            <textarea
              className="!min-h-[480px] font-mono text-xs"
              value={layout}
              onChange={(e) => setLayout(e.target.value)}
            />
          </label>
          {save.error && (
            <p className="text-sm text-red-700">{save.error.message}</p>
          )}
          <button className="command-button w-fit">
            <Save size={16} />
            Save draft
          </button>
        </form>
        <section
          className={`panel mx-auto w-full self-start overflow-hidden transition-all ${mobile ? "max-w-[390px]" : "max-w-full"}`}
        >
          <div className="panel-header">
            <h2 className="font-semibold">Preview</h2>
          </div>
          <div className="min-h-[560px] bg-white p-6">
            {blocks.map((block, index) => (
              <div
                className="border-b border-line py-5"
                key={block.id ?? index}
              >
                <div className="text-xs font-semibold uppercase text-emerald-800">
                  {block.type}
                </div>
                {block.settings?.headline && (
                  <h3 className="mt-2 text-2xl font-semibold">
                    {block.settings.headline}
                  </h3>
                )}
                {block.settings?.subheading && (
                  <p className="mt-2 text-sm text-muted">
                    {block.settings.subheading}
                  </p>
                )}
                {block.type?.toLowerCase() === "entryform" && (
                  <button className="command-button mt-3">
                    Enter competition
                  </button>
                )}
              </div>
            ))}
            {!blocks.length && (
              <div className="empty-state">
                Enter valid block JSON to preview.
              </div>
            )}
          </div>
        </section>
      </div>
    </main>
  );
}
