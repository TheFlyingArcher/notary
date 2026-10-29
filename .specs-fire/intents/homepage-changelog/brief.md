---
id: homepage-changelog
title: Replace the homepage with the Notary change log
status: completed
created: 2026-10-29T00:00:00Z
completed_at: 2026-10-29T23:11:21.973Z
---

# Intent: Replace the homepage with the Notary change log

## Goal

Replace the four tiles on the homepage with a Notary change log: a running list of releases, newest first, backed by a `CHANGELOG.md` that release automation keeps current. Source: GitHub issue #85.

## Users

Anyone who opens the Notary web UI, mainly the operator of the Pi server farm.

## Problem

The four tiles (three certificate statistics and a lorem ipsum filler) look awkward, serve little purpose, and leave a lot of whitespace.

## Success Criteria

- `/` shows the change log from `CHANGELOG.md`, newest first, scrolling back to v1.0.15
- The statistics tiles, the lorem ipsum tile and the "Please wait" gate are removed, and the page no longer loads certificates
- The existing GitHub Releases are backfilled into `CHANGELOG.md`
- A new workflow runs when a GitHub Release is published; it prepends that release's version, date and notes to `CHANGELOG.md` and pushes the commit straight to `main`
- That push creates no tag and no image build (commit message carries `[skip ci]`; verify the `ci.yml` release job does not fire)
- `docs/ci-cd.md` has step-by-step instructions for creating the token that bypasses branch protection on `main` and adding it as a repository secret

## Constraints

- Blazor Server page using MudBlazor: `src/web/Pages/Index.razor` and `Index.razor.cs`
- `CHANGELOG.md` must be included in the published output; verifiable only through `dotnet build`/`publish` (Docker cannot run locally)
- The default `GITHUB_TOKEN` cannot bypass branch protection, so the workflow uses a user-created token stored as a secret
- The page lists published releases only; unreleased merges do not appear
- The deployed app's change log lags one release behind (a running `v1.2.20` shows entries up to `v1.2.19`; its own entry arrives in the next image). Accepted by the user; document it in `docs/ci-cd.md`
- Allman braces, NUnit 4 + Moq; changes must build and run from the Visual Studio solution for UAT

## Notes

Every merge to `main` creates a `v1.2.N` tag, but only releases published by hand become GitHub Releases and are deployable. The change log follows published releases, not tags.
