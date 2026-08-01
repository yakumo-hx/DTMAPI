# Codex API Rebuild Workflow

This workflow is for rebuilding DTMAPI APIs after a native-owner audit. It is separate from manual QA feedback organization and from ordinary mod bug fixing.

Use it when the user asks for:

- API 重做、底层 API 重做、GameBridge 重做、原生责任函数审查后的实现；
- deciding whether an API is stable, experimental, debug-only, registry-only, internal, or blocked;
- turning `docs/reviews/api` findings into a narrow implementation change;
- fixing a mod bug whose root cause is an untrustworthy API boundary rather than mod logic.

Do not use this workflow for pure manual-QA triage unless the manual issue points at a public API, GameBridge boundary, or native-owner gap.

## Required Reading

Before reviewing or implementing an API rebuild, read:

- `AGENTS.md`
- `PROJECT.md`
- `docs/reviews/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/api/public-api-matrix.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/api/local-mods-native-owner/INDEX.md`
- `docs/reviews/api/smapi-ecosystem-map/INDEX.md` when the API is an ecosystem-level surface inspired by mature mod-loader patterns, UI/HUD/content pipelines, or cross-mod integration
- `docs/reviews/api/native-function-map/README.md` when function-map coverage, call relationships, or all-method context would help scope the native-owner review
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits-index.md`
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits-index.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`
- `docs/reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md`
- `docs/reviews/code/2026/20260720-0001-batch5-and-batch6-prerequisite-audit.md`

For implementation, also read the task-specific reverse maps, decompiled build, official docs, and research notes named by the review or update record.

## Current API Truth

The 0008/0009/0010 audits close the current DTMAPI public API surface. They cover existing APIs, not every possible future API and not every Doloc Town native method.

For future domains that are not already an existing public API row, start from `docs/reviews/api/native-owner-domains/INDEX.md`. That fixed library records native responsibility candidates and blocked/gap status for broad user-facing goals such as world refresh, NPCs, animals, birds, drones, vehicles, maps, equipment, effects, stacks, follower pets, and ranged weapons. It is discovery evidence only; it does not promote API stability.

For future APIs that are motivated by an existing local mod, also read `docs/reviews/api/local-mods-native-owner/INDEX.md`. That library maps current `testmods`, legacy local own-mod sources, and local third-party sample groups to semantic demand, native-owner candidates, shared owner conflicts, and final review confidence. It helps avoid rebuilding an API from one mod's convenience behavior while missing another local mod that shares the same native owner.

For ecosystem-level surfaces inspired by mature mod-loader patterns, also read `docs/reviews/api/smapi-ecosystem-map/INDEX.md`. That library maps SMAPI ecosystem semantics to DTMAPI candidate layers such as Core, UI host, GameBridge read-only query, mutation, Diagnostic, Blocked, and Future-reserved. It is clean-room research only and must not be used as SMAPI compatibility or public API stability proof.

The generated native function map under `docs/reviews/api/native-function-map/` can be used before or during Phase 1 to inspect all methods, system-map tags, native-owner report coverage, and internal call relationships. A colored or connected node is still only a research signal; method-body review and runtime evidence remain required.

Future API/native work starts from the native owner, then uses the canonical ownership classification in `PROJECT.md` before choosing a physical assembly:

```text
native responsibility function / state holder
  -> Platform / SharedNative / ProductNative / ContentOwner classification
     -> Platform: Core / Abstractions / Bootstrap / ModConfigMenu
     -> SharedNative: GameBridge adapter -> stable Abstractions contract when public
     -> ProductNative: managed Advanced CodeMod after product-specific admission
     -> ContentOwner: optional Content Host -> ContentPack contract
  -> authoritative save-fixture validation
  -> docs and API matrix
```

Do not start from a mod convenience method and then search for a native hook later. That pattern created several APIs that looked successful in UI or smoke tests but did not own the true game state. Corrected Phase 0, the G2 synthetic PASS and every bounded real-product decision are frozen in `docs/architecture/batch6-managed-mod-identity-contract.md`. G2 still verifies only the SDK-generated `DTMAPI.AdvancedFixture`; the contract alone owns the current admitted-product set and each product's runtime-evidence state. None authorizes bypassing `SDK160`, hand-written Advanced manifests/receipts/packages, arbitrary real-product migration or any product beyond that exact set.

