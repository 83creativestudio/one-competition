"use client";
import { useQuery } from "@tanstack/react-query";
import {
  ErrorState,
  formatDate,
  LoadingState,
  PageHeading,
  StatusBadge,
} from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { TenantUser } from "@/lib/contracts";
export default function TeamPage() {
  const query = useQuery({
    queryKey: ["team"],
    queryFn: () => apiFetch<TenantUser[]>("api/tenants/current/users"),
  });
  return (
    <main className="page">
      <PageHeading
        title="Team"
        description="Active tenant memberships and assigned roles."
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
                <th>User</th>
                <th>Role</th>
                <th>Status</th>
                <th>Joined</th>
              </tr>
            </thead>
            <tbody>
              {query.data?.map((x) => (
                <tr key={x.id}>
                  <td className="font-semibold">{x.email}</td>
                  <td>{x.role}</td>
                  <td>
                    <StatusBadge value={x.status} />
                  </td>
                  <td>{formatDate(x.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
