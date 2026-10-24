import assert from "node:assert/strict";
import test from "node:test";
import { entryEligibilityFields, entryEligibilityValues } from "../lib/entry-eligibility.ts";

test("adds missing eligibility inputs only when required by the competition", () => {
  const required = entryEligibilityFields([], ["CY"], 18);
  assert.equal(required.addCountry, true);
  assert.equal(required.addDateOfBirth, true);
  assert.equal(entryEligibilityFields([], []).addCountry, false);
  assert.equal(entryEligibilityFields([], []).addDateOfBirth, false);
  assert.deepEqual(entryEligibilityValues([], { __eligibility_country: " cy ", __eligibility_date_of_birth: "1990-01-02" }), {
    countryCode: "CY", dateOfBirth: "1990-01-02"
  });
});

test("uses typed custom fields without relying on a specific field key", () => {
  const fields = [{ fieldKey: "residence", fieldType: "Country" }, { fieldKey: "birthday", fieldType: "DateOfBirth" }];
  const config = entryEligibilityFields(fields, ["CY"], 18);
  assert.equal(config.addCountry, false);
  assert.equal(config.addDateOfBirth, false);
  assert.deepEqual(entryEligibilityValues(fields, { residence: "CY", birthday: "2000-04-05" }), { countryCode: "CY", dateOfBirth: "2000-04-05" });
});

test("keeps legacy field keys and does not treat hidden fields as visible eligibility inputs", () => {
  assert.equal(entryEligibilityFields([{ fieldKey: "country", fieldType: "Text" }], ["CY"]).addCountry, false);
  assert.equal(entryEligibilityFields([{ fieldKey: "country", fieldType: "Hidden" }], ["CY"]).addCountry, true);
  assert.deepEqual(entryEligibilityValues([], {}), { countryCode: undefined, dateOfBirth: undefined });
});
