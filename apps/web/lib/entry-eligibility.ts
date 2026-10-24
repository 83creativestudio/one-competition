type EligibilityField = { fieldKey: string; fieldType: string };

export function entryEligibilityFields(fields: EligibilityField[], allowedCountryCodes: string[], minimumAge?: number | null) {
  const visible = fields.filter(field => !["Hidden", "Consent"].includes(field.fieldType));
  const country = visible.find(field => field.fieldType === "Country") ?? visible.find(field => field.fieldKey === "country");
  const dateOfBirth = visible.find(field => field.fieldType === "DateOfBirth") ?? visible.find(field => field.fieldKey === "date_of_birth");
  return {
    countryKey: country?.fieldKey ?? "__eligibility_country",
    dateOfBirthKey: dateOfBirth?.fieldKey ?? "__eligibility_date_of_birth",
    addCountry: allowedCountryCodes.length > 0 && !country,
    addDateOfBirth: minimumAge != null && !dateOfBirth
  };
}

export function entryEligibilityValues(fields: EligibilityField[], values: Record<string, string>) {
  const keys = entryEligibilityFields(fields, []);
  return {
    countryCode: values[keys.countryKey]?.trim().toUpperCase() || undefined,
    dateOfBirth: values[keys.dateOfBirthKey] || undefined
  };
}
