# Audio Hook Idempotent Status and All-Mod Manual Profile

## Metadata

- Update ID: `20260731-0005`
- Date: `2026-07-31`
- Lifecycle Status: `implemented`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `partial`
- Related Issue State: `mitigated`
- Area: gamebridge/audio/hooks/lifecycle/diagnostics/workshop/subscriptions/manual-profile/runtime/0.5.5
- Source: User-confirmed combined-Mod functional pass, request for the minimum audio-warning correction, roadmap capture, semantic commit, and preparation of the remaining subscribed functional Mods plus the latest DTMAPI for manual testing.

## Source Request

The user confirmed the current combined functional-Mod hand test passed. One retained warning showed that a later Hook scheduler review downgraded a successfully verified Manbo audio Hook and was misclassified as a duplicate install. The user selected a minimum current-release correction, deferred the unified audio bridge to a later version, authorized graceful game shutdown, and requested that the remaining six subscribed functional-Mod directories and latest Runtime be staged for a new all-Mod hand test.

## Scope

- Freeze the manual feedback and exact root cause before implementation.
- Publish audio physical Hook state only on initial observation or real physical transition.
- Preserve unavailable-target retry and true duplicate-install diagnostics.
- Record, but do not implement, the next-version unified audio bridge route.
- Run focused source, Unit and governance validation; no complete Release or game smoke is required before player staging.
- Commit the tracked correction semantically.
- Preserve and replace the subscribed Zoom, DebugConsole, MoreEquipmentSlots, MoreSaves, OneActionComplete and ChestLocatorEnhancer directories with the exact accepted version for the `0.5.5` boundary; preserve Workshop metadata and rollback material.
- Rebuild/install the latest DTMAPI player package, enable the complete requested manual profile, keep the game stopped, and retain the shared Runtime lock for the user.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementHookBridge.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260731-0002-audio-hook-idempotent-status-republish.md`
- `docs/debug/issues/ISSUE-016-20260731-audio-hook-idempotent-status-republish.md`
- `docs/debug/issues/README.md`
- `docs/planning/20260731-audio-replacement-bridge-roadmap.md`
- `docs/planning/README.md`
- `docs/updates/2026/20260731-0004-functional-four-workshop-manual-profile.md`
- `docs/updates/2026/20260731-0005-audio-hook-idempotent-status-and-all-mod-profile.md`
- `docs/updates/INDEX-2026-07.md`

Generated package/staging/verification scripts, live Workshop/profile changes, and retained rollback material are test/deployment state rather than production source files. Their exact locations and results are recorded below.

## Package and Player-State Selection

- The corrected Runtime was built from semantic source commit `48433bd7e5e66bb4adb171e75972926d9695a5b7`. Its player package has exactly 30 files, only root BAT files `1` through `4`, version `0.5.5`, and manifest provenance `48433bd7e5e6`.
- Zoom, DebugConsole, MoreSaves, OneActionComplete, and ChestLocatorEnhancer use their already admitted Advanced `1.0.0` ZIPs. The verifier compared every live payload file, length, and SHA-256 with the corresponding ZIP rather than trusting only its manifest.
- AutoFishing, ActionSpeed, Fish Roe Info, and Animal Bell Info remain the exact Advanced packages verified by their preceding player-profile Updates. Manbo remains the exact current legacy-compatible package verified by `20260731-0004`.
- MoreEquipmentSlots intentionally remains the frozen old `0.3.1-dtmapi` package required by the Runtime `0.5.5` transition route: 9 files, 539,565 bytes, normalized tree SHA-256 `e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6`. The unpublished Product `1.0.0` migration candidate was not substituted into this player test.
- Every replaced subscription retained its original `workshop.json` byte-for-byte. Manbo was not given an invented `workshop.json`, matching its pre-existing subscription shape.

## Validation

- Release build completed. Every game-loaded DTMAPI project remained `netstandard2.0`; the relevant projects built with zero errors. The aggregate Unit project still reports ten pre-existing nullable warnings from DebugConsole source inclusion, unrelated to this change.
- Focus `DTMAPI_UNIT_TEST_FOCUS=audio-hook-publication` passed both required cases:
  - initial install -> behavioral `verified` -> unchanged physical review retains `verified`, records one install signal and produces no duplicate diagnostic;
  - initial `pending` -> later installed publishes `experimental` and records exactly one install signal.
- Complete `DTMAPI.UnitTests` passed.
- Document governance passed `6,148` checks; Product Catalog passed at `27/11/22/48`; test-artifact governance passed.
- A standalone `check-release-contract.ps1` call was not an applicable focused gate: without the two required Author SDK artifact roots the script rejects its empty mandatory `ArtifactRoot` before reporting contract results. No complete Release or Author SDK freeze was requested or claimed.
- Source correction commit: `48433bd7e5e66bb4adb171e75972926d9695a5b7` (`fix(runtime): preserve verified audio hook state`).
- A fresh Runtime-only player package built successfully from that commit. Its candidate and actual subscription both passed the packaged-entrypoint matrix; the live subscription contains exactly the frozen 30-file payload plus its preserved `workshop.json`.
- The actual packaged install and status entry point ran under Windows PowerShell 5.1. Player Doctor reported five expected Runtime assemblies, zero errors and zero warnings; installed version/provenance, every Runtime assembly, and the dormant Compatibility Host match the release manifest.
- Independent read-only all-Mod verification passed all nine Advanced ZIP comparisons, exact Manbo inventory comparison, the frozen MoreEquipmentSlots tree, preserved Workshop metadata, and reparse-point checks.
- Both official profiles enable exactly, in order: Runtime, AutoFishing, ActionSpeed, Manbo audio, Fish Roe Info, Animal Bell Info, Zoom, DebugConsole, frozen MoreEquipmentSlots, MoreSaves, OneActionComplete, and ChestLocatorEnhancer. Every other profile entry is disabled.
- Direct game `Mods` and persistent official-local `MODS` are empty; `BepInEx/plugins` contains only DTMAPI; Author state contains only the read-only `workshop-subscriptions.json` cache.
- A direct Windows PowerShell 5.1 invocation of the repository-only matrix harness stopped before inspecting the package because that host decoded the harness's UTF-8 Chinese temporary-path literal as ANSI. This is retained as non-acceptance harness evidence. It does not affect the actual packaged BAT entry points, which passed under Windows PowerShell 5.1; the repository PowerShell 7 host rerun of the same matrix passed.
- `DolocTown.exe` remained stopped throughout correction staging and verification. No save or sidecar was read or changed. The corrected delayed ItemDisplayName/audio-status behavior and the expanded 12-item profile remain pending the user's interactive run.

## Evidence

Pre-correction player evidence is retained under:

`E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\manual-qa-20260731-functional-four-audio-hook`

- All-Mod package/install/profile verification: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\verification-all-mod-48433bd7.json`
- Runtime install/status logs: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\install-all-mod-48433bd7.log` and `status-all-mod-48433bd7.log`
- Actual subscription package matrix: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\package-matrix-all-mod-48433bd7.log`
- Pre-all-Mod Workshop revision: `D:\steam\steamapps\workshop\content\.dtmapi-manual-autofishing-player-20260731-074235\Revisions\before-all-mod-48433bd7`
- Pre-all-Mod profile revision: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\.dtmapi-manual-autofishing-player-20260731-074235\Revisions\before-all-mod-48433bd7`
- Checked restore script: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\restore-before-all-mod-48433bd7.ps1`

## Rollback

The source correction is a single transition-publication guard and can be reverted without changing audio ownership or public API. After the game exits, the retained restore script can move the seven newly tested subscription directories aside, restore the exact pre-all-Mod Workshop directories and both official profiles, and reinstall the preceding Runtime package. Earlier functional-Mod revisions remain nested in the same manual-test lease, so the complete player setup is recoverable.

## Follow-up

- The user owns the final interactive all-Mod test.
- The broader audio bridge remains deferred to [`20260731 audio replacement bridge roadmap`](../../planning/20260731-audio-replacement-bridge-roadmap.md).
- Keep this Update at `implemented` and ISSUE-016 at `mitigated`; move them to `verified` only after the player's corrected delayed-demand result and expanded combined-feature result.
