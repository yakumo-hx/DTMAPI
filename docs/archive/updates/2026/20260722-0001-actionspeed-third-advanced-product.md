# ActionSpeed Third Advanced Product

## Metadata

- Update ID: `20260722-0001`
- Date: `2026-07-22`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: action-speed/advanced/product-native/sdk/package/compatibility/hooks/lifecycle

## Source Request

Split the third mod according to the lightweight functional-mod roadmap after the two-product baseline was frozen. The user asked to minimize long and complete-suite testing; they did not impose a numeric cap on focused short game launches.

## Owning Review

- [ActionSpeed Third Product Admission Review](../../reviews/code/2026/20260722-0002-actionspeed-third-product-admission-review.md)
- [ActionSpeed Weight And Next-product Audit](../../reviews/code/2026/20260722-0004-actionspeed-weight-and-next-product-audit.md)

## Scope

This Update owns the singular migration of `Yuuka.DTMAPI.ActionSpeed` from a Strict consumer of GameBridge's experimental ActionSpeed API into a managed Advanced ProductNative assembly. It preserves the Workshop identity, configuration and player behavior, the frozen old ABI boundary, atomic Hook installation, both owner load orders, fail-closed collision handling, duplicate-settlement protection, and native/animation/timer restoration.

It does not admit a fourth product, open general Advanced authoring, create a SharedNative ActionSpeed API, implement G7, publish `0.5.5`, or run the complete Release suite, L0–L5 ladder, or long soak.

## Planned Slices

1. Bind ActionSpeed identity, policy, native owner, nine Hook targets, and compatibility collision rules.
2. Move production source into `products/first-party/ActionSpeed`, generate the Advanced manifest/receipt through the existing SDK path, and retain one thin Catalog-driven build wrapper.
3. Rehome ProductNative execution and lifecycle state; leave GameBridge only the exact old ABI compatibility route.
4. Extend Catalog-driven package, Doctor/Manager, release-common, and live zero-leftover validation without adding a product-specific checker family.
5. Pass focused checks, then run the bounded third-save acceptance and freeze the three-product baseline; a valid fixable failure returns to focused repair and the same smallest smoke rather than consuming a one-run allowance.
6. Close the acceptance audit: reconcile AutoFill-only Compatibility before updater work, make both ActionSpeed and OneActionComplete unpatch exception-safe, observe the real Harmony target inventories, and run final Loader owner deactivation after title recovery.

## Changed Files