## API Status Vocabulary

Use these categories consistently:

- `stable open`: ordinary mods may depend on it.
- `experimental open`: ordinary mods may try it, with documented limits and evidence gaps.
- `debug-only`: console/test/user-debug path only; ordinary mods must not rely on it for stable gameplay.
- `registry-only`: safe definition/index/status contract, but no runtime native creation or mutation promise.
- `DTMAPI-internal`: usable by DTMAPI's own migrated or diagnostic mods, not a general author API.
- `blocked-rebuild`: public wording or current implementation is misleading or unsafe until native-owner adapters are redesigned.

These statuses describe API visibility and promise, not physical ownership. `public`, `internal`, friend access, provider/facade shape, or a demand route cannot prove that single-product ProductNative code belongs in mandatory Runtime.

Changing an API's status is a project-facing change and must update the public API matrix, developer docs, update record, and any relevant review records.

## Rebuild Phases

### Phase 0 - Pick One Boundary

API rebuild changes should be narrow. Pick one domain or one tightly related set of APIs. Good first targets are:

- CameraZoom background/fog/room rendering;
- MachineProduction scheduler/storage/electric integration;
- EquipmentSlots native UI/storage/save transaction;
- Vehicle second motor/native multi-instance feasibility;
- CustomEntity runtime verbs;
- Input suppression native consumer.

Do not combine all high-risk APIs into one implementation change.

### Phase 1 - Native Owner Method-Body Review

Before runtime changes, inspect the relevant native method bodies or maps. The review must answer:

- Which native function actually performs the player-visible effect?
- Which object owns the authoritative state?
- Which hooks currently fire, and which are only UI/debug evidence?
- What happens on save/load, returned-to-title, room transition, map transition, disable/re-enable, and game exit?
- For save-bound gameplay data, how do in-session working state, the last successful native commit, and any sidecar/journal candidate relate?
- What singleton/global state can be polluted?
- What would break when two ordinary mods use the same API?
- Which evidence would prove the API is connected to native state?

If the native owner cannot be identified, stop and mark the update blocked or docs-only. Do not patch a mod around the missing owner.

### Phase 2 - Contract Correction

Before or during implementation, correct API semantics:

- split stable DTO/registration contracts from experimental runtime adapters;
- downgrade debug/internal/blocked APIs in docs and matrix;
- remove or annotate DTO names that imply runtime success when they only report DTMAPI registry/status;
- add owner-token, priority, stacking, and restore rules for global mutable features;
- make failure reasons explicit and user/developer visible.
- make save-bound contracts inherit successful native `SaveGame` as the commit
  fact and `SaveSaved` as its normal in-process notification; do not stabilize
  immediate sidecar persistence as an implicit autosave capability.

### Phase 3 - Physical Owner Rebuild

Apply the canonical `PROJECT.md` classification before writing Runtime/native code:

- Platform work remains in the platform component that owns the lifecycle or UI responsibility. If it needs a reusable native adapter, that adapter must separately satisfy the SharedNative gate before entering GameBridge.
- SharedNative work belongs in `DTMAPI.GameBridge.DolocTown` only when at least two independent real consumers share the native owner, conflict point, or global lifecycle invariant.
- ProductNative work belongs in the product's managed Advanced CodeMod after its own admission. G2 proves the atomic manifest, loader, SDK, Doctor/Manager, package, Harmony owner, restart/failure-diagnostic and test chain only for the synthetic fixture. The Batch 6 identity contract owns the exact admitted real-product set; every product beyond that set stays blocked until a separate bounded decision changes that canonical authority.
- ContentOwner work belongs in an optional Content Host; individual content remains in ContentPacks.

For an admitted SharedNative route, GameBridge owns the fragile native references, state reads/writes, hook lifecycle and restore, transition boundaries, arbitration and native-owner evidence. A single consumer, future reuse, Harmony use, centralized testing, an internal API, or a dormant demand route is not sufficient evidence for SharedNative.

Any public API must expose stable DTMAPI DTOs and result objects, not raw Unity, Harmony, BepInEx or decompiled game types. Advanced ProductNative code may use native types internally, but it does not thereby become a public API or a SharedNative platform capability.

