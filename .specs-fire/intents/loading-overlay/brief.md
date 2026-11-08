---
id: loading-overlay
title: Display loading overlay while data is being retrieved
status: completed
created: 2026-11-07T00:00:00Z
completed_at: 2026-11-08T18:12:24.195Z
---

# Intent: Display loading overlay while data is being retrieved

## Goal

Show a viewport-wide loading overlay in the Notary web UI whenever a service call is in flight, so users can see the app is working and cannot trigger further actions until it finishes.

## Users

Anyone using the Notary Blazor UI (admin and operator roles).

## Problem

Pages go quiet while service calls run, so the app looks broken. Users can also click again mid-request, which risks duplicate or conflicting writes and, as issue #97 puts it, data corruption.

## Success Criteria

- A translucent scrim covers the whole viewport, with a centered card holding a spinner and "Loading…"
- It appears for every service call: page loads, create, revoke, download and so on
- It stays up until all overlapping calls have finished
- It never gets stuck: it clears when a call throws or is cancelled
- It appears only after about 200 ms and stays at least about 300 ms once shown, so fast calls do not flicker
- It works in light and dark mode, and assistive technology is told the page is busy
- Unit tests cover the tracker's counting, delay and failure behavior; the build and all tests pass

## Constraints

- A scoped tracker service counts in-flight operations; `MainLayout` renders the overlay from it; pages and dialogs wrap their calls in the tracker
- Stock MudBlazor only (`MudOverlay`, `MudProgressCircular`); no new packages
- Allman style, SOLID, constructor injection; registered in `Program.cs`
- Stays runnable and debuggable from Visual Studio (UAT workflow)
- Out of scope: per-component skeletons and a branded logo loader

## Notes

Source: https://github.com/TheFlyingArcher/notary/issues/97

The web project is Blazor Server (MudBlazor 9.5). Pages call library services directly, for example `CaService.GetAllAsync()`, so "backend requests" means awaited service calls rather than `HttpClient` calls.

The issue's alternative, skeletons per component, was rejected because independent loading regions would still let users act on other parts of the page mid-request, which is the corruption risk the overlay prevents.

Open point for decomposition: a hung call would lock the whole UI, so decide on a timeout or cancellation behavior.
