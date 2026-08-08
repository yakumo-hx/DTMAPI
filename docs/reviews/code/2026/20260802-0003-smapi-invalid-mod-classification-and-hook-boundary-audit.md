# SMAPI 失效 Mod 判定与 Hook 边界审计

- Review ID: `20260802-0003`
- Date: `2026-08-02`
- Status: `recorded`
- Scope: current local SMAPI startup failure classification, non-executing assembly analysis, Entry/runtime error handling, Harmony ownership and rollback boundary
- Change type: audit-only Review; no Runtime, API, package, Workshop, save or game mutation
- Related baseline: [DTMAPI 0.5.5 收尾后与 SMAPI 的能力复比审计](20260801-0001-dtmapi-055-closeout-smapi-capability-recomparison.md)

## Source Request

The user asked whether SMAPI decides that a Mod is invalid through two stages:

1. a non-executing DLL preflight which proves assembly, type and member
   references can resolve before the DLL enters the process;
2. a required/optional Hook transaction which rolls back already installed
   Hooks when a group cannot be located or installed, then disables the whole
   product or only the optional feature.

This Review checks those claims against the current local SMAPI source rather
than inferring behavior from log wording or from DTMAPI's ProductNative model.

## Audited Baseline

- SMAPI repository: `E:\Python_project\SMAPIlearning\SMAPI`
- branch: `develop`
- commit: `5689c8d6aeecf54f670559ffaaed6684a5febc25`
- description: `4.5.2-54-g5689c8d6`
- local worktree was clean at inspection time

Primary source paths:

- `src/SMAPI/Framework/ModLoading/ModResolver.cs`
- `src/SMAPI/Framework/ModLoading/AssemblyLoader.cs`
- `src/SMAPI/Framework/ModLoading/Finders/ReferenceToInvalidMemberFinder.cs`
- `src/SMAPI/Metadata/InstructionMetadata.cs`
- `src/SMAPI/Framework/SCore.cs`
- `src/SMAPI/Framework/Events/ManagedEvent.cs`
- `src/SMAPI/Framework/ModRegistry.cs`
- `src/SMAPI/Framework/ModLoading/ModFailReason.cs`
- `src/SMAPI/Framework/ModLoading/ModMetadataStatus.cs`
- `src/SMAPI.Toolkit/Framework/ManifestValidator.cs`
- `src/SMAPI.Toolkit/Framework/BundledModData/ModStatus.cs`
- `src/SMAPI.Toolkit/Framework/BundledModData/ModWarning.cs`
- `src/SMAPI.Toolkit/Framework/ModBlacklistData/ModBlacklist.cs`

## Verdict

The proposed description is not SMAPI's actual generic state machine.

- The first paragraph is directionally related to SMAPI's Cecil pass, but it
  overstates both its coverage and isolation. SMAPI parses metadata and scans or
  rewrites IL without executing Mod code, but the invalid-member finder calls
  itself purely heuristic and explicitly says it does not detect every case.
  SMAPI then loads the assembly into the current AppDomain before it enumerates
  the Mod types and before `Entry` runs. A later `TypeLoadException` can still be
  caught as `LoadFailed`; the preflight does not guarantee that the assembly
  never entered the process.
- The second paragraph is not a SMAPI Loader contract. Harmony usage is detected
  as the non-error warning `PatchesGame`. Each Mod installs its own patches from
  arbitrary `Entry` code. SMAPI has no manifest concept for required/optional
  Hook groups, no Loader-controlled Hook installation transaction, and no
  general `Unpatch` rollback when `Entry` fails.

SMAPI's formal startup status is only `Found` or `Failed`. The richer distinction
is carried by the failure reason and by whether the Mod was registered before
`Entry`. There is no formal `Active`, `Degraded`, `Quarantined`, or
`AutoDisabled` Mod state in this path.

## Actual Startup Decision Pipeline

