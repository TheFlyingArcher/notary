---
run: run-specsmd-fire-528fef-010
work_item: leaf-profile-ui
intent: ca-purpose-profiles
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Create Certificate follows the issuing CA profile

## Approach

On the Create Certificate page, a certificate issued by a purpose CA no longer shows the Key Usage and Extended Key Usage pickers. The CA's usages are applied automatically and a short note says so (you chose the note over hiding the section entirely).

**Core**
- `CaProfile.LocksLeafUsages` (computed: the CA issues leaf certificates and restricts what they carry). This is the single rule the page uses, and it is unit-tested.

**Page behavior**
- When the selected issuing CA's resolved profile locks leaf usages:
  - The Key Usage and Extended Key Usage pickers are removed from the form.
  - The view model's selected usages are set to the profile's leaf key usages and extended key usages, so the existing submit path sends them unchanged.
  - An info note reads, for example: "Usages are set by the TLS/SSL CA. Key usages: Digital Signature, Key Encipherment. Extended key usages: Server Authentication."
- Self-signed certificates, legacy CAs with no purpose, and Custom CAs keep the pickers exactly as today.
- Switching from a locked CA back to self-signed or to an unlocked CA restores the picker defaults (Server and Client Authentication, no key usage chosen), so locked values never leak into an editable form. A new `ResetUsages()` on the view model does this.
- A CA that never issues leaf certificates (Root) is no longer offered in the issuer list. The list shows only CAs marked as issuers whose purpose can issue leaves, and each name shows its purpose in brackets, for example "Web CA (TLS/SSL)", with older CAs shown as "Legacy".
- The usage names come from the existing name lists on the view model, falling back to the catalog's names.
- The server check from the previous run stays as the backstop.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Profiles/CaProfile.cs` | Add `LocksLeafUsages` |
| `src/web/ViewModels/CreateCertificateViewModel.cs` | Add `ResetUsages()` and use it for the constructor defaults |
| `src/web/Pages/CreateCertificate.razor` | Pickers shown only when not locked; info note when locked; issuer names show purposes |
| `src/web/Pages/CreateCertificate.razor.cs` | Lock and unlock usages when the issuer changes; filter the issuer list; usage note text |
| `src/test/CaProfileCatalogTest.cs` | `LocksLeafUsages` per profile |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaProfileCatalogTest.cs` | `LocksLeafUsages` is true for every named purpose, false for Root (it issues no leaves) and for unrestricted and Custom profiles |

The page itself cannot be run here (no MongoDB or login), so it is for your UAT. The web project is compile-checked by building it to a temporary folder, because the normal output folder is locked by the running app.

---
*Plan approved at checkpoint. Execution follows.*
