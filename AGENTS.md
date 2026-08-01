# DTMAPI Agent Rules

This workspace is a clean DTMAPI rebuild. Do not copy or imitate the old DLKsmapi implementation from any private predecessor workspace, package cache, archive, or temp directory.

## Required Context

Before design or code work, read:

- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`

Before turning user manual test feedback into durable Codex constraints or a review record, read:

- `docs/workflows/codex-feedback-to-goal.md`
- `docs/reviews/README.md` when doing review/root-cause work

For documentation lifecycle, status, index, and canonical-fact ownership, read:

- `docs/workflows/document-governance.md`

Before API rebuild, native-owner follow-up, or GameBridge boundary redesign work, also read:

- `docs/workflows/codex-api-rebuild.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md` for Advanced CodeMod, Batch 6 ownership, Loader/SDK/Doctor/Manager/package identity, or product-rehome work
- the latest relevant `docs/reviews/api/YYYY/...` records
- `docs/api/public-api-matrix.md`

Manual feedback review must preserve the user's numbered issue order, translate screenshot-only details into text, and attach analysis immediately after each issue so a fresh Codex or compacted context can resume without losing continuity.

Repeated, previously "fixed", lifecycle, UI flicker, stale-state, hook, input, save/load, vehicle, machine, or official-content issues require review/root-cause notes before another implementation attempt. Store durable review records under `docs/reviews/manual-qa/YYYY/` when the analysis must survive context compaction or feed later implementation.

For long-running work that needs a durable boundary, create the required `docs/updates/YYYY/...` record with `proposed` or `in-progress` status and update that same record through completion or blocker reporting. Use a task-specific review record first when root-cause analysis is required. Historical task handoffs are audit material, not current task sources.

For hook work, also read:

- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- The relevant decompiled build under `references/doloc-town/reverse/builds`

## Source Boundaries

- Doloc Town reverse data and official Workshop docs are reference material only.
- Do not distribute official `Assembly-CSharp.dll` or copied decompiled source.
- Third-party Doloc Town mods under `references/third-party-mods` are samples for compatibility research only; do not merge their binaries or code into DTMAPI.
- Stardew Valley SMAPI under `references/stardew-smapi` is architectural reference only; do not copy SMAPI source or license-sensitive implementation details.
- DTMAPI-managed Strict/Advanced CodeMods and ContentPacks must not be installed under `BepInEx/plugins`. The current player Runtime uses one BepInEx plugin entry (Bootstrap) plus four co-located DTMAPI Runtime dependencies under `BepInEx/plugins/DTMAPI`; those dependencies are framework files, not managed Mods or External plugins. A third-party plugin installed under `BepInEx/plugins` is an External BepInEx Plugin outside DTMAPI ownership and management promises.

## Development Direction

The canonical Strict/Advanced/ContentPack/External identities and Platform/SharedNative/ProductNative/ContentOwner ownership rules are defined only in `PROJECT.md`. DTMAPI should be built from zero as:

```text
BepInEx bootstrap -> DTMAPI Core -> stable public API -> Strict CodeMod
                         |-> shared-native GameBridge -> stable adapter/API
                         |-> future optional Content Host (G7) -> ContentPack
                         `-> managed Advanced CodeMod -> ProductNative

External BepInEx Plugin ------------------------------------ outside DTMAPI ownership
```

Current Batch 6 status, the exact admitted-product set and runtime-evidence state are owned only by `docs/architecture/batch6-managed-mod-identity-contract.md` and its linked Updates/receipts; do not copy the fast-changing product count, milestone commits or receipt hashes into onboarding rules. Those proofs do not open a general Advanced authoring lane. `SDK160` still protects Strict, and no one may invent or hand-author the Advanced manifest value, receipt, or package; every admitted fixture/product package remains SDK- and tracked-policy-generated only. Any product beyond the contract's exact admitted set, Content Host G7 and the 0.5.5 release remain blocked until their own bounded authority changes.

Platform work stays in the platform component that owns its responsibility. GameBridge owns only SharedNative adapters, and only after at least two independent real consumers share a native owner, conflict point, or global lifecycle invariant. A single-product state machine, patch, cache, animation, or gameplay rule is ProductNative and belongs in that product's managed Advanced CodeMod after its own admission gates; a declarative domain engine belongs in an optional Content Host. `internal`, friend assemblies, providers, facades, demand routes, future reuse, Harmony use, or centralized testing do not by themselves prove SharedNative ownership.

