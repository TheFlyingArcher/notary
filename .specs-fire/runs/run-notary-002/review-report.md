# Code Review: run-notary-002

Files reviewed: `docs/ci-cd.md`, `.github/workflows/codeql.yml`.

### Auto-fixed
- Added `workflow_dispatch` to `codeql.yml` so the UAT can trigger a scan on demand; the detection script already treats manual runs as "scan".

### Findings
- Info: The `CHANGELOG.md` under `src/web/` counts as a code change. Its automatic commits use `[skip ci]`, so they are unaffected.
