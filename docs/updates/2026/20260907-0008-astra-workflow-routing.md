# 20260907-0008: Astra workflow and test routing

## Metadata

- Update ID: `20260907-0008`
- Date: `2026-09-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户继续要求依据官方 Astra 指南开展工作空间去冗余审查和流程设计。
- Review: [bounded audit](../../reviews/code/2026/20260907-0008-astra-workflow-redundancy-review.md)

## Summary

Remove conflicting handoff instructions and redundant default build steps; make focused versus full testing explicit and prevent mistyped or leaked test filters from changing the intended validation scope.

## Changed Files

- Guidance: [AGENTS](../../../AGENTS.md), [root README](../../../README.md), [script reference](../../../tools/scripts/README.md), [product validation](../../workflows/product-change-validation.md), [governance](../../workflows/document-governance.md).
- Platform routing: [handoff](../../planning/platform-next/README.md), [status](../../planning/platform-next/status.md), [task scope](../../planning/platform-next/tasks.md), [acceptance](../../planning/platform-next/acceptance.md). Task dependencies, decision gates and implementation states are preserved.
- Test entrypoints: [build](../../../tools/scripts/build.ps1), [complete test driver](../../../tools/scripts/test.ps1), shared development-only [test helper](../../../tools/scripts/test-common.ps1).
- Focus dispatch: [Unit](../../../tests/DTMAPI.UnitTests/Program.cs), [Author SDK](../../../tests/DTMAPI.AuthorSdk.Tests/Program.cs), [Doctor](../../../tests/DTMAPI.InstallDoctor.Tests/Program.cs), with [routing regressions](../../../tools/scripts/test-test-focus-routing.ps1).
- Records: this Update, its linked Review and [monthly row](../INDEX-2026-09.md). Earlier 0007 and unrelated Wiki/artifact work are retained.

## Validation

- PASS: selected Release builds of Unit, Author SDK and Doctor test projects through `Get-DotNetExe`; final builds have no warnings/errors. No workspace-wide `build.ps1` or full Release run was needed.
- PASS: final `test-test-focus-routing.ps1` under PowerShell 7. Existing `platform-sdk-targets` runs in all three programs; unknown and whitespace focus fail before default test execution; SDK CLI selection overrides ambient focus. Invalid and explicitly empty CLI focus are rejected, preserving the CLI's previous validation boundary. Only the affected SDK test project needed rebuilding for the final empty-input correction.
- PASS: final shared-helper entry guards under PowerShell 7 and Windows PowerShell 5.1 using `test-test-focus-routing.ps1 -EntryGuardsOnly`. Each suite's ambient focus is rejected by both complete entrypoints before build/toolchain/session setup. WinPS execution-policy override is process-local only. The regression script explicitly returns success after its expected negative child exits.
- PASS: changed PowerShell syntax, changed-document local links, `check-doc-governance.ps1 -Quiet`, monthly synchronization/check and `git diff --check`.
- Static walkthrough of the four representative task routes below. This is instruction/call-chain review, not a fresh model A/B evaluation or a game test. The scoped routing regressions passed on the final candidate; no full source/package suite was inferred from those results.
- Game, installed Runtime, published packages, Catalog mutation and full Release: not required/not run. No smoke row or new assurance receipt was created.

## Evidence

- Official guidance, source findings and retained boundaries are in the linked Review. The new guard lives in `test-common.ps1`; player/shared `common.ps1` has no content diff.

| Representative task | Selected validation | Finish / retained boundary |
| --- | --- | --- |
| Documentation correction | Changed links/format/governance | No build, Catalog audit or game run from the documentation change alone |
| Existing product logic defect | Selected project/dependency build and supported focus | Add actual game/native evidence only for the changed behavior claimed; unknown focus cannot silently widen the run |
| Record-only correction after a passing candidate | Record checks; reuse unchanged binary evidence | No binary rebuild or repeated game acceptance caused by an Update edit |
| Runtime installer/package boundary change | Matching real package/host lane and named integration checks | Existing byte, wrapper, compatibility and publication gates remain; focused evidence does not become a full PASS |

Measurements below compare LF-normalized character counts with the working tree at this follow-up's start, after 0007:

| Active guide | Before | After |
| --- | ---: | ---: |
| Platform handoff | 3,806 | 2,014 |
| Product validation | 7,065 | 5,836 |
| Eight audited guidance files combined | 46,269 | 46,191 |

The handoff is 47.1% shorter and product validation 17.4% shorter. Total guidance text is almost unchanged: the script reference adds an executable focused example and clearer command scope, consulted only when selecting that command. This round's primary result is removing conflicting triggers, the README's extra whole build/test cycle, and accidental focus fallthrough; it does not claim a measured token or wall-clock reduction.

One status queue now selects the next platform task; active handoff prompts no longer copy a changing task ID or the previous planning turn's stop instruction. Required-but-unfinished acceptance retains its evidence gap; unrelated E families need no repeated exemption checklist. Historical summaries and already narrow skills were inspected and retained rather than rewritten.

## Rollback Notes

- Revert only this follow-up's changes, restoring shared guidance to its post-0007 state. Revert both build/test helper imports together if removing `test-common.ps1`; restore focus behavior only with the three entrypoint changes. Preserve 0007, unrelated Wiki work and artifacts. No player or published package bytes are changed.

## Follow-Up

- None required for this bounded audit/change. Actual model tokens and end-to-end product-fix duration remain unmeasured; use a later real task's existing record if evaluating those outcomes, without adding a separate metrics ledger.
- A remaining architectural cost is the Unit project's compilation closure: its csproj has 17 direct project references and links several products' sources. A focus selects executed tests, not an independently compiled Mod. If real task timings identify this as a recurring bottleneck, split compilation dependencies through the existing regression-lab work rather than adding another test framework. That restructuring was not performed or claimed here.
