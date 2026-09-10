# 20260908-0001: Development entrypoint simplification

## Metadata

- Update ID: `20260908-0001`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户允许继续拓展工作空间优化与精简范围；[bounded Review](../../reviews/code/2026/20260908-0001-development-entrypoint-review.md)。

## Summary

Unify the ordinary solution/build project authority, remove stale development routes and prevent read-only status or ordinary test runs from triggering unrelated preparation.

## Changed Files

- Build authority: [solution](../../../DTMAPI.sln), [build entrypoint](../../../tools/scripts/build.ps1) and [shared MSBuild properties](../../../Directory.Build.props). The solution retains all former build roots and its existing compatibility project; no Advanced product source project is added.
- Read-only status: [status script](../../../tools/scripts/status.ps1); the shared/player `common.ps1` resolver is unchanged by this round.
- Navigation and triggers: [root README](../../../README.md), [script reference](../../../tools/scripts/README.md), [test-artifact protocol](../../debug/protocols/test-artifact-retention.md).
- Records: linked Review, this Update and its [monthly row](../INDEX-2026-09.md). Prior 0007/0008 and unrelated Wiki/artifact edits remain intact.

## Validation

- PASS: all 26 solution project paths and their Debug/Release build mappings resolve. All former 25 build roots are retained; the additional explicit solution member is the already required GameBridge compatibility dependency. Six moved paths are corrected, three deleted Strict product entries are removed, and the missing QA Unit/ABI harness entries are restored.
- PASS: final Release `build.ps1 -SkipTests`, zero warnings/errors. The old and final commands emit the same 58 unique output paths, all in Release; no output is missing or added.
- PASS: Debug `build.ps1 -SkipTests`, including its required separate Release Abstractions build. Only that intentional compatibility output uses Release in this run.
- PASS: existing Unit focuses `compatibility-host`, `chestlocator-product`, `moreequipment-product`, `strongplantinggun-product`, `zoom-product`, `mine-product`, `debugconsole-product`; these exercise the affected compatibility and independent Harmony fixtures. QA Unit, InstallDoctor and MultiPlatformInstaller source suites also pass. All use already built binaries.
- PASS: existing `test-test-focus-routing.ps1` under PowerShell 7, covering supported Unit/SDK/Doctor focus and invalid-filter rejection. Entry guards also pass under Windows PowerShell 5.1. No new test framework or implementation-mirroring tests were added.
- PASS: status under PowerShell 7 and Windows PowerShell 5.1; missing-toolchain branch tested with an in-memory probe that rejects any provisioning call; invalid configured game path reports the problem and continues. Probes restore process environment variables. No SDK installation or live runtime mutation occurs.
- PASS: changed PowerShell syntax/MSBuild XML, changed-document local links, document governance, monthly synchronization/check and diff whitespace.
- Not required/not run: complete Release package/ABI matrix, game, player acceptance or publication. This Update verifies development routing, not a new Runtime/product release.

## Evidence

Source findings and retained boundaries are in the linked Review. Timings below measure one old entry and the corrected candidate on this Windows host with existing outputs and package caches:

| Release compile-only entry | Wall seconds | Unique output paths |
| --- | ---: | ---: |
| Previous script: 25 separate dotnet build invocations | 54.39 | 58 |
| Final script: one solution build invocation | 12.93 | 58 |

The observed reduction is 76.2% for this build step. It is not a cold-build benchmark, model token measurement or end-to-end Mod-fix duration. One intermediate candidate was rejected during output review because solution-external fixtures fell back to Debug; its shorter time is not used as accepted evidence.

The installed .NET 8 SDK's `Microsoft.Common.CurrentVersion.targets`, around `AssignProjectConfiguration`, defaults `ShouldUnsetParentConfigurationAndPlatform` to true for solution/Visual Studio builds. Setting it to false in the existing root properties keeps out-of-solution fixtures in the requested configuration without maintaining another fixture list. The corrected output comparison and existing fixture tests verify that behavior. The installer already emits the required host-RID output; its existing standalone test-subject build and SDK fixed-Release special case are retained.

Raw console logs are temporary local diagnostics under `tmp/workflow-20260908`; the durable measurements and results are recorded here. No game smoke row, parallel performance ledger or assurance receipt is created.

## Rollback Notes

Reverse this round's solution, MSBuild property, build/status scripts and documentation changes together. Restore `build.ps1` to the post-0008 entry so its focus guard remains. Preserve prior workflow/Catalog/record tooling and unrelated edits. Generated local build outputs can be rebuilt normally; no player data or deployment rollback is needed.

## Follow-Up

None required for this bounded change. A focus still compiles the Unit project's shared dependency closure; splitting it remains a separate regression-lab design if actual product-task timings justify it. Existing deterministic package double-builds, native-save boundaries and Runtime publication checks remain necessary at their specified triggers.
