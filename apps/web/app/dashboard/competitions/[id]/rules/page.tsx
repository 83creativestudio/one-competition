"use client";
import { useMutation } from "@tanstack/react-query";
import { Save } from "lucide-react";
import { useParams } from "next/navigation";
import { useState } from "react";
import { PageHeading } from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
export default function RulesPage() {
  const id = useParams<{ id: string }>().id;
  const [language, setLanguage] = useState("en");
  const [title, setTitle] = useState("Competition rules");
  const [content, setContent] = useState("");
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
    </main>
  );
}
