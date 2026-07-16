"use client";

import { AlertCircle, LoaderCircle } from "lucide-react";

export function PageHeading({ title, description, action }: { title: string; description?: string; action?: React.ReactNode }) {
  return <header className="page-header"><div><h1 className="page-title">{title}</h1>{description && <p className="mt-2 max-w-3xl text-sm leading-6 text-muted">{description}</p>}</div>{action}</header>;
}

export function LoadingState({ label = "Loading data" }: { label?: string }) {
  return <div className="empty-state"><div><LoaderCircle className="mx-auto mb-3 animate-spin" size={20} /><span>{label}</span></div></div>;
}

export function ErrorState({ error }: { error: unknown }) {
  return <div className="empty-state text-red-700"><div><AlertCircle className="mx-auto mb-3" size={20} /><span>{error instanceof Error ? error.message : "The request failed."}</span></div></div>;
}

export function StatusBadge({ value }: { value: string }) {
  const normalized = value.toLowerCase();
  const style = ["active", "live", "approved", "completed", "verified", "eligible", "prizedelivered"].some(x => normalized.includes(x))
    ? "status-active"
    : ["error", "rejected", "failed", "blocked", "disqualified", "cancelled"].some(x => normalized.includes(x))
      ? "status-danger"
      : ["pending", "awaiting", "draft", "review", "preparing", "selected"].some(x => normalized.includes(x)) ? "status-warning" : "status-neutral";
  return <span className={`status ${style}`}>{humanize(value)}</span>;
}

export function humanize(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/[-_]/g, " ");
}

export function formatDate(value?: string) {
  if (!value) return "Not yet";
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}
