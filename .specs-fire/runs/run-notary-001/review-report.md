# Code Review: run-notary-001

## Work Item: ci-change-detection

Files reviewed: `.github/scripts/detect-dotnet-changes.sh`, `.github/workflows/ci.yml`.

### Auto-fixed
(none)

### Findings
- Info: Values from `github.event.*` reach the script only through `env:`, never interpolated into `run:`, so there is no script injection path.
- Info: `fetch-depth: 0` is needed for the three-dot diff; cost is small for this repository.
- Info: If `changes` itself fails, `quality` is skipped and reads as passing. The script never exits non-zero on diff problems (falls back to true); the docs item will advise also requiring **Detect Changes**.
- Info: A pull request that adds only a new file under `src/` that is not .NET (for example a markdown file) still runs CI. This is intentional: `src/` is also copied into the Docker image.

No changes required.

---

## Work Item: codeql-advanced-workflow

Files reviewed: `.github/workflows/codeql.yml`.

### Auto-fixed
- Renamed the detection job to `Detect Changes (CodeQL)` so it cannot be confused with the CI job of the same purpose in the required-checks list.

### Findings
- Info: Default setup must be turned off or two scans run per PR. Documented in the file header and, next, in `docs/ci-cd.md`.
- Info: The check name `Analyze (csharp)` matches Default setup's name, so an existing required check by that name keeps working.
- Info: Same `env:` only handling of event values as in CI; no injection path.
