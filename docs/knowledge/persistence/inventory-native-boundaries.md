# 物品与装备 API 的历史归属判断

当前公共契约由 [public API matrix](../../api/public-api-matrix.md)、原生物理归属由 [PROJECT](../../../PROJECT.md)拥有。本页只提取早期审查的有效区分，不宣称其旧路径或状态仍是当前能力。

2026-06-07 Inventory/GiveItem 审查区分了来源索引与原生 runtime 表：索引有条目不能证明官方加载器已合并并允许生成；启用状态、stack 上限与部分放入结果各有语义。直接 GiveItem 是诊断入口，不能替代奖励、掉落、任务或邮件规则。该审查承认部分 owner 只有 DTMAPI 反射与旧 smoke 证据，未直接核对原生方法体。

同日 EquipmentSlots 审查的实现还是 GameBridge 中的额外属性函数、侧车与克隆 UI；它既不扩展原生槽保存格式，也不让额外帽子取得原生外观槽语义。当时发现按 owner 而非 save identity 的污染风险。后来固定三槽 ProductNative 与冻结兼容 Host 的分工不能反向写进这份早期结论。

2026-06-13 Local Mod Inventory 是旧 testmods 集合快照；保留其中“展示、诊断、真实原生操作、需求参考”必须分开的原则。它列出的产品集合、Experimental 状态和旧路径由当前 Catalog/源码路由取代。第三方样本、旧自有 Mod 源码只记录需求来源，不取得实现或复制授权。

同批 native-owner domains 区分无人机内容定义、配件交易与单一 active drone；旧调查没有找到多个同时活跃原生无人机的 owner。食品 `DoEffects` 与实际消费物品是不同责任，已知 buff 的重复添加、持续时间和清理也不能推导为任意代码效果扩展能力。这些旧基线需在目标 build 重新核实，不能把表格 confidence 数字当现行验收。

20260804-0002 ChestLocator 审查把容器资格、caller、数量/max/实际扣料连起来：共享 Case 与合资格 Shelf/ItemBox 追加同一原生数组，未共享容器及显式排除的 FarmingGun 路径不参与。预览与提交是否共用集合是实际验证问题；静态 caller 未变化可保留旧 package 走 Drift，人工行为未执行仍须如实保留，不因出现新游戏版本就全产品重包。

装备 domain 页已用 2026-08-06 amendment 替换旧固定 Passive1/Passive2 模型，但末尾仍保留后续候选未发布叙述。保留该 owner library 路由；提取固定槽历史与失败后，应由当前 Hook/Product owner 回答现在的实现状态。

## 符号索引与人工归属判断

20260607-0006 的第 05 卷包含 367 个符号块。[生成记录](../../archive/updates/2026/20260607-0006-native-responsibility-method-audit.md)明确说明符号清单来自 Abstractions；[后续人工审查](../../archive/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md)只把它用于定位，并明确拒绝用 1,013 行低风险属性模板冒充更多人工归属审查。没有找到当时的生成器，不能宣称这份历史索引能够精确重建。

第 05 卷的独立判断集中在以下七个家族。表中是 2026-06-07 的历史判断，不能据此恢复旧标记或重新开出全部平台审计。

| 家族 | 历史归属与限制 |
| --- | --- |
| 机器生产 | 配方、科技、物品表和原生电子组件启动，与 DTMAPI 自有生产状态/循环混合；UI 或 telemetry 成功不能证明离线房间、电力和产出已由原生生命周期接管。 |
| 装备扩展 | 原生装备管理、配件栏、背包放入与侧车混合；侧车不等于原生额外槽，风险在事务、恢复、跨档清理和克隆 UI。 |
| 载具 | 原车 singleton 与第二辆车克隆状态混合；原车/副车共享状态、跨房间和骑乘/外观残留必须分别论证。 |
| 相机 | 只改 orthographicSize 不代表接管 CameraController、背景、景深雾和房间边界。 |
| 存档槽 | archiveFileCount 与 GameDataPanel 是当时定位到的原生 owner；当时缺完整建档/读档/删档/复制/重启验收，后续人工验证见存档迁移知识页。 |
| 箱子定位 | 可用库存集合与 LinearInventory 的计数/扣料消费者需要一致；成员 DTO 的重复记录不增加运行时证据。 |
| 种植枪 | 构造、使用和 UI 转交，以及 PlantBasin 的种子/地膜/肥料检查共同构成行为边界。 |

索引中的 367 个块仅有 7 个不同 owner 段、6 个建议段；231 个 Gap、124 个 MatrixGap、12 个 Watch 是模板投影的行数，不是 367 次独立代码审查。323 行缺显式矩阵项也不等于 323 个运行时 bug。`Symbol` 是主要定位键，历史行号会漂移；路径、签名和数量核验只能证明索引结构及基线未损坏，不能替代最新实现的行为验证。

本次对正文标题、Top Risks、定位规则及所有不重复判断字段完整阅读；重复字段与机械定位部分按混合索引核验，具体 reviewBasis 保存在[统一迁移清单](../../archive/migrations/20260908-workspace.json)，没有把该卷标成逐字全文完成。该处理不适用于包含新观察、失败或补记的人工记录。
