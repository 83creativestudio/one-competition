"use client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Save } from "lucide-react";
import { useParams } from "next/navigation";
import { useState } from "react";
import { PageHeading } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { ConsentDefinition } from "@/lib/contracts";
export default function RulesPage() {
  const id = useParams<{ id: string }>().id;
  const cache = useQueryClient();
  const [language, setLanguage] = useState("en");
  const [title, setTitle] = useState("Competition rules");
  const [content, setContent] = useState("");
  const [consent, setConsent] = useState({ consentType: "CompetitionTerms", languageCode: "en", text: "", isRequired: true });
  const consentQuery = useQuery({ queryKey: ["consents", id], queryFn: () => apiFetch<ConsentDefinition[]>(`api/competitions/${id}/consents`) });
  const addConsent = useMutation({ mutationFn: () => apiFetch<ConsentDefinition>(`api/competitions/${id}/consents`, { method: "POST", body: JSON.stringify(consent) }), onSuccess: () => { setConsent({ ...consent, text: "" }); cache.invalidateQueries({ queryKey: ["consents", id] }); } });
  const save = useMutation({
    mutationFn: () =>
      apiFetch<{ versionNumber: number; contentHash: string }>(
        `api/competitions/${id}/rules`,
        {
          method: "POST",
          body: JSON.stringify({
            languageCode: language,
            title,
            content,
            effectiveAt: null,
          }),
        },
      ),
  });
  return (
    <main className="page max-w-4xl">
      <PageHeading
        title="Rules"
        description="Create an immutable language-specific rules version. Published versions are never overwritten."
      />
      <form
        className="panel grid gap-4 p-5"
        onSubmit={(e) => {
          e.preventDefault();
          save.mutate();
        }}
      >
        <div className="grid gap-4 sm:grid-cols-[140px_1fr]">
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
            <span>Title</span>
            <input
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
            />
          </label>
        </div>
        <label className="field">
          <span>Rules content</span>
          <textarea
            className="!min-h-80"
            required
            value={content}
            onChange={(e) => setContent(e.target.value)}
          />
        </label>
        {save.data && (
          <p className="text-sm text-emerald-800">
            Version {save.data.versionNumber} saved. Hash:{" "}
            <code>{save.data.contentHash}</code>
          </p>
        )}
        {save.error && (
          <p className="text-sm text-red-700">{save.error.message}</p>
        )}
        <button className="command-button w-fit">
          <Save size={16} />
          Save new version
        </button>
      </form>
      <section className="mt-6 border-t border-line pt-6"><h2 className="text-lg font-semibold">Consent versions</h2><p className="mt-2 text-sm text-muted">Competition participation and optional marketing consent are recorded separately.</p><form className="panel mt-4 grid gap-4 p-5 sm:grid-cols-2" onSubmit={e => { e.preventDefault(); addConsent.mutate(); }}><label className="field"><span>Consent type</span><select value={consent.consentType} onChange={e => setConsent({ ...consent, consentType: e.target.value, isRequired: !e.target.value.startsWith("Marketing") })}><option>CompetitionTerms</option><option>PrivacyNotice</option><option>MarketingEmail</option><option>MarketingSms</option><option>MarketingPhone</option><option>MarketingProfiling</option><option>ContentUsageRights</option><option>PublicWinnerAnnouncement</option></select></label><label className="field"><span>Language</span><select value={consent.languageCode} onChange={e => setConsent({ ...consent, languageCode: e.target.value })}><option value="en">English</option><option value="el">Greek</option></select></label><label className="field sm:col-span-2"><span>Consent text</span><textarea required value={consent.text} onChange={e => setConsent({ ...consent, text: e.target.value })} /></label><label className="flex items-center gap-2 text-sm"><input disabled={consent.consentType.startsWith("Marketing")} type="checkbox" checked={consent.isRequired} onChange={e => setConsent({ ...consent, isRequired: e.target.checked })} />Required for entry</label><button className="command-button w-fit"><Plus size={16} />Create version</button></form><div className="table-wrap mt-4"><table className="data-table"><thead><tr><th>Type</th><th>Language</th><th>Version</th><th>Required</th><th>Text</th></tr></thead><tbody>{consentQuery.data?.map(x => <tr key={x.id}><td>{x.consentType}</td><td>{x.languageCode}</td><td>{x.version}</td><td>{x.isRequired ? "Yes" : "No"}</td><td>{x.text}</td></tr>)}</tbody></table></div></section>
    </main>
  );
}
