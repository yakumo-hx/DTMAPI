# 20260722-0001 Two-Product Baseline Closeout Feedback

## Review Header

- Time: `2026-07-22`
- Resolution status: `accepted — all five corrections passed and the two-product baseline is frozen`
- Source: user follow-up after the AutoFishing slimming and OneActionComplete migration review.
- Scope: durable review followed by corrections and bounded validation.
- User constraints: preserve the reported issue order; run focused checks per slice; run only one new short fifth-save AutoFishing process and one new short third-save OneActionComplete process; do not publish 0.5.5; do not run the complete Release suite, L0-L5, soak or other long tests; do not admit or split a third product before the two-product baseline is frozen.
- Related code review: [Batch 6 Multi-Product Continuation Readiness Audit](../../code/2026/20260722-0001-batch6-multi-product-continuation-readiness-audit.md)
- Owning Updates: [AutoFishing Product Slimming](../../../updates/2026/20260721-0004-autofishing-product-slimming.md), [OneActionComplete Second Advanced Product](../../../updates/2026/20260721-0005-oneactioncomplete-second-advanced-product.md)
- Files/docs inspected: current AutoFishing QA observer/recovery holder map, OneActionComplete product Hook installer, frozen ActionCompletion compatibility service and GameBridge installation boundary, Advanced builders/release checks, Catalog, the two owning Updates and current Batch 6/roadmap authorities.
- Not inspected: no screenshot was supplied; no game process or new runtime log had been inspected when this review was recorded.

## Issue Review

### Issue 1: OneActionComplete dual-owner load-order gap

Original feedback:

- If the old Strict compatibility consumer requests demand first and the Advanced product loads afterwards, GameBridge can still install the same action-completion Hook on the next frame. Existing coverage proves only the reverse order. Both orders must fail closed.

Screenshot/log transcription:

- No screenshot or log was supplied.

Review record:

- User-confirmed facts: compatibility-demand-first is not covered by the current tests.
- Code/doc facts inspected: product-first is rejected while configuring Compatibility, and Compatibility-first is rejected once the physical GameBridge owner already exists. A pending demand is nevertheless stored before GameBridge owns the Hook, and the next-frame GameBridge installer did not re-check the managed product owner before consuming it.
- Codex inference: the collision window is between demand registration and the next-frame physical install, not a failure of the product's own two-target pre-resolution/atomic patch transaction.
- Ownership: frozen Compatibility demand lifecycle plus the GameBridge install boundary; the product retains ProductNative ownership of its two Hooks.
- Root-cause hypotheses: the owner check is located only at request time and therefore cannot guard owner changes between request and installation.
- Rejected/unproven hypotheses: there is no evidence that both owners have already patched the same method in a runtime process; the defect is a reachable fail-closed gap found by code-path review.
- Required downstream updates: OneActionComplete Update, focused ActionCompletion Hook map, and focused unit/static gates.
- Acceptance checks: prove product-first/request-second rejects without storing demand; prove request-first/product-second clears or rejects pending ActionCompletion demand synchronously before GameBridge installs either action-completion Hook; keep unrelated shared demands functional.
- Blocker conditions: any order can leave an ActionCompletion callback demand active while the managed product owner is present, or either owner can install a duplicate action-completion Hook.

Resolution (2026-07-22): PASS. Product-first/request-second now rejects before storing demand. Request-first/product-second is reconciled at GameBridge's physical Hook-install boundary, removes all pending ActionCompletion policy/parent/child demand and preserves unrelated shared demand. The focused bidirectional unit passed.

### Issue 2: AutoFishing disable recovery proof is shallow

Original feedback:

- `nativeTransientCount` omits input override, Ready collections/current state, Animator speed, gravity, velocity and Pull-duration snapshots. Restore code exists and no actual leak has been found, but current evidence cannot support a claim that all native state is cleared.

Screenshot/log transcription:

- No screenshot or log was supplied.

Review record:

- User-confirmed facts: the published aggregate does not count the named deeper holders.
- Code/doc facts inspected: the product has explicit restore/clear paths for those holders, while optional QA observes only sessions, leases and scheduler work. Ordinary player packages correctly exclude the QA assembly.
- Codex inference: this is an evidence-coverage defect, not a demonstrated player-state leak. Reflection-only, on-demand QA can enumerate the product-held deep snapshots without adding resident product diagnostics or a product-to-QA reference.
- Ownership: optional product QA owns observation; ProductNative continues to own restoration behavior.
- Root-cause hypotheses: D.5 reduced the resident observation seam but did not preserve a complete replacement inventory for every restore holder.
- Rejected/unproven hypotheses: `nativeTransientCount=0` did not prove the underlying native objects equal every original value; the corrected assertion must be limited to zero product-held overrides/snapshots plus successful behavior recovery.
- Required downstream updates: AutoFishing Update, QA observer/recovery fixture, focused behavior matrix and one actual smoke-matrix row after the reserved run.
- Acceptance checks: on-demand observation reports every named holder individually and includes it in the aggregate; focused QA populates and detects each holder; the one final fifth-save disable/title/re-entry run ends with each field zero and clean native behavior recovery.
- Blocker conditions: any named holder is unobserved, nonzero at final cleanup, or the docs still claim more than the observer can prove.

