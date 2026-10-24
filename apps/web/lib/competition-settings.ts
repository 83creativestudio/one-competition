import { z } from "zod";
import type { Competition } from "./contracts";

export const participantProviders = ["Email", "Google", "Apple", "Facebook", "X", "TikTok"] as const;
const dateTime = z.string().min(1, "Choose a date and time.")
  .refine(value => Number.isFinite(new Date(value).getTime()), "Choose a valid date and time.");

export const competitionSettingsSchema = z.object({
  name: z.string().trim().min(1, "Enter a competition name.").max(200),
  description: z.string(), startsAt: dateTime, endsAt: dateTime,
  entryLimit: z.number().int().positive("Enter a positive entry limit.").optional(),
  perParticipantEntryLimit: z.coerce.number<number>().int().min(1),
  numberOfWinners: z.coerce.number<number>().int().min(1),
  numberOfReserveWinners: z.coerce.number<number>().int().min(0),
  minimumAge: z.number().int().min(0).max(120).optional(),
  allowedCountryCodes: z.string().refine(value => value.split(",").every(code => !code.trim() || /^[a-z]{2}$/i.test(code.trim())), "Use two-letter country codes, such as CY, GR."),
  requiresManualApproval: z.boolean(), requiresEmailVerification: z.boolean(), requiresPhoneVerification: z.boolean(),
  allowedParticipantAuthProviders: z.array(z.enum(participantProviders)).min(1, "Select at least one login method.")
}).refine(value => new Date(value.endsAt) > new Date(value.startsAt), {
  path: ["endsAt"], message: "Closing time must be after opening time."
});

export type CompetitionSettings = z.infer<typeof competitionSettingsSchema>;

export function localDateTime(value: string): string {
  const date = new Date(value);
  if (!Number.isFinite(date.getTime())) return "";
  const pad = (number: number, width = 2) => String(number).padStart(width, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}.${pad(date.getMilliseconds(), 3)}`;
}

export function competitionSettings(item: Competition): CompetitionSettings {
  return {
    name: item.name, description: item.description ?? "", startsAt: localDateTime(item.startsAt), endsAt: localDateTime(item.endsAt),
    entryLimit: item.entryLimit ?? undefined, perParticipantEntryLimit: item.perParticipantEntryLimit,
    numberOfWinners: item.numberOfWinners, numberOfReserveWinners: item.numberOfReserveWinners,
    minimumAge: item.minimumAge ?? undefined, allowedCountryCodes: item.allowedCountryCodes.join(", "),
    requiresManualApproval: item.requiresManualApproval, requiresEmailVerification: item.requiresEmailVerification,
    requiresPhoneVerification: item.requiresPhoneVerification,
    allowedParticipantAuthProviders: item.allowedParticipantAuthProviders as CompetitionSettings["allowedParticipantAuthProviders"]
  };
}

export function competitionSettingsRequest(values: CompetitionSettings) {
  return {
    ...values, description: values.description.trim() || null,
    entryLimit: values.entryLimit ?? null, minimumAge: values.minimumAge ?? null,
    startsAt: new Date(values.startsAt).toISOString(), endsAt: new Date(values.endsAt).toISOString(),
    allowedCountryCodes: [...new Set(values.allowedCountryCodes.split(",").map(code => code.trim().toUpperCase()).filter(Boolean))]
  };
}
