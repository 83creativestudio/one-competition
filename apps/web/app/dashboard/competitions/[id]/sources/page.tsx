"use client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { useParams } from "next/navigation";
import { useState } from "react";
import {
  ErrorState,
  LoadingState,
  PageHeading,
  StatusBadge,
} from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { CampaignSource } from "@/lib/contracts";
export default function SourcesPage() {
  const id = useParams<{ id: string }>().id;
  const cache = useQueryClient();
  const [name, setName] = useState("");
  const [type, setType] = useState("Website");
  const query = useQuery({
    queryKey: ["sources", id],
    queryFn: () => apiFetch<CampaignSource[]>(`api/competitions/${id}/sources`),
  });
  const create = useMutation({
    mutationFn: () =>
      apiFetch(`api/competitions/${id}/sources`, {
        method: "POST",
        body: JSON.stringify({
          name,
          sourceType: type,
          code: name
            .toUpperCase()
            .replace(/[^A-Z0-9]+/g, "_")
            .slice(0, 30),
          utmSource: null,
          utmMedium: null,
          utmCampaign: null,
          utmContent: null,
        }),
      }),
    onSuccess: () => {
      setName("");
      cache.invalidateQueries({ queryKey: ["sources", id] });
    },
  });
  return (
    <main className="page">
      <PageHeading
        title="Campaign sources"
        description="Create attribution channels for campaign links and QR redirects."
      />
      <form
        className="panel mb-5 flex flex-wrap items-end gap-3 p-4"
        onSubmit={(e) => {
          e.preventDefault();
          create.mutate();
        }}
      >
        <label className="field min-w-64 flex-1">
          <span>Name</span>
          <input
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </label>
        <label className="field w-48">
          <span>Type</span>
          <select value={type} onChange={(e) => setType(e.target.value)}>
            {[
              "Website",
              "Television",
              "Instagram",
              "Facebook",
              "Email",
              "Poster",
              "Flyer",
              "Packaging",
              "Event",
              "Store",
              "Custom",
            ].map((x) => (
              <option key={x}>{x}</option>
            ))}
          </select>
        </label>
        <button className="command-button">
          <Plus size={16} />
          Add source
        </button>
      </form>
      {query.isLoading ? (
        <LoadingState />
      ) : query.error ? (
        <ErrorState error={query.error} />
      ) : (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Type</th>
                <th>Code</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {query.data?.map((x) => (
                <tr key={x.id}>
                  <td className="font-semibold">{x.name}</td>
                  <td>{x.sourceType}</td>
                  <td className="font-mono text-xs">{x.code}</td>
                  <td>
                    <StatusBadge value={x.isActive ? "Active" : "Disabled"} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
