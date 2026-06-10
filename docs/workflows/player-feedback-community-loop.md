# Player Feedback And Community Review Loop

Status: workflow proposal for the Refactor branch.

Date: 2026-06-11

## Purpose

This workflow defines how player reports should move from Manager UI evidence into actionable reviews, issue triage, known issues, and future implementation goals. It complements `codex-feedback-to-goal.md`; that workflow turns manual feedback into Codex task files, while this one focuses on player-facing report collection and community support.

## Expected Player Flow

1. Open DTMAPI Manager.
2. Check the summary state: overall status, loaded/blocked/disabled mods, errors, warnings, failed hooks, and failed features.
3. Open the failed or warning row that best matches the issue.
4. Click Export Report.
5. Upload the generated report zip, or the compact web evidence package when a zip is too large for the review channel.
6. Include a short description of what the player expected, what happened, and whether the issue repeats after restart.

The player should not need to read raw logs before sharing the report.

## Report Contents Needed For Triage

A useful report should include:

- DTMAPI version and game context when available.
- Loaded mod list.
- Discovered mod status rows with manifest/dependency/entry type details.
- Disabled and blocked mod reasons.
- Diagnostics errors and warnings.
- Hook statuses.
- Feature statuses.
- Latest log path and latest report path.
- Config summary or relevant config page state when available.
- Third-save or manual reproduction notes when applicable.

Oversized screenshots, full evidence folders, and report zips may be omitted from web packages, but the package must say what was omitted and why.

## Maintainer Triage

When a report arrives:

1. Check source commit or package note first.
2. Confirm the report zip or compact evidence is fresh, not a stale `latest-report.txt`.
3. Classify the issue:
   - install/package issue
   - dependency/manifest issue
   - hook failure
   - feature failure
   - gameplay behavior regression
   - API contract confusion
   - manual QA only
4. Link existing known issue or compatibility entry if one exists.
5. If the issue is new, create a review record under `docs/reviews/...`.
6. If implementation is needed, create a dedicated goal file under `docs/goals/YYYY/` and a sibling `.goal.txt` prompt.
7. After fixing, update `docs/updates/INDEX.md`, relevant hook/API/smoke docs, and known issue notes.

## Known Issues And Compatibility Database

The first version can be plain Markdown. Later versions may become structured data.

Required fields:

- Issue ID.
- First seen date.
- Affected DTMAPI version or commit.
- Affected mod IDs.
- Symptoms.
- Diagnostics owner/kind/message when available.
- Hook or feature status keys when available.
- Workaround.
- Fixed in commit/update record when resolved.
- Required evidence for closing.

Repeated reports should update the existing entry instead of creating duplicate root-cause notes.

## Manager UI Support Requirements

Manager should support this loop by:

- Sorting failed and blocked rows first.
- Showing enough detail to identify owner, status, reason, and path.
- Copying selected rows as text.
- Exporting a report through the runtime diagnostics helper.
- Refreshing the snapshot after export and showing whether the returned report path matches `LatestReportPath`.
- Showing when no report path or log path is available.
- Avoiding Stable wording for Experimental or Diagnostic surfaces.

## Boundaries

- Do not ask players to place ordinary mods in `BepInEx/plugins`.
- Do not ask players to share official DLLs, decompiled code, or third-party binaries.
- Do not treat a ready feature status as proof that a gameplay API is Stable.
- Do not convert screenshot-only feedback into implementation work without text reproduction notes and a review or goal file when needed.
