# 独立 Workshop 发布构建器 Advanced reference fixture 接线审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：DTMAPI 0.6.0 final candidate build root-cause review
- 失败候选 HEAD：`d85bd07e2ac69a0a8b93ea3621a03295b60d5e9c`
- Owning Update：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

## 1. 失败事实

从干净 `d85bd07e` 执行：

```powershell
pwsh -NoProfile -File tools/scripts/build-release-workshop-packages.ps1 `
  -Configuration Release `
  -OutputRoot E:\Python_project\DTMAPI\dist\dtmapi-060-candidate-d85bd07e
```

Runtime、Player Doctor 与第一个 AutoFishing Advanced 包均成功；AutoFishing SHA-256 为既有确定值 `23AEA43A...BF36`。第二个 ActionSpeed 在 Author SDK `build-report` fail-closed：

```text
SDK202: Doloc Town Steam build 24456188 does not match tracked Advanced reference policy build 23762374.
```

全部平台编译为 `0 error`；只保留 Unit 内既有 DebugConsole `10` 条 nullable warning。失败发生在产品构建按 policy 核对游戏参考字节时，部分 staging 不能作为候选。

## 2. 根因

当前九产品集合含三项 `24456188` policy 与六项仍冻结在 `23762374` policy。默认完整 Release driver 已经按每个 Catalog `referencePolicyId`：

1. 调用 `build-advanced-reference-game-fixture.ps1`；
2. 从唯一 exact reverse `Assembly-CSharp.dll` 与 tracked offline BepInEx archive 构造最小 Steam game root；
3. 将该 root 通过 `-GameDir` 显式传给 shared Advanced builder；
4. 对同一产品 primary/repeat 两次构建。

独立 `build-release-workshop-packages.ps1` 虽使用同一精确九产品 selection 和 shared builder，却没有传 `-GameDir`。generic builder 因而调用 `Resolve-DolocTownGamePath`，错误读取当前安装游戏 `24456188`。AutoFishing 恰好同 build，掩盖了缺口；ActionSpeed 是排序中的首个旧 policy 产品，所以稳定暴露。

这不是 ActionSpeed 源码、receipt 或 policy 过期，也不授权把六项旧产品重签到 24456188。完整 Release 已有绿色 exact-policy 构建证明；缺失的是最终独立 Workshop staging 入口的同一接线。

## 3. 有界修复

- `build-release-workshop-packages.ps1` 继续从既有 `Get-DtmApiReleaseContractAdvancedProducts` 选择九项，不新增 selection authority。
- 非 `SkipBuild` 的 Author SDK 产品必须从其唯一 Catalog row 读取 `referencePolicyId`。
- 每个唯一 policy 在一个随机、专用的仓库 `temp/release-workshop-reference-games-*` 会话下只构造一次 exact reference fixture，并向 shared builder 显式传 `-GameDir`。
- `finally` 只在 canonical repo `temp` 子路径、非 temp 根且名称前缀精确时递归清理该会话；成功与失败都不留 reference fixture。
- `SkipBuild` 继续只消费已有 Author SDK ZIP，不要求本机游戏或重建 reference fixture。
- 不改变 policy、receipt、Catalog schema、SDK target、产品源码、发布版本、Runtime 包布局或九产品集合。

## 4. 验收边界

1. 既有 Advanced fixture matrix 继续通过 23762374/24456188 positives 与 missing/ambiguous/archive/path/existing-output negatives。
2. release artifact set gate 锁住 Workshop builder 必须按 policy 构造 fixture、显式传 `-GameDir`、并在 `finally` 清理；九项选择与三项 forbidden-before-builder 保持。
3. PowerShell 7 与 Windows PowerShell 5.1 都能解析变动脚本并通过 focused gate。
4. 从干净修复提交重新执行独立 Workshop build；九个 source candidate 与 Runtime 必须完整生成，旧 policy 产品不再读取当前安装游戏。
5. 构建后仍需另行组装 two retained trees、执行 Candidate11 exact preflight，并在 Runtime lock 下安装/运行 Local11；本修复本身不是游戏 PASS。

## 5. 解决路由

本 Review 保持 `recorded`，不拥有实现状态、changed files 或 PASS。fixture 接线提交、聚焦验证、clean standalone rebuild、候选摘要与后续原子发布纠正统一记录在 [DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)。

## 6. 回滚

可回滚独立 builder 的 fixture 接线，让旧 policy 产品继续明确 fail-closed；不得用当前游戏替换冻结 reference、手改 receipt/policy、跳过 SDK202 或把部分 staging 标成候选。
