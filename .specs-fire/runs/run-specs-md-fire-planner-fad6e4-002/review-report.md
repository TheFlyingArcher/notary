# Code Review Report

**Run**: run-specs-md-fire-planner-fad6e4-002
**Intent**: encrypt-stored-private-keys
**Reviewed**: 2026-10-15
**Files Reviewed**: 3

---

## Summary

| Category | Auto-Fixed | Applied | Skipped |
|----------|------------|---------|---------|
| Code Quality | 0 | 0 | 1 |
| Security | 0 | 0 | 2 |
| Architecture | 0 | 0 | 0 |
| Testing | 0 | 0 | 1 |
| **Total** | **0** | **0** | **4** |

**Tests Status**: Passing (335/335)

---

## Files Reviewed

- `src/library/service/AsymmetricKeyService.cs` (modified)
- `src/library/service/PasswordFinder.cs` (deleted, no remaining references)
- `src/test/AsymmetricKeyServiceTest.cs` (new)

---

## Auto-Fixed Issues

No auto-fixes applied.

---

## Applied Suggestions

No suggestions were applied. This was a self-review by the Builder agent, not a separate pass; the project has no configured linter beyond `dotnet format`.

---

## Skipped Suggestions

### 1. [Security] Passphrase kept as a managed string

- **File**: `src/library/service/AsymmetricKeyService.cs`
- **Description**: `ApplicationKey` and its UTF-8 bytes live in managed memory until collected.
- **Rationale**: Unavoidable with a configuration string; the threat addressed here is data at rest.
- **Risk Level**: Low
- **Reason Skipped**: Out of scope.

### 2. [Security] One shared passphrase for every key

- **File**: `src/library/service/AsymmetricKeyService.cs`
- **Description**: All keys share `NOTARY_APP_KEY`, each with its own salt and IV. Rotating the passphrase has no re-key path.
- **Rationale**: Per-key passphrases or a KMS would need a design of their own.
- **Risk Level**: Medium
- **Reason Skipped**: Declared out of scope in the intent; the docs item will state it.

### 3. [Code Quality] EC curve handling still uses string manipulation

- **File**: `src/library/service/AsymmetricKeyService.cs:111`
- **Description**: `GenerateKeyPair` still builds `P-256K`-style names and casts to `FpCurve`.
- **Rationale**: Tracked by #43 (explicit parameters) and #44 (P256K); mixing those fixes in would blur this change.
- **Risk Level**: Low
- **Reason Skipped**: Separate issues.

### 4. [Testing] "Output is not ENCRYPTED PRIVATE KEY" guard is untested

- **File**: `src/library/service/AsymmetricKeyService.cs`
- **Description**: The post-write assertion cannot fail without a seam to inject a different writer.
- **Rationale**: It is a last-resort tripwire; the format is asserted by the storage tests.
- **Risk Level**: Low
- **Reason Skipped**: Adding a seam only for this is not worth the complexity.

---

## Project Tooling Used

No project linters detected. Used built-in review rules.

---

## Standards Referenced

- `.specs-fire/standards/testing-standards.md`
- `.specs-fire/standards/tech-stack.md`
