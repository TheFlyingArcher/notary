# Code Review Report

**Run**: run-specs-md-fire-planner-fad6e4-004
**Intent**: encrypt-stored-private-keys
**Reviewed**: 2026-10-16
**Files Reviewed**: 6

---

## Summary

| Category | Auto-Fixed | Applied | Skipped |
|----------|------------|---------|---------|
| Code Quality | 0 | 0 | 0 |
| Security | 0 | 0 | 2 |
| Architecture | 0 | 0 | 1 |
| Testing | 0 | 0 | 1 |
| **Total** | **0** | **0** | **4** |

**Tests Status**: Passing (352/352)

---

## Files Reviewed

- `src/library/service/AsymmetricKeyService.cs` (modified)
- `src/library/core/Contract/KeyMigrationResult.cs` (new)
- `src/library/core/Interface/Service/IAsymmetricKeyService.cs` (modified)
- `src/web/Program.cs` (modified)
- `src/test/InMemoryFakes.cs` (modified)
- `src/test/AsymmetricKeyMigrationTest.cs` (new)

---

## Auto-Fixed Issues

No auto-fixes applied.

---

## Applied Suggestions

No suggestions were applied. This was a self-review by the Builder agent.

---

## Skipped Suggestions

### 1. [Security] Legacy reader accepts any PEM key type BouncyCastle can read

- **File**: `src/library/service/AsymmetricKeyService.cs` (`ReadLegacyKey`)
- **Description**: The migration reader is deliberately broad (plaintext PKCS #8, traditional RSA/EC PEM, PBES1). It runs only over rows already in the database and its output goes straight into the strict writer, with a decrypt-and-compare check before the row is replaced.
- **Rationale**: Narrowing it risks stranding rows written by older versions.
- **Risk Level**: Low
- **Reason Skipped**: By design.

### 2. [Security] Failure messages include the exception message

- **File**: `src/library/service/AsymmetricKeyService.cs`
- **Description**: The per-row error log writes `ex.Message`. BouncyCastle messages for bad PEM/password do not contain key bytes; the slug is logged, never the stored value.
- **Risk Level**: Low
- **Reason Skipped**: Checked the messages raised by this code path; none echo key material.

### 3. [Architecture] Migration runs in the web host startup

- **File**: `src/web/Program.cs`
- **Description**: Startup blocks until all keys are migrated. A separate setup command would avoid slowing a restart, but then an operator could forget it and keys would stay unloadable.
- **Risk Level**: Low
- **Reason Skipped**: Matches the plan; runs only once per database.

### 4. [Testing] Startup wiring untested

- **File**: `src/web/Program.cs`
- **Description**: The call is compiled but not run against MongoDB.
- **Risk Level**: Medium
- **Reason Skipped**: Manual UAT steps are in the walkthrough.

---

## Project Tooling Used

No project linters detected. Used built-in review rules.

---

## Standards Referenced

- `.specs-fire/standards/testing-standards.md`
