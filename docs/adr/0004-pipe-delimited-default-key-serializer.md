# 0004 — Pipe-delimited default entity-key serializer

- **Status:** Accepted
- **Date:** 2026-06-28
- **Deciders:** Chris Wolfgang

## Context and problem statement

Each `AuditHeader` row records *which* entity changed by storing its primary-key
value as a string. Keys may be single-valued (`int`, `Guid`, `string`) or
composite (multiple columns). The library needs a default rendering of a key
into the header's key column that is stable, human-legible, and queryable.

## Considered options

- **JSON array** — unambiguous for any value, but verbose and noisy for the
  overwhelmingly common single-`int`/`Guid` key.
- **Pipe-delimited (`a|b|c`)** — compact and legible for the common case; can be
  ambiguous if a key *value* itself contains a pipe.

## Decision

Default to `PipeDelimitedEntityKeySerializer`: join key parts with `'|'`,
rendering each value via `ToString` under `CultureInfo.InvariantCulture` where
applicable, and `null` parts as empty string. Make the serializer **pluggable**
via `IAuditEntityKeySerializer` so consumers with pipe-containing composite key
values can swap in a JSON serializer (a `JsonEntityKeySerializer` is shown in the
tests/examples).

### Rationale

- **Optimise for the common case.** The vast majority of keys are a single
  `int` or `Guid`; for those, `42` and `3f2504e0-...` are far more readable and
  compact than `["42"]`.
- **Invariant culture** keeps the rendered key stable across server locales — a
  German server must not render a numeric key differently from a US one.
- **Escape hatch over cleverness.** Rather than invent an escaping scheme that
  every query would then have to reverse, document the pipe-collision edge case
  and let affected consumers choose JSON. The common path stays simple.

## Consequences

- **Positive:** keys are compact, legible, and `WHERE EntityKey = '42'`-queryable
  for the common case; culture-stable.
- **Negative:** a composite key whose values can contain `'|'` is ambiguous under
  the default serializer; such consumers must opt into a different
  `IAuditEntityKeySerializer`. This is called out in the serializer's XML docs.
- **Follow-ups:** globalization tests pin invariant-culture rendering so a
  non-`en-US` server can't silently change key formatting (see issue #55).
