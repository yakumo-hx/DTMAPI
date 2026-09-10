# 20260823-0003：Y 键控制台文本输入热键保护

## Metadata

- Update ID: `20260823-0003`
- Date: `2026-08-23`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime, player`
- Runtime Validation: `passed`
- Related Issue State: `verified`
- Source: 用户分别复现控制台搜索框中按 Y 会关闭、游戏箱子改名框中按 Y 会打开控制台，要求检查日志并研究共同修复；允许重构或精简，但不能增加过多性能负担。无日志的崩溃报告暂不处理。独立手测通过后，用户要求装入上传目录并自行提交 Workshop，随后要求核对订阅哈希、更新相关文档并提交本次变动。

## Summary

- 将 DebugConsole 的文本输入占用判断前移到 typed `debug-console.toggle` 执行边界，使焦点判断发生在 `ui.Toggle` 之前。
- 统一识别控制台自身输入框、当前 EventSystem 选中的 `UnityEngine.UI.InputField` 和 `TMPro.TMP_InputField`；只有组件真实 `isFocused` 时才保留 Y 给文本输入。
- 缓存 Unity Type、PropertyInfo 和 `GameObject.GetComponent(Type)` MethodInfo；查询只发生在 Y pressed 边缘和既有控制台 raw-Y 边缘，不增加常驻更新、场景扫描或新 Harmony Hook。
- 将候选版本提升为 `1.1.1`；Codex 只完成确定性构建、现有官方上传目录同步和预提交审计，没有代替用户操作 Steam 提交。
- 用户独立确认搜索框、箱子改名框和离开输入框后的普通 Y 三项均通过。用户随后自行提交，Steam installed/latest manifest 已收敛到 `6693520158465470410`；下载订阅树与原上传目录逐路径、逐长度、逐 SHA-256 完全一致，发布闭环完成。

## Performance Boundary

- 控制台关闭且没有移动倍率租约时，产品仍取消 `UpdateTicked` 订阅；本修复在普通帧中执行零次焦点查询。
- 控制台收到物理 Y pressed 时只检查一个很小的受管输入框列表、`EventSystem.currentSelectedGameObject`，并最多查询标准 InputField 与 TMP InputField 两个组件。控制台打开时同一边缘可能再经过既有 raw-Y 保护一次，但不会扩展为逐帧工作。
- Type、PropertyInfo 和 `GetComponent(Type)` MethodInfo 只解析一次并复用；没有 `FindObjectsOfType`、场景遍历、`Expression.Compile`、新 Harmony Hook 或常驻分配循环。其成本只随实际 Y 按键次数发生，相比 Canvas 渲染和游戏帧更新可忽略。

## User-Visible Impact

- 在 Y 键控制台搜索框中输入 Y 时，控制台保持打开。
- 在箱子改名及同类标准 Unity 文本输入框中输入 Y 时，控制台保持关闭。
- 文本框不再聚焦后，Y 仍按原规则打开或关闭控制台；Escape、按钮关闭、开启键释放保护和旧 ABI Compatibility 行为保持不变。

## Changed Files

- `products/first-party/DebugConsole/src/ModEntry.cs`
- `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs`
- `products/first-party/DebugConsole/manifest.json`
- `products/first-party/DebugConsole/official-info.json`
- `tools/release/dtmapi-product-catalog.json`
- `tools/release/current-subscription-manifest.json`
- `tools/scripts/check-product-catalog.ps1`
- `docs/architecture/managed-product-admission-registry.md`（由 Catalog 机械再生成）
- DebugConsole 相关 source/unit/contract tests
- `docs/reviews/manual-qa/2026/20260823-0002-y-console-focused-y-dispatch-order-regression.md`
- `docs/debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md`
- `docs/hook-map/focused/DebugConsoleInput.md`
- `docs/updates/2026/20260823-0003-y-console-text-input-hotkey-guard.md`
- `docs/updates/INDEX-2026-08.md`

## Validation

- `DTMAPI_UNIT_TEST_FOCUS=debugconsole-product` passed. It covers the typed focus consumer before `Toggle`, focused/unfocused tracked input, selected native-style `InputField`, cached standard/TMP metadata, ordinary raw-Y ownership, opener protection and the existing ProductNative DebugConsole matrix.
- `tools/scripts/check-product-catalog.ps1`, `tools/scripts/test-dtmapi-060-debugconsole-native-trace.ps1`, `tools/scripts/test-dtmapi-060-runtime-floor-compatibility.ps1`, `tools/scripts/check-doc-governance.ps1` and `git diff --check` passed.
- The exact `doloctown-24456188-debugconsole-v1` reference fixture was generated under `temp/y-console-focus-guard-fixture/steamapps/common/Doloc Town`. Two independent Author SDK builds passed with identical package SHA-256 `70FD50874B6227F4D865087206ECA5D2C19146745E4BB386AFFEF7E1D007EA73`; entry DLL SHA-256 is `784FD83E3174F80773926DE937171B1294D8F3E8D21240762492EC5E93B0E050` and each receipt validated `47` files.
- A full Unit invocation reached an unrelated pre-existing Runtime publication assertion before the new focused lane: Catalog Runtime artifact hashes are `846665A979E17AADA210B3960441312A403C72B3C198A0FBA91AF08972801F88` / `B4EC6A441B4930B5174D4CAED8799748FB4E4701AC0D8E72FC6BF6BD48EEE4AA`, while the old test assertion still expects `8280DCFB2BDFAE72107CB7E813E057C6F3E76FDD01ACC7141AC7B85BBE934AA3` / `C733593445CB784854E3CA105247F4097CDF61EFABF1A0C9C309ED63AC174FEF`. Neither Runtime publication row nor that assertion belongs to this change, so no unrelated historical authority was rewritten.
- A full release-contract invocation is independently blocked by stale global authority: current Catalog selection returns ten Advanced products, while the checker still asserts an exact count of nine; a single-product invocation also does not supply artifact roots for unrelated products. The focused double Author build and Catalog/native-trace checks are the bounded product result; this Update does not claim a full Release-suite pass.
- `GAME-SMOKE/20260823-025619` loaded local DebugConsole `1.1.1` from the exact deployed candidate. Its ordinary typed-input submatrix passed Y open, Y close, Escape close, reopen, ten short taps, held-Y no-flicker and owner-bound input. While the console search field was visibly focused, a physical `y` entered the Chinese IME candidate flow and the console remained open. That edge was consumed by the IME before Core logged a typed Y event, so the observation proves the visible outcome but is not causal proof of the new pre-toggle guard.
- The same game run passed SaveLoaded, no-fatal, process exit, exact profile restoration, and proved the third-save archives plus committed sidecars unchanged before cleanup under `NoNativeSave`. The overall ordinary-player eleven-product gate is deliberately classified non-acceptance because its unrelated Animal Viewer/Equipment UI portions were not completed before the deadline. No smoke-matrix PASS row was added.
- Final independent player acceptance passed on the exact local `1.1.1` candidate: search-field Y kept the console open, chest-rename Y did not open it, and unfocused Y continued to toggle normally.
- The final `latest.log` (`155868` bytes; SHA-256 `F1B3B72A8BCBCF1F786E8E7B716294649FA45BCE8816E484DC70D5CF0380AF32`) provides causal search-field evidence: at `07:01:54.893` the typed Y reached the product, emitted the focused-input guard marker and produced no close boundary. Later unfocused Y edges closed, reopened and closed normally while preserving `searchText=伊萨多`.
- Under the shared Runtime lock, the existing official upload directory was compared path-for-path and SHA-for-SHA with all `26` files in the verified Author package. It already matched, so no byte rewrite was needed. With the preserved `workshop.json`, the prepared tree has `27` files, `610335` bytes, `DTMAPI-Retained-SHA256SUMS-v1` SHA-256 `BC464BB2EA4DAF7B7911B270E21AE0BCBC81108339C8B97AE6FD43FD441C2F77`, zero reparse points and zero alternate streams.

## Post-Publication Subscription Closeout

- At the final stable observation `2026-08-22T23:43:44Z`, the native Steam manifest at `<SteamLibrary>/steamapps/workshop/appworkshop_2285550.acf` had last-write time `2026-08-22T23:32:22.5444221Z`, SHA-256 `AEEB35B4E071FDE2283E869378C889B06FE3A64036A138DFAA1D66C245C4BAA9`, `51` installed and `51` detail members, `84763464` bytes on disk, and both `NeedsUpdate` and `NeedsDownload` equal to zero. Y-console installed/latest manifest and time agree at `6693520158465470410` / `1787440923` (`2026-08-22T23:22:03Z`); among Runtime plus all eleven tracked managed subscriptions, only the Y-console row changed from the previous authority observation.
- The downloaded subscription at `<SteamLibrary>/steamapps/workshop/content/2285550/3742714442` and the official upload directory each contain exactly `27` files and `610335` bytes. Missing paths, extra paths and per-file content drift are all zero. Both reproduce `050C8E33F267D3D16B5C847C721C9220A3031EC7C54D9A11F2C39A1FB4F0D172` under `DTMAPI-Published-SHA256SUMS-v1`, and `BC464BB2EA4DAF7B7911B270E21AE0BCBC81108339C8B97AE6FD43FD441C2F77` under the retained case-insensitive ordering.
- Both trees identify `DTMAPI.DebugConsoleMod / 1.1.1 / minimum Runtime 0.6.1 / Advanced`; the `221696`-byte entry DLL SHA-256 is `784FD83E3174F80773926DE937171B1294D8F3E8D21240762492EC5E93B0E050`. `info.json`, `Content/DTMAPI/manifest.json` and preserved `workshop.json` are respectively `B9C916469C393435441C17AC74A723D894BC008D5EC9775E15F9F732083896A7`, `3B2639D1C81C1571E13DB5112DFFD037CEF450D74A11D924A1C6C18222D900CD` and `514C3829DA8FABDA78EC0B3934408F9B73D3D40089FAEC7DB6E6B011A953FA6F`.
- Two independent read-only file-tree snapshots reproduced the same aggregate and DLL identities; the native ACF hash was unchanged between observations. Both roots have zero reparse points and zero alternate streams, and no `DolocTown.exe` process was running.
- This Advanced product contains no Runtime installer BAT/PowerShell entrypoints, so the Workshop release-audit skill's installer stress matrix is not applicable. The closeout was read-only: it did not acquire the Runtime lock, launch the game, write the official upload folder, mutate saves or copy anything into Steam's subscription cache.
- Catalog now freezes the exact public `1.1.1` artifact on game build `24788406`, the current-subscription manifest owns the converged native Steam observation, and `releaseStop` remains `ActiveNoUploadAuthorization` with zero future mutation entrypoints.
- After the closeout projection, the Product Catalog checker passed under PowerShell 7 and Windows PowerShell 5.1; the generated admission-registry check, focused `debugconsole-product` Unit run, DebugConsole native trace, Runtime-floor compatibility, JSON parsing, document governance (`6903` checks) and final diff checks all passed. The focused build retained the existing nullable warnings and produced zero errors.

## Evidence

- The pre-implementation runtime and native-owner evidence is recorded in the linked Manual QA Review and ISSUE-014.
- Bounded game evidence: [`GAME-SMOKE/20260823-025619`](../../../debug/evidence/GAME-SMOKE/20260823-025619). This directory is a non-acceptance package whose Y-console subresults and no-save/process invariants may be used independently; its overall `RunStatus=Failed` must not be projected as a PASS.
- Local player candidate: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_YKeyConsole`, version `1.1.1`, entry DLL SHA-256 `784FD83E3174F80773926DE937171B1294D8F3E8D21240762492EC5E93B0E050`.
- Pre-deployment backup: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\DTMAPI\backups\y-console-local-1.1.0-before-1.1.1-20260823-025606-635`.
- Deployment receipt: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\DTMAPI\transactions\y-console-focus-guard-20260823-025606-635\deployment.json`.
- At the preparation boundary, authoritative `SAVE\mod_infos.json` selected the local Y-console and disabled Workshop `3742714442`; this source-selection fact is independent from the later read-only subscription observation.
- Final prepared upload audit before the user's submission: package payload `26/26` exact, package SHA-256 `70FD50874B6227F4D865087206ECA5D2C19146745E4BB386AFFEF7E1D007EA73`; upload `info.json` SHA-256 `B9C916469C393435441C17AC74A723D894BC008D5EC9775E15F9F732083896A7`; preserved `workshop.json` SHA-256 `514C3829DA8FABDA78EC0B3934408F9B73D3D40089FAEC7DB6E6B011A953FA6F`. Codex did not perform the Steam submission.
- Post-publication player artifact: Workshop `3742714442`, manifest `6693520158465470410`, exact `27 / 610335 / 050C8E33F267D3D16B5C847C721C9220A3031EC7C54D9A11F2C39A1FB4F0D172` published tree.

