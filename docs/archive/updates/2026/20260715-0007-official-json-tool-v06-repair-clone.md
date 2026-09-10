# 20260715-0007 Official JSON Tool v0.6 Repair Clone

## Metadata

- Update ID: `20260715-0007`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `verified`
- Area: third-party/official-json/author-tool/clone/import/validation/round-trip/package
- Source: user requested a narrow fix and retest after the Touhou Fumo regression, with truthful naming as a repair version based on v0.6 and no feature expansion

## Scope

Repair only the importer, validator, and round-trip defects proven by the Touhou Fumo regression: one safe enclosing directory, explicit near-match official table-name errors, exact nested project directories, imported-path validation, exact relocation, and preservation of imported ordinary-shop seasonal records.

Rename the active handoff presentation from the retired internal v0.7/fork wording to a local repair clone based on v0.6. The source Workshop item and its author remain factual provenance; whether these fixes are merged, versioned, or published is the author's decision.

No new project type, form field, official JSON capability, game runtime behavior, DTMAPI code, or third-party Mod content is in scope. The original `D:\下载\touhou_fumo.zip` remains read-only.

## Owning Review

- `docs/reviews/code/2026/20260715-0004-touhou-fumo-author-tool-regression.md`
  - owns the reproduced defects, attribution, root causes, and acceptance order.

## Implemented Changes

### Archive and filename handling

- Normalizes exactly one common enclosing directory only when every non-directory entry is inside it and it contains both root `info.json` and at least one `Content/` file.
- Re-exports normalized imports at the standard Mod root and reports which wrapper was removed.
- Checks only the eight table basenames already supported by the tool. A unique near match or case mismatch becomes an explicit blocking import error containing the exact path and suggested official basename.
- Does not auto-rename the suspected file. Arbitrary unknown JSON remains preserve-only and non-blocking.

### Exact project boundaries and round-trip

- Discovers projects from each recognized table's exact containing directory instead of `Content/<first-segment>`.
- Stores the exact source directory separately from the editable/generated folder ID. An unchanged safe imported relative path such as `Shop/Hult` is not subjected to the generated lowercase-ID rule.
- Assigns overlapping nested files to the deepest recognized project and relocates only that project's exact source subtree. Parent/sibling files remain in place, and the replaced table cannot survive as a second nested copy.
- Lets imported assets satisfy validation by basename while preserving their project-relative path, covering the real `Content/Equipment/Texture/` layout without changing new-project output conventions.

### Ordinary-shop preservation

- Carries imported `season_spawn_data` through the existing shop form without adding UI fields.
- Reuses the imported array, including different per-season counts, weights, and unknown fields, after editing.
- Keeps the existing four `1..1` records for newly created normal-shop projects.

### Truthful handoff identity

- Moved the active local working copy to `clone/v0.6-repair/` and renamed the entry HTML to `Content/模组工具 v0.6 修复版本.html`.
- Changed the visible page label to `基于 v0.6 的修复版本`.
- Restored `info.json` to the source `0.6` baseline and describes the package as a local repair clone. The source author metadata remains intact; merge, release numbering, and publication are explicitly the author's decision.
- Replaced the current README, handoff, test, package, and evidence names with v0.6 repair-clone wording. The earlier `20260715-r2` package remains only as superseded audit evidence.

## Changed Files

Local ignored working copy:

- `references/third-party-mods/official-json-authoring-tool-fork/clone/v0.6-repair/Content/模组工具 v0.6 修复版本.html`;
- `references/third-party-mods/official-json-authoring-tool-fork/clone/v0.6-repair/info.json`;
- `references/third-party-mods/official-json-authoring-tool-fork/tests/verify-repair-clone.cjs`;
- `references/third-party-mods/official-json-authoring-tool-fork/README.md`;
- `references/third-party-mods/official-json-authoring-tool-fork/HANDOFF-20260715-v06-repair.md`.

Tracked lifecycle records:

