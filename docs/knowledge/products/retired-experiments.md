# 实验原型与退役产品的原生责任

本页保留原实验范围和退役原因；不恢复产品、Hook 或旧验收任务。当前产品身份归 [Catalog](../../../tools/release/dtmapi-product-catalog.json)，原生责任归 [PROJECT](../../../PROJECT.md)。

## SecondMotor：局部双车不等于通用多实体

6 月初曾证明 key→第二 key、同场景双可见、骑乘/下车和速度恢复。首个双可见失败只是原车在另一场景；对照必须先走原生 key 把原车带到同一室外。实现仍绕 `DolocAPI.Motor` / motorController 单例，局部 PASS 不能晋升为稳定多车 API。

复制官方 `sprite_vehicle_motor*` 全局资源使原车与新车同皮肤；停止复制不会移除已装旧文件。后续只克隆实例 tint、排除 rider，仍不能证明所有灯光和房间状态隔离。跨图旧 smoke 仅查“不 stuck”，实际玩家仍在旧地图；改用房间、距离、原车位置和退出条件才发现单例 `AgentPosition` 的污染。骑第二车时镜像隐藏原车 transform 是当时权宜，不是正确 owner 的证明。

6 月 14 日用户指定第 8/9 槽及真实输入、官方商店、多钥匙，局部通过后仍在 6 月 15 日报告灯光、农场和跨地图污染，遂明确退役。第一步停产品/包/测试；后查无活跃消费者，再删除 API/DTO、注册、Update、Hook、回调和 QA。只删 sample 或目录会留下活跃成本。`-AutoExerciseVehicle` 的 blocked 与旧 disable 参数 no-op 只服务兼容调用，不授权重新启用。

未保存的失败召唤残影与禁用后的空附件邮件也保留为反例：先证明真实附件与 enabled source，不能发空信；native 邮件成功不等于最终双车行为。旧包可比源码慢一步，真正打包/部署时检查明确陈旧 payload，不通删其他 Mod 或官方资源。

来源：[ISSUE-005](../../debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md)、[ISSUE-006](../../debug/issues/ISSUE-006-20260604-critical-manual-qa-025.md)、6 月 14–15 日退役 Review/Updates；精确原件路径可按日期在[迁移清单](../../archive/migrations/20260908-workspace.json)定位。未来载具应从新窄 native owner 和明确范围开始，不继承旧“Stable 多车”或槽 8/9 gate。

## LightningChicken 与 Shell Crab 的能力不能由其它物种代验

LightningChicken 是从废弃分支导入的研究，基线 `23762374_public_C416D4`；商店 TbStore 有商品不证明缓存 UI 可见，精确 nested AI 类型与清 cache 只完成 precheck，没有购买/释放/外观 PASS。它已退役，不成为统一四 PNG 动物包的延期输入。

Shell Crab 当时独立保留为 AssetBundle 研究，不随四 PNG 动物合包。其路线曾因 Mono 闭合泛型共享，patch `DolocAssetCache.GetAsset/CheckAsset<RuntimeAnimatorController>` 污染天气/UI prefab，停产品也不能消除已装 patch。后收窄到非泛型 `AnimatorAsset.TryLoadAsset` 和注册自定义 key，未知 key 走原生。幼体 controller、AI 模板映射、过夜醒来和繁殖各有不同证据；Hatch/Mole/Drecko/Oilfloater 后继通过不能替 Shell Crab 的成年/繁殖或 bundle unload 补 PASS。6 月 30 日历史对话中繁殖仍是静态推断，后续如需该能力须沿真实个案 owner 查证，不自动加入普通动物修复门。

来源：[统一动物包边界](../../archive/reviews/code/2026/20260713-0009-first-party-animal-pack-product-boundary-review.md)、[动物资源与模板研究](../api/audio-and-native-assets.md)、[已维护动物内容](animal-content.md)。

## LocalWikiTeleport：未到达动作就无法验坐标

8 月 23 日三轮分别把 InputSystem 订阅、自建 PlayerLoop、OnInputSystemAfterUpdate 接线成功当回调存活，实际都未触发；同进程真正 F10 来自 DTMAPI PlayerLoop/反射回退。后继线索应复用已验证的 owner-bound Keybind，不继续换键位或平行帧驱动。用户完成图片工作后要求停用并保留，包移出官方发现根，ignored 原型仍留；没有传送行为 PASS，也不自动准入新产品。

来源：[连续失败与反证](../../archive/reviews/manual-qa/2026/20260823-0001-local-wiki-teleport-hotkeys-no-dispatch.md)、[停用收口](../../archive/updates/2026/20260823-0001-local-wiki-teleport-disable-and-retain.md)。
