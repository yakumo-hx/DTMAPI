# 20260712-0008 DTMAPI Full Boundary Audit

## Metadata

- Update ID: `20260712-0008`
- Date: 2026-07-12
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: user requested a full durable boundary audit before beginning the long-running major DTMAPI update

## Summary

Recorded the repository-wide pre-implementation boundary audit for runtime loading, QA/Smoke, Compatibility, first-party products, public APIs, versions, release packaging, Workshop/product identity, author guidance, inactive work, and the open Unity/Mono GC class.

The audit confirms that the outer project-reference direction is healthy but GameBridge physically combines production adapters, product services, compatibility, diagnostics, and a 13,427-line embedded Smoke harness. It records two P0 correctness boundaries that must precede structural work:

1. Oil's coal-drop product behavior is active in base GameBridge without an OilMod owner/demand boundary and is directly coupled to OneActionComplete.
2. The author-documented `dtmapi-package.json` is treated by uninstall as DTMAPI installer ownership based only on file existence, so third-party content can be included in official-package removal/backup.

The review also records current version/product/release drift, the missing CodeMod author/doctor path, per-frame Smoke/content-signature work, unconditional Feature/Hook activation, product-versus-test classification drift, API consumer/status boundaries, protected JSON+PNG+WAV animal behavior, and the ordered Batch 0-8 implementation program.

No runtime, API, Hook, Mod identity/version, package, game, Workshop, or issue-state implementation changed in this audit.

## Source Request And Review

- Source request: begin the major long-running update with a full boundary audit and write all necessary durable records.
- Canonical review: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`.
- Related roadmap: `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md` remains orientation; the Review owns the expanded current ordering and gates.
- Related issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md` remains open and was not reclassified.

## Recorded Boundary Decisions

- Freeze UniqueID, WorkshopID, official folder, package/config/save identity, public API status, and protected player behavior before moving products.
- Fix Oil demand/OneAction coupling and installer receipt ownership before broad source movement.
- Establish one machine-readable Runtime version projection and one product/release catalog before correcting individual product versions.
- Keep the existing numeric prerelease-suffix compatibility policy; no strict SemVer policy change is authorized.
- Keep the player runtime at five production DLLs with no QA/test assembly during the planned optional-QA extraction.
- Do not add public Abstractions APIs for QA and do not move Unity/Harmony/native implementation into ordinary Mods.
- Preserve CustomAnimals JSON+PNG+WAV as a behavior regression baseline without promoting blocked C# CustomEntity runtime verbs.
- Treat repository zero-consumer results as deprecation research, not public API deletion permission.
- Keep ISSUE-010 open; recurring-work findings and package/source reduction are not a GC root-cause or fix claim.

## Ordered Follow-Up

1. Batch 0 identity/product/behavior freeze and release catalog baseline.
2. Batch 1A Oil owner-demand safety; Batch 1B installer receipt ownership safety.
3. Batch 2 Runtime/Mod/version/release/status authority.
4. Batch 3 CodeMod SDK/template/packager/doctor.
5. Batch 4 optional QA host and staged Smoke/Core/Bootstrap seam extraction.
6. Batch 5 recurring-work reduction and demand-driven Hooks/updaters.
7. Batch 6 behavior-equivalent first-party productization, with MoreEquipmentSlots last.
8. Batch 7 API metadata/compatibility warning and optionalization policy.
9. Batch 8 GMCM, MoreSaves, Y-console, animal economy, audio, and future-platform feature work.

Each implementation batch must create its own Update and any required task-specific Review. This audit is not a mutable implementation ledger.

## Changed Files

- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- this Update
- `docs/updates/INDEX-2026-07.md`

## Validation

- `tools/scripts/check-doc-governance.ps1`: passed, 4,188 checks.
- `git diff --check`: passed; only the existing Windows line-ending notice was emitted.
- `tools/scripts/test.ps1 -Configuration Release`: passed in 122.5 seconds, with zero build warnings/errors, `DTMAPI.UnitTests: OK`, runtime evidence retention tests OK, and evidence allowlist OK.
- Review citations and static inventories were cross-checked against the current source, manifests, release scripts, author docs, API matrix, and related durable records.

## Runtime Evidence

Not required for this docs/source audit. No runtime lock, install/uninstall, game launch, third-save smoke, Workshop mutation, or player validation was performed. Existing runtime evidence is cited only within its recorded scope.

Steam's live item versions and WorkshopID mappings were not externally verified; that is an explicit Batch 0-2 release-catalog gate.

## Rollback

Remove this Update row/record and the linked Review. No runtime, API, Hook, package, Mod, game, Workshop, or issue rollback is needed.

## Follow-Up Entry Point

The next implementation must start with Batch 0 plus the two independently rollbackable P0 safety tasks. Do not start QA movement, mass directory changes, product version rewriting, GMCM/Y-console feature work, or compatibility deletion before those ownership and identity gates are recorded.
