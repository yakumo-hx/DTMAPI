# 20260728-0005 既有外部消费者原生引用准入根因审查

**Date:** 2026-07-28  
**Status:** recorded  
**Source audit:** `docs/reviews/code/2026/20260728-0004-dtmapi-055-prerelease-route-completion-audit.md` P1-3
**Owning Update:** `docs/updates/2026/20260727-0001-dtmapi-055-prerelease-route.md`  
**Non-acceptance evidence:** `docs/debug/evidence/GAME-SMOKE/20260728-224404`

## 观察

审计要求把 Workshop `3743621104`、`3743644065` 和 `3754869009`
从静态 ABI 解析推进到 Unity Mono 真实加载。首次组合运行在启动前因 Windows
PowerShell 5.1 解析中文 runner 失败，没有启动游戏。runner 改为 ASCII 后，
第二次第三存档 `NoNativeSave` 运行完成，基础 smoke、标题返回、进程退出、
官方 Mod 配置恢复和玩家存档/sidecar 不变均通过，但三个目标 Mod 都没有进入
`Assembly.LoadFrom`。

`DTMAPI/logs/latest.log` 对三项分别记录：

```text
Manifest discovery failed: ... strict-native-reference-forbidden
```

其中：

- `3743621104` 的冻结入口引用 `0Harmony`、`Assembly-CSharp`、
  `UnityEngine`/`Unity` 和 `AK.Wwise`；
- `3743644065` 的冻结入口引用 `0Harmony` 和 `Assembly-CSharp`；
- `3754869009` 的冻结入口引用 `0Harmony` 和 `Assembly-CSharp`。

因此 `20260728-224404` 是有效的非验收根因证据，不能记为外部消费者 PASS。

## 代码与权威事实

`ManagedModClassifier.Classify` 把缺少 `CodeModKind` 的旧 manifest 归为
Strict，再无条件执行 `ValidateCodeModAssemblyBoundary(... advanced:false ...)`。
该边界会把所有非平台 AssemblyRef 当作 `SDK160` 禁止项。这个检查对当前
Author SDK 生成的 Strict 包是正确的，但它也覆盖了 0.5.5 之前已经公开、已在
Catalog 和 retained ABI baseline 中冻结的外部 Workshop 消费者。

现有静态门已经冻结三项的：

- Workshop ID；
- UniqueID；
- 入口 DLL 相对位置；
- 入口 DLL SHA-256；
- DTMAPI.Abstractions MemberRef 集。

但 Runtime 分类器没有消费这组既有兼容身份，所以静态 ABI 的通过无法转化为
旧输入的加载授权。

## 所有权与最小修正

这是 Loader 的旧输入兼容准入，不是 ProductNative，也不是新的 Advanced
作者通道。最小修正应由 Core manifest 分类器拥有，并且必须同时满足：

1. 来源为原生订阅快照已验证的 Workshop 根；
2. Workshop ID 与 Catalog 冻结行一致；
3. manifest UniqueID 一致；
4. manifest 仍为未声明 `CodeModKind` 的旧输入；
5. 入口相对路径一致；
6. 入口 DLL SHA-256 一致。

只有上述精确入口可以跳过 Strict 原生 AssemblyRef 拒绝；包内其余 PE 仍经过
原有 Strict closure 检查。普通 Local、OfficialLocal、未验证 Workshop、
显式 `CodeModKind=Strict`、未知 ID、错误路径、错误 hash 和额外原生 DLL
继续 fail closed。分类仍是旧版兼容的 Strict 声明，不得伪装为 receipt-bound
Advanced，也不得获得 Advanced owner/receipt 承诺。

Catalog 继续作为产品/兼容身份权威；现有 `externalCompatibilityBaseline`
行增加精确的 legacy native admission 投影，Core 只嵌入并读取该 Catalog，
不建立新的 receipt、schema 或 builder 家族。`SDK160` 的 Author SDK 检查和
普通 Strict Runtime 检查不变。

## 已排除或未证明

- 已排除“候选 Abstractions 缺失 MemberRef”为本次失败原因；23/23 静态解析
  已通过，实际失败发生在 assembly load 之前。
- 已排除“订阅来源未验证”；runner 的 preflight 和游戏内官方订阅快照均绑定
  精确 Workshop 根。
- 尚未证明三个旧消费者在放行后能够完成 Entry/provider 行为；必须以新的
  Unity Mono 运行验证。
- 尚未证明任意其他旧 Mod 可以安全使用原生引用；本修正不得扩展到未冻结输入。

## 验收

- Unit：精确五元身份加旧声明通过；hash、ID、UniqueID、路径、来源、原生订阅
  验证和显式 Strict 任一漂移均拒绝；额外原生 DLL 仍拒绝。
- Catalog：三项 admission 投影与 retained ABI baseline 的精确 DLL hash
  交叉一致，第四项外部安装器不进入该受管加载通道。
- 游戏：一次第三存档 `NoNativeSave` 组合运行分别证明三项精确 Workshop
  load-source、Entry、适用 provider/API 行为、无 CLR load exception、标题/
  退出清理，以及订阅树和玩家存档不变。
- 只有游戏门通过后，三项 Catalog `releaseGate` 才能从
  `ActualLoadLanePending` 改为 `ActualLoadLaneVerified`。
- 因 Core/Runtime 字节会改变，旧冻结 Runtime 身份和依赖其字节的运行证据
  不能继续冒充新候选证据；Owning Update 必须重新冻结并明确最小复验范围。
