# ISSUE-017: Official Mod UI Source Transaction

## State

`verified`

The source authority boundary is covered by Unit tests and the rebuilt player
candidate. A real official Mod-page open/close produced one preview, one close
candidate, one successful native close commit and one next-frame DTMAPI
refresh, with no new DTMAPI warning.

## Reproduction

Opening and then closing the official Mod page in the 2026-08-01 combined
profile emitted two native `ModManager.ReloadMods()` completions. DTMAPI treated
both as committed changes, ran two full discovery/load publications, and
retained five repeated lifecycle/resource warnings despite zero source diff,
zero feature failure and zero Runtime error.

## Known facts and rejected hypotheses

- `ModUiState.Register()` calls `ReloadMods()` while preparing the editable
  page, before the player has committed enablement or order changes.
- `ModUiState.Hide()` runs a delayed close transaction containing
  `ReloadMods()`, `SaveModManager(modManager)`, config/cache/language reload and
  pending-box dismissal.
- `SaveModManager` returns the native persistence result. A close reload alone
  can expose edited in-memory state but cannot prove it reached disk.
- The two calls are not an arbitrary noisy burst, so debounce/coalescing would
  preserve the wrong authority model. Suppressing the five warning strings
  would also hide a future changing rebuild loop.
- Ignoring the first generic reload is unsafe because constructor, debug and
  upload paths also call `ReloadMods()`.

## Mitigation

- Exact Register/Hide Prefixes create a UI-bound preview/close transaction.
- Reload captures immutable scalar candidates but opening never publishes.
- Only a successful `SaveModManager` result for the exact bound manager can
  authorize the close candidate.
- The generated `<Hide>b__27_1` Postfix proves the complete native close
  callback returned; the following GameBridge frame publishes authority, runs
  one Core refresh, then emits QA completion.
- Save failure, missing candidate, wrong manager or interrupted close discards
  the transaction and retains the last committed source authority.
- Generic reloads outside the official page retain immediate legacy behavior.

## Validation

- Unit covers opening preview retention, native save failure, mismatched
  manager identity, same-stack non-publication and exactly one next-frame
  successful commit.
- Exact target signatures are pinned to deployed game build `23762374` in
  `docs/hook-map/focused/WorkshopSourceAuthority.md`.
- Final player verification passed in the `15:28:45–15:31:39` session. The
  matching DTMAPI log contains 0 Warning and 0 Error/Fatal; BepInEx/Unity logs
  contain no hidden exception or crash record. Package and log closeout is
  owned by Update `20260801-0002`.

## Acceptance criteria

1. Opening the official page performs no DTMAPI rediscovery/activation and adds
   no repeated lifecycle/resource warning.
2. Closing without a successful native save retains prior committed authority.
3. A successful close produces one delayed DTMAPI refresh after the native
   close transaction and reflects the committed official enablement/order.
4. Normal Mod loading, title return and clean process exit remain intact.

## Related records

- `docs/reviews/manual-qa/2026/20260801-0001-zoom-debugconsole-d4-input-review.md`
- `docs/updates/2026/20260801-0001-zoom-debugconsole-d4-correction.md`
- `docs/updates/2026/20260801-0002-workshop-upload-release-closeout.md`
- `docs/hook-map/focused/WorkshopSourceAuthority.md`
