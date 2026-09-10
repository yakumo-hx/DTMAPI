# 20260810-0001：正式版 public 1.00.03 全量捕获与基线差异审查

## Metadata

- Update ID: `20260810-0001`
- Date: `2026-08-10`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求解包最新版正式版 public payload，并审查它与当前基线的实际差异。

## Scope

- 冻结当前已一致挂载的 Steam `public` payload，执行 AssetRipper 全量 Unity 工程恢复与 ILSpy 主程序集/firstpass 反编译。
- 独立验证 raw、AssetRipper、主反编译与 firstpass 四份完整清单。
- 按现有权威边界分别比较：最近完整捕获 `24585411_test_68AEA1`、中间快照 `24567135_test_08C846`、最后明确准入的 final-test 基线 `24456188_test_E861E0`。
- 形成一份独立 Code Review，区分打包/序列化噪声、官方内容变化、原生代码变化与 DTMAPI/第一方产品真实消费者影响。
- 本任务不自动生成 Author policy、不批量重签产品，也不把静态比较冒充玩家行为验收。

## Start Identity

- Steam app: `2285550`
- Steam build: `24650773`
- Branch: `public`；`UserConfig` 与 `MountedConfig` 均为 `public`
- Manifest SHA-256: `CB84BFBCDC9AF3555FA98B2F68C59B9B5C78995C95019B36F358F7578D1EB6E3`
- PlayerSettings/Application version slot: `1.00.03`
- `Assembly-CSharp.dll`: 6,384,640 bytes；SHA-256 `76C24EE0E4BBBF27C7A68C7DFAA36A9B0B7BD0903FA6617BA05177E4AA45F0DD`
- No `DolocTown.exe` process was running before capture; the shared Runtime lock was free.

## Changed Files

- `docs/updates/2026/20260810-0001-public-10003-full-reverse-capture-and-diff.md`
- `docs/updates/INDEX-2026-08.md`
- `docs/reviews/code/2026/20260811-0001-public-10003-baseline-difference-audit.md`
- ignored local capture under `references/doloc-town/reverse/builds/24650773_public_76C24E/`

## Validation

- Full capture exited `0`. Raw snapshot is 542 files / 1,804,556,553 bytes with exact frozen-source parity; AssetRipper recovered 51,988 files / 4,381,949,450 bytes, 90 scenes, 195 config tables and 181 managed assemblies; ILSpy recovered 3,666 main files and 29 firstpass files.
- Independently rehashed the four result trees: raw `542/542`, AssetRipper `51,988/51,988`, main `3,666/3,666` and firstpass `29/29`, with zero missing, extra, length or SHA-256 mismatch. Raw pollution hits are zero.
- Classified all 93 AssetRipper errors as the existing 90 Cubemap + 3 globalgamemanagers limitation, with zero other error class.
- Compared public against `24585411`, `24567135` and `24456188` across raw, managed assemblies, scenes, GenDatas, ILSpy main/firstpass and AssetRipper exports.
- Performed GUID/timestamp normalization across 45,861 changed text/YAML exports, content-set comparison for reordered river-valley room assets, scene fileID/order normalization and 90-scene value-multiset comparison.
- Compared the six changed code files method by method, five GenDatas by stable record key, three dialogue CSVs by 9,800 stable IDs, Addressables internal IDs and all 12 raw audio/Wwise paths.
- Scanned `src`, `products`, `first-party-mods`, `tools`, `tests` and `author-sdk` for changed-member and new-content consumers. No production consumer requires a source change.
- Parsed all three ignored comparison JSON receipts, ran document governance, `git diff --check` and final process/runtime-lock checks.
- No game, full Release or Workshop publication test was run; this task is a static capture/compatibility audit and does not claim player behavior acceptance.

## Evidence

- Capture root: `references/doloc-town/reverse/builds/24650773_public_76C24E/`.
- Capture summary: `full-baseline-inventory/summary.json` and `portable-full-capture-summary.json`.
- Source parity: `full-baseline-inventory/snapshot-source-parity.json`.
- Detailed ignored comparisons:
  - `full-baseline-inventory/comparison-to-24585411.json`
  - `full-baseline-inventory/comparison-to-24567135.json`
  - `full-baseline-inventory/comparison-to-24456188.json`
- Durable derived findings: [20260811-0001 public 1.00.03 baseline difference audit](../../reviews/code/2026/20260811-0001-public-10003-baseline-difference-audit.md).

## Related Records

- Last explicitly admitted final-test baseline: `docs/updates/2026/20260801-0004-final-test-build-full-reverse-baseline.md`.
- Nearest retained test capture: `docs/updates/2026/20260806-0001-current-running-10002-full-reverse-capture.md`.
- Public compatibility Review: `docs/reviews/code/2026/20260811-0001-public-10003-baseline-difference-audit.md`.

## Rollback Notes

- Remove only this Update/Review navigation and the new ignored public capture directory; do not alter or overwrite any retained reverse build.
- No Runtime, game installation, official `MODS`, Workshop subscription or save data is changed by this capture.

## Follow-Up

- Use `24650773_public_76C24E` as the newest public reverse observation head for future native-owner and resource research.
- Keep existing `24456188` exact Author policies and receipts unchanged; current products continue through reviewed drift activation.
- If a real public player run reports an Entry, Hook or gameplay transaction failure, reopen only the affected product and replay the smallest relevant behavior gate.
- A deliberate migration of product compilation/policy authority to `24650773` requires its own bounded SDK, policy, receipt and release lifecycle.
