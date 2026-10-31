---
run: run-notary-001
work_item: ci-change-detection
intent: selective-ci-runs
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Skip the .NET build and release for non-code changes

## Approach

One shared script decides whether a change touches .NET build inputs; both `ci.yml` and the new `codeql.yml` call it, so the path list lives in one place.

- `.github/scripts/detect-dotnet-changes.sh` writes `dotnet=true|false` to `$GITHUB_OUTPUT`. Run with `bash` so no executable bit is needed.
  - `pull_request`: `git diff --name-only <base.sha>...<head.sha>` (three-dot, so a moved `main` does not count).
  - `push`: `git diff --name-only <before> <sha>`.
  - Any other event (schedule, manual) and any failure (zero `before`, missing commit, diff error) result in `true`, so the safe default is to run everything.
  - Relevant paths (case-insensitive): `src/`, `Notary.AppHost/`, `Notary.ServiceDefaults/`, `.github/workflows/ci.yml`, `.github/workflows/codeql.yml`, `.github/scripts/detect-dotnet-changes.sh`, and anywhere: `Directory.*.props/targets`, `global.json`, `nuget.config`, `.editorconfig`, `.globalconfig`.
- `ci.yml`: new `changes` job (checkout with `fetch-depth: 0`, run the script). `quality` keeps its name and gets `needs: changes` plus `if: needs.changes.outputs.dotnet == 'true'`. `release` becomes `needs: [changes, quality]`; a skipped `quality` skips it, and the existing `[skip ci]` condition stays.
- Skipped job = passing for the required `Build and Quality Gates` check (workflow-level `paths:` would leave it pending).

## Files to Create

| File | Purpose |
|------|---------|
| `.github/scripts/detect-dotnet-changes.sh` | Single source of truth for the relevant-path list and the event handling |

## Files to Modify

| File | Changes |
|------|---------|
| `.github/workflows/ci.yml` | Add `changes` job; gate `quality` and `release` on it |

## Tests

| Test File | Coverage |
|-----------|----------|
| (none; YAML and shell) | Script exercised locally against git ranges: docs-only, `src/`, ci.yml, props file, `before` of zeros, bad ref. Workflow checked with a YAML parse; real behaviour is verified in UAT with a docs-only and a code PR. actionlint is not installed here. |

## Technical Details

Residual risk: if the `changes` job itself fails (for example checkout), `quality` is skipped and would read as passing. The script never exits non-zero on a diff problem, and the docs will tell the maintainer to also require the **Detect Changes** check.

---

## Work Item: codeql-advanced-workflow

### Approach

New `.github/workflows/codeql.yml`: triggers `pull_request` and `push` on `main`, plus a weekly `schedule`. A `changes` job reuses the script above (schedule and manual always `true`). The `Analyze (csharp)` job runs only when `dotnet == 'true'`: checkout, `setup-dotnet` 10.0.x, `github/codeql-action/init@v3` (`languages: csharp`, `build-mode: manual`), `dotnet build src/Notary.sln -c Release`, `github/codeql-action/analyze@v3`. Permissions: `contents: read`, `security-events: write`; `actions: read` is added only if required for private repos.

### Files to Create

| File | Purpose |
|------|---------|
| `.github/workflows/codeql.yml` | Path-gated CodeQL scan for C# |

### Files to Modify

(none; the detection script is shared)

### Tests

YAML parse plus the same script checks. Real behaviour is verified in UAT. The maintainer must turn off CodeQL Default setup or both scans run.
