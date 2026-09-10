# 20260708-0004 - Title Idle Input Pressure Protocol

Status: docs-only / reusable-debug-protocol / issue-010-title-idle-methodology

## Source Request

User provided a recap of the ISSUE-010 title/main-menu branch and asked to turn it into a reusable investigation, fix, or caution file. The requested scope was documentation only.

## Changed Files

- `docs/debug/protocols/title-idle-input-pressure.md`
- `docs/debug/protocols/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260708-0004-title-idle-input-pressure-protocol.md`

## Summary

Added a repeatable debug protocol for the title/main-menu idle input-pressure route of ISSUE-010.

The protocol records:

- The precise problem shape: continuous title idle followed by native save entry, failing in the native `DolocAPI.LoadGame` / terrain / dungeon activation window.
- The final classification: old registered-string input polling was not proven to be the only native GC root cause, but was proven to be a sufficient title-idle pressure amplifier.
- The triage sequence: route confirmation, negative controls, owner activity review, root-type split, input counters, synthetic pressure amplification, and native-window evidence.
- The fix principle: rebuild the input mechanism instead of deleting one consumer such as YConsole, Zoom, or AutoFishing.
- The validation ladder: synthetic pressure route, original failure route, and FullKnown/near-real title-idle route.
- The main wrong turns: lifecycle review without hot-path frequency, overlong native continuation probing after input roots were visible, and mod-owner bisection before root-type bisection.

## Validation

No game launch or runtime smoke was run; this is a docs-only methodology record.

Static checks:

- The protocol file was written under `docs/debug/protocols/`.
- `docs/debug/protocols/README.md` now links the protocol.
- `docs/updates/INDEX.md` now links this update record.

## Evidence Links

- Debug issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Initial SaveLoad/root review: `docs/reviews/code/2026/20260705-0001-saveload-lifecycle-root-retention-review.md`
- Synthetic input pressure reproducer: `docs/updates/2026/20260707-0003-input-polling-virtual-pressure.md`
- Hotkey rebuild: `docs/updates/2026/20260707-0004-hotkey-rebuild.md`
- Edge follow-up: `docs/updates/2026/20260707-0007-hotkey-edge-followup.md`
- FullKnown validation: `docs/updates/2026/20260708-0002-fullknown-long-title-cycle-validation.md`

## Rollback

Remove `docs/debug/protocols/title-idle-input-pressure.md`, remove its README entry, and remove this update row from `docs/updates/INDEX.md`.

## Follow-Up

When a future report concerns active gameplay or long AutoFishing loops rather than title/main-menu idle, do not reuse this protocol as proof of a fix. Use it only for the title-idle input-pressure branch, then create a separate protocol or review for the gameplay-native GC route if needed.

