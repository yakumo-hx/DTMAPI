# 20260609-0001 Custom Entity Status Alignment

## Metadata

- Update ID: 20260609-0001
- Date: 2026-06-09
- Status: verified
- Source: Active goal: align CustomEntity API status markers with `public-api-matrix`
- Owner: Codex

## Summary

- Added `DtmApiStatus.StableCandidate` so source annotations can match the public API matrix vocabulary.
- Changed `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi`, and `ICustomDroneApi` annotations from `Stable` to `StableCandidate`.
- Added annotation notes stating these APIs are definition/registry contracts only and that native runtime creation is blocked until Doloc Town adapters are verified.
- Updated the CustomEntity unit-test assertion messages so they describe these interfaces as registry APIs.
- Updated `docs/api/040-stable-custom-entity-apis.md` wording and heading to describe stable-candidate registry contracts plus experimental blocked runtime creation.
- Updated the public API matrix status date without changing its CustomEntity classification rows; those rows already separated `StableCandidate` registry contracts from blocked `Experimental` runtime verbs.
- No CustomEntity registry, GameBridge, hook, smoke harness, or native runtime behavior was changed.

## Changed Files

- `src/DTMAPI.Abstractions/ApiStatus.cs`
- `src/DTMAPI.Abstractions/CustomEntities.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/040-stable-custom-entity-apis.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/2026/20260609-0001-custom-entity-status-alignment.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Game smoke was not run because this update changes metadata, test text, and documentation only. It does not change GameBridge, CustomEntity registry behavior, native adapters, hooks, smoke harness behavior, or installed game behavior.

## Evidence

- API matrix rows for Custom Entities continue to state:
  - Definition/registry portions of `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi`, and `ICustomDroneApi` are `StableCandidate`.
  - Native runtime creation verbs are `Experimental` and blocked, returning `runtime-creation-blocked` / `RuntimeCreationBlocked`.
- Native-owner audit `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/04-custom-entity-runtime-verbs.md` remains the source record for the blocked runtime verdict.
- Existing smoke evidence `GAME-SMOKE/20260606-191219` remains registry/blocker evidence only; it is not promoted to native runtime creation proof.

## Known Facts And Rejected Hypotheses

- Known fact: Core registry registration, validation, snapshots, status, owner cleanup, and blocked request DTOs remain unchanged.
- Known fact: native animal, monster, bullet/projectile, attack, and drone adapters are not verified.
- Rejected hypothesis: interface annotations can stay `Stable` because runtime verbs return blocked results. The public matrix requires the full interface surface to be marked no higher than `StableCandidate` while runtime creation remains blocked.
- Rejected hypothesis: this metadata-only goal requires a new third-save smoke. Build and unit tests are sufficient for this scope because no runtime or hook path changed.

## Related Records

- API matrix: `docs/api/public-api-matrix.md`
- CustomEntity API contract doc: `docs/api/040-stable-custom-entity-apis.md`
- Native-owner audit: `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/04-custom-entity-runtime-verbs.md`
- Prior CustomEntity implementation record: `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md`

## Rollback Notes

- Revert the four `DtmApiStatus` annotations and test/doc wording if the project intentionally reclassifies the full CustomEntity surface as stable after native adapters are verified.
- Do not remove `DtmApiStatus.StableCandidate` while `docs/api/public-api-matrix.md` uses that status vocabulary.

## Follow-Up

- Future native adapter work should split or further annotate registry/status contracts from animal, monster, attack/projectile, and drone runtime adapter verbs before any promotion to `Stable`.
