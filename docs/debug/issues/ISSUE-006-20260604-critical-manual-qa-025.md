# ISSUE-006: 2026-06-04 0.2.5 critical manual QA regressions

## Current Status

- Status: verified by smoke / manual visual recheck useful
- Opened: 2026-06-04 +08:00
- Target: DTMAPI 0.2.5
- Source: user manual QA feedback summarized in `readme.md`
- Important boundary: 0.2.4 smoke evidence is useful for orientation only. It does not prove these newer manual QA failures are solved.

## Known Facts Before Changes

- `readme.md` now scopes this round to SecondMotor failed-summon residue and disabled-mod empty mail, Mine official research/internal storage, and Y-console search lifecycle.
- The runtime version still has controlled `0.2.4` sources before this implementation pass, so 0.2.5 must be a real version bump with evidence.
- Current Mine JSON uses `EquipmentFuncDecorator`, default-unlocked recipe inputs `stone x80`, `iron_ingot x8`, `electric_wire x4`, and `dtmapi_oil x5`, and the Machine runtime loop reports native backpack output.
- Current Y-console search state is stored in the Bootstrap Unity UI host and is not cleared on save/title boundaries.
- Current SecondMotor mod sends key mail on `SaveLoaded` if the mod DLL is loaded, and official disable-after-load is a restart-required state unless the mod/API self-gates.

## Rejected Hypotheses And Shortcuts

- Do not treat `GAME-SMOKE/20260603-202948` as proof against the user's new cross-save residue report; that smoke covered a successful edge transition, not failed summon cleanup or disabled-mod mail.
- Do not keep Mine as a DTMAPI-only machine shell with backpack output; the new acceptance requires the official tech tree route and Mine-owned E-key storage.
- Do not solve Y-console search leakage by disabling same-save search persistence; the requested lifecycle is clear at save entry/exit while preserving same-save reopen when possible.
- Do not unload already-loaded mod DLLs after official disable. Use runtime/bridge/mod self-gating and safe DTMAPI-owned cleanup instead.
- Do not mutate third-party or Steam Workshop package files while validating enablement.

## Manual QA Items

### 1. SecondMotor failed summon residue and disabled-mod empty mail

- User-confirmed facts: alternate motor summon can fail; a motor-like texture remains visible in the farm; the residue crosses save boundaries; disabling the mod and restarting clears it; after disabling the mod, mail is still sent but without an attachment.
- Initial analysis: failure cleanup must destroy or hide only DTMAPI-owned clone/controller/interactable objects, restore any original motor routing/snapshot, and run on failed summon, save load/switch, title return, and safe bridge lifecycle paths. Mail must require a runtime-loaded, enabled content source for the key item and must not call native email delivery when the source is disabled or missing.
- Acceptance: failed summon leaves no DTMAPI motor object, trail, marker, or rider routing behind; switching saves and returning to title do not show residue; disabled SecondMotor does not register ordinary behavior or send key mail; empty attachment mail is treated as a failure.

### 2. Mine official research, recipe, scale, and internal storage

- User-confirmed facts: Mine is missing from official research; the visible Mine is not sufficiently enlarged; output goes to backpack; target research position is Industrial, right of `合金材料`, above `指挥官`; recipe must be Oil x10 plus Steel Ingot x10; E must open Mine-owned 16-slot non-stacking storage.
- Initial analysis: official docs do not expose a tech-tree JSON route, so the bridge may need safe native `TechTreeDatabase` graph/table injection while still using the official research UI. Output delivery should prefer the equipment's native `IContainer`/`LinearInventory` when Mine JSON uses `EquipmentFuncCase`, and only generic future machines should fall back to backpack.
- Acceptance: third-save evidence shows the node in the official Industrial tech tree at the requested relationship, recipe default lock follows the official route, placed Mine is visually 2x, E opens 16 slots, and production stores per-slot output inside the Mine rather than the backpack.

### 3. Y-console search text persists across save-session boundaries

