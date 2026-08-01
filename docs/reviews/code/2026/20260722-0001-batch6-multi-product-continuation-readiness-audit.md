# 20260722-0001 Batch 6 Multi-product Continuation Readiness Audit

Date: `2026-07-22`

Status: `closed — findings corrected; third real-product implementation still requires separate admission`

Scope: Audit the working-tree AutoFishing D.5 closeout and OneActionComplete second-product migration, then decide whether DTMAPI can continue implementing additional real Advanced products. This is an audit-only Review. It does not implement a fix, admit a third product, publish 0.5.5, or authorize a complete Release, L0-L5 ladder, or long run.

## Executive conclusion

Do not start a third or multi-product Advanced migration from the current working tree. AutoFishing D.5 and OneActionComplete are directionally consistent with the ProductNative route, and focused source/SDK/package checks pass, but the candidate has two behavior-boundary defects and one scaling defect that must be closed first:

1. OneActionComplete and frozen `IActionCompletionApi` compatibility can both patch `ToolCollider.HandleTools` under one valid load order.
2. AutoFishing's short-run `nativeTransientCount=0` does not observe its deeper input/animation/native state, so the current cleanup claim is stronger than the evidence.
3. Per-product build and release validation is already growing by copy rather than by the generic authority claimed by the two-product comparison.

Candidate inventory, native-owner method review, API/consumer scans, Strict-only cleanup and official-JSON content work may continue. Actual ProductNative migration remains one-product-at-a-time and requires an explicit third-product admission after this closeout.

Follow-up disposition (2026-07-22): all four implementation findings and the additional missing OneActionComplete runtime-acceptance boundary were closed by the existing AutoFishing/OneActionComplete Updates and manual-QA Review. The two-product baseline is frozen; this does not itself admit a third product.

## Audited state

- Branch: `codex/major-update-batch0-20260713`.
- Audited base HEAD: `23002c8f54d0` (`docs: add AutoFishing slimming checkpoint`).
- The AutoFishing D.5 implementation, OneActionComplete product, compatibility relocation, SDK policy, release wiring and their records are still an uncommitted working-tree candidate. Unrelated reverse-capture work is also present and was excluded from this audit.
- The existing authority admits exactly G2, AutoFishing and OneActionComplete. It still blocks every third real product, general Advanced authoring, Content Host G7 and 0.5.5 publication.

## Findings

### P1 — OneActionComplete compatibility exclusion is load-order incomplete

The admission Review requires concurrent product and compatibility ownership of `ToolCollider.HandleTools` to be rejected or avoided. The current implementation checks only Harmony owners that are already physically installed:

- frozen compatibility checks for the product owner when `IActionCompletionApi.Configure` runs in `src/DTMAPI.GameBridge.DolocTown/Compatibility/ActionCompletion/ActionCompletionService.cs`;
- the product checks for the GameBridge owner before installing its two Hooks in `products/first-party/OneActionComplete/src/Native/OneActionHookInstaller.cs`.

Compatibility demand is not installed synchronously. `OnRuntimeDemandTransitioned` queues the route, `CommitPendingDemandRoutesAtFrameBoundary` requests Hook installation later, and `ToolColliderHitHookBridge.InstallHooks` does not repeat the product-owner conflict check. This permits:

```text
old Strict consumer Configure
-> compatibility demand is pending, no GameBridge ToolCollider patch exists yet
-> OneActionComplete Entry sees no GameBridge owner and installs its product patch
-> next frame installs the pending GameBridge patch
-> both owners can reach ToolCollider.HandleTools
```

The focused script checks only the opposite order, where the product is already installed before compatibility `Configure`. Therefore the stated bidirectional fail-closed boundary is not proved and is incorrect for the pending-demand order.

Acceptance before continuation:

- make both load orders fail closed before duplicate settlement can occur;
- add one focused two-order test using the existing ownership/demand authorities;
- prove the final target has at most one resource-completion owner;
- do not introduce a new receipt family or a second Hook-governance system.

