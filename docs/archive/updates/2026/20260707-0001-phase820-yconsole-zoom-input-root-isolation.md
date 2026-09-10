# 20260707-0001 - Phase 8.20 YConsole + Zoom Input Root Isolation

Date: 2026-07-07 +08:00
Status: source-and-runtime-evidence-captured / NoInput passed twice under DirectExe fallback / issue-010-open

## Source Request

External review requested Phase 8.20:

- stop mod bisection and isolate root type inside the already reproduced `YConsole + Zoom` pair;
- add/use `-SmokeOwnerRootIsolationProfile`;
- first run `YConsoleZoomNoInput`;
- if it passes, repeat after a clean restart;
- do not run PreLoad GC, HookProbe, UI triples, service hard-disable, content/native-heavy isolation, `Off`, public API changes, GameBridge rewrite, or unknown native object destruction.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/code/2026/20260707-0001-phase820-yconsole-zoom-input-root-isolation.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260707-0001-phase820-yconsole-zoom-input-root-isolation.md`
- `docs/debug/evidence/GAME-SMOKE/20260707-000526/validation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260707-011257/validation-summary.txt`

## Runtime Evidence

Invalid/control attempts retained but excluded:

- `docs/debug/evidence/GAME-SMOKE/20260706-234314`: Steam launch did not reach usable game runtime.
- `docs/debug/evidence/GAME-SMOKE/20260706-235527`: Steam launch did not reach usable game runtime.
- `docs/debug/evidence/GAME-SMOKE/20260706-235733`: instrumentation-invalid, suppression was applied before owner roots existed.

Valid evidence:

- `docs/debug/evidence/GAME-SMOKE/20260707-000526`: passed.
- `docs/debug/evidence/GAME-SMOKE/20260707-011257`: passed after clean process restart.

Both valid runs used no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, `SmokeOwnerRootIsolationProfile=YConsoleZoomNoInput`, `3600s` continuous title idle, max two LoadGames, and the Phase 8.19 `YConsole + Zoom` pair profile.

## Result

`YConsoleZoomNoInput` removed only the target pair's input roots:

```text
removed={InputButton=7; EventHandler=0; ConfigPage=0}
DTMAPI.DebugConsoleMod: InputButton 2 -> 0, EventHandler 4 -> 4, ConfigPage 1 -> 1, LoadedCodeMod 1 -> 1
DTMAPI.ZoomMod: InputButton 5 -> 0, EventHandler 3 -> 3, ConfigPage 1 -> 1, LoadedCodeMod 1 -> 1
```

Both runs then passed:

```text
requests=2
nativeEnter=2
nativeReturn=2
saveLoaded=2
duplicateRequests=0
fatalWindows=0
```

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- Runtime lock was released.
- No leftover `DolocTown.exe`.
- Profiles were restored.

## Classification

Suppressing only the `YConsole + Zoom` `InputButton` roots made the previously reproducing pair profile pass twice under the same light diagnostic route. This makes those input roots a strong suspect / pressure amplifier, with a DirectExe launch-mode caveat.

Next phase should split the input suspect by owner:

```text
ZoomNoInput
YConsoleNoInput
```

## Rollback

The source changes are smoke-only diagnostics. Rollback removes `SmokeOwnerRootIsolationProfile` support and the associated documents/evidence summaries. No player profile or public API rollback is needed.

## Non-Changes

This phase did not:

- change public API;
- change ordinary player runtime behavior;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run HookProbe;
- run UI triples;
- run service hard-disable;
- destroy unknown native Unity objects;
- mark ISSUE-010 solved.
