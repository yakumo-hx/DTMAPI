# 20260610-0012 CustomEntity Status Naming Cleanup

## Metadata

- Update ID: 20260610-0012
- Date: 2026-06-10
- Status: verified
- Source: User-requested midterm stabilization plan, step 5: `codex/chore-customentity-status-naming`
- Owner: Codex

## Summary

- Renamed the current GameBridge CustomEntity family hook/status IDs from stable-sounding API labels to registry-contract labels:
  - `CustomAnimals.RegistryContract`
  - `CustomMonsters.RegistryContract`
  - `CustomAttacks.RegistryContract`
  - `CustomDrones.RegistryContract`
- Kept the existing `configured-blocked` status semantics: registry/status contracts are verified, but native runtime creation remains blocked.
- Changed family status details to `StableCandidate registry contract; runtime creation remains blocked`.
- Updated smoke pending text and docs so CustomEntity registry contracts are not confused with verified native runtime adapters.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/api/040-stable-custom-entity-apis.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260610-0012-customentity-status-naming.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check`
  - Only expected CRLF warnings were reported.
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseCustomEntityApis -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260610-045414`
  - Result JSON: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `CustomEntityApis=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`

## Evidence

- DTMAPI log:
  - `CustomEntities.CoreRegistry = verified. StableCandidate 0.4.0 custom entity registry contracts are registered...`
  - `CustomAnimals.RegistryContract = configured-blocked. StableCandidate registry contract; runtime creation remains blocked`
  - `CustomMonsters.RegistryContract = configured-blocked. StableCandidate registry contract; runtime creation remains blocked`
  - `CustomAttacks.RegistryContract = configured-blocked. StableCandidate registry contract; runtime creation remains blocked`
  - `CustomDrones.RegistryContract = configured-blocked. StableCandidate registry contract; runtime creation remains blocked`
  - `Smoke.CustomEntityApis = verified. registered=animal,monster,attack,drone; invalidAnimal=invalid-definition; duplicateAnimal=duplicate-definition-id; requests=runtime-creation-blocked; cleanupRemoved=5; lifecycleEvents=2/3/2/2.`
- Process/fatal checks:
  - `process-check.txt`: `No DolocTown.exe process found.`
  - `fatal-window-check.txt`: `No fatal instance popup found.`
- Report pointer:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-045451.zip`

## Known Facts And Rejected Hypotheses

- Known fact: Core registry registration, validation, snapshots, status, owner cleanup, and blocked request DTOs are unchanged.
- Known fact: native animal, monster, bullet/projectile, attack, and drone adapters are not verified.
- Rejected hypothesis: hook/status names should keep `StableApi` because the registry contract is candidate-stable. The old names overstate the current boundary; the status path should say registry contract while runtime creation remains blocked.
- Rejected hypothesis: renaming the GameBridge status IDs implements native runtime creation. It does not; runtime verbs still return `runtime-creation-blocked` / `RuntimeCreationBlocked`.

## Related Records

- API matrix: `docs/api/public-api-matrix.md`
- CustomEntity API contract doc: `docs/api/040-stable-custom-entity-apis.md`
- Hook map: `docs/hook-map/README.md`
- Native-owner audit: `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/04-custom-entity-runtime-verbs.md`
- Prior status-alignment update: `docs/updates/2026/20260609-0001-custom-entity-status-alignment.md`
- Prior implementation update: `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md`

## Rollback Notes

- Restore the four family status IDs only if the project deliberately wants the old `*.StableApi` / projectile naming in hook/status output.
- Do not treat rollback as native adapter work; runtime creation remains blocked unless future adapter evidence proves otherwise.

## Follow-Up

- Continue the midterm route with the ChestLocator smoke case split.
