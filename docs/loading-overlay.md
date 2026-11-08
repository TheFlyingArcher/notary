# Loading overlay

While the web UI is waiting on a backend service, a translucent overlay covers the whole window with a centered
"Loading…" card. It tells the user the app is working and stops them from clicking or typing into the page until the
call finishes, so a second request cannot be started by accident.

## How it works

- `ILoadingTracker` (`Notary.Loading`, in the core library) counts the operations in flight. One tracker exists per
  Blazor circuit (scoped), so one user's loading never shows on another user's screen.
- The overlay appears only after work has run for **200 ms**, and once shown it stays for at least **300 ms**. Quick
  calls therefore never flash it. Both values are in `LoadingTrackerOptions.Default`.
- Overlapping operations are counted: the overlay stays up until the last one ends. If a new operation starts while the
  overlay is waiting out its 300 ms, it stays up.
- `MainLayout` listens to the tracker and draws `LoadingOverlay` (`src/web/Shared/LoadingOverlay.razor`) above
  everything except snackbars. While it shows, the app bar, drawer and page content are marked `inert`, so the mouse and
  the keyboard cannot reach them, and the content is marked `aria-busy`. The card has `role="status"`, so a screen reader
  announces "Loading…".
- It never gets stuck on an error: `RunAsync` ends the operation when the call finishes, throws or is cancelled, and the
  exception still reaches the caller.
- For UAT on a fast machine, `SimulatedLatencyLoadingTracker` can wrap the tracker and delay every tracked call (see the
  UAT section). It is registered only in the Development environment.
- There is no time limit. A call that never returns keeps the overlay up; that is a bug in the call, which the overlay
  makes easier to see.

## Wrapping a new call

Inject the tracker and wrap the awaited service call:

```csharp
[Inject]
public ILoadingTracker LoadingTracker { get; set; } = default!;

var authorities = await LoadingTracker.RunAsync(() => CaService.GetAllAsync());
```

Rules:

- **Wrap the whole sequence when a page makes several calls in a row** (see `CertificateDetail.LoadAsync`). If each call
  is wrapped on its own, the count drops to zero between calls and the 200 ms delay starts over each time, so the
  overlay may never appear.
- **Never wrap something that waits for the user**: `DialogService.ShowAsync`, `dialog.Result` and
  `ShowMessageBoxAsync`. The overlay would cover the dialog the user has to answer.
- **Do not wrap things that are not backend work**: JS interop, `ProtectedLocalStorage`, `form.Validate()` and the
  authentication state.
- **A dialog needs its own guard.** A dialog is drawn outside the layout, so it is not made `inert`. The scrim blocks the
  mouse, but the keyboard can still reach the dialog's buttons. Keep a `_busy` flag, ignore a second click while it is
  set and disable the buttons (see `DownloadCertificateDialog` and `RevokeCertificateDialog`).
- HTTP endpoints (`CrlController`, `AuthenticateController`) are not part of the UI and are not tracked.

## UAT checklist (Visual Studio)

Most data loads on this app are far too quick to see the overlay on a fast machine, so the project has a launch
profile that slows every tracked call. In Visual Studio pick **https (slow calls)** in the startup profile drop-down
and start it with the development Auth0 tenant, as usual. The profile exists on both startup projects: `Notary.AppHost`
(the Aspire host, which is how Notary is normally run) and `src/web`. It adds 1.5 seconds before each tracked call by
setting `NOTARY_SIMULATED_LATENCY_MS=1500`. Change the number in the matching `Properties/launchSettings.json` for a
longer or shorter wait, or set the variable yourself, for example with `dotnet user-secrets`. A new launch profile must
be added to both files to be available from both.

The delay applies **only in the Development environment**. In any other environment (the Raspberry Pi included) the
setting is ignored, so it cannot slow production. Switch back to the plain **https** profile to see the real speed.

Work through these in order.

1. **Slow load shows the overlay.** With the **https (slow calls)** profile, open Certificate Authorities (or
   Certificates). After about a quarter of a second a dark scrim with a centered card (spinner and "Loading…") covers the whole window,
   including the top bar and the menu. It disappears when the page has loaded.
2. **Fast load does not flash.** Switch to the plain **https** profile and reload several pages. The overlay should not
   appear at all, or at most for the minimum 300 ms on a genuinely slow load.
3. **Whole sequence, one overlay.** With the slow profile, open a certificate's detail page, then a CA's detail page (many
   calls in a row). The overlay stays up until the page is complete, with no flicker between calls. A page that wraps
   its calls as one operation waits 1.5 seconds once, not once per call.
4. **Clicks are blocked.** While the overlay shows, click the menu, the top-bar buttons and buttons on the page. Nothing
   responds.
5. **Keyboard is blocked.** While it shows, press Tab and Enter. Focus does not move into the page behind. After it
   clears, click into the page to continue (a control that had focus before the overlay may have lost it).
6. **A failing call clears it.** Make a call throw (for example stop MongoDB, or add `throw new Exception("test")` inside
   a `RunAsync` lambda). The overlay must disappear and the page must show its usual error behavior. Undo the change.
7. **Dark mode.** Switch Windows between light and dark and repeat check 1. The card and spinner follow the theme.
8. **Dialogs.** With the slow profile, open the Download dialog on a certificate with a password-protected format and
   press OK. OK and Cancel grey out while the call runs, a second click does nothing, and the file downloads once. Repeat
   with the Revoke dialog and a hold or revoke: one revocation only, and the snackbar still appears above the overlay.
9. **Create and reinstate.** Create a certificate, a root CA and an intermediate CA; reinstate a certificate on hold. Each
   shows the overlay while it saves, and you cannot submit twice by clicking quickly.
10. **Login page unaffected.** Sign out. The login page shows no overlay.

## After deploying to the Raspberry Pi

No configuration changes are needed. After the release deploys, open the site over HTTPS and check:

- Certificate Authorities, Certificates and a certificate's detail page load and the overlay never gets stuck on.
- Create and revoke a throwaway test certificate; the overlay appears briefly on the slower Pi and clears when done.
- `docker compose logs notary-web` shows no new errors.
