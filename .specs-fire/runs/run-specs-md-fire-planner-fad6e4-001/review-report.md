---
run: run-specs-md-fire-planner-fad6e4-001
intent: hierarchical-ca-ordering
---

# Code Review

## Work Item: ca-hierarchy-ordering

Files: `src/library/core/CaHierarchy.cs`, `src/library/core/Contract/CaHierarchyRow.cs`, `src/test/CaHierarchyTest.cs`

- No issues requiring changes. Pure function, no I/O, no async, no secrets, no logging.
- Reference-equality visited set avoids the `Entity` Equals/slug-based comparison.
- Traversal is iterative, so deep chains cannot overflow the stack.
- Auto-fixes applied: none. Suggestions: none.

---

## Work Item: create-root-ca-form

Files: `src/web/Pages/CreateCertificateAuthority.razor`, `src/web/Pages/CreateCertificateAuthority.razor.cs`

- Diff is minimal (19 lines). Original LF line endings and BOM preserved.
- `ParentCaSlug = "nop"` fires the existing slug handler, which clears the parent state synchronously; then the purpose is set and `RefreshValidity` picks a root-appropriate period.
- No issues requiring changes.

---

## Work Item: create-intermediate-ca-under-parent

Files: `src/web/Pages/CreateCertificateAuthority.razor`, `src/web/Pages/CreateCertificateAuthority.razor.cs`

- Reuses the existing parent-slug handler for subject, key and validity inheritance rather than duplicating it.
- `ParentLabel` falls back to the looked-up parent until the handler finishes loading `SelectedCa`, so the label never dereferences null.
- No issues requiring changes.

---

## Work Item: hierarchical-ca-table

Files: `src/web/Pages/CertificateAuthorities.razor`, `src/web/Pages/CertificateAuthorities.razor.cs`

- Fixed during build: the nested AuthorizeView needed its own `Context` name to avoid clashing with the MudTable row `context`.
- Line endings (CRLF + BOM in .razor) preserved.
- `CanAddIntermediate` is a UI convenience only; the server still enforces who can create and under which parent.
- No other issues.
