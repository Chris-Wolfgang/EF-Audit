# Template drift — intentional deviations from `repo-template`

This repo is configured from the canonical [`repo-template`](https://github.com/Chris-Wolfgang/repo-template).
A periodic drift scan (fleet initiative **C1**) compares the canonical-tracked
files against the template. This document records the deviations that are
**intentional** so future scans don't flag them as regressions.

Every file below differs from the template **on purpose**. None of these should
be "re-synced" from the template — several are improvements AuditTrail adopted
first and are **feed-back candidates** for the template itself.

| File | Deviation | Why | Issue |
|---|---|---|---|
| `Directory.Build.props` | Adds SourceLink, `.snupkg` symbol packages, `Deterministic`, `EmbedUntrackedSources`, `PublishRepositoryUrl`, and pinned analyzer versions (Roslynator / AsyncFixer / Meziantou / Sonar / VS-Threading / BannedApiAnalyzers). | NuGet package-quality hardening for a published library. | #69 |
| `.editorconfig` | Adds the PublicApiAnalyzers `RS00xx` severities; minor comment wording. | Enforce `PublicAPI.{Shipped,Unshipped}.txt` breaking-change detection. | #67 |
| `.github/workflows/pr.yaml` | Multi-stage gated build (Linux → Windows → macOS), 90% coverage gate, integration-test gating, protected-config guard tuned for this repo's project layout. | This is a multi-package library family with Testcontainers integration tests; the template's single-stage CI doesn't fit. | — |
| `.github/workflows/codeql.yaml` | `queries: security-extended` query pack. | Stricter security analysis than the template's default pack. | #87 |
| `.github/workflows/docfx.yaml` | Full version-picker deploy logic: generates `versions.json` from tags, deploys to `/versions/v<n>/`, preserves prior versions, overlays canonical picker assets onto old tags. | Versioned documentation site (D6/D7/D8 fleet initiatives). | #83, #85, #86 |
| `.github/dependabot.yml` | Adds the `github-actions` package ecosystem alongside `nuget`. | Keep pinned action SHAs current. | #68 |
| `BannedSymbols.txt` | `{{PROJECT_NAME}}` placeholder resolved to `Wolfgang.AuditTrail`; trailing newline. | Standard template instantiation. | — |
| `CONTRIBUTING.md` | Repo-specific build/test instructions (slnx, integration-test opt-in, analyzer notes). | Reflects this repo's actual layout. | — |

## Feed-back candidates

These AuditTrail deviations are arguably improvements the canonical template
should adopt (tracked separately as template feed-back, not as drift to revert):

- SourceLink + `.snupkg` + deterministic build knobs in `Directory.Build.props`.
- The PublicApiAnalyzers `RS00xx` block in `.editorconfig`.
- The `github-actions` Dependabot ecosystem.
- The version-picker deploy logic in `docfx.yaml`.

## Re-scanning

When the next drift scan runs, cross-reference its output against this table.
Anything listed here is expected; anything **not** listed is genuine drift to
investigate.
