# Touhou Fumo Author-Tool Import/Export Regression

Status: recorded

Date: 2026-07-15

Scope: test the final authorized v0.7 handoff tool with the original typo-bearing `D:\下载\touhou_fumo.zip`, then isolate the known `item_tbtiem.json` defect without changing any inner file bytes

Related Update: `docs/updates/2026/20260715-0006-touhou-fumo-author-tool-regression.md`

Resolution: fixed and reverified by `docs/updates/2026/20260715-0007-official-json-tool-v06-repair-clone.md`; this review remains the frozen owner of the pre-fix evidence and root causes.

## Source Request And Boundaries

The user asked to test the corrected browser authoring tool with the previously audited Touhou Fumo Mod whose item table is misspelled `item_tbtiem.json`.

Inputs:

- original third-party archive: `D:\下载\touhou_fumo.zip`;
- size: `30051` bytes;
- SHA-256: `BE7346EC8AEB00B3915CB637EEF3C0F55536FF8338E99F4AC6610DDDA6F0EC7D`;
- tested tool: the exact staged contents of `output/handoff/doloc-town-json-authoring-tool-v0.7-authorized-20260715-r2.zip`;
- tool HTML SHA-256: `BA56B0DEA224BD72660A9FBCD1DBF728BD9A177E8A67E5F6E5943FF7D70B5D3E`.

The original archive was read-only. No third-party source file was renamed or edited, no Mod was installed, no game process was launched, and no DTMAPI runtime path was touched.

The original archive contains one enclosing `touhou_fumo/` directory. To distinguish wrapper handling from table-name handling, a temporary normalized archive was generated under `output/` by stripping only that directory prefix. All nine inner files have byte-identical SHA-256 hashes before and after normalization. The typo, JSON, PNG, and metadata contents were unchanged.

## Executive Verdict

The handoff tool does not detect or correct the known `item_tbtiem.json` defect.

The test also exposes two broader import/re-export defects not covered by the earlier synthetic browser fixture:

1. a ZIP with exactly one enclosing Mod directory is not auto-unwrapped, so the original artifact imports as zero editable projects and is reported as missing root `info.json`;
2. after root normalization, the importer groups all nested files by only the first directory below `Content`, causing a valid nested shop path to fail directory-ID validation and, after ordinary UI editing, to be relocated and duplicated on export.

The current handoff ZIP remains useful as a test artifact, but it should not be sent to the author as a final release candidate until these import/validator/data-integrity defects are corrected and the Touhou-derived regression passes.

## Provenance Attribution

| Observed problem | Attribution | Evidence |
| --- | --- | --- |
| `item_tbtiem.json` instead of `item_tbitem.json` | original Touhou Fumo Mod defect | the supplied archive already contains that exact basename; neither upstream nor fork created it |
| One enclosing `touhou_fumo/` directory imports as zero projects | inherited upstream v0.6 tool limitation | both versions look up exact root `info.json` and only paths starting `Content/`; neither normalizes a single wrapper root |
| Equipment directory is not recognized and the typo is not named | inherited upstream v0.6 limitation, still unfixed by the fork | both versions require exact `item_tbitem.json` before classifying an equipment project and have no near-match basename validator |
| Nested paths collapse to the first segment below `Content` | inherited upstream v0.6 importer design | both versions assign `folder = parts[1]`, so `Content/Shop/Hult/...` becomes project folder `Shop` |
| Imported `Shop` is rejected by the lowercase generated-ID rule | fork-introduced validator regression | upstream v0.6 had no package preflight; the fork applies its new generated-project ID rule to imported path segments |
| Ordinary-shop seasonal counts reset from `99` to generated `1` | inherited upstream v0.6 data-model limitation, still unfixed | neither importer models normal-shop `season_spawn_data`; both generators hard-code four `1..1` entries |
| Re-export contains both relocated original and generated shop tables | fork-introduced round-trip regression | upstream discarded/rebuilt unsupported data; the fork's new preservation plus first-segment relocation keeps the nested original while also writing a generated replacement |

Therefore the known table typo is not something the fork created. The specific duplicate-table outcome and imported-directory false rejection are regressions introduced by the authorized fork while trying to add preservation and validation. The remaining import failures are inherited defects that the fork did not yet fix.

## Test 1 — Original Archive With One Enclosing Directory

Actual browser import result:

```text
成功导入 0 个可编辑子项目。未识别内容会在再次导出时保留。

注意：
- 缺少 info.json；导出前需要补全模组基本信息。
```

Cause:

- `importMod` looks up only exact root entries `info.json`, `icon.png`, `preview.png`, and paths beginning `Content/`;
- it does not detect the single common prefix `touhou_fumo/` and retry from that Mod root.

This prevents the tool from reaching the known filename typo when given the original archive as supplied.

## Test 2 — Byte-Identical Contents With Only The Wrapper Removed

Temporary fixture:

- path: `output/touhou-fumo-tool-test/touhou_fumo-root-normalized-still-typo.zip`;
- SHA-256: `0DC2E042C5AD664A63340E4F407FB3AC0E311CD6D3611D8B05D68701965113FA`;
- inner files: 9;
- byte-hash differences from the original archive's `touhou_fumo/` contents: 0.

Browser import result:

```text
成功导入 1 个可编辑子项目。未识别内容会在再次导出时保留。

注意：
- Content/Equipment/ 无法映射到现有六类表单，目录会原样保留。
```

Only the Hult shop became editable. The equipment directory did not become an equipment project because type recognition requires both `equipment_tbequipment.json` and exact `item_tbitem.json`. The typo-bearing file was parsed as JSON but treated as an unknown basename. The warning did not name `item_tbtiem.json`, suggest `item_tbitem.json`, or classify the table as blocking.

