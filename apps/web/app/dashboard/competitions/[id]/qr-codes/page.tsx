"use client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ExternalLink, Plus } from "lucide-react";
import { useParams } from "next/navigation";
import { useState } from "react";
import {
  ErrorState,
  LoadingState,
  PageHeading,
  StatusBadge,
} from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { CampaignSource, QrCode } from "@/lib/contracts";
export default function QrCodesPage() {
  const id = useParams<{ id: string }>().id;
  const cache = useQueryClient();
  const [source, setSource] = useState("");
  const [destination, setDestination] = useState("");
  const sources = useQuery({
    queryKey: ["sources", id],
    queryFn: () => apiFetch<CampaignSource[]>(`api/competitions/${id}/sources`),
  });
  const query = useQuery({
    queryKey: ["qr", id],
    queryFn: () => apiFetch<QrCode[]>(`api/competitions/${id}/qr-codes`),
  });
  const create = useMutation({
    mutationFn: () =>
      apiFetch(`api/competitions/${id}/qr-codes`, {
        method: "POST",
        body: JSON.stringify({
          campaignSourceId: source,
          destinationUrl: destination,
          styleJson: null,
          expiresAt: null,
        }),
      }),
    onSuccess: () => {
      setDestination("");
      cache.invalidateQueries({ queryKey: ["qr", id] });
    },
  });
  return (
    <main className="page">
      <PageHeading
        title="QR codes"
        description="Create dynamic redirects that preserve source attribution and scan analytics."
      />
      <form
        className="panel mb-5 grid gap-3 p-4 sm:grid-cols-[220px_1fr_auto] sm:items-end"
        onSubmit={(e) => {
          e.preventDefault();
          create.mutate();
        }}
      >
        <label className="field">
          <span>Source</span>
          <select
            required
            value={source}
            onChange={(e) => setSource(e.target.value)}
          >
            <option value="">Select source</option>
            {sources.data?.map((x) => (
              <option value={x.id} key={x.id}>
                {x.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Destination URL</span>
          <input
            required
            type="url"
            value={destination}
            onChange={(e) => setDestination(e.target.value)}
          />
        </label>
        <button className="command-button">
          <Plus size={16} />
          Create QR
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
                <th>Short code</th>
                <th>Destination</th>
                <th>Status</th>
                <th>Scans</th>
                <th>Open</th>
              </tr>
            </thead>
            <tbody>
              {query.data?.map((x) => (
                <tr key={x.id}>
                  <td className="font-mono font-semibold">{x.shortCode}</td>
                  <td className="max-w-sm truncate">{x.destinationUrl}</td>
                  <td>
                    <StatusBadge value={x.status} />
                  </td>
                  <td>
                    {x.scanCount} / {x.uniqueScanCount} unique
                  </td>
                  <td>
                    <a
                      className="icon-button"
                      title="Open redirect"
                      href={`/q/${x.shortCode}`}
                      target="_blank"
                    >
                      <ExternalLink size={15} />
                    </a>
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
