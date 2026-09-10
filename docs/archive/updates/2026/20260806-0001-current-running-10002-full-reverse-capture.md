# 20260806-0001：当前运行 1.00.02 完整逆向捕获

## Metadata

- Update ID: `20260806-0001`
- Date: `2026-08-06`
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求对当前正在运行的 Doloc Town 版本执行全量解包和反编译，并允许在确认版本后关闭游戏。

## Scope

- 在关闭前确认正在运行进程对应的 Steam build、实际挂载分支、游戏版本槽与主程序集哈希。
- 冻结当前官方玩家文件，执行 AssetRipper Unity 工程恢复和 ILSpy 主程序集/firstpass 反编译。
- 保留 Steam `UserConfig=public`、`MountedConfig=test` 的待切分支事实，以实际挂载的 `test` 标记被冻结字节。
- 不把本次捕获自动准入为新的兼容或发布基线；后续比较和准入另行决定。

## Result

- Completed full capture at ignored local path `references/doloc-town/reverse/builds/24585411_test_68AEA1`.
- Frozen source identity remained exact from capture start through frozen manifest and source-end recheck: build `24585411`, mounted branch `test`, manifest SHA-256 `06ECBDF1...35C90D`, assembly SHA-256 `68AEA11B...15739`.
- AssetRipper `1.3.14` recovered 51,988 files, 90 scenes with exact build-index mapping, 195 config tables, 8 bundles and 181 managed assemblies.
- ILSpy `9.1.0.7988` produced 3,666 main files and 29 firstpass files.
- The 93 AssetRipper error lines are the same shape and count as the preceding capture: 90 per-scene Cubemap size mismatches plus three known custom-Unity `globalgamemanagers` setting-object read errors; no other error class appeared and raw bytes remain the authority.
- This is a complete research snapshot, not an automatic compatibility/baseline admission decision.

## Changed Files

- `tools/scripts/steam-appmanifest-identity.ps1`
- `tools/scripts/capture-doloctown-reverse-baseline.ps1`
- `tools/portable-reverse-capture/run-doloctown-full-capture.ps1`
- `tools/portable-reverse-capture/README.zh-CN.md`
- `tools/scripts/test-steam-appmanifest-identity.ps1`
- `docs/updates/2026/20260806-0001-current-running-10002-full-reverse-capture.md`
- `docs/updates/INDEX-2026-08.md`
- ignored local capture under `references/doloc-town/reverse/builds/`

## Current Identity Evidence

- Running process path resolved to the repository-configured Steam game directory.
- Steam top-level and target build: `24585411`.
- Manifest transition: requested `UserConfig=public`; actual `MountedConfig=test`; installed depot manifest matches the private `test` branch entry.
- PlayerSettings/Application version slot in `globalgamemanagers`: `1.00.02`.
- `Assembly-CSharp.dll`: 6,384,128 bytes; SHA-256 `68AEA11BD040805712673E506D1D429BC71338DD52D2659683ADC60B62715739`.
- The game process exited after identity confirmation; the on-disk assembly hash remained unchanged.
- The portable wrapper now falls back to the tracked repository capture engine and repository root when run from its source directory; packaged copies still use their co-located engine and package root.

## Validation

- Windows PowerShell parser accepted all four changed PowerShell scripts.
- Windows PowerShell 5.1 ran the focused identity test successfully; document governance passed 6,316 checks and `git diff --check` reported no whitespace error.
- Focused `test-steam-appmanifest-identity.ps1` passed, preserving default conflict rejection and requiring both explicit mounted branch and explicit pending-switch opt-in.
- The real pending manifest resolved to build `24585411`, branch `test`, source `appmanifest.MountedConfig(pending UserConfig mismatch)`, requested branch `public`, mounted branch `test`.
- Full capture returned exit code `0`; source parity checked 542 official game files plus the frozen manifest with zero mismatches.
- Independent post-run SHA-256 verification matched every inventory exactly: raw `542/542`, AssetRipper `51,988/51,988`, main decompile `3,666/3,666`, firstpass `29/29`; all four sets had zero missing, extra, length mismatch or hash mismatch.
- Raw snapshot scan found zero BepInEx, Mods, Saves, `Player.log` or `LogOutput.log` paths.
- AssetRipper error classification matched the previous build exactly at 90 Cubemap plus 3 `globalgamemanagers`, with zero other errors.
- The game process was absent after capture, the live assembly hash still matched the frozen identity, the capture root was confirmed ignored by Git, and the shared Runtime lock was released.
- No game behavior smoke was run or required; this task captured player bytes and did not claim DTMAPI/runtime compatibility.

## Evidence

- `references/doloc-town/reverse/builds/24585411_test_68AEA1/full-baseline-inventory/summary.json`
- `references/doloc-town/reverse/builds/24585411_test_68AEA1/full-baseline-inventory/portable-full-capture-summary.json`
- `references/doloc-town/reverse/builds/24585411_test_68AEA1/full-baseline-inventory/snapshot-source-parity.json`
- `references/doloc-town/reverse/builds/24585411_test_68AEA1/full-baseline-inventory/*-files.json`
- `references/doloc-town/reverse/builds/24585411_test_68AEA1/asset-ripper/AssetRipper-1.3.14.log`
- `references/doloc-town/reverse/builds/24585411_test_68AEA1/code-decompile/`

## Rollback Notes

- Revert the pending-switch opt-in and its tests/docs if the mounted-branch provenance cannot be validated.
- Remove only the new ignored build directory if the capture fails; never delete or overwrite an earlier reverse build.

## Follow-Up

- The requested comparison is now recorded in [the 24585411 compatibility audit](../../reviews/code/2026/20260806-0003-current-test-24585411-compatibility-audit.md). It admits these bytes only as the latest local `test` compatibility research baseline: the sole incremental main-code change has no production DTMAPI/first-party consumer, so no code, policy, or package change is required. The actual `public` payload remains a separate conditional gate because these captured bytes are still mounted from `test`.
