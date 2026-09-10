# 20260713-0001 MoreSaves Long-Term Player Baseline Review

Status: recorded / user manual evidence / automation conversion pending
Date: 2026-07-13
Scope: durable interpretation of the user's long-term MoreSaves slot lifecycle and layout feedback
Source: `D:/下载/第四轮.md`
Related API review: `docs/reviews/api/2026/20260713-0002-saveslots-fixed12-product-boundary-review.md`
Related decision docket: `docs/reviews/code/2026/20260713-0008-major-update-fourth-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0004-fourth-round-closure-fifth-decision-docket.md`

## Review Boundary

The earlier native-owner review correctly identified which official owners handle save discovery/create/save/load/copy/delete/UI, but its “unverified” list no longer describes the user's accumulated product experience. This record preserves the supplied feedback in its original order and separates player-proven behavior from automation and API-stability claims.

No screenshot was supplied. No game run, save mutation, install, or runtime-lock operation occurred in this review.

## 1. Slots 7-12 Create, Save, Return To Title, And Reload

User feedback: slots 7-12 can be created, saved, returned to title, and loaded again.

Analysis: treat the expanded 12-slot native data lifecycle as a protected MoreSaves product baseline, not a purely hypothetical path. A fresh 1.0.0 release matrix must automate or formally re-run it, but future planning must not describe the behavior as never manually verified.

## 2. Fresh Game Restart Recognition

User feedback: the extra saves remain recognized after restarting the game.

Analysis: long-term manual evidence covers persistence across process lifetime for the current product path. The 1.0.0 gate should convert this into an exact fresh-process case with the selected source/version recorded; it is regression conversion, not first discovery.

## 3. Copy And Delete

User feedback: copying and deleting expanded saves work normally.

Analysis: official native owners remain responsible for these operations. DTMAPI should protect the observed behavior and add precise copy-target, metadata/index, delete, restart, and failure-recovery assertions rather than replacing the native path.

## 4. Disable Restores The Vanilla Six-Slot View Without Damaging Extra Saves

User feedback: disabling MoreSaves restores the vanilla six-slot UI and does not damage extra save files.

Analysis: hiding slots beyond six is not deletion. Product messaging and tests must preserve this non-destructive meaning. The player uninstaller and Manager must not infer that hidden extra saves are safe to remove.

## 5. Re-Enable Restores Extra Saves

User feedback: re-enabling MoreSaves makes the extra saves appear again.

Analysis: this completes the disable/restore product expectation. The future demand-activation and owner cleanup refactor must preserve rediscovery after re-enable/restart and cannot treat the reduced native count as proof that extra files are obsolete.

## 6. Twelve And Sixteen Slots Work In Long-Term Use

User feedback: both 12-slot and 16-slot configurations work normally.

Analysis: the protected current release baseline remains default twelve, but the manual evidence narrows the prior hypothesis: native data capacity is not inherently capped at twelve. Sixteen-slot support is historical/manual evidence, not yet a new 1.0.0 configurable-count promise or a Stable public API.

## 7. Eighteen Slots Overflow The Single-Page UI

User feedback: at 18 slots every entry stacks on one page and the UI extends beyond the screen.

Analysis: the observed blocker is primarily official-panel layout/navigation at this count, not demonstrated save-array corruption. Future counts above twelve need a reviewed page/scroll adapter and visual/input QA. Retained historical paging code does not prove the actual 18-slot player UI is fixed.

## Durable Classification

```text
protected MoreSaves behavior
  default 12 slots
  slots 7-12 native lifecycle
  restart recognition
  copy/delete
  non-destructive disable and re-enable

historical/manual additional evidence
  16 slots usable

known UI failure
  18 slots overflow/stack in one page

future features
  naming
  configurable counts above 12
  real pagination/scrolling
  name identity/copy/delete/migration rules
```

This evidence does not stabilize `ISaveSlotsApi`, prove arbitrary counts up to its historical clamp, or eliminate the need for exact current-build regression evidence before MoreSaves 1.0.0.
