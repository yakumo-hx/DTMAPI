# 20260719-0004 - Batch 5 Published11 Transient Steam Tree Drift Review

- Date: 2026-07-19
- Status: classified; exact replay required after the active GC ladder
- Scope: read-only classification of `GAME-SMOKE/20260719-010116`
- Owning Update: [20260718-0003 Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Evidence: `docs/debug/evidence/GAME-SMOKE/20260719-010116/public-product-artifact-gate.json`

## Result

The `010116` Published11 preflight remains a valid failed/blocked receipt. It observed all eleven expected Workshop identities but rejected five Steam-managed subscription trees before launching the game. ChestLocatorEnhancer, MoreEquipmentSlots, MoreSaves, YConsole and Zoom each retained the frozen file count and total byte count while their aggregate tree digest changed. That evidence did not retain per-file hashes, the DLL hash or an ACF snapshot, so the historical writer and exact changed bytes cannot be reconstructed.

A later read-only audit found that all eleven current Steam subscription trees, including those five, once again match the Catalog's frozen expected digests exactly. Steam currently reports `NeedsUpdate=0`, `NeedsDownload=0` and equal installed/latest manifest identities. The five directories received a common later touch during Steam/game activity. This supports classification as a transient Steam-managed mutable-cache or local mutation which was later restored, not as a new official Workshop manifest or an authorized baseline change. The precise historical cause remains unproven.

No subscription, Catalog baseline or Steam ACF was modified during either audit. The original `010116` result must not be rewritten as a pass. Since the physical trees are currently exact, the correct next action is a fresh fail-closed Published11 replay after the active Runtime lock holder finishes, not rebaselining.

## Current Steam Versus Batch 5 Candidate

The restored Published11 trees represent the retained published packages and are deliberately not byte-identical to the Batch 5 final-worktree candidate. Each Steam directory has its normal ID-bound `workshop.json`; the candidate does not. The remaining differences are explained by the rebuilt product DLL, deterministic package provenance, `info.version`, and, for MoreEquipmentSlots/YConsole, the Catalog current-source minimum DTMAPI version. Static manifests/assets/i18n not owned by those version/provenance changes match, and the candidate contains no unknown file, reparse point or alternate data stream observed by the audit.

The five retained-published to candidate DLL SHA-256 transitions are:

| Product | Published DLL | Batch 5 candidate DLL |
| --- | --- | --- |
| ChestLocatorEnhancer | `125704135fff47778993b268f89a90e911b7757d14938b731ab91ccc7bcde2ce` | `ffa470fa89d2ed8ae83b24e4b4e166f80727c404f2c317ffd321e106cc96cf2b` |
| MoreEquipmentSlots | `092807cc5c5db359b40d5325edd6ebc6860238e51f7f65014f5b39fbc0cf71ed` | `ec00cd03849f3f83d639594262c2d2d4c32a1e6134d4eb5596ef589e094e0a48` |
| MoreSaves | `f9cb4ce42bbecf7541c963b600440c65007e0c9f088c4414046658539862226e` | `4a98553fe7859e436e9fc7ccf3d64b1c8707ac56c8ac7e0801b1bbf3195335f4` |
| YConsole | `e5a34963c0b66d6168104af27db849d707ee644f07917d8274868f8b8299b41e` | `d1ccef6b4060c40cf9ec155f8b5bade4ec0b83b364c7999bb8b643229504ec42` |
| Zoom | `dfa74bdd3561a9e9647fec01ab9b48f095826d2a752ae920566be2c5063971b3` | `49ab4cab2087b7bff5e1a2c2963fd4308bc0412aec6d714e251d8208ecf338f1` |

This is a known retained-published versus refactor-candidate version boundary, not evidence of unknown contamination.

## Acceptance Boundaries

Published11 and a current Batch 5 Candidate11 answer different questions:

- a fresh Published11 run may prove the retained Steam packages still pass the frozen public-artifact and runtime compatibility boundary;
- it cannot prove the rebuilt Batch 5 product DLLs were loaded;
- Local11 `GAME-SMOKE/20260719-034102` proves current local product behavior, but it predates candidate `040700` and does not by itself bind every loaded product byte to that directory;
- an authoritative Candidate11 boundary therefore needs a newly frozen final candidate plus a Local11 run whose source/tree/DLL receipts equal that exact candidate.

The project may establish Candidate11 as a local release-candidate validation name, but it must not call it Published11, mutate the retained Workshop baseline, or imply that the products were publicly uploaded. Live Workshop upload, ownership/name/version cutoff validation and clearing any Catalog release stop require explicit release authority outside this read-only classification.

## Next Gate

After the formal GC ladder releases the shared Runtime lock:

1. re-run Published11 against the unchanged Catalog baseline and current exact Steam trees;
2. finish the no-QA single-deadline correction and rebuild a final candidate;
3. freeze Runtime plus eleven candidate product hashes and run a byte-bound Candidate11/Local11 acceptance;
4. retain `010116` as negative history even if the new replay passes.

No product source, game state, save, profile, subscription, ACF, baseline or Runtime lock was changed by this review.