| Gate | Conditions checked | Failure/result | Loader action |
| --- | --- | --- | --- |
| Folder and manifest discovery | harmful blacklist match; dot-disabled folder; empty folder; XNB-only folder; unreadable/missing manifest | `Malicious`, `DisabledByDotConvention`, `EmptyFolder`, `XnbMod`, or `InvalidManifest` | set `Failed`; do not load |
| Manifest compatibility | bundled metadata says `Obsolete` or `AssumeBroken`; `MinimumApiVersion` or `MinimumGameVersion` is newer than the running host; required fields or ID/dependency syntax is invalid; both/neither `EntryDll` and `ContentPackFor` are set; declared DLL is absent; duplicate UniqueID | `Obsolete`, `Incompatible`, `InvalidManifest`, or `Duplicate` | set `Failed`; do not load |
| Dependency graph | required dependency absent, below minimum version, already failed, or in a required cycle; a Content Pack's parent is treated as required | `MissingDependencies` | set `Failed`; required dependents fail transitively; failed optional dependencies are ignored |
| Cecil parse and compatibility pass | assembly cannot be parsed; root name already loaded; local dependency graph; known rewrites; unresolved non-System assembly reference; detected incompatible IL/member reference | `Incompatible` for detected incompatible code; otherwise `LoadFailed` for an assembly-load failure | skip unless bundled metadata says `AssumeCompatible`; that override may load detected broken code with a warning |
| Actual CLR/Mono load | `Assembly.Load` for rewritten bytes or `Assembly.UnsafeLoadFrom` otherwise; bad image, file-load/security and other load errors | `LoadFailed` | catch and skip; an assembly already loaded into the AppDomain is not generally unloaded |
| Entry type and helper initialization | enumerate `DefinedTypes`; require exactly one concrete `Mod` subclass; instantiate it; create helpers; register the Mod | `LoadFailed` | skip if any step fails |
| `Mod.Entry` | arbitrary Mod startup code, including subscriptions, file I/O and Harmony patching | exception is logged as “crashed on entry and might not work correctly” | **does not set `Failed`, unregister, dispose, unpatch, or stop the later API attempt** |
| `GetApi` | Mod-provided API creation and public-type check | exception or non-public API type | API unavailable and error/warning logged; Mod remains registered |
| Event callback | each managed event handler is invoked inside its own try/catch | handler exception | attribute and log the error, continue fanout; no failure counter, automatic unsubscribe, or whole-Mod disable |

`TryLoadMod` rechecks required dependencies immediately before loading because a
dependency may have passed topology validation but then failed during its real
assembly load. This is startup failure propagation, not runtime health
monitoring.

## Exact Meaning Of The Cecil Preflight

`AssemblyLoader.GetReferencedLocalAssemblies` first reads the root and local
referenced DLLs through Mono.Cecil. That read is non-executing. The subsequent
pass can:

- rewrite known historical/platform IL patterns;
- detect known incompatible instruction patterns;
- resolve assembly-level references, with an explicit missing-reference check
  for non-`System.*` assemblies;
- detect some invalid fields, constructors, properties and methods;
- detect risky patterns such as direct game patching, save serializer changes,
  unvalidated update events, console, filesystem and shell access.

It is not a complete CLR verifier or a general proof that all types and members
will resolve:

- `ReferenceToInvalidMemberFinder` is documented as “purely heuristic” and
  “won't detect all cases”;
- field validation and method return-shape heuristics are constrained by a
  selected always-present assembly set: `StardewModdingAPI`, `Stardew Valley` /
  `StardewValley`, and `Netcode`;
- missing non-array method references are probed more broadly, but this still
  walks statically visible IL operands rather than reflection strings, generated
  code paths, dynamic native lookups, Harmony target discovery or every generic
  runtime case;
- type enumeration occurs only after the root assembly is loaded. A
  `ReflectionTypeLoadException`, `TypeLoadException`, `MissingMethodException`
  or similar failure can therefore still appear during type discovery,
  construction, `Entry`, an event, or a later feature path;
- bundled `AssumeCompatible` metadata deliberately overrides detected broken
  code, while `AssumeBroken` can reject a Mod even when this scan finds nothing.

The accurate description is therefore **non-executing compatibility reduction
followed by real in-process loading**, not **complete non-executing ABI proof
before admission to the process**.

## Hook And Rollback Boundary

Current SMAPI source provides no Mod-facing Hook declaration or installation
service. The assembly scan's `HarmonyDetector` only sets `ModWarning.PatchesGame`.
That enum describes a detected non-error issue, not a loading failure.

