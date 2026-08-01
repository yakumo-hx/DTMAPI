# 20260713-0009 Workshop Subscription And Prerelease Baselines

## Metadata

- Update ID: `20260713-0009`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Area: review/release/workshop/subscription/compatibility/version/gc/autofishing
- Source: user baseline clarification plus read-only local subscription and package audit

## Source Request

The user asked whether the retained Steam subscription directory still contains the published DTMAPI and first-party Mod data, clarified that GC reports concern a minority of long-session players, confirmed that the current developer line and future 0.5.5 have not been published, and selected the retained subscription artifacts plus the current title-idle-mitigated local line as low-effort safety baselines before republishing DTMAPI and AutoFishing.

## Lifecycle Outcome

Created `docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md` as the focused owner of the three-baseline model:

- published player artifact: retained DTMAPI 0.5.2-alpha plus eleven first-party functional-Mod packages;
- unpublished tested local line: DTMAPI 0.5.3-alpha and its existing behavior/runtime evidence;
- future release target: DTMAPI 0.5.5, which does not yet exist as a final public artifact.

Captured a durable subscription inventory with versions, minimum DTMAPI requirements, sizes and tree digests. The subscription root had 40 item directories and ended the audit with all eleven known first-party functional-Mod identities plus DTMAPI present. ChestLocatorEnhancer arrived during the audit, so the record explicitly treats Steam content as mutable cache rather than an immutable archive.

Refreshed the managed compatibility inventory after the cache change. The current tree contains 15 non-Runtime DLLs referencing `DTMAPI.Abstractions`: eleven first-party products plus four other public consumers, with a `9 x 0.5.1.0 / 6 x 0.5.2.0` reference-version split. The four external samples now have explicit layout/minimum-version classification work in the 0.5.5 compatibility Review.

Corrected the GC communication boundary from high incidence to a user-reported minority/long-session field pattern. Recorded the 2026-07-08 one-hour-title plus ten-save-load `FullKnown` pass as the practical current title-idle baseline while keeping active gameplay and the broader Unity/Mono Fatal class open.

Preserved V1 revised as the publication sequence: DTMAPI 0.5.5 once, then AutoFishing 1.0.0 alone as the old-Runtime update/recovery Canary; the other products remain on their retained packages until their own 1.0.0 gates.

## Changed Files

- `docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/DTMAPI Workshop Audit 20260713-124155/Results/*`
- `docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md`
- `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md`
- `docs/reviews/code/2026/20260713-0004-first-party-product-catalog-fact-review.md`
- `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/updates/INDEX-2026-07.md`
- this Update

## Validation

- read-only subscription root inventory: 40 item directories; DTMAPI plus 11/11 known first-party functional Mods present at final capture;
- package-tree SHA-256 inventory: captured for DTMAPI and all eleven first-party packages;
- Mono.Cecil managed-reference refresh: 15 non-Runtime `DTMAPI.Abstractions` consumers, split into 11 first-party plus 4 external public packages and 9 references to `0.5.1.0` plus 6 to `0.5.2.0`;
- retained DTMAPI package audit under Windows PowerShell `5.1.26100.8655`: all package scripts parsed; missing/empty/valid install, status, collect-logs, uninstall and post-uninstall cases matched expected exit codes; blockers `0` within this fake-directory matrix; the separate author-content ownership P0 was not exercised or waived;
- durable matrix summary: `docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/DTMAPI Workshop Audit 20260713-124155/Results/stress-summary.md`;
- real Doloc Town launch/game runtime: not required and not run;
- live Steam Workshop backend inspection: not run; remains a Batch 0 release-cutoff gate.

## Evidence Links

- focused baseline Review: `docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md`;
- subscription inventory: `docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/DTMAPI Workshop Audit 20260713-124155/Results/subscription-inventory.md`;
- package stress matrix: `docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/DTMAPI Workshop Audit 20260713-124155/Results/stress-summary.md`;
- Catalog facts: `docs/reviews/code/2026/20260713-0004-first-party-product-catalog-fact-review.md`;
- compatibility gate: `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`;
- GC issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`.

## Rollback

Remove this focused Review, Update, generated fake-directory audit evidence and monthly-index row, then revert only the dated cross-reference/wording additions in the Catalog, compatibility, GC Review and ISSUE-010 records. Do not modify or delete Steam subscription content as rollback.

## Follow-Up

Batch 0 must refresh the physical inventory and query live Workshop metadata at release cutoff. DTMAPI 0.5.5 remains blocked on the previously selected ownership, packaging, version/ABI, QA and GC gates; AutoFishing 1.0.0 remains a separate second public wave.