## Test 3 — Preflight Behavior

Initial preflight correctly reported the tool's deliberate three-part-version policy for original `version: "0.1"`.

It also rejected the imported project folder as an invalid ID because `folderMap` collapses `Content/Shop/Hult/...` to the first segment `Shop`, then applies the ordinary lowercase-ID regex to that imported path. Official JSON permits arbitrary nested subdirectories under `Content`; this is an importer/validator false positive, not a Touhou Fumo schema defect.

After changing only the test UI's version to `0.1.0` and submitting the shop form so it received a generated safe folder ID, preflight had warnings but no errors:

```text
提醒（请人工确认）：
- Content/Equipment/ 无法映射到现有六类表单，目录会原样保留。
- 子项目 1（shop_hult）的商品引用 “reimufumo”，它不在工具的当前内置列表或本次新增道具中；请确认由游戏或其他模组提供。
```

The tool therefore allowed generation while the blocking item-table typo remained.

## Test 4 — Re-Export Result

Generated artifact:

- path: `output/touhou-fumo-tool-test/touhou_fumo-reexported-by-tool.zip`;
- SHA-256: `9F1864BAEC773B9058C44F0D8A59A47D761C650953575AC485EBB6A36B46AFC3`.

Confirmed results:

- `Content/Equipment/item_tbtiem.json` remains present and byte-identical;
- its SHA-256 before and after is `2F00936C3A6252AF72B699ACE3D800F0370E1B3CA74ED021BCE825818F900774`;
- the required `Content/Equipment/item_tbitem.json` is absent;
- the equipment table and all three equipment PNGs remain byte-identical;
- the original `Content/Shop/Hult/mod_tbmodstoreextension.json` path is absent;
- an unchanged copy of the original shop table moved to `Content/reimufumo/Hult/mod_tbmodstoreextension.json` and still contains seasonal counts `99`;
- a second generated table appeared at `Content/reimufumo/mod_tbmodstoreextension.json` with seasonal counts reset to `1`.

The output therefore contains two shop extension records for the same `hult_shop`/`reimufumo` relationship while still lacking the item. This is not lossless round trip and can create merge-order-dependent ambiguity in addition to preserving the original blocking typo.

## Root Causes

### R1 — No single-wrapper-root normalization

Exact root lookup is performed before any common-prefix detection. A normal “folder zipped as one directory” archive is treated as though all required root files are missing.

### R2 — Table basenames have no known/near-match validator

The importer recognizes a fixed set of exact filenames for choosing an edit form but never validates every JSON basename against the official table allowlist. Unknown JSON is preserved without a filename-specific warning. As a result, `item_tbtiem.json` is not distinguished from an intentional custom/unknown file.

### R3 — Nested paths are grouped at the wrong level

Every file under `Content/Shop/...` is grouped into one project named `Shop`, regardless of the actual directory that owns the recognized JSON table. The validator then treats `Shop` as a new-project ID instead of an imported relative path.

### R4 — Relocation uses the first `Content/<segment>/` prefix

When an imported nested project is edited, `relocateImportedProject` moves the entire first-segment prefix while retaining deeper relative paths. The new generated table is then written at the generated folder root, leaving both the relocated original table and generated replacement.

### R5 — Normal-shop seasonal values are not modeled on import

The shop form imports item, type, store, stock, gold, and exchange costs, but not ordinary-shop `season_spawn_data`. Export always generates four `1..1` entries. The relocated original retains `99..99`, so the duplicate visibly disagrees with the generated record.

## Required Fix Order

1. Detect and safely strip exactly one common enclosing directory only when it contains the complete Mod root (`info.json` and `Content/`); report the normalization.
2. Validate every Content JSON basename against the current official table allowlist. Treat a near-match such as `item_tbtiem.json` as a blocking error with an explicit `item_tbitem.json` suggestion; do not silently rename it.
3. Discover a project from the directory containing each recognized table, not only `Content/<first-segment>`; preserve safe nested relative paths separately from generated project IDs.
4. Relocate only the exact source project directory and ensure the original recognized table is removed after its replacement is written, so no duplicate nested table survives.
5. Model and preserve all four normal-shop seasonal count/weight records, or refuse editable import when the current form cannot represent them losslessly.
6. Add synthetic wrapper-root, near-miss basename, nested-shop, and `99..99` seasonal fixtures. Do not commit the third-party Touhou PNG/JSON package itself.
7. Rebuild the author handoff ZIP only after both the existing browser suite and this synthetic regression pass.

## Validation And Limits

Completed:

- exact original archive identity and entry inventory;
- real headed Google Chrome import through Playwright CLI;
- original wrapper import result;
- byte-identical nine-file root-normalized fixture generation and verification;
- normalized import alert, project list, preflight, controlled UI edit, warning confirmation, and browser download;
- generated ZIP inventory, exact entry hashes, correct-name absence, nested relocation, duplicate-table, and seasonal-value comparison;
- source inspection of importer grouping, type inference, validation, relocation, shop import, and shop export.

Evidence:

- `output/playwright/touhou-fumo-tool-test-original-wrapper.png`;
- `output/playwright/touhou-fumo-tool-test-normalized-import.png`;
- `output/touhou-fumo-tool-test/touhou_fumo-root-normalized-still-typo.zip`;
- `output/touhou-fumo-tool-test/touhou_fumo-reexported-by-tool.zip`.

No game/runtime run was performed. This review proves the browser-tool import/validation/export defects; it does not add new claims about player-visible Touhou Fumo behavior beyond the original static audit.
