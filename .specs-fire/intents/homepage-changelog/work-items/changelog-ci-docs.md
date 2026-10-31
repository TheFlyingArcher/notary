---
id: changelog-ci-docs
title: CI/CD docs for the changelog automation
intent: homepage-changelog
complexity: low
mode: autopilot
status: completed
depends_on: [changelog-release-workflow]
created: 2026-10-29T00:00:00Z
---

# Work Item: CI/CD docs for the changelog automation

## Description

Document the automation in `docs/ci-cd.md`, including step-by-step setup of the token that bypasses branch protection on `main`.

## Acceptance Criteria

- [ ] Step-by-step instructions to create the token (type, scopes, expiry) and add it as a repository secret
- [ ] Instructions to let that token bypass branch protection on `main`
- [ ] Explains the trigger (release published), the `[skip ci]` push with no tag, and the one-release lag in the deployed app
- [ ] Notes that unreleased merges do not appear in the change log

## Technical Notes

Follow the layout of the existing Tailscale setup section in `docs/ci-cd.md`.

## Dependencies

- changelog-release-workflow
