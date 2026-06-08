# DTMAPI Agent Rules

This workspace is a clean DTMAPI rebuild. Do not copy or imitate the old DLKsmapi implementation from any private predecessor workspace, package cache, archive, or temp directory.

## Required Context

Before design or code work, read:

- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`

Before turning user manual test feedback into Codex constraints, a dedicated `docs/goals/YYYY/...` task file, or a short `/goal` prompt, read:

- `docs/workflows/codex-feedback-to-goal.md`
- `docs/goals/README.md`
- `docs/reviews/README.md` when doing review/root-cause work

Before API rebuild, native-owner follow-up, or GameBridge boundary redesign work, also read:

- `docs/workflows/codex-api-rebuild.md`
- the latest relevant `docs/reviews/api/YYYY/...` records
- `docs/api/public-api-matrix.md`

Manual feedback review must preserve the user's numbered issue order, translate screenshot-only details into text, and attach analysis immediately after each issue so a fresh Codex or compacted context can resume without losing continuity.

Repeated, previously "fixed", lifecycle, UI flicker, stale-state, hook, input, save/load, vehicle, machine, or official-content issues require review/root-cause notes before a new implementation goal. Store durable review records under `docs/reviews/manual-qa/YYYY/` when the review must feed a dedicated goal file or a future `/goal`.

Each implementation handoff must use one exact file under `docs/goals/YYYY/` plus a sibling `.goal.txt` backup of the short `/goal` prompt. The repository root must not contain or rely on a mutable task-ledger file.

For hook work, also read:

- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- The relevant decompiled build under `references/doloc-town/reverse/builds`

## Source Boundaries

- Doloc Town reverse data and official Workshop docs are reference material only.
- Do not distribute official `Assembly-CSharp.dll` or copied decompiled source.
- Third-party Doloc Town mods under `references/third-party-mods` are samples for compatibility research only; do not merge their binaries or code into DTMAPI.
- Stardew Valley SMAPI under `references/stardew-smapi` is architectural reference only; do not copy SMAPI source or license-sensitive implementation details.
- Ordinary DTMAPI mods must not be placed under `BepInEx/plugins`; only the DTMAPI bootstrap belongs there.

## Development Direction

DTMAPI should be built from zero as:

```text
BepInEx bootstrap -> DTMAPI Core -> DolocTown GameBridge -> stable public API -> DTMAPI mods
```

Fragile Unity/Harmony/reflection logic belongs in `DTMAPI.GameBridge.DolocTown`. Public APIs in `DTMAPI.Abstractions` should stay stable and avoid exposing raw decompiled game types unless there is a deliberate adapter.

API rebuild work must start from the native responsibility function or state holder, then build through GameBridge into public abstractions. Do not stabilize a public API from UI success, registry success, or smoke-helper success alone.

## Testing Rule

Codex may launch local Doloc Town and test hooks. Unless a task says otherwise, use the local game's third save slot.

Build success is not enough for hook work. Capture evidence:

- DTMAPI startup log.
- HookProbe/TestMod log line.
- Third-save load evidence.
- Exit check: no leftover `DolocTown.exe`, no Steam waiting-for-exit regression.
- Collected logs/report when available.

Do not hard-code local Steam or game paths; use local settings, environment variables, or scripts.

## Update Record Rule

Every non-trivial update must be traceable. When changing runtime, hooks, UI, config menu, input, mod loading, Workshop loading, installer/package layout, public API, migrated mods, project assets, or project direction:

1. Add an update record under `docs/updates/YYYY/YYYYMMDD-NNNN-short-slug.md`.
2. Link it from `docs/updates/INDEX.md`.
3. Include changed files, source request/goal, validation, evidence links, related debug/hook/smoke/API records, rollback notes, and follow-up.
4. If validation was not run, say so explicitly.

## Debug Rule

When touching runtime lifecycle, shutdown, BepInEx, Harmony patches, event dispatch, input, config menu, Workshop loading, or mod loading:

1. Check known debug issues first.
2. Summarize known facts and rejected hypotheses before changing code.
3. Make the smallest useful change.
4. Run automatic checks and game smoke checks when relevant.
5. Append dated evidence to debug docs.
6. Do not mark solved without clean restart, game test, logs, and regression matrix evidence.