### P1 — AutoFishing cleanup evidence omits deep native transients

The D.5 observer computes `nativeTransientCount` from only:

- active session;
- input lease;
- animation lease;
- pending cast scheduler.

The actual `FishingPrimitiveHookRuntime` also owns an input override, visible-reel state, ready-state sets, current ready state, animator-speed snapshots, Hook gravity and velocity snapshots, and pull-duration snapshots. Its reset/restore paths do clear these structures, and this audit found no confirmed leak, but the current observer cannot detect a residual value in them.

The short regression therefore proves F6/title/reload flow, real fishing progress, updater/session inactivity and process exit, but `nativeTransientCount=0` cannot by itself prove all input/animation/native state was restored. The smoke matrix and D.5 Update currently describe that evidence too broadly.

Acceptance before continuation:

- expose one on-demand deep transient snapshot or count for QA reflection; do not restore resident player telemetry;
- include the input override, visible-reel state, ready-state holders and all restoration dictionaries;
- rerun only the same bounded fifth-save enable/disable/title/reload/exit regression;
- no complete Release, L0-L5 ladder or long soak is required.

### P1 — AutoFishing slimming metrics mix physical and non-empty lines

The D.5 closeout compares a physical-line baseline with a non-empty-line result. Recounting every C# file with both definitions gives:

| Scope | Baseline physical | Current physical | Physical change | Baseline non-empty | Current non-empty | Non-empty change |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| AutoFishing player `src` | 6,266 | 5,393 | -873 (-13.93%) | 5,765 | 4,964 | -801 (-13.89%) |
| optional AutoFishing QA | 8,348 | 8,358 | +10 | 7,760 | 7,769 | +9 |

The published `6,266 -> 4,964`, `-1,302`, `20.8%` statement mixes the first baseline column with the second current column. The current `4,964` value is valid only when explicitly labeled non-empty lines. The DLL reduction from 155,136 to 131,072 bytes and its recorded hashes remain correct.

This does not show that AutoFishing became heavier; it shows a real but smaller source reduction and no QA source reduction. Correct the D.5 Update, Batch 6 identity contract, roadmap and monthly Update row before treating the metric as an authority. OneActionComplete's `772` should likewise be labeled as non-empty lines; its current source is 847 physical lines.

### P2 — Product build/release validation is scaling by duplication

`build-batch6-autofishing-advanced-pilot.ps1` and `build-batch6-oneactioncomplete-advanced-pilot.ps1` are both 207 lines. Their diff changes only 25 lines on each side, mostly product identity, version, path and wording; about 182 lines, or 88%, are structurally identical.

The default full suite then invokes each product builder twice for primary/repeat artifacts. `check-release-contract.ps1` has product-specific artifact-root parameters and an identity `if/elseif`, while `check-product-catalog.ps1` has separate large AutoFishing and OneActionComplete zero-leftover blocks even though the Catalog already contains the generic `migrationAcceptance` data.

The release and install callers have begun using `AuthorSdkBuildScript`, `AuthorSdkPackageFile` and UniqueID-derived roots, but the underlying builder and release-contract path is not yet generically scalable. Copying this pattern for a third product would add another builder, roots, branches, duplicate deterministic builds and checker block.

Before a third product, consolidate the existing path rather than add another assurance family:

- one Catalog/manifest/author-intent/policy-registry-driven Advanced product build/package validator;
- at most a thin product wrapper when a stable named entry point is useful;
- build the Author SDK once per suite and use it to package the admitted products;
- keep determinism checks only where they prove package/binary reproducibility;
- iterate the existing Catalog `migrationAcceptance` contract for live zero-leftover checks;
- preserve the existing receipt, schema, Catalog, ABI, Doctor and Manager authorities.

This correction is source/tooling-only and does not require a game run.

### P2 — OneActionComplete remains an unverified runtime candidate

