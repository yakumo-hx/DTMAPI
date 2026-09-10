# DTMAPI Agent Rules

## Required Context

Before design or code work, read:

- `PROJECT.md`
- `docs/onboarding/current-state.md`

Read these once per task; reuse unchanged context after compaction. Search the affected source/tests and latest relevant record for the needed symbols/sections; routers do not require reading every link. Preserve unrelated working-tree changes.

## Conditional Context

Before using reverse data, official/third-party material or SMAPI references, read `references/README.md`; never copy predecessor DLKsmapi code, decompiled game code or unlicensed third-party content into this rebuild.

Before runtime lifecycle or repeated-bug work, use `docs/debug/INDEX.md` to locate only the matching issue/protocol and current evidence. Reuse established native-owner analysis; new Hook/API boundaries require the matching native methods and focused Hook/API rows, not a new whole-platform audit.

Use the task routes in current-state for product tests, API redesign, Runtime installers, manual feedback and platform-next work. Historical plans, old packages and past PASS results are evidence, not new task authority. Workshop wording alone does not trigger Runtime installer audits.

## Execution

- `PROJECT.md` owns Mod identity, native-code ownership and save semantics. Managed Mods never belong under `BepInEx/plugins`; generated Advanced admission/receipts must come from Catalog/SDK.
- Game-loaded assemblies stay `netstandard2.0`. Use tracked build/test scripts and `Get-DotNetExe` from `tools/scripts/common.ps1` for the compatible .NET 8 host; do not use PATH SDK selection or Major roll-forward as a shortcut.
- Before touching the shared game, official MODS or live upload directory, acquire `tools/scripts/wait-runtime-lock.ps1 -Reason "..."`; release from this worktree with `tools/scripts/release-runtime-lock.ps1`. Force-release only a proven stale lock with no DolocTown process. Resolve paths from settings/scripts.
- Ordinary gameplay/Hook/UI tests run in place without native saving or routine archive backup/restore. Test scope, disposable-slot use, evidence and stop conditions are in `docs/workflows/product-change-validation.md`. Restore deliberately changed non-save test assets.
- Choose tests from this task's changed behavior. Do not add tests that merely restate low-impact reversible edits. After required checks pass, finish; changed inputs, a new failure or a remaining named gate justify the smallest rerun. Evidence reuse follows the product workflow.
- Each non-trivial implementation owns one Update and monthly row through completion. Use `docs/updates/README.md` for the template/sync command. Read `docs/workflows/document-governance.md` when changing governance/ownership or resolving a lifecycle exception. Update specialized records only when their facts change.
- Keep user-numbered feedback ordered and observations separate from inference. Create/reuse a Review only for unresolved or changed root cause/architecture, repeated failure, or explicit audit work; routine corrections with an established boundary stay in their Update.
