# Batch 6 AutoFishing Abstractions compatibility 重签审查

## 记录状态

- 日期：2026-07-20
- 状态：`recorded / implementation authorized / validation required`
- 性质：AutoFishing ProductNative 重归属删除私有 friend/primitive seam 后，Author SDK 0.5.5 compatibility payload 的根因与最小重签边界；不是公共 API 变更或发布授权
- Source：Batch 6 AutoFishing pilot 实现中发现，删除 `FirstPartyFishingPrimitives.cs` 与 `InternalsVisibleTo("AutoFishingMod")` 会改变 `DTMAPI.Abstractions.dll` 的确定性字节，而当前 compatibility contract 仍固定旧 SHA-256 `1773527AB8D28A904C6C185FD8BC4A1502746CB7FA228E27AB80D15370357D44`
- Owning Update：[20260720-0008 Batch 6 AutoFishing Advanced Pilot](../../../updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md)
- Parent Review：[Batch 6 AutoFishing Advanced Pilot 前置审查](20260720-0006-batch6-autofishing-advanced-pilot-prerequisite.md)

## 一、已知事实

1. `FirstPartyFishingPrimitives.cs` 和 `InternalsVisibleTo("AutoFishingMod")` 是单一旧产品通道的 `internal` contract，不属于公开 ABI；AutoFishing 新产品不得继续依赖它们。
2. Author SDK 的 `compatibility.contract.json` 绑定完整 `DTMAPI.Abstractions.dll` 字节，而不仅是 assembly identity 或公开 surface。因此，即使只删除私有 IL，当前 `build-author-sdk.ps1` 也会在打包前正确 fail closed。
3. 初始并发构建观测到删除后的候选 SHA-256 为 `4792C0AC...961B`；该值只是迁移中的诊断，不得写入权威 contract。最终值只能从干净、确定性 Release 构建生成。
4. G2 的不可变边界是 synthetic policy、compiler surface、fixture/package/runtime/ownership receipts 及其语义。不能为解决本问题修改或重生成 G2 policy、surface 或历史 receipts。
5. 保留旧私有 seam 以追求旧 DLL hash 会继续把单消费者 ProductNative contract 留在 mandatory Runtime，与已接受的 G4 物理归属结论冲突。

## 二、根因与拒绝路径

根因不是公共 ABI 漂移，而是 compatibility contract 把“完整 payload 字节”当作信任锚；本批次有意删除了 payload 内的私有产品专用 IL。contract 按设计拒绝了未重签的新 payload。

拒绝以下路径：

- 恢复 friend/primitive seam、用未引用或生成代码保留等价私有 IL；
- 绕过 `build-author-sdk.ps1` 的 SHA 校验，或手改打包结果中的 compatibility manifest；
- 修改 G2 policy/surface/历史 receipt 来吸收新的 Abstractions hash；
- 只比较版本号、公开类型数量或手写 JSON 常量后宣称兼容。

## 三、授权的最小修正

完成 ProductNative 删除并稳定源码后，允许只更新 `author-sdk/compatibility/0.5.5/compatibility.contract.json` 的 `abstractionsSha256`，把它绑定到同一源码提交的确定性 Release `DTMAPI.Abstractions.dll`。若构建脚本生成的 compatibility/release manifest 因 payload hash 自然变化，可生成新的候选产物；不得修改 assembly version `0.5.3.0`、file version `0.5.5.0`、SDK version、props、NETStandard inventory 或公开 API 来追逐旧 hash。

## 四、强制证明

重签只有在以下检查全部通过时成立：

- 两次干净 Release 构建产生相同候选 Abstractions SHA-256；
- retained 0.5.2 public-surface diff 与 exact retained AutoFishing binary ABI canary 通过，公开删除为零，`StopOnManualMove` getter/setter/default/`Obsolete(false)` 保持；
- assembly name/version/public surface 与重签前一致，变化只来自已审查的 internal primitive/friend 删除；
- Author SDK build/check/test 通过，embedded contract 仍能拒绝共同替换的 manifest/payload；
- G2 policy与 surface 的 exact hash 不变，synthetic build/pack/runtime/ownership 永久门在 PowerShell 7 和 Windows PowerShell 5.1 继续通过；
- AutoFishing package 使用新的 SDK 候选正常 build/pack，且 package 不携带 Abstractions 或其他 Runtime/native DLL。

任一检查失败时，不得更新 contract 或把 Update 标为 verified。0.5.5 发布仍由独立 release provenance、候选和发布门控制；本重签不构成发布授权。

## 五、回滚

回滚时撤销本次 contract hash 与 AutoFishing ProductNative 迁移的同一逻辑边界，恢复最后一个可构建候选；不得单独放宽 SDK hash 校验，也不得删除 retained ABI canary。G2 历史 authority 始终保持不变。
