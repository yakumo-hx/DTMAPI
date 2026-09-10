# 长期方法预设复盘与 0.7.0 兼容决定

- Lifecycle: `accepted-design`
- Scope: 用户要求将 SDK 取舍教训用于未来路径，找出可先比较的方法再实施；允许暂缓 0.7.0，但现有 Mod 与接入方式必须正常。
- Baseline: HEAD `98b04998` 和保留的当前工作树规划/治理改动；SDK/Runtime r5、多平台 r1 的原验收范围不变。当前公开仍为 0.6.1。
- Owners: [方法规格](../../../planning/platform-next/method-validation.md)、[路线](../../../planning/platform-next/roadmap.md)、[首发兼容执行包](../../../planning/platform-next/execution-compatibility.md)；本次文件改动/验证归 [Update 0010](../../../updates/2026/20260910-0010-platform-method-validation.md)。

Resolution 2026-09-10：用户确认 CSV 为第一方临时接口、删除有效且不需要恢复；旧 SDK 从未发布。[0011](../../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)已撤销本 Review 的 CSV 恢复/默认不豁免推论和旧 SDK 迁移义务，当前执行包已修正。其余方法验证路线保留，以下为当时推理，不重新成为已撤销任务的授权。

## 判断

存在与 SDK 相似的路线风险：已有规划详尽描述了自建方案完成后如何验证，却在方法比较之前固定了 sidecar/journal、通用候选编辑、首个补丁 Host、声明式 UI 和自存实体模型。严格测试一个预选方案，并不能证明该方案是作者和维护者成本最低的选择。

保留已经成立的 owner、事件、调度、原生准入、共享类型、配置/数据、输入及交付机制；不再审一遍全部平台，也不为减少代码而撤掉真实保护。改变的是尚未公开能力的选择顺序：先用原生/标准做法跑通作者旅程和最早反例，再为真实缺口增加机制。原有完整故障/Mono/游戏验收继续约束被选方案。

## 源码与历史证据

| 发现 | 本轮核对与证据边界 | 路线修正 |
| --- | --- | --- |
| SDK 标准后端已接受，但实际 IDE 接缝主要放在后段 | [0004](20260910-0004-sdk-msbuild-architecture.md)的标准编译证据只证明基础原型；当前模板仍覆盖 Build/Restore 回调 CLI，真实 IDE 和完整 pack/Mono 未贯通 | 不重开后端选择；PN-041.a 前移薄集成/实际 IDE 骨架、工具链解析和一个旧工程映射证伪，后续完整验收保留 |
| 通用 SaveData 过早选择侧车 | [ISSUE-028](../../../debug/issues/ISSUE-028-20260824-moreequipment-save-slot-reuse-v3-sidecar.md)在 8 月已讨论同档字符串容器；[历史审查](../../../archive/reviews/manual-qa/2026/20260824-0001-moreequipment-deleted-slot-reuse-review.md)暂缓与 MoreEquipment 停用后退物/暂存的产品选择有关，非技术否决 | PN-024 先比较同档与侧车，不继承单产品 orphan 恢复系统作为通用平台前提 |
| 当前同档候选仍存在 | build 25163613 / Assembly-CSharp SHA `60489873c645886c5a523fd0d17c4d451a7d68df133f552c6667501245110ac6`：CityArchiveData 序列化 dialogueManager，DialogueManager 序列化 variableStorage，DialogueVariableStorage 有值/类型字典和 string setter，DialogueRunner 交给 Yarn 并 SetProgram。参考根在 [M4 实验](../../../planning/platform-next/m4-experiments.md)；只是源码候选，尚未证明初始化保留未知值 | 最早试无 Runtime 冷加载/对话/重存，再扩两 owner、容量/碰撞、复制回滚；成功不授权迁移旧产品，失败不自动升级全局 serializer patch |
| 原生存档提交与外层异常不同 | 当前 LocalSave temp→Replace/Move 与 AfterSaveData/DolocAPI 外层步骤分开；DuplicateGame 的 JObject 操作可能携带同档字段，但本轮未实测 | 保留 native/outer 结果区分；外部 journal/提升阶段按后端适用，不要求所有 owner 两阶段独立持久化 |
| 内容已有原生实现可比较 | 当前 ModManager.UpdateCache / LoadWithMods / LoadSpriteFromFile 已有启用顺序、JSON 合并/校验和 Sprite 覆盖；DolocConfig.Loader/TbItem 与 SpriteAsset/DolocAssetCache 的实际消费者输入已准备 | R4b 加官方内容→薄适配→受限编辑对照，只有后一种需要原先的回调深隔离机制 |
| Host 容易被原生扫描绕过 | ModInfo 会扫描官方 Content 下 PNG/JSON。新 Host 仅标 inactive 可能不能阻止原生生效或双重加载 | 通用绑定先做一 Host 两 Pack；受控资源必须验证原生侧没有旁路。已有合法官方内容保持原方式，不强迫转新格式 |
| 领域/UI/实体尚未证明必须自建 | 当前领域冻结壳和 UI 入口不能证明完整能力，也不能证明必须拥有通用事务、布局 DSL 或实体保存重建引擎 | 两作者分别试 native 单次操作、现有 UI 复用、官方家族原生保存；只为缺口设计新层 |
| 首发兼容存在默认删除豁免 | 当前候选说明和 ABI harness 有 CSV 族删除豁免，harness 主要是更早 0.5.2 与保留样本；不能等价于完整公开 0.6.1→候选升级证明 | PN-042 用准确公开字节重建兼容基线，默认最小旧 ABI/可工作适配，旧调用者和实现者都测；本次不沿用删除豁免 |
| 多平台候选已经完成 | [0008](../../../updates/2026/20260910-0008-multiplatform-070-candidate.md)已接受独立 0.7.0 r1 的 Windows/WSL 安装升级恢复；设备游戏注入仍未测 | 不重复“没有构建多平台”的旧发现；Runtime 若因兼容修正改变，再更新准确两投影及受影响测试 |

