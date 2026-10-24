import test from "node:test";
import assert from "node:assert/strict";
import { competitionSettings, competitionSettingsRequest, competitionSettingsSchema, localDateTime } from "../lib/competition-settings.ts";

const item = {
  name: "Summer draw", slug: "summer-draw", description: "Original description",
  startsAt: "2026-10-24T08:30:25.123Z", endsAt: "2026-11-24T08:30:25.123Z",
  entryLimit: null, perParticipantEntryLimit: 1, numberOfWinners: 1, numberOfReserveWinners: 2,
  minimumAge: 0, allowedCountryCodes: ["CY"], requiresManualApproval: true,
  requiresEmailVerification: true, requiresPhoneVerification: false, allowedParticipantAuthProviders: ["Email"]
};

test("editing preserves dates, zero age and an unlimited entry count", () => {
  const values = competitionSettingsSchema.parse(competitionSettings(item));
  const request = competitionSettingsRequest(values);
  assert.equal(request.startsAt, item.startsAt);
  assert.equal(request.endsAt, item.endsAt);
  assert.equal(request.minimumAge, 0);
  assert.equal(request.entryLimit, null);
  assert.equal(request.requiresEmailVerification, true);
  assert.equal("slug" in request, false);
});

test("rejects invalid dates, limits, country codes and empty providers", () => {
  const values = competitionSettings(item);
  for (const patch of [{ endsAt: values.startsAt }, { startsAt: "invalid" }, { entryLimit: 0 },
    { numberOfReserveWinners: -1 }, { numberOfWinners: 0 }, { minimumAge: 121 },
    { allowedCountryCodes: "Cyprus" }, { allowedParticipantAuthProviders: [] }])
    assert.equal(competitionSettingsSchema.safeParse({ ...values, ...patch }).success, false);
});

test("normalises edited values without discarding optional settings", () => {
  const request = competitionSettingsRequest({ ...competitionSettings(item),
    description: "  ", allowedCountryCodes: "cy, GR, cy,", minimumAge: undefined });
  assert.deepEqual(request.allowedCountryCodes, ["CY", "GR"]);
  assert.equal(request.description, null);
  assert.equal(request.minimumAge, null);
  assert.equal(localDateTime("invalid"), "");
});