- `.gitignore`, which keeps root `output/` delivery, extraction, and browser-evidence artifacts local instead of publishing duplicate generated copies;
- `docs/reviews/code/2026/20260715-0004-touhou-fumo-author-tool-regression.md`;
- `docs/updates/2026/20260715-0005-official-json-tool-crop-month-fix-and-handoff.md`;
- `docs/updates/2026/20260715-0006-touhou-fumo-author-tool-regression.md`;
- `docs/updates/2026/20260715-0007-official-json-tool-v06-repair-clone.md`;
- `docs/updates/INDEX-2026-07.md`.

Generated ignored delivery/evidence:

- `output/handoff/doloc-town-json-authoring-tool-v0.6-repair-clone-20260715-r3.zip`;
- its staging and two fresh-extraction verification directories under `output/handoff/`;
- source and extracted-package browser screenshots under `output/playwright/json-tool-v06-repair-*`.

## Validation

Passed:

- root `output/` is excluded by the tracked ignore rule while the authoritative review/update records remain visible to Git;
- HTML inline-script syntax and `verify-repair-clone.cjs` syntax.
- Real Chrome source suite: original six-project/24-file generation, six item-function payloads, existing unknown/multi-record round trip, wrapper typo blocker, nested shop exact round trip, four imported season records, and four new-shop defaults.
- The exact original `D:\下载\touhou_fumo.zip` (`BE7346EC8AEB00B3915CB637EEF3C0F55536FF8338E99F4AC6610DDDA6F0EC7D`) now imports through its `touhou_fumo/` wrapper as one nested shop and is blocked by a message naming both `item_tbtiem.json` and `item_tbitem.json`; no auto-renamed entry or export blob is produced.
- An in-memory derivative changing only that table basename, plus the deliberate test UI normalization `0.1` to `0.1.0`, imports as equipment plus shop and exports three nested `Texture/` PNGs, four `99..99` seasons, and exactly one shop table at `Content/Shop/Hult/`.
- Headed Playwright CLI at `1440×1000`: no clipping, overlap, width drift, or broken page layout. Against the prior same-size full-page image, pixels differing by more than `10/255` were approximately `0.84%`, concentrated in the title text. The only console error was the temporary server's optional `favicon.ico` 404.
- Final ZIP fresh extraction: 15 files total, 14 manifest-listed payload files, all SHA-256 values matched, no upstream snapshot or Touhou file, and no retired user-facing wording.
- The complete real-Chrome suite, including the local read-only Touhou comparison, passed again from the final extracted ZIP.
- Documentation governance passed after the final record updates.

Doloc Town and DTMAPI runtime validation are not required because this is a standalone third-party browser authoring tool.

## Evidence And Identity

- HTML SHA-256: `76CA591CC5781D6B61AA4DC94DDA02561EEDD34CD7F9D5ECCE506EE7C896E38A`.
- Browser test SHA-256: `F1767A2174EEDFF0C122271E2FA6577FBAD396AD62107B2959316195765112CD`.
- `info.json` SHA-256: `51ABF182B14F2263E710B8B79B6B3365826673DFA426F9CCEDDB1EAF3553366E`.
- Final ZIP size: `499159` bytes.
- Final ZIP SHA-256: `10474AFA11FFC91984C82EF1CF60B030444CBA46CA5A6B03BED26DF642B3B54A`.
- Source evidence: `output/playwright/json-tool-v06-repair-regression/`.
- Final extracted-package evidence: `output/playwright/json-tool-v06-repair-extracted-r3-final/`.

## Rollback

- Restore the prior local working-copy paths and files from the retained `20260715-r2` artifact.
- Remove the new synthetic evidence and replacement handoff ZIP.
- Revert this Update and its monthly-ledger row.
- No game, Workshop subscription, save, or DTMAPI runtime state needs rollback.

## Follow-Up

- Give the hashed r3 repair-clone package to the author for review; whether to merge and how to number/publish it remain author decisions.
- If later work adds more official tables, multi-item shops, or multi-record visual editing, start a separate design rather than expanding this bounded repair.
- After any public release, test the actual Workshop-downloaded copy offline before treating publication as verified.
