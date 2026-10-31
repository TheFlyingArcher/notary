# Test Report: run-notary-001

## Work Item: ci-change-detection

### Test Results
- Passed: 13 (9 script scenarios against real git ranges, 3 YAML structure checks, `bash -n`)
- Failed: 0
- Skipped: 1 (actionlint, not installed on this machine)

No .NET code changed, so `dotnet test` and the coverage gate are unaffected and were not re-run.

Script scenarios (`detect-dotnet-changes.sh`):

| Scenario | Result |
|----------|--------|
| docs-only push (82176ec, 46db489) | `false` |
| push touching `src/` (0082e4c) | `true` |
| pull_request range `cead4ca...e04cdfe` | `true` |
| workflow/deploy-only push (4fe25ba, `deploy.yml`, `deploy/`, `docs/`) | `false` |
| `before` all zeros / bad ref / empty diff | `true` (safe fallback) |
| `schedule` event | `true` |
| Path regex: `Directory.Build.props`, nested `Directory.Packages.props`, `global.json`, `NuGet.Config`, `.editorconfig`, `ci.yml`, `codeql.yml` | match; `docs/`, `deploy/`, `README.md`, `deploy.yml`, `global.json.bak` do not |

YAML checks (parsed with the `yaml` package): `quality.needs = [changes]` with the `dotnet == 'true'` condition; `release.needs = [changes, quality]` and its `[skip ci]` condition is intact; `changes` exposes the `dotnet` output.

### Acceptance Criteria Validation
- [x] `changes` job outputs `dotnet` from the PR diff or push range using the brief's path list
- [x] `quality` keeps its name and is gated by a job-level `if`
- [x] Unreadable diff or first push falls back to true
- [x] `release` skipped when `quality` is skipped; `[skip ci]` condition kept
- [x] Changing `ci.yml` counts as a code change
- [ ] Workflow lints clean with actionlint: not run (not installed); verified by YAML parse. Real behaviour is covered by UAT.

---

## Work Item: codeql-advanced-workflow

### Test Results
- Passed: 4 (YAML parse; triggers, permissions and `needs`/`if` structure; reuse of the verified detection script; `bash -n`)
- Failed: 0
- Skipped: 1 (actionlint, not installed). The CodeQL scan itself can only run on GitHub; UAT covers it.

### Acceptance Criteria Validation
- [x] `codeql.yml` analyzes `csharp` with `build-mode: manual` building `src/Notary.sln`; permissions are `contents: read` and `security-events: write`
- [x] Pull request runs are skipped by a job-level `if` on `changes`, so the `Analyze (csharp)` check reads as passing
- [x] Pushes to `main` and a weekly schedule (Mondays 03:17 UTC) always scan (the script returns true for those events)
- [x] The path list is shared with CI through `detect-dotnet-changes.sh`
- [x] Actions are `github/codeql-action/{init,analyze}@v3`, `actions/checkout@v4`, `actions/setup-dotnet@v4`
