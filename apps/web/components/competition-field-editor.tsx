"use client";

import { useMutation } from "@tanstack/react-query";
import { Save, X } from "lucide-react";
import { useState } from "react";
import { apiFetch } from "@/lib/api";
import type { CompetitionField } from "@/lib/contracts";
import { ErrorState } from "@/components/operations-ui";

const fieldTypes = ["Text", "Textarea", "Email", "Phone", "Number", "Date", "DateOfBirth", "Country", "City", "Postcode", "Dropdown", "Radio", "Checkbox", "Consent", "FileUpload", "ImageUpload", "ReceiptUpload", "PromotionalCode", "SocialUsername", "Hidden"];

export function CompetitionFieldEditor({ competitionId, field, published, onCancel, onSaved }: {
  competitionId: string; field: CompetitionField; published: boolean; onCancel: () => void; onSaved: () => void;
}) {
  const [form, setForm] = useState({ ...field, placeholder: field.placeholder ?? "", helpText: field.helpText ?? "" });
  const [confirmed, setConfirmed] = useState(false);
  const save = useMutation({ mutationFn: () => {
    try { JSON.parse(form.validationJson); JSON.parse(form.optionsJson); }
    catch { throw new Error("Validation and options must contain valid JSON."); }
    return apiFetch(`api/competitions/${competitionId}/fields/${field.id}`, { method: "PATCH", body: JSON.stringify(form) });
  }, onSuccess: onSaved });
  return <form className="mb-6 grid gap-4 border-y border-line py-5 sm:grid-cols-2" onSubmit={event => { event.preventDefault(); save.mutate(); }}>
    <div className="flex items-center justify-between sm:col-span-2"><h2 className="text-lg font-semibold">Edit field</h2><button type="button" className="icon-button" title="Cancel field editing" onClick={onCancel}><X size={18} /></button></div>
    <label className="field sm:col-span-2"><span>Label / consent wording</span><input required maxLength={220} value={form.label} onChange={event => setForm({ ...form, label: event.target.value })} /></label>
    <label className="field"><span>Field key</span><input required maxLength={120} pattern="[a-zA-Z0-9_]+" value={form.fieldKey} onChange={event => setForm({ ...form, fieldKey: event.target.value })} /></label>
    <label className="field"><span>Type</span><select value={form.fieldType} onChange={event => setForm({ ...form, fieldType: event.target.value })}>{fieldTypes.map(type => <option key={type}>{type}</option>)}</select></label>
    <label className="field"><span>Placeholder</span><input value={form.placeholder} onChange={event => setForm({ ...form, placeholder: event.target.value })} /></label>
    <label className="field"><span>Display order</span><input required type="number" min={0} step={1} value={form.displayOrder} onChange={event => setForm({ ...form, displayOrder: Number(event.target.value) })} /></label>
    <label className="field sm:col-span-2"><span>Help text</span><textarea value={form.helpText} onChange={event => setForm({ ...form, helpText: event.target.value })} /></label>
    {([ ["isRequired", "Required"], ["isSensitive", "Sensitive"], ["isSearchable", "Searchable"], ["isExportable", "Exportable"] ] as const).map(([key, label]) => <label key={key} className="flex items-center gap-3 text-sm"><input type="checkbox" checked={form[key]} onChange={event => setForm({ ...form, [key]: event.target.checked })} />{label}</label>)}
    <label className="field"><span>Validation JSON</span><textarea required rows={4} value={form.validationJson} onChange={event => setForm({ ...form, validationJson: event.target.value })} /></label>
    <label className="field"><span>Options JSON</span><textarea required rows={4} value={form.optionsJson} onChange={event => setForm({ ...form, optionsJson: event.target.value })} /></label>
    {published && <label className="flex items-start gap-3 text-sm sm:col-span-2"><input required type="checkbox" className="mt-1" checked={confirmed} onChange={event => setConfirmed(event.target.checked)} />Apply this field change to the published competition.</label>}
    {save.error && <div className="sm:col-span-2"><ErrorState error={save.error} /></div>}
    <div className="flex gap-3 sm:col-span-2"><button className="command-button" disabled={save.isPending || (published && !confirmed)}><Save size={16} />Save field</button><button type="button" className="secondary-button" onClick={onCancel}>Cancel</button></div>
  </form>;
}