Resolution (2026-07-22): PASS. The on-demand QA observer fail-closes over all named holders and its seeded unit detects each one. `AUTOFISHING-SLIMMING/20260722-deep-state-closeout` ended with both transient aggregates zero, both booleans false and all eight counts zero after the fifth-save F6/title/re-entry flow. The accepted claim is limited to empty product-held overrides/snapshots plus recovered native behavior.

### Issue 3: AutoFishing slimming metrics mix line-count definitions

Original feedback:

- Physical lines are `6,266 -> 5,393` (`-873`, about `13.93%`); non-empty lines are `5,765 -> 4,964` (`-801`, about `13.89%`); the DLL is `155,136 -> 131,072` bytes (about `15.5%`). The slimming conclusion stands, but authoritative records must use one explicit denominator per comparison.

Screenshot/log transcription:

- No screenshot or log was supplied.

Review record:

- User-confirmed facts: the supplied physical/non-empty/DLL figures and corrected percentages replace the mixed `6,266 -> 4,964` statement.
- Code/doc facts inspected: the D.5 Update, monthly row, Batch 6 contract and roadmap repeat the mixed line-count statement.
- Codex inference: this is a documentation-accounting correction; it does not require another product source edit and should preserve the current `22` player-source files.
- Ownership: the D.5 Update owns implementation metrics; architecture/roadmap/index only summarize and link.
- Root-cause hypotheses: the baseline physical count was compared against the current non-empty count.
- Rejected/unproven hypotheses: line reduction alone is not performance or allocation evidence; DLL size alone is not GC evidence.
- Required downstream updates: D.5 Update and its active summaries.
- Acceptance checks: every current-authority slimming statement labels physical and non-empty lines separately and uses the corrected deltas/percentages; DLL reduction is labeled approximately `15.5%`.
- Blocker conditions: any active authority retains `6,266 -> 4,964`, `-1,302`, or `20.8%` as one line-count comparison.

Resolution (2026-07-22): PASS. Active authorities now separately record physical `6,266 -> 5,393` (`-873`, `13.93%`), non-empty `5,765 -> 4,964` (`-801`, `13.89%`) and DLL `155,136 -> 131,072` bytes (about `15.5%`). Remaining appearances of the mixed statement are this Review and the readiness audit describing the rejected claim.

### Issue 4: Advanced product tooling is still copied per product

Original feedback:

- The two product builders are about 88% identical, and Release/zero-leftover checks still have product-specific branches. Before any third product, replace them with one Catalog-driven Advanced builder/validator and retain at most thin compatibility wrappers.

Screenshot/log transcription:

- No screenshot or log was supplied.

Review record:

- User-confirmed facts: a third copied implementation is not acceptable.
- Code/doc facts inspected: both 207-line builders repeat SDK validate/build/pack and package checks; release wiring and the Catalog checker branch on AutoFishing versus OneActionComplete even where both rows carry the required identity, package, policy, owner and migration-acceptance facts.
- Codex inference: the Catalog already supplies enough shared authority for one loop-driven builder/validator. Identity-specific gameplay/static tests remain product-owned and should not be forced into a generic platform abstraction.
- Ownership: Platform tooling owns generic SDK/package/Catalog validation; product wrappers and gameplay tests stay product-specific.
- Root-cause hypotheses: the second pilot copied the first bounded proof before the two-consumer comparison exposed the stable shared shape.
- Rejected/unproven hypotheses: shared script shape does not make product Hook/state engines SharedNative; no new runtime/public API is justified.
- Required downstream updates: both owning Updates, release/common/full-suite wiring (without running the full suite), Catalog checker and focused tooling tests.
- Acceptance checks: one generic implementation iterates or selects Catalog rows; wrappers only forward stable product IDs; common release/zero-leftover validation has no AutoFishing/OneAction identity branch; both products build deterministically and pass the same package checks.
- Blocker conditions: adding a product still requires copying a builder/validator branch, or generic validation weakens either product's live zero-leftover gate.

