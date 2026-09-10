# ISSUE-027: Config Mod list next page snaps back to page one

- State: `mitigated`
- Current boundary: Config selection following is separated from explicit paging; a real 16-page Next/Mod-selection/Previous title smoke passed with enlarged non-overlapping controls and zero QA-owner residue. Lower-resolution player usability confirmation remains open.

## Status

- Opened: `2026-08-23`
- Severity: medium
- Area: title settings / config menu / pagination / UI layout
- Related review: `docs/reviews/manual-qa/2026/20260823-0004-config-mod-list-pager-snapback.md`
- Owning update: `docs/updates/2026/20260823-0005-config-mod-list-pager-state-layout.md`

## Player Symptom

On DTMAPI `0.6.1`, a player with sixteen registered config pages sees
`1-14/16  第 1/2 页`, but pressing the Config Mod list `>` button leaves the
first fourteen rows visible. The pager also overlaps the selected Mod title.

## Root Cause

The Next callback changes `configListPageIndex` from `0` to `1` and marks the
reflected title UI dirty. On the resulting `RenderConfig`, the selected Mod is
still a first-page row. The unconditional selected-row visibility rule then
sets the list index back to `selectedIndex / ConfigListPageSize`, which is `0`,
before page-two rows can become visible.

This is deterministic for a list with more than fourteen config pages while a
first-page Mod remains selected. Resolution does not create the state failure.
The `1920x1080` CanvasScaler makes it portable across resolutions, although a
lower resolution shrinks the existing `30x22` logical buttons and aggravates
the separate hit-target problem.

The pager's design coordinates are independently invalid for this use: it
begins at `x=172` and reaches approximately `x=404`, while the right config
pane begins at `x=326`.

## Historical Coverage Gap

- `20260612-0012` introduced both list paging and the unconditional selected-row
  follow rule. Its title smoke requested named config pages directly and did
  not exercise `DTMAPI.Config.ListPager.Next`.
- `20260726-0003` exercised real next/last/previous interactions for the
  Manager Mods pager, whose render path does not contain this snapback rule.
  That evidence does not cover the Config Mod list pager.

## Rejected Hypotheses

- Pagination data is not absent: the player UI reports sixteen entries and two
  pages.
- A resolution-only failure cannot explain the source-level same-render reset.
- A global EventSystem failure is unnecessary to explain the symptom and is
  inconsistent with other working controls in the same panel.

## Correction Boundary

- Follow a selected config Mod only when entering/reopening Config or handling
  a new explicit config request; preserve a pager-selected list page across
  ordinary dirty renders.
- Browsing the directory must not implicitly select a Mod or cancel its pending
  values.
- Move the Config list pager into its own fixed navigation row below the
  left-column heading and enlarge only that pager's hit targets; do not perturb
  Manager or config-item pagination.

## Implemented Correction

- The render path now follows the selected Mod only for a new overlay session,
  a newly requested Config page or an actual selection change. An ordinary
  dirty render after Next/Previous preserves the explicit list-page index.
- The Config pager now occupies a left-only navigation row; its right edge is
  statically bounded before the detail pane and its two targets are `60x30`
  logical units.
- The title QA route now exercises both real reflected Unity pager buttons. If
  fewer than fifteen pages are installed, it adds owner-bound QA-only pages to
  reach sixteen and proves those owners are completely removed afterward.

## Runtime Evidence

- `GAME-SMOKE/20260824-003058` passed a title-only `SaveSlot=0` run at
  `2560x1440`. It observed ten installed pages, temporarily reached sixteen,
  clicked Next from `1-14/16 第 1/2 页`, rendered and captured
  `15-16/16 第 2/2 页`, selected
  `Yuuka.DTMAPI.ManboCardboardAudio`, verified its matching detail title,
  clicked Previous, and returned to page one.
- The receipt records `interactions=next|mod-row|previous`, deactivates all six
  QA-only owners with `remaining=0`, passes QA lifecycle/cleanup and process
  exit, and reports no fatal window or routine player-save backup.
- The preceding `GAME-SMOKE/20260824-002647` is rejected evidence: the stricter
  row-selection step exposed a stale QA overlay-session receipt and failed
  closed. Its exact retained stage was hash-verified and removed with the game
  process absent before the corrected smallest smoke reran.
- Evidence: [`GAME-SMOKE/20260824-003058`](../evidence/GAME-SMOKE/20260824-003058/)
  and its [selected page-two screenshot](../evidence/GAME-SMOKE/20260824-003058/qa-host/g4/ui/title-settings.png).

## Remaining Verification

The logical snapback and overlap are evidence-closed. The issue stays
`mitigated` until the enlarged pager is independently exercised at a lower
resolution comparable to the player's feedback; that remaining check is about
physical usability, not the page-state fix.

## Acceptance Criteria

1. With sixteen config pages and a first-page selection, Next remains on page
   two across the following render and exposes rows fifteen and sixteen.
2. Selecting an item or opening an explicit config request follows that item to
   its owning page; stale page indexes remain clamped after count shrink.
3. Previous returns to page one, and Save/Reset/Cancel transaction behavior is
   unchanged.
4. The Config list pager lies wholly left of the `x=326` detail pane, occupies
   a separate row from the Mod entries, and has larger click targets than
   `30x22`.
5. A real title-settings interaction or independent player retest verifies the
   two-page route before this issue becomes `verified`.