SMAPI 仅用本地源码比较职责：[DataHelper](E:/Python_project/SMAPIlearning/SMAPI/src/SMAPI/Framework/ModHelpers/DataHelper.cs)借助游戏 CustomData 和原生保存，[IContentPack](E:/Python_project/SMAPIlearning/SMAPI/src/SMAPI/IContentPack.cs)及归属帮助器提供包访问/归属，不要求先实现 Content Patcher。借鉴的是由原生或成熟工具承担已有责任；没有假定 Doloc 有同名官方 ModData，也没有复制第三方或反编译实现。

## 已决定的调整

1. V 标识并入现有 PN / R 节点，不新增一套审批、收据或实时状态账本。记录作者操作/额外规则、旧行为、接缝/状态/恢复成本与反例，不用“统一可控”代替比较。
2. SaveData 后端在 R4a 方法段选择；当前优先证伪同档变量容器。sidecar 完整故障矩阵保留作候选，不预先冻结永久公共 SaveIdentity 或独立 journal。
3. PN-013 拆只读包输入/资源与 GameContent 方法/实现。Host 仅依前者及依赖/owner；第一 Host 不强制成为平台补丁 DSL，官方内容不强迫迁移。
4. PN-027/028/029 先对照原生操作/UI/持久家族。额外随档状态才依 SaveData，原生已存的实体不重复保存/生成。具体公开模型在方法证据后冻结，产品级失败/缺失恢复门保留。
5. PN-040.b / R-Assets 按标准资产与真实 Mono 组合增量，PN-032 / R6 按已有工具和真实负载补自动化；不预设通用 IL 重写、每 build 全套 Bridge、中心实验室或服务器。
6. 0.7.0 保持暂不放行：PN-041 完整 SDK 门与新增 PN-042 兼容门并列。CSV 以最小可工作兼容适配为推荐，不恢复旧 UI，不把无 provider 的所有历史壳升级成玩法承诺。冻结作者载荷原字节保留；若 Runtime/recipe 必须改变，用准确新候选，不原地冒名改 hash。
7. 近期同任务完成 PN-041.a–f＋PN-042.a–c。a 阶段早期证伪/基线独立准备，中段同步必要修正，最终准确候选合并 Release/游戏验收；不因两个任务编号重复全量验证。
8. 0.7.X 能力分组保留，保存/内容可根据方法结果换顺序，Host 可先独立成立。未来实验不一律阻塞 0.7.0；若发现影响现有公共契约/数据/入口的新问题，才转具名首发修正。0.8 正式清退仍按逐族公告/日期/迁移条件。

## 证明与未证明

本轮进行了源码、当前规划和历史记录复核，三项独立检查分别覆盖保存、内容/Host、兼容/渠道。没有运行新的作者原型、IDE、Mono、保存/内容游戏实验；没有实测“同档容器可靠”，没有恢复 CSV，也没有建立新 SDK 后端。

本 Review 接受的是路线和方法规格。PN-041/042 仍待实施，R4a/R4b 仍未通过；“可先验证”的发现不等于已选择可公开的实现。对旧 Mod 的目标是守住已支持行为并覆盖真实代表组合，不宣称能数学证明任意未知第三方 Hook/反射/副作用完全不受影响。
