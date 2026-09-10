# 20260715-0003 Official Tutorial Real-Environment Audit

## Metadata

- Update ID: `20260715-0003`
- Date: 2026-07-15
- Lifecycle Status: `in-progress`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `partial`
- Related Issue State: `open`
- Area: official Workshop tutorials, crawl fidelity, JSON/sample parity and real-game validation
- Source: user request to check and proofread the official tutorials in the real game environment

## Review

- [Official Tutorial Real-Environment Audit](../../archive/reviews/code/2026/20260715-0002-official-tutorial-real-environment-audit.md)

## Scope

- Repair virtualized code-block collection in the current official Feishu crawl.
- Generate a current tutorial/code/sample audit matrix without committing official sample assets.
- Add an isolated official-mod smoke profile that can restore the user's enablement state.
- Run the official example package against Steam public build `23762374` and save slot 3.
- Separate package-load proof from native-table and representative player-visible behavior evidence.

## Changed files

- `tools/scripts/crawl-official-workshop-docs.py`
- `tools/scripts/audit-official-tutorials.py`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `references/doloc-town/official-workshop-docs/README.md`
- `references/doloc-town/official-workshop-docs/feishu-crawl-20260715/**`
- `docs/reviews/code/2026/20260715-0002-official-tutorial-audit-data.json`
- `docs/reviews/code/2026/20260715-0002-official-tutorial-matrix.tsv`
- `docs/reviews/code/2026/20260715-0002-official-tutorial-runtime-expectations.tsv`
- `docs/debug/regressions/smoke-matrix.md`
- the linked Review, this Update and `docs/updates/INDEX-2026-07.md`

## Results

### Archive fidelity

- Located the prior DLK static audit and official-example ModDoctor report and retained them as historical hypotheses only.
- Corrected long virtualized code blocks by merging stable block fragments through `data-line-num`; corrected duplicated descendant-list text by exporting each block's own text.
- The final crawl is `complete=true`: 55 pages, 86/86 complete code blocks across 24 pages, 107/107 sheets, 138/138 rendered images, zero page failures and an empty remaining queue.
- Verified all 1,081 manifest inventory entries: zero missing files, size mismatches or SHA-256 mismatches.
- The first full v1.1.0 pass hit its outer 15-minute command timeout after caching 46 corrected pages. `--resume` reused only schema-v2 complete pages, fetched the remaining pages and completed successfully.

### Tutorial and official-example parity

- Mapped 31 tutorial/sample domains: 18 static pass, one warning, one fail and 11 visual/reference-only pages.
- Parsed 81 mapped tutorial JSON/JSONC blocks: 80 valid and one invalid. No mapped block was incomplete or reduced to a placeholder.
- Parsed all 113 JSON files under the current official example's `Content` tree successfully; ten are explicitly parked under `_IGNORE` and are excluded from runtime expectations.
- Blocking tutorial defect: `07 综合案例二（新增鱼)` (`KkZ0wpgT1iLu4Vk2P32cNxnrnCd`), code block 5, heading `11. recipe_tbrecipe.json`, line 20. The object opened at line 17 is missing its closing `}` before the `input_items` array closes, producing `Expecting ',' delimiter: line 20 column 5`.
- Current tutorial/sample mismatch: `04 新增配方` documents recipe/output ID `equipment0`; the current official example `recipe_tbrecipe.json` uses `decoration0`.
- Three of the four JSON syntax failures recorded by DLK in May no longer reproduce in the current live pages. They are treated as fixed upstream, not carried forward as current defects.
- Excluding `_IGNORE` templates, the combined official sample contains 35 repeated table+ID pairs across tutorial subfolders. These remain aggregation warnings; the audit does not misclassify them as 35 tutorial defects.

### Isolated real-game gate

- Confirmed Steam public build `23762374` and official example `Workshop.3705665433`, version `0.96.06`, 1,349 installed files.
- Added fail-closed `-IsolateAllOfficialMods` support to the tracked smoke runner. With profile `CoreOnly`, the official example was the only enabled official-local/Workshop entry and all other 64 entries were disabled temporarily.
- Final acceptance run `GAME-SMOKE/20260715-015314` passed startup, HookProbe, native `LoadGame`/`SaveLoaded` for user save slot 3 (native index `2`), profile gates, profile restoration, no-fatal-window, forced close and process exit. Crash evidence was stale-only and no `DolocTown.exe` remained.
- The original `mod_infos.json` state was restored by the runner; the shared runtime lock was released.

## Validation

- `python -m py_compile tools/scripts/crawl-official-workshop-docs.py tools/scripts/audit-official-tutorials.py`: passed.
- `tools/scripts/build.ps1 -Configuration Release`: passed before the runtime run, including `DTMAPI.UnitTests: OK`, zero warnings and zero errors.
- Static tutorial audit intentionally exits nonzero while the one current invalid JSON block remains; generated results are retained in the linked JSON/TSV files.
- Corrected snapshot inventory audit: passed, 1,081/1,081.
- Game smoke: passed at `GAME-SMOKE/20260715-015314`; recorded in the active smoke matrix.
- Player-visible manual behavior validation was not run.

## Evidence boundary

- Raw official sample files remain outside the tracked repository.
- Runtime evidence follows the repository evidence-retention protocol; only the final actual run was added to the active smoke matrix.
- A clean package load is not evidence that each recipe, crop, fish, shop, resource, visual replacement or vehicle interaction works.

## Rollback

- Revert task-specific tooling and documentation changes.
- Restore the pre-run `mod_infos.json` from the smoke evidence backup if automatic restoration fails.
- No rollback may delete or overwrite the official Workshop subscription.

## Follow-up

- Keep the lifecycle `in-progress`: archive fidelity, static parity and isolated package loading are complete, but the 93-entry native-table expectation matrix is still `not-run` and representative player-visible behavior is still `not-run`.
- Before publishing a DTMAPI compatibility guide, decide whether its correction notes should quote the live tutorial identity (`equipment0`) or the shipped sample identity (`decoration0`); do not silently choose one.
- For the next runtime phase, isolate representative tutorial subfolders or add a read-only native-table probe. Do not infer per-tutorial correctness from the combined example package.