Resolution (2026-07-22): PASS. `build-batch6-advanced-product.ps1` selects the admitted Catalog row and owns the common validate/build/pack/package checks; both old product entry points are thin Catalog-ID wrappers. Release contract and live mandatory-Runtime zero-leftover checks iterate Catalog Advanced rows without AutoFishing/OneAction identity branches. Both final products passed the generic path.

### Issue 5: OneActionComplete lacks post-migration game acceptance

Original feedback:

- Source boundary, SDK, package hash, Catalog and zero-leftover checks pass, but the migrated Advanced DLL has not run in the game.

Screenshot/log transcription:

- No screenshot or log was supplied.

Review record:

- User-confirmed facts: existing runtime evidence predates the new Advanced assembly.
- Code/doc facts inspected: Update `20260721-0005` correctly records runtime validation as not run, while product config copy incorrectly says the migrated resource and fuel/feed paths are already verified.
- Codex inference: historical Strict/Compatibility smoke is a behavior baseline only. A bounded third-save run must bind the new package/owner and exercise representative positive, negative, coexistence, config and cleanup paths.
- Ownership: OneActionComplete Update owns admission completion; actual run evidence belongs in the smoke matrix/evidence root.
- Root-cause hypotheses: migration intentionally stopped at focused source/package checks before this user-authorized acceptance run.
- Rejected/unproven hypotheses: a valid SDK receipt or package hash cannot prove Unity Mono load, real Harmony execution, F11 config persistence, title reload or owner cleanup.
- Required downstream updates: neutral product config wording before the run, OneActionComplete Update, ActionCompletion Hook evidence and one smoke-matrix row after the actual process.
- Acceptance checks: one third-save process loads the exact Advanced package, exercises resource success and wrong-tool non-match, fuel/feed and ActionSpeed coexistence, config-menu persistence, title/reload/disable cleanup, exits cleanly and leaves no duplicate Compatibility owner.
- Blocker conditions: the migrated assembly does not load/patch exactly twice, any representative behavior or persistence case fails, both owners become active, cleanup fails, or a process remains.

Resolution correction (2026-07-22): PARTIAL. `ONEACTIONCOMPLETE-ADVANCED/20260722-third-save-accepted` binds the final package and proves exactly two product patches/callback ready, representative positive/negative/fuel/feed/ActionSpeed cases, foreground physical F11, title/save restoration, restored test state and clean process exit. It does not prove partial-energy behavior, saving/reloading a ConfigMenu value or actual OneActionComplete owner deactivation. Follow-up fixtures were added, but `GAME-SMOKE/20260722-125239` stopped at the earlier ActionSpeed failure before they executed; those three conditions remain open.

## Cross-Issue Summary

- Confirmed user facts: the two load orders, deep AutoFishing holder coverage, exact line-count definitions, duplicated two-product tooling and missing OneActionComplete runtime acceptance are all explicit acceptance boundaries.
- Screenshot/log facts: none supplied.
- Code-path findings: the owner collision is a request/install time-of-check gap; AutoFishing restoration logic exists but optional QA observation is incomplete; the tooling duplication is package-policy glue, not proof of SharedNative gameplay ownership.
- Risks: duplicate Harmony settlement, overclaiming cleanup, weakening zero-leftover validation during consolidation, and treating a package proof as runtime proof.
- Suggested implementation scope: close issues 1-4 with focused source/unit/package checks, run the reserved AutoFishing fifth-save process, then run the reserved OneActionComplete third-save process and freeze the two-product baseline.
- Items that should not be carried forward: no third product, no 0.5.5 publication, no complete Release/L0-L5/long run, and no new shared runtime/API merely because two build scripts were similar.

## Implementation Record Decision

- Create/update an implementation update record: update the existing AutoFishing `20260721-0004` and OneActionComplete `20260721-0005` records; do not create a third lifecycle owner for their acceptance corrections.
- Additional debug/API/hook/smoke records required: update the focused ActionCompletion Hook map because the install lifecycle changes; add smoke rows only after actual game processes. No public API matrix change is expected because both legacy ABIs remain frozen.
- Suggested task title: two-product Advanced baseline closeout.
- Completion standard: all five acceptance gates pass in order, the two allowed short runtime processes pass, documentation uses bounded claims, and the scoped two-product baseline is committed without admitting another product.

## Final Disposition

All five issues are closed. AutoFishing and OneActionComplete remain the only two admitted real Advanced products; the generic two-consumer tooling is Platform, while SharedNative promotion remains zero because the two products share no native owner. No 0.5.5 publication, complete Release, L0-L5, long test or third-product split was performed.