### Phase 4 - Validation

Build success is not enough. For any runtime rebuild, require:

- Release build and unit tests where available;
- the authoritative domain save fixture: third local save by default, and the fifth save for current AutoFishing native behavior/GC work;
- before/after logs showing native owner state, not only UI state;
- screenshot or log evidence for the exact player-visible issue;
- returned-to-title or clean exit check when lifecycle/global state is touched;
- no leftover `DolocTown.exe`;
- an updated `docs/updates` record, plus `docs/debug`, smoke matrix, hook map, and public API matrix only where their facts changed.

For save-bound gameplay state, also require the smallest applicable matrix from
the `PROJECT.md` save-commit rule: no-save rollback, successful native-save
commit, native-save failure, and cross-store interruption with exactly-one-item
reconciliation. Diagnostic InstantSave proves the event path only; it does not
replace normal-save and no-save player semantics.

## Rebuild Implementation Record Requirements

Every API rebuild implementation must create or update:

- one task-specific API review before runtime changes when the native-owner boundary is not already established;
- one `docs/updates/YYYY/YYYYMMDD-NNNN-short-slug.md` record, initially `proposed` or `in-progress`, then finalized with the actual validation state;
- only the debug, smoke, hook, and public API records whose facts changed.

The review and update record together must include:

- target version, or an explicit docs-only/no-version statement;
- exact API/domain scope;
- prior review records to read;
- native owner questions that must be answered before code changes;
- API status changes expected;
- physical-owner classification and the resulting Platform/GameBridge/Advanced CodeMod/Content Host boundary;
- player-visible and developer-visible acceptance checks;
- blocker rules that prevent marking complete.

### Assurance reuse and staged admission

- One API/product implementation uses one owning Update. Reuse an existing native-owner Review when its boundary remains valid; create another Review only for a new ambiguity or root cause.
- Keep a multi-step implementation blocked or hidden while identity, SDK/package, native lifecycle, compatibility/QA, and runtime acceptance are delivered as small reversible slices. Only the final admission must be atomic.
- Reuse generic ownership/evidence manifests and the existing Catalog, SDK package receipt, ABI, Doctor, and release checks. G4/G5/G6-style concerns for one product should be projections of one migration evidence set, not independent product-specific schema/builder/checker families.
- A historical all-path inventory is supporting audit material, not a permanent per-product closure gate. The live gate should inspect the migration delta, any mixed files whose ownership changed, and the known pre-migration product-owned symbols/owners that must be absent from mandatory Runtime after rehome. A net-zero aggregate alone cannot prove zero leftover ProductNative code.
- Run focused checks after each slice. At final integration, run one complete Release suite and the authoritative game/long-run matrix against the exact final package. If the complete suite fails, pass the failed gate first and then exercise the unreached gates as a non-acceptance diagnostic tail; only after that tail is green should the frozen candidate receive another from-start complete run. Partial runs are not a spliced acceptance result and require no checkpoint receipt infrastructure. Reuse already passing independent runtime levels while their relevant product, Runtime, and package inputs remain unchanged.

## Strong Rules

- Existing API audits cover existing DTMAPI APIs only. New APIs still need native-owner discovery.
- Do not treat "hook fired", "UI displayed", "registry contains item", or "smoke helper passed" as native-owner proof.
- Do not stabilize a mutating API until save/load (including unsaved rollback
  and the last native commit), transition, disable/re-enable, and multi-mod
  ownership are understood.
- Do not hide a blocked shared/public API by piling replacement code into a Strict CodeMod. This does not prohibit an admitted Advanced CodeMod from owning its reviewed single-product ProductNative implementation.
- Do not expose debug-only APIs as ordinary author conveniences.
- Do not copy decompiled source or third-party mod code into DTMAPI.
- If a task cannot find a native owner, the correct result is a blocker with evidence, not a speculative runtime patch.

## Required Safety Clause

Preserve this safety clause in the review or in-progress update record:

```text
先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。
```

This clause applies to Advanced CodeMods too: they must identify the native owner, state/session boundary and cleanup responsibility. A product-owned patch may implement ProductNative behavior, but it cannot be cited as proof that a stable shared/public API was rebuilt.
