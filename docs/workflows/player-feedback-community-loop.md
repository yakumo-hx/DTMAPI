# Player Feedback And Community Review Loop

Status: workflow proposal for the Refactor branch.

Date: 2026-06-11

## Purpose

This workflow defines how player reports should move from Manager UI evidence into actionable reviews, issue triage, known issues, and future implementation goals. It complements `codex-feedback-to-goal.md`; that workflow turns manual feedback into Codex task files, while this one focuses on player-facing report collection and community support.

## Expected Player Flow

1. Open DTMAPI Manager.
2. Check Status for overall status, loaded/blocked/disabled mods, errors, warnings, failed/missing hooks, failed/degraded features, and latest log/report state.
3. Use Copy Summary when support asks for a compact text summary. If the local clipboard is unavailable, the Manager should report `copy-unavailable` and write the same summary to the runtime log.
4. Open Mods, Errors/Warnings, Hooks, or Features to find the failed or warning row that best matches the issue.
5. Open Logs and click Export logs.
6. Upload the generated report zip, or the compact web evidence package when a zip is too large for the review channel.
7. Include a short description of what the player expected, what happened, and whether the issue repeats after restart.

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
- Showing Status, Mods, Errors/Warnings, Hooks, Features, and Logs as real view-model consumers in the title Settings UI.
- Copying the Status support summary as text, with a runtime-log fallback when the local clipboard is unavailable.
- Copying selected rows as text in a future slice.
- Exporting a report through the runtime diagnostics helper.
- Refreshing the snapshot after export and showing whether the returned report path matches `LatestReportPath`.
- Showing when no report path or log path is available.
- Avoiding Stable wording for Experimental or Diagnostic surfaces.

## Phase 1 Implemented Support Path

As of `GAME-SMOKE/20260611-112148`, the title Settings Manager MVP supports the first support loop without raw log reading: Status summary, Mods, Errors/Warnings, Hooks, Features, Logs, and Logs Export Report all consume the internal Manager model or report-export state.

The Developer Preview polish slice adds Status Copy Summary over the same internal Manager model. The copied/fallback text includes overall status, mod counts, diagnostics counts, failed/missing hook counts, failed/degraded feature counts, refresh state, report/export state, latest log state, and latest report path state. Copy selected row and row-detail panels remain future work.

For the `0.5.0-alpha` release-preparation slice, Status/Logs also expose install-state presence, installed version, legacy moved/detected counts, and uninstall-helper availability. This lets support quickly separate "runtime not installed", "old SMAPI/DLK content still present", and "runtime installed but mod failed" reports before asking for a full report zip.

## Boundaries

- Do not ask players to place ordinary mods in `BepInEx/plugins`.
- Do not ask players to share official DLLs, decompiled code, or third-party binaries.
- Do not treat a ready feature status as proof that a gameplay API is Stable.
- Do not convert screenshot-only feedback into implementation work without text reproduction notes and a review or goal file when needed.