Public APIs in `DTMAPI.Abstractions` should stay stable and avoid raw Unity, Harmony, BepInEx, or decompiled game types. API/native work must start from the native responsibility function or state holder, then classify its physical owner before implementation. Do not stabilize a public API from UI success, registry success, or smoke-helper success alone.

## Testing Rule

Codex may launch local Doloc Town and test hooks. Unless a task says otherwise, use the local game's third save slot. When a domain already has an authoritative fixture, follow its Review/Update; current AutoFishing native behavior and GC validation use the fifth save slot.

Generated Unit/QA fixtures must remain inside the managed test session described by `docs/debug/protocols/test-artifact-retention.md`; do not create new persistent top-level `%TEMP%\DTMAPI-*` roots. Fatal-window process dumps stay opt-in, require verified handoff receipts, and follow that protocol plus the evidence-retention allowlist.

## Save Commit Rule

Before changing save/load, a per-save sidecar or journal, or gameplay-state persistence, read the canonical save-commit semantics in `PROJECT.md` and classify the data as save-bound gameplay state, configuration/diagnostics, or explicit owner/orphan recovery. Do not assume that an immediate disk write is safer: ordinary gameplay state must not advance beyond the last successful native `SaveGame`; DTMAPI `SaveSaved` is its normal successful in-process notification, while an interrupted notification window requires exact native-commit proof.

The minimum relevant regression matrix is: mutate and return to title without saving restores the prior state; consume or break an item and exit without saving restores exactly one item; mutate and complete a normal native save retains the new state; and a failure around `SaveSaving` / native save / `SaveSaved` leaves the previous committed state intact or reconciles an already successful native commit without loss or duplication. Persistent owner/orphan recovery must be tested separately from ordinary gameplay mutation. Diagnostic InstantSave or a direct callback invocation proves only that technical path and does not replace normal-save plus no-save rollback evidence.

Classify each game test before launch. A `NoNativeSave` run forbids every native-save entry, records only length/hash/mtime by default, must prove the selected current/prev/bak archive files and relevant committed sidecars are unchanged **before** any runner or external file restoration, and must perform no routine byte backup or player-archive writeback on its green path. An emergency snapshot requires an explicit high-risk reason and is not semantic evidence; if it must be restored, the run is non-acceptance. A test that intentionally invokes native `SaveGame`, mutates save archives, or exercises startup repair must instead use a disposable fixture isolated from the player's live Steam AutoCloud state. Post-exit `Copy-Item` restoration of a live cloud-managed save is not an accepted isolation strategy. Keep the runtime lock and restore deliberately changed deployment, profile, config, Author source and other non-save test assets independently.

## Toolchain Rule

- Doloc Town, DTMAPI runtime projects, and every game-loaded DTMAPI-managed assembly, including Strict CodeMods, Advanced CodeMods, and managed Content Hosts, run under Unity Mono and must remain `netstandard2.0` unless a dedicated compatibility review changes that boundary. Do not retarget game-loaded assemblies to `net8.0`, `net9.0`, or another CoreCLR target.
- Until a dedicated toolchain migration changes the test target, build and source-test scripts must use a host with both an SDK and `Microsoft.NETCore.App 8.x`. Prefer the repository-local `.tools\dotnet\dotnet.exe` resolved by `Get-DotNetExe`; do not select PATH `dotnet` merely because it has any SDK.
- Do not use `DOTNET_ROLL_FORWARD=Major` as the normal validation path. Run the tracked build/test scripts so the common resolver can select or provision the compatible repository-local .NET 8 toolchain.

## Runtime Lock Rule

Multiple Codex worktrees may edit code in parallel, but the local Doloc Town runtime environment is shared. Before installing DTMAPI to the game directory, uninstalling it, launching Doloc Town, running game smoke, writing local official `MODS`, or preparing Workshop upload folders that point at the local game/user runtime, acquire the shared runtime lock:

```powershell
tools/scripts/wait-runtime-lock.ps1 -Reason "short reason"
```

After the shared-runtime operation finishes, release it from the same worktree:

```powershell
tools/scripts/release-runtime-lock.ps1
```

Use `tools/scripts/runtime-lock-status.ps1` or `tools/scripts/status.ps1` to see the current owner. Do not force-release another worktree's lock unless you verified the owner is stale or blocked and no `DolocTown.exe` process is running.