The product's seven C# files contain 847 physical / 772 non-empty lines. Its identity, config defaults, two-target pre-resolution, exact-owner rollback, ProductNative physical ownership, no-GameBridge dependency, SDK policy and package shape are coherent. The tracked product package rebuild reproduced:

- package SHA-256 `6559AEC1CD7F4694EC478E2226B3077E719BB99371CD3C3DDCBB403820474A5C`;
- one 34,304-byte entry DLL;
- no bundled native dependencies.

However, current tests are primarily identity, token, source topology, package and policy checks. Existing third-save OneAction evidence predates the Advanced product migration and cannot prove this new assembly. Update `20260721-0005` correctly remains `implemented` with runtime validation not run.

After the load-order fix, run one bounded third-save acceptance covering the new package's valid resource path, wrong tool/level, energy shortage or partial energy, fuel and feeder consumption, F11/config save, ActionSpeed coexistence, title/reload/disable cleanup and clean exit. Do not run full Release, L0-L5 or a long soak for this closure.

### P3 — OneActionComplete player copy overstates current verification

The English and Chinese config text and its code fallback say the resource and fuel/feed paths “are verified”, while the migrated Advanced assembly has no runtime acceptance. Use neutral “supports/covers” wording until the bounded runtime check passes, or qualify that only the historical behavior baseline was verified.

## Facts that passed this audit

- AutoFishing retains 22 product-owned Hook targets, full target resolution before installation, exact-owner rollback and on-demand QA diagnostics.
- The bounded fifth-save run did execute the new AutoFishing package, two SaveLoaded cycles, F6 enable/disable/re-enable, real PullExited progress, return to title and clean process exit.
- OneActionComplete builds as a self-contained `netstandard2.0` Advanced product and does not consume `IActionCompletionApi` or GameBridge.
- OneActionComplete's resource/fuel/feed logic remains ProductNative; the comparison found no common native owner with AutoFishing and no basis for a new SharedNative API.
- Catalog and document governance pass against the current working tree.

## Go/no-go and next route

Current decision: **NO-GO for a third or batch of real Advanced product migrations.**

Allowed now:

- close the four findings above;
- continue read-only comparison of multiple candidate Mods, native-owner method-body review and ABI consumer scans;
- continue true Strict products that need only existing Abstractions/Platform services;
- continue Oil/Mine official-JSON content and non-runtime product design within their already decided boundaries.

After closeout, admit only one third product through one Review. The current roadmap names ActionSpeed next, but it carries the separate GC/action-throughput ladder and is not a low-test candidate. Choosing Zoom or a smaller display-only product instead would be a route decision, not an automatic consequence of this audit. MoreSaves and MoreEquipmentSlots remain later, separate migrations because player save/UI/sidecar rollback risks cannot be combined safely.

## Validation performed

- `tools/scripts/test-batch6-oneactioncomplete-advanced-product.ps1`: PASS.
- `tools/scripts/build-batch6-oneactioncomplete-advanced-pilot.ps1 -Configuration Release`: PASS; recorded package hash reproduced.
- `DTMAPI.UnitTests` Release: PASS. Its completed direct-run temp session initially reported cleanup pending and was then removed through the tracked managed-session cleanup script.
- `tools/scripts/check-product-catalog.ps1`: PASS (`products=27`, `public=11`, `workshop-items=21`, `api-rows=47`).
- `tools/scripts/check-doc-governance.ps1`: PASS (`5583` checks).
- `git diff --check`: PASS; line-ending conversion warnings only.
- Source counts, builder similarity, demand activation flow, Harmony owner checks, native cleanup holders and existing evidence were inspected directly.
- No game process, complete Release suite, L0-L5 ladder or long test was run by this audit.

## Follow-up document ownership

An implementation should use the existing D.5 and OneActionComplete Updates rather than create one Update per correction. Update the smoke matrix only for the two actual bounded game runs. Update the Batch 6 contract, roadmap and monthly ledger only for facts they own. This Review remains the durable audit/root-cause source and must not be copied into a new receipt or gate family.
