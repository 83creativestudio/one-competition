"use client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, Plus, Trash2 } from "lucide-react";
import { useParams } from "next/navigation";
import { useState } from "react";
import {
  ErrorState,
  LoadingState,
  PageHeading,
  humanize,
} from "@/components/operations-ui";
import { apiFetch } from "@/lib/api";
import type { CompetitionField } from "@/lib/contracts";

export default function FieldsPage() {
  const id = useParams<{ id: string }>().id;
  const cache = useQueryClient();
  const [label, setLabel] = useState("");
  const [type, setType] = useState("Text");
  const query = useQuery({
    queryKey: ["fields", id],
    queryFn: () =>
      apiFetch<CompetitionField[]>(`api/competitions/${id}/fields`),
  });
  const refresh = () => cache.invalidateQueries({ queryKey: ["fields", id] });
  const create = useMutation({
    mutationFn: () =>
      apiFetch(`api/competitions/${id}/fields`, {
        method: "POST",
        body: JSON.stringify({
          fieldKey: label
            .toLowerCase()
            .trim()
            .replace(/[^a-z0-9]+/g, "_"),
          fieldType: type,
          label,
          isRequired: true,
          displayOrder: (query.data?.length ?? 0) + 1,
          validationJson: "{}",
          optionsJson: "[]",
          isSensitive: false,
          isSearchable: true,
          isExportable: true,
        }),
      }),
    onSuccess: () => {
      setLabel("");
      refresh();
    },
  });
  const remove = useMutation({
    mutationFn: (fieldId: string) =>
      apiFetch(`api/competitions/${id}/fields/${fieldId}`, {
        method: "DELETE",
      }),
    onSuccess: refresh,
  });
  const reorder = useMutation({
    mutationFn: (ids: string[]) =>
      apiFetch(`api/competitions/${id}/fields/reorder`, {
        method: "POST",
        body: JSON.stringify({ fieldIds: ids }),
      }),
    onSuccess: refresh,
  });
  function move(index: number, offset: number) {
    const values = [...(query.data ?? [])];
    const target = index + offset;
    if (target < 0 || target >= values.length) return;
    [values[index], values[target]] = [values[target], values[index]];
    reorder.mutate(values.map((x) => x.id));
  }
  return (
    <main className="page">
      <PageHeading
        title="Entry fields"
        description="Create and order the structured fields collected from participants."
      />
      <form
        className="panel mb-5 flex flex-wrap items-end gap-3 p-4"
        onSubmit={(e) => {
          e.preventDefault();
          create.mutate();
        }}
      >
        <label className="field min-w-64 flex-1">
          <span>Field label</span>
          <input
            required
            value={label}
            onChange={(e) => setLabel(e.target.value)}
          />
        </label>
        <label className="field w-48">
          <span>Type</span>
          <select value={type} onChange={(e) => setType(e.target.value)}>
            {[
              "Text",
              "Email",
              "Phone",
              "Number",
              "Date",
              "Checkbox",
              "Dropdown",
              "Consent",
            ].map((x) => (
              <option key={x}>{x}</option>
            ))}
          </select>
        </label>
        <button className="command-button">
          <Plus size={16} />
          Add field
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
                <th>Order</th>
                <th>Field</th>
                <th>Type</th>
                <th>Required</th>
                <th>Controls</th>
              </tr>
            </thead>
            <tbody>
              {query.data?.map((field, index) => (
                <tr key={field.id}>
                  <td>{index + 1}</td>
                  <td className="font-semibold">
                    {field.label}
                    <div className="text-xs font-normal text-muted">
                      {field.fieldKey}
                    </div>
                  </td>
                  <td>{humanize(field.fieldType)}</td>
                  <td>{field.isRequired ? "Yes" : "No"}</td>
                  <td>
                    <div className="flex gap-1">
                      <button
                        className="icon-button"
                        title="Move up"
                        onClick={() => move(index, -1)}
                      >
                        <ArrowUp size={15} />
                      </button>
                      <button
                        className="icon-button"
                        title="Move down"
                        onClick={() => move(index, 1)}
                      >
                        <ArrowDown size={15} />
                      </button>
                      <button
                        className="icon-button text-red-700"
                        title="Delete"
                        onClick={() => remove.mutate(field.id)}
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
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
