# 20260713-0014 Workshop Subscription And Prerelease Baseline Review

Status: recorded / retained subscription snapshot complete / 0.5.5 implementation and live release scan pending
Date: 2026-07-13
Scope: retained Workshop data, published-versus-local release history, GC communication boundary, and the minimum-effort baseline for the next DTMAPI/AutoFishing release
Related Update: `docs/updates/2026/20260713-0009-workshop-subscription-and-prerelease-baselines.md`
Related Catalog Review: `docs/reviews/code/2026/20260713-0004-first-party-product-catalog-fact-review.md`
Related Compatibility Review: `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`
Related GC Issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
Durable Evidence: `docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/DTMAPI Workshop Audit 20260713-124155/Results/subscription-inventory.md` and `stress-summary.md` in the same directory

## Source Request

The user asked whether the current Steam subscription directory still retains the published DTMAPI package and all first-party Mod data. They clarified that the GC problem is reported by a minority of players and appears during long sessions, that no build after the retained subscription baseline through the current local line or the future 0.5.5 target has been publicly released, and that the retained subscription artifacts and current tested local build should serve as low-effort safety baselines. The immediate public work is to republish DTMAPI and AutoFishing.

This Review preserves the request in its original order and distinguishes physical artifact evidence, user-confirmed release history and runtime evidence.

## 1. Retained Subscription Data

At the 2026-07-13 12:40 +08:00 snapshot, `D:/Steam/steamapps/workshop/content/2285550` contained 40 item directories. The following relevant package set was physically present:

- DTMAPI Workshop item `3743016467`, package version `0.5.2-alpha`, binary version `0.5.2.0`;
- all eleven first-party functional-Mod Workshop identities already established by the Product Catalog Review;
- AutoFishing item `3743799721`, internal product version `1.4.3-dtmapi`;
- ActionSpeed item `3742763309`, internal product version `1.3.4-dtmapi`;
- the remaining nine first-party products at the exact internal versions and hashes in the durable inventory.

ChestLocatorEnhancer was missing from the first preliminary check and was downloaded into the subscription tree during this audit. The final recorded state is therefore **DTMAPI plus 11/11 known first-party functional-Mod packages present**, not 10/11.

Analysis immediately following issue 1: yes, the currently known first-party published package data is available locally and is sufficient to establish a retained player-artifact regression set. The broader 40-item directory is not proof that every public external DTMAPI Mod is subscribed, and the in-audit Chest download proves that Steam owns mutation of this cache. The recorded manifest/version/tree hashes make the snapshot identifiable but do not reconstruct deleted binaries. The subscription tree must remain read-only; it is a compatibility/rollback reference, not a development output directory.

A follow-up managed-reference scan found 15 non-Runtime DLLs which reference `DTMAPI.Abstractions`: eleven first-party products and four other public subscription packages. The external samples are Workshop `3743621104`, `3743644065`, `3754869009` and `3759797170`; their exact identities, reference versions and layout caveats are in the durable inventory. They belong to the external Compatibility Matrix, not the first-party Catalog. Their presence improves the local regression set but still does not establish a complete public ecosystem.

## 2. Retained Runtime Package Quality

The retained DTMAPI `0.5.2-alpha` package was copied to a temporary path containing spaces and Chinese characters and exercised only against fake game directories. Windows PowerShell `5.1.26100.8655` parsed all eight package scripts without errors. The matrix produced the expected results:

| Case | Expected/actual exit |
| --- | ---: |
| install to missing game directory | `1` |
| install to empty game directory | `1` |
| check empty game directory | `1` |
| install to valid fake game directory | `0` |
| check after install | `0` |
| collect logs after install | `0` |
| uninstall after install | `0` |
| check after uninstall | `1` |

All required files existed after valid installation and the audit reported zero blockers **within this package stress matrix**. The matrix does not simulate an unrelated author package carrying `dtmapi-package.json`; it therefore does not waive the already-recorded installer-ownership P0. No real game directory, local official `MODS` directory, subscription package or Workshop upload folder was modified. The shared runtime lock was not required because the test used temporary copies and fake game directories.

Analysis immediately following issue 2: the retained Runtime is more than a loose DLL sample. It is a verified historical player installer/uninstaller/status/log-collection artifact and is safe to use in controlled fake-directory/compatibility tests as the old-package side of upgrade and recovery analysis. It is not a recommendation to reinstall 0.5.2-alpha into a live player directory or a production rollback package while the ownership P0 remains. This also does **not** make it an upload candidate for 0.5.5: its product version is 0.5.2-alpha and the new ownership, version-authority, packaging and release gates still apply.

## 3. Release-History Boundary

The user confirms the following publication history:

```text
retained public subscription baseline
  DTMAPI 0.5.2-alpha and the retained functional-Mod packages

unpublished local development line
  current 0.5.3-alpha source/local installs and all subsequent workspace work

future public target
  DTMAPI 0.5.5, not yet built or published as the selected final package
```