- User-confirmed facts: first Y-console open after entering a save can contain stale text such as `石油` or `摩托钥匙`; first entry must be empty; same-save reopen may preserve search; exiting a save should clear it.
- Initial analysis: the reflected debug console host persists across save sessions and does not observe runtime save/title boundaries, so search/category/source/page state survives unintentionally.
- Acceptance: first Y-console open after entering slot 3 is empty; search can persist during that same active save session; returned-to-title, save switch, and new save load clear search and filters before the first next open.

## Required Evidence

- Release build and unit tests with 0 errors.
- Third-save smoke that includes DTMAPI startup, SaveLoaded, Hook/TestMod lines, and clean exit.
- Cross-save or title-return evidence for SecondMotor cleanup.
- Disabled SecondMotor mail gating evidence.
- Official Mine tech-tree UI evidence and Mine storage evidence.
- Y-console search lifecycle evidence.
- Process check showing no leftover `DolocTown.exe` and no Steam waiting-for-exit regression.

## Attempts

### 2026-06-04 / DTMAPI 0.2.5 baseline

- Change: opened this issue before runtime/hook changes.
- Evidence: pending.
- Result: implementation pending.

### 2026-06-04 / DTMAPI 0.2.5 implementation and validation

- Change: bumped controlled version sources from 0.2.4 to 0.2.5; added SecondMotor official enablement gates, mail source/attachment preflight, and lifecycle cleanup; moved Mine to official Industrial tech-tree injection plus `EquipmentFuncCase` storage; reset Y-console search/filter state on title/save boundaries and logged open-state lifecycle evidence.
- Build evidence: `tools/scripts/build.ps1 -Configuration Release` passed with unit tests. Subsequent smoke-install builds also passed. The only warnings were NU1900 package vulnerability metadata lookups caused by restricted NuGet network access.
- Full third-save evidence: `docs/debug/evidence/GAME-SMOKE/20260604-111533`.
  - `VehicleSecondMotor=true`, `NewContentMineOfficialJson=true`, `NewContentMineProduction=true`, `NewContentEquipmentSlots=true`, `NewContentOilItemMetadata=true`, `NewContentOilCoalDrop=true`, `DebugConsoleOpenY1=true`, `DebugConsoleHoldYNoFlicker=true`, `InstantSave=true`, `DebugTeleportCsv=true`, `DebugTeleport=true`, `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`.
  - SecondMotor passed enabled-state checks: original and second motors visible together, appearance isolation `originalScopedTint=0/10` and `secondScopedTint=5/10`, second-motor ride, edge transition with `secondInCurrentRoom=True`, `originalVisibleAfterTransition=False`, `originalAtNewEntry=False`, `noStuck=True`, dismount/original snapshot restore, and original motor summon after restore.
  - Mine passed official/content/storage checks: `recipeInputs=dtmapi_oilx10|steel_ingotx10`, `EquipmentFuncCase`, `caseStorage=16/4`, native tech route `node=dtmapi_mine`, `parent=alloy_material`, `rightOfParent=True`, `aboveCommander=True`, renderer scale `2x2`, production `outputTarget=equipment-storage`, `storage=2/16`, `storageLineCapacity=4`.
- Disabled SecondMotor evidence: `docs/debug/evidence/GAME-SMOKE/20260604-111901`.
  - Log audit: `Skip=2 Registration=0 Mail=0`, proving disabled `Local.DTMAPI_SecondMotor` did not load ordinary vehicle behavior or call key-mail delivery. Process check: no `DolocTown.exe`.
- Y-console lifecycle evidence: `docs/debug/evidence/GAME-SMOKE/20260604-112250`.
  - Boundary logs reset search/filter state for `ReturnedToTitle` and `SaveLoaded slot=2 isNewGame=False`.
  - Open-state audit: `OpenEmpty=1 OpenOil=7 SecondMotorMail=0`; first open after save load logged `searchText=<empty> category=<empty> sourceFilter=__base`, while same-save reopens preserved the smoke's `searchText=石油` and `sourceFilter=Local.DTMAPI_Oil`.
  - Process check: no `DolocTown.exe`.
- Result: smoke-verified. Manual visual QA can still inspect the official research tree placement and summon animation feel, but no blocker remains in the automated evidence.
