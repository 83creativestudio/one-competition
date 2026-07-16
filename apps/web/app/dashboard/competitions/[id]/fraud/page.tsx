"use client";
import { useQuery } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import {
  ErrorState,
  LoadingState,
  PageHeading,
  StatusBadge,
} from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { Entry } from "@/lib/contracts";
export default function FraudPage() {
  const id = useParams<{ id: string }>().id;
  const query = useQuery({
    queryKey: ["entries", id],
    queryFn: () => apiFetch<Entry[]>(`api/competitions/${id}/entries`),
  });
  const risks =
    query.data?.filter(
      (x) =>
        x.riskScore > 0 || ["Medium", "High", "Blocked"].includes(x.riskLevel),
    ) ?? [];
  return (
    <main className="page">
      <PageHeading
        title="Fraud review"
        description="Entries with duplicate or elevated risk signals requiring attention."
      />
      {query.isLoading ? (
        <LoadingState />
      ) : query.error ? (
        <ErrorState error={query.error} />
      ) : (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Reference</th>
                <th>Status</th>
                <th>Eligibility</th>
                <th>Risk level</th>
                <th>Score</th>
              </tr>
            </thead>
            <tbody>
              {risks.map((x) => (
                <tr key={x.id}>
                  <td className="font-mono text-xs font-semibold">
                    {x.entryReference}
                  </td>
                  <td>
                    <StatusBadge value={x.status} />
                  </td>
                  <td>
                    <StatusBadge value={x.eligibilityStatus} />
                  </td>
                  <td>
                    <StatusBadge value={x.riskLevel} />
                  </td>
                  <td>{x.riskScore}</td>
                </tr>
              ))}
              {!risks.length && (
                <tr>
                  <td colSpan={5}>
                    <div className="empty-state">No elevated-risk entries.</div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