- `products/first-party/ActionSpeed`: Advanced author intent/project/manifest, preserved Workshop assets and i18n/config, six ProductNative production sources, and the external optional-QA observer.
- `testmods/ActionSpeedMod`, `DTMAPI.sln`, `tools/scripts/build.ps1`: removed the old Strict product shell and its obsolete solution/build wiring.
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/ActionSpeed`, GameBridge feature composition and Unit tests: retained the old ABI executor as explicit Compatibility and added bidirectional pending/installed owner fail-closed arbitration.
- `author-sdk/advanced-reference-policies`, `DTMAPI.AuthorSdk`, `DTMAPI.Core`, `DTMAPI.InstallDoctor` and their focused tests: added the exact build-23762374 ActionSpeed policy and four-authority identity projections without weakening Strict `SDK160`.
- Catalog, generic release/build/install/uninstall/check scripts, publish inventory and focused ActionSpeed wrapper/test: extended the existing Catalog-driven three-product loop instead of copying another builder/checker family.
- optional QA fixture/controller/observer wiring: removed old experimental API reads, observed the loaded product externally, counted all product-held native/timer transients, and ordered G6 after configured G5 completion.
- `run-game-smoke.ps1` and installer transaction coverage: route managed Advanced smoke sources through game `Mods/<UniqueID>` with an exact Author receipt, use Catalog product versions, retain exact managed deployments during legacy backup, and tolerate an absent optional `activeOperation` status property.
- this Update plus the admission/orchestration Reviews, ActionSpeed focused Hook map, smoke matrix, API matrix, architecture contract, roadmap, `PROJECT.md`, monthly ledger, and the assurance rules clarifying that bounded acceptance is not a launch quota.

## Validation

- `tools/scripts/test-batch6-actionspeed-advanced-product.ps1`: PASS (`source-files=6`, `hooks=9`, `policies=1`, `native-authorities=7`).
- Catalog validation: PASS (`products=27`, `public=11`, `workshop-items=21`, `api-rows=47`).
- Catalog-driven Advanced validate/build/pack: PASS after the runtime-found target-name correction. The corrected package accepted in `GAME-SMOKE/20260722-141220` has SHA-256 `4882CA6D734FAAFC2123AF2B828083C8107118CF6117C152AD0D1C973D56ED6C`. The later additive `IItemDisplayNameApi` rebuild produced package `C0EF5F7A74A7F15ACCF2F67992701452B3404C870310020A9DB4E91F04582BF9`; ActionSpeed source/behavior did not change. `GAME-SMOKE/20260722-161022` re-exercised Tool/ConfigApply/Interaction and exact nine-patch owner deactivation against that current byte image.
- GameBridge, optional QA and Unit builds: PASS with zero warnings/errors. `DTMAPI.UnitTests`, `DTMAPI.QaUnitTests`, Author SDK tests and all ten InstallDoctor cases pass.
- Generic player Runtime-only uninstall: PASS. The final single-host developer official-local install transaction passes after the Catalog-version, managed-receipt backup and optional-status fixes.
- PowerShell parse checks pass for the changed installer, smoke runner, transaction and ActionSpeed focused scripts.
- No complete `test.ps1`/Release, L0-L5 ladder or long test was run.
- 2026-07-22 audit-correction focused checks: PASS for ActionSpeed and OneActionComplete product source checks, the focused GameBridge demand unit, optional-QA Release build/QaUnit assertions, Catalog/zero-leftover validation, and both Catalog-driven SDK packages. The first bounded correction process failed for the product defect recorded below; no Release, L0-L5, GC or long test was run.

## Evidence

- The pre-migration third-save Tool, Interact, Eat, and ContinuousUse fixture is the behavior baseline.
- The existing 24-child L0–L5 ladder remains release-gate history and will not be replayed in this bounded migration.
- `GAME-SMOKE/20260722-100419` is the first post-migration attempt and is explicitly non-acceptance. It loaded the retained old Strict ActionSpeed package from persistent OfficialLocal rather than the new managed Advanced package; G6 title navigation then interrupted the three configured G5 ActionSpeed cases.
- That run still passed save/profile/source/deployment/QA restoration, fatal-window, forced-close and process-exit gates with no residual `DolocTown.exe`. The runtime lock was released normally.
- [Runtime orchestration Review](../../reviews/code/2026/20260722-0003-actionspeed-runtime-acceptance-orchestration-review.md) records the two root causes. Managed-Advanced source selection and G5-before-G6 ordering are corrected and pass focused source/QA-unit checks. They were not immediately rerun because "one bounded acceptance" was incorrectly interpreted as a numeric process quota; the user has retracted that interpretation.
- The later weight/next-product audit keeps this Update open through the correction and final acceptance boundary. Until the new run completes, no migrated-DLL behavior, actual nine-owner-Hook, final owner-deactivation or three-product baseline claim is made.
- `GAME-SMOKE/20260722-125239` is the first audit-correction game process. It selected the receipt-verified LocalDevelopment Advanced DLL and proved fail-closed loading, but `ActionSpeedHookInstaller` used the nonexistent `DolocTown.AgentStateBase` name for its ninth pre-resolved target. Entry threw before the first patch, Core rolled the owner transaction back, and the expected Harmony owner remained at zero. The runner stopped at the first G5 case, so the OneActionComplete partial-energy/config-reload/final-deactivation continuations did not execute. Save, config, source, profile and QA state restored; no fatal window or residual process remained.
- The tracked build authority places `AgentStateBase` in the global namespace. Product installation, the QA target inventory, the focused Hook map and the source gate now use that exact name. The corrected package passes focused build/package checks and is ready for the same bounded third-save acceptance. No separate authorization is required for that short rerun; Runtime status remains failed/pending only until it passes.
- `GAME-SMOKE/20260722-141220` is the corrected-package acceptance. Tool, ConfigApply and Interaction passed; title lifecycle and exact environment restoration passed. Real Loader deactivation reduced ActionSpeed from one loaded instance, nine actual Harmony patches across nine targets and one callback to zero instance/patch/target/callback/Core roots. The same process passed OneActionComplete resource, partial-energy and configuration save/readback/reload evidence and reduced its exact owner, callback, instance and Core roots to zero. No fatal window or residual `DolocTown.exe` remained.

## Rollback

Remove the ActionSpeed Advanced Catalog row, SDK policy, product source/package wiring, and compatibility arbitration changes together; restore the prior Strict product source and GameBridge execution route. Do not leave a hand-authored Advanced manifest or receipt.

## Follow-up

Freeze the accepted ActionSpeed ownership baseline. The later Fish/Animal work may rebuild this product against an additive Abstractions surface, but it does not reopen ActionSpeed behavior unless its source/native policy changes. Complete Release, L0–L5, long-run and 0.5.5 publication remain separate and unrun.