Local evidence is consistent with that statement: the subscription Runtime identifies itself as 0.5.2-alpha, while the current local game install identifies itself as package kind `local-install`, DTMAPI `0.5.3-alpha`, binary `0.5.3.0`, build commit `4ad6899a7757`, installed 2026-07-12. The repository version source is also still 0.5.3-alpha/0.5.3.0. There is no local 0.5.5 release artifact.

Analysis immediately following issue 3: the user statement is the current release-history authority; local files corroborate but cannot independently prove the live Steam backend history. Batch 0 must still read the live Workshop metadata before upload. Until then, documents must not describe the current local developer preview, the major-update work or 0.5.5 as already public. The local 0.5.3-alpha installation also contains developer/product prototypes and is not itself the frozen product roster.

## 4. GC Incidence And Current Tested Baseline

The accepted field characterization is:

> GC/Fatal reports concern a minority of players and generally arise during long-running play. This is not a measured prevalence statistic.

The current tested baseline has materially improved the previously reproducible title-long-idle path. After the input-pressure/hotkey mitigation, the Steam `FullKnown` route passed one uninterrupted title hour plus ten one-minute save-load cycles on 2026-07-08. The user therefore accepts “标题长挂机 GC 问题基本解决” as the practical current-baseline description.

Analysis immediately following issue 4: this supports using the current 0.5.3-alpha behavior and its evidence as the forward engineering baseline. The precise public claim remains narrower: **the tested title-long-idle/repeated-load route is mitigated and passed**. Active AutoFishing, ActionSpeed acceleration, other long gameplay routes and the overall Unity/Mono `Unexpected mark stack overflow` class remain open under ISSUE-010. Neither “broadly prevalent” nor “all GC solved” is accurate.

## 5. Three Baselines For The Long Update

| Baseline | Authority | Use | Do not use it as |
| --- | --- | --- | --- |
| B-PUBLISHED retained artifact | subscription DTMAPI 0.5.2-alpha plus 11 first-party packages and their recorded hashes | old-player install, ABI, manifest, upgrade, disable and rollback regression | mutable source tree or proof of every external Mod |
| B-CURRENT tested local line | current 0.5.3-alpha source/local install plus existing unit/smoke/player evidence | forward behavior, title-idle mitigation, diagnostics and implementation starting point | evidence that this build was public or a package safe to upload unchanged |
| B-055 future release target | selected DTMAPI 0.5.5 product/version gates | one new public Runtime release after RC gates | an artifact which already exists or an excuse to overwrite 0.5.5 in place later |

This model means the large update does not need to reconstruct every old Mod before work begins. The retained packages are the old side of the regression matrix; the current source/evidence is the new side. Differences must be deliberate and classified rather than erased by rebuilding old packages from new source.

## 6. Minimum-Effort Publication Sequence

The immediate release sequence remains V1 revised:

1. keep the retained subscription artifacts unchanged as the legacy compatibility/rollback set;
2. complete Batch 0, the two P0 ownership fixes, version/package authority, old-binary compatibility, player-package QA and the selected 0.5.5 GC gates;
3. publish DTMAPI 0.5.5 once, with any later Runtime fix receiving a new version rather than silently replacing the same package identity/version;
4. publish AutoFishing 1.0.0 alone as the stale-manually-installed-Runtime block/update/recovery Canary after its own active fishing gate;
5. leave every other Workshop product on its retained public package until that product reaches its separate 1.0.0 rewrite and release gate.

ActionSpeed remains part of the pre-0.5.5 Runtime GC integration gate even though this immediate publication statement names only DTMAPI and AutoFishing. Passing the integration gate does not require publishing ActionSpeed in the same wave.

Analysis immediately following issue 5: this is the safest low-effort use of the two real baselines. It limits public change to the Runtime and then the one Canary product, preserves player rollback evidence, and avoids falsely treating the unshipped developer line as an already deployed compatibility burden.

## Follow-Up

- Batch 0 records the live Workshop name/version/ownership state at release cutoff and completes the external compatibility inventory.
- Do not copy subscription binaries into source or a distributable archive as ordinary project material. If an immutable owned recovery archive is later required, create it out of the source/distribution tree with an explicit retention and non-distribution record.
- The current inventory should be rechecked immediately before 0.5.5 RC because Steam may update the cache; any digest change is evidence to classify, not a reason to overwrite this snapshot.

## Validation Boundary

This Review used read-only subscription/package inspection, SHA-256 file/tree inventory, the Workshop release-audit fake-directory matrix, current local install manifests, repository version sources and existing ISSUE-010 runtime evidence. No source/runtime product behavior was changed, no real game was launched, and no live Workshop service was queried or mutated.