## Related Records

- Review: [Y 键控制台文本输入占用 Y 回归审查](../../reviews/manual-qa/2026/20260823-0002-y-console-focused-y-dispatch-order-regression.md)
- Debug: [ISSUE-014: Y Console Close Double Toggle](../../../debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md)
- Hook map: [DebugConsole Input Isolation Hook Map](../../../hook-map/focused/DebugConsoleInput.md)
- Smoke matrix: [Active smoke matrix](../../../debug/regressions/smoke-matrix.md)
- API matrix: unchanged; this implementation adds no public API or signature.

## Rollback Notes

- Revert the ProductNative pre-toggle focus gate, reflection-cache additions, tests and `1.1.1` source projection together.
- Do not remove the existing raw-Y opener/focused-input protection independently: the frozen Compatibility route still depends on its pre-sample UI update order.
- Rollback must not rewrite the Catalog's immutable `currentPublishedArtifact` or current subscription manifest; those continue to describe the exact public `1.1.1` bytes. A public rollback is a new user-authorized Workshop update followed by a new subscription audit.

## Follow-Up

- Independent player acceptance is complete; this Update and ISSUE-014 are verified for the scoped text-input regression.
- The screenshot-only crash report remains outside this task unless a log or dump establishes a separate failure path.
- Publication and downloaded-subscription verification are complete. Catalog remains `ActiveNoUploadAuthorization`; any later upload requires a new bounded Update, explicit existing-item authorization and another post-publication subscription audit.
