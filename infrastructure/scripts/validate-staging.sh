#!/usr/bin/env bash
set -euo pipefail

: "${STAGING_BASE_URL:?Set STAGING_BASE_URL, for example https://competitions.staging.example.com}"
: "${STAGING_TENANT_SLUG:?Set STAGING_TENANT_SLUG}"
: "${STAGING_COMPETITION_SLUG:?Set STAGING_COMPETITION_SLUG}"

base="${STAGING_BASE_URL%/}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

assert_status() {
  local expected="$1" url="$2"
  local actual
  actual="$(curl --silent --show-error --output "$work/body" --write-out '%{http_code}' "$url")"
  if [[ "$actual" != "$expected" ]]; then
    printf 'Expected HTTP %s from %s, received %s\n' "$expected" "$url" "$actual" >&2
    cat "$work/body" >&2
    exit 1
  fi
}

assert_status 200 "$base/health/live"
assert_status 200 "$base/health/ready"
assert_status 200 "$base/login"
assert_status 200 "$base/c/$STAGING_TENANT_SLUG/$STAGING_COMPETITION_SLUG"

if ! grep -q '<html' "$work/body"; then
  printf 'Public campaign did not return an HTML document.\n' >&2
  exit 1
fi

printf 'Staging HTTP smoke checks passed for %s.\n' "$base"
