# 相机、输入与历史 GC 归因

历史控制实验的结论受当次游戏、启动方式和场景限制。当前产品查 [Zoom](../../../products/first-party/Zoom/README.md)，长期崩溃状态查 [ISSUE-010](../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)。

2026-07 的 8.19–8.21 记录构成一条连续证据链：Y/Zoom 各自单次通过、组合两次 Fatal；移除组合输入根后两次通过；分别移除任一侧输入根也各两次通过。它缩小了输入根压力假设，没有证明某一个产品必然有缺陷。后两轮使用 DirectExe fallback，不能当成普通 Steam 玩家路径的完全同条件复验。过早压制尚未注册的 owner 的尝试被排除，不能累计为成功。

Fatal 发生于 SaveLoaded 前的原生加载阶段；不同尝试的分配栈落在 terrain 或 dungeon，不能仅据栈顶宣布该模块泄漏。已关闭服务、GC 强收集、破坏未知 Unity 对象均不能替代责任归因。后续若重复这个问题，先查 Issue 的最新证据，不自动重新运行全部历史隔离矩阵。

2026-07 Zoom 第一方产品化审查保留了相机租约、owner 清理、拒绝陈旧 facade 和加载程序集需重启的边界；当时的 Abstractions-only 产品路线及 CameraView 背景非目标属于那次切片，不能覆盖后来 ProductNative 归属或扩大背景/雾/全景能力承诺。

来源：[8.19](../../archive/reviews/code/2026/20260706-0009-phase819-yconsole-zoom-pair-decomposition.md)、[8.20](../../archive/reviews/code/2026/20260707-0001-phase820-yconsole-zoom-input-root-isolation.md)、[8.21](../../archive/reviews/code/2026/20260707-0002-phase821-yconsole-vs-zoom-input-owner-isolation.md)、[Zoom owner 审查](../../archive/reviews/code/2026/20260711-0002-zoom-first-party-owner-lifetime-audit.md)。

7 月 24 日后续准入推翻“Zoom 必须继续 Strict”的中间结论：产品可直接拥有 orthographicSize 原值、1x–4x 比例及 SetEnvCamera 非抑制 Postfix，旧 CameraView/CameraZoom executor 进入既有 Compatibility Host。ItemDisplayName 的环境清理仍是独立共享责任，不能因调用时机相同合并 gameplay owner。冻结 CameraView 的真实旧 DLL 有 35 个 MemberRef，先前 36 或仅三成员摘要均被纠正。此切片不承诺背景、雾或全景同步；最小验收为 NoNativeSave 相机行为和恢复。

来源：[Zoom 第十产品准入及后续限定](../../archive/reviews/code/2026/20260724-0005-tenth-product-zoom-admission-review.md)。

后续最终恢复审查又补充了 native derived state：回到 1x、禁用和清理时 RefreshResolution 成为恢复的一部分，失败必须显式失败，而不是仅看到 orthographicSize=原值就算完成。这修正了上述准入文件的绝对“不调用”表述。4x → 原生分辨率刷新 → 2x 曾是未覆盖 P2，后续 disposition 已记录 e7276b88 以 focused Unit/游戏证据关闭；不重开旧全矩阵。

来源：[十产品恢复与后续处置](../../archive/reviews/code/2026/20260725-0001-ten-product-closeout-and-a-w-roadmap-audit.md)。

6 月早期“加背景/雾 scale 即补齐”的提案被后续玩家移动失败推翻：每帧 RefreshResolution/SetPosition 会扰动 camera range/follow，单帧 4x 截图也未覆盖 yOrigin/parallax 动态。普通游玩视野与隐藏角色、fit room、PhotoState 的全景模式是不同语义。早期 Panorama API 只是提案，后续 ProductNative Zoom 的背景非目标及恢复边界优先。

来源：[早期全景比较](../../archive/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md)、[0.4.2 动态失败](../../archive/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md)。

Zoom 7 月移入 first-party 的正式操作手测接受了 orthographic-only 的背景小矩形/灰边：它是未同步 background/fog/panorama 的产品限制，不是已经实现同步的证明。owner 生命周期专项通过真实产品禁用 roots 19→0、Camera lease 1→0，以及同进程 restart-required 不重入；该专项不关闭更广 Mono 崩溃，也未单独证明 reload/reacquire。来源：[first-party 与 owner 验收](../../archive/updates/2026/20260711-0013-first-party-zoom-owner-lifetime.md)。

Zoom 7 月 24 日第一轮 QA 还曾把 67.5 当 vanilla 再乘 4 得到 270，随后‘恢复’到污染基线也算通过；真实 SetEnvCamera 不改变 orthographic size，模拟它自动换基线的 Unit oracle 本身错误。后续先修 2x→1x 和 disable 的原生 16.875 基线，再发现分辨率刷新衍生的 camSize/房间范围未恢复，最后补 4x→刷新→2x 的 active 派生值。每次新增具名边界最小复验，旧 ownership/NoNativeSave 证据仍可用；仅 manifest 指向父 commit 不独立触发全重跑。来源：[三层纠正与最终恢复](../../archive/updates/2026/20260724-0002-zoom-tenth-advanced-product.md)、[4x 刷新到 2x](../../archive/updates/2026/20260726-0001-zoom-active-scale-refresh-coherence.md)。

8 月 1 日 Z1 **再次替代了上文 7 月主动 RefreshResolution 的方案**：产品只写 orthographicSize，不主动调用刷新或写 camSize/range/follow/position。真实 native RefreshResolution 发生时，由 Prefix 临时提供 1x 基线、Finalizer 恢复倍率，并保留原异常；三 patch / 两 target 的当时最终手测通过。不能把 7 月已关闭的纠偏重新采纳为最新实现。当前契约仍以 Zoom 源码/Hook owner 为准。来源：[Z1、D4 与第二轮手测闭合](../../archive/updates/2026/20260801-0001-zoom-debugconsole-d4-correction.md)。