Build success is not enough for hook work. Capture evidence:

- DTMAPI startup log.
- HookProbe/TestMod log line.
- The task's authoritative save-fixture load evidence: third save by default, fifth save for current AutoFishing native/GC work.
- Exit check: no leftover `DolocTown.exe`, no Steam waiting-for-exit regression.
- Collected logs/report when available.

Do not hard-code local Steam or game paths; use local settings, environment variables, or scripts.

## Assurance Proportionality Rule

- When independent acceptance is required, keep the owning Update at `implemented` until that review passes; do not mark it `verified` and then schedule the acceptance audit.
- An audit-only task owns one Review and no Update unless it changes project files. An implementation owns one Update; create an additional root-cause Review only when new durable reasoning is actually required.
- "Atomic" means the final admission, visibility, or compatibility switch is indivisible. Hidden or blocked implementation may use several small, reversible commits and focused checks.
- Reuse existing package, Catalog, ABI, ownership, and runtime-evidence authorities. A new receipt/schema/builder/checker family requires a genuinely new canonical authority boundary and an explanation of why an existing generic mechanism cannot express it. Do not create separate receipt families for adjacent gates of one product migration. Consolidation must preserve a small live zero-leftover check for known product-owned code in mandatory Runtime; a net-zero line delta alone is insufficient.
- Historical milestone receipts are audit material. Do not add them permanently to the default full suite unless they protect a current invariant that can regress on ordinary changes.
- Validation cost follows risk: docs-only changes use document/link checks; source-only changes use focused source/unit checks; Loader/package/lifecycle changes use their focused integration checks; game smoke and the complete Release suite run at the final relevant integration or release boundary, not after every intermediate correction.
- A requested or planned "one bounded game acceptance" defines the smallest intended acceptance evidence, not a launch quota. After a failed or non-acceptance run, pass the affected focused checks and rerun the smallest necessary game smoke without separate authorization. Only an explicit current user instruction such as "launch at most once" creates a hard process cap; requests to avoid complete Release, ladders, long tests, or repeated broad regression do not imply one.
- If a complete Release suite fails, do not restart the whole suite after each repair. First pass the failed gate through its focused entry point, then exercise the still-unreached gates once as a non-acceptance diagnostic tail where they are safely callable, and batch any resulting corrections. After the tail is green, run one clean complete suite from the start against the frozen final candidate. Replay earlier gates sooner only when the change can affect them, runner/provenance integrity is uncertain, or no safe focused/tail route exists. Partial or tail runs never count as a complete-suite PASS.
- Do not create checkpoint receipts, cached-pass authorities, or a second assurance system merely to resume validation. Prefer existing child scripts or a lightweight non-authoritative `-StartAt`/`-Only` entry. A formally interrupted run may resume only when the runner already proves the exact HEAD, tracked tree, configuration, toolchain, and required artifacts are unchanged; any relevant change rejects resume and leaves the one final from-start suite as the acceptance authority.

## Update Record Rule

Every non-trivial update must be traceable. When changing runtime, hooks, UI, config menu, input, mod loading, Workshop loading, installer/package layout, public API, migrated mods, project assets, or project direction:

1. Add an update record under `docs/updates/YYYY/YYYYMMDD-NNNN-short-slug.md`.
2. Link it from the matching monthly ledger `docs/updates/INDEX-YYYY-MM.md`; the root and year indexes are navigation only.
3. Include changed files, source request and relevant review, validation, evidence links, related debug/hook/smoke/API records, rollback notes, and follow-up.
4. If validation was not run, say so explicitly.

Follow `docs/workflows/document-governance.md`: Update is the implementation lifecycle owner. Modify Review, Debug, smoke, Hook, and API records only when facts owned by those systems changed; do not copy the same completion narrative into every ledger.

## Debug Rule

When touching runtime lifecycle, shutdown, BepInEx, Harmony patches, event dispatch, input, config menu, save/load, per-save sidecars, gameplay-state persistence, Workshop loading, or mod loading:

1. Check known debug issues first.
2. Summarize known facts and rejected hypotheses before changing code.
3. Make the smallest useful change.
4. Run automatic checks and game smoke checks when relevant.
5. Append dated evidence to debug docs.
6. Do not mark solved without clean restart, game test, logs, and regression matrix evidence.

The root Debug Index is navigation only. Put issue facts in the relevant issue file and add smoke rows only for actual runtime/game runs.