The current `FixHarmony` setting applies SMAPI's own MonoMod/Harmony compatibility
hotfix. A separate `SuppressHarmonyDebugMode` setting suppresses harmful Harmony
debug behavior, and the `harmony_summary` command reports patches and owners.
None of those mechanisms gives SMAPI the
semantic information needed to decide that a patch is required, optional, part
of one feature, or safe to roll back independently.

The inspected SMAPI Core contains no generic Mod `Unpatch` call. `ModRegistry`
supports registration but not runtime removal. Mod disposal is invoked at
process/game shutdown; it is not the response to an `Entry` or event exception.
Consequently, if a Mod installs some patches or creates other side effects and
then throws in `Entry`, SMAPI logs the crash and continues. It does not guarantee
that those side effects were reversed. A Mod may implement its own transaction
and `Unpatch` discipline, but that is Mod behavior, not SMAPI's invalid-Mod
classifier.

## Three Different Meanings Of “Invalid Mod”

These must not be collapsed into one state:

1. **Startup-rejected/skipped:** SMAPI has a formal `Failed` status and a
   `ModFailReason`; the Mod is not admitted to the registry.
2. **Registered but failed during Entry/API initialization:** the Mod is loaded
   and remains registered, but SMAPI logs that it may not work correctly. Its
   subscriptions, patches or partial state may already exist.
3. **Runtime feature/callback failure:** SMAPI isolates many callback exceptions
   so the game and other handlers continue, but it does not generally change the
   Mod's status or automatically disable it.

The public compatibility repository's labels (`Broken`, `Abandoned`,
`Obsolete`, and so on) are also not the same state machine as `ModFailReason`.
The startup resolver consumes bounded bundled metadata overrides
(`Obsolete`, `AssumeBroken`, `AssumeCompatible`).

## Corrected Formulation

A source-faithful summary of SMAPI is:

> **Metadata/IL compatibility pass:** parse the Mod and its local DLL graph
> without executing Mod code, apply known compatibility rewrites, and reject
> detectable manifest, dependency, assembly-reference or heuristic IL/member
> incompatibilities. This reduces ABI failures but does not prove every type,
> member, dynamic lookup or Hook target.
>
> **Real load and entry:** load the assembly into the current AppDomain, find
> and instantiate exactly one concrete `Mod` subclass, initialize helpers, then
> call `Entry`. Failures before registration are skipped; an `Entry` crash is
> logged but does not atomically disable, unregister or roll back the Mod.
>
> **Mod-owned patch activation:** a Mod may install Harmony patches in `Entry`.
> Required/optional grouping, rollback and feature degradation are the Mod's own
> contract unless another host explicitly provides them; SMAPI does not.

## DTMAPI Design Implication

The proposed required/optional Hook transaction would be a deliberate DTMAPI
ProductNative lifecycle guarantee, not something to copy from SMAPI. If DTMAPI
adopts or retains it, its statuses should remain explicit:

- package/manifest/dependency rejection;
- non-executing compatibility-scan rejection;
- in-process assembly/type/Entry failure;
- required activation failure with complete owner rollback;
- optional feature degradation with only that feature's roots removed;
- callback quarantine versus whole-Mod deactivation;
- process-pinned dormant versus physically unloaded.

Those distinctions are stronger and more auditable than treating every logged
exception as “the Mod is invalid”. They also avoid promising physical assembly
unload on Unity Mono when only owned runtime roots and Harmony patches can be
removed.

## Validation And Limits

- Inspected the current SMAPI resolver, loader, manifest validator, compatibility
  metadata, blacklist, registry, startup, event and Harmony-detection paths.
- Confirmed all writes of `ModMetadataStatus.Failed` in current SMAPI occur in
  discovery/validation/dependency/load paths; `Entry` and managed-event catches
  only log.
- Confirmed no generic Mod `Unpatch`, registry removal or runtime automatic
  disable path exists in the inspected SMAPI Core.
- Cross-checked the claim against the existing DTMAPI 0.5.5/SMAPI capability
  Review; this focused Review does not reopen the closed 0.5.5 baseline.
- No build, unit test, game launch, runtime lock, save mutation or external write
  was needed. This is a source audit, not runtime acceptance evidence.
