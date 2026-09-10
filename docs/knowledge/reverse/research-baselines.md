# 研究输入、生成地图与游戏基线

本页整理建设期间读到的研究证据。它用于选择资料，不替代当前 API/Hook 状态；完整捕获命令从 [references](../../../references/README.md) 进入。

## 旧研究能指出入口，不能代替新版本证明

[6 月 17 日基线审查](../../archive/reviews/api/2026/20260617-0003-reverse-baseline-23762374-audit.md)将研究参考切到 `23762374_public_C416D4`，但明确保留原生领域报告的 `23465763_workshop_38581E` 作者基线。其“直接目标没有变化”检查只覆盖当时的选定目标和签名/方法体大小；上传器、无人机贴图与地窖仍有其他变化。签名 CSV 的分隔符从 `;` 变为 `|` 也曾制造大量伪差异。

[函数地图交付](../../archive/updates/2026/20260613-0010-native-function-map-workbench.md)的 42,925 个方法、74,488 条内部调用边属于 `23465763_workshop_38581E`。地图颜色表示研究匹配或生成系统索引覆盖，不能证明方法已经接入 Runtime，亦不能把节点数冒充最新游戏覆盖。

早期 [API 反向覆盖](../../archive/reviews/api/2026/20260607-0007-native-responsibility-code-review/05-abstractions-reverse-coverage.md)区分了“没有逐符号文档行”和“原生行为没有实现”。DTMAPI 自有框架、DTO、枚举无需为了补文档去寻找不存在的原生 owner；带有原生行为含义的结果字段则必须说明其数据来源。逐字段生成清单和逐条原生责任审阅各有用途。

## 前身和第三方材料继续留在参考边界

`references/doloc-town/research-notes/` 中六份旧笔记全文已经阅读。前身 Mod/API 记录包含旧路径、旧实验版本和实现片段；DolocPlus 三份笔记区分 DLL 元数据、CE 调用、游戏反编译和推断。它们适合定位当时的行为线索，既不是当前 DTMAPI 公共契约，也不提供可复制的实现。特别是旧文“一切脆弱 Hook 放 GameBridge”已由 PROJECT 的现行原生责任分类取代。

跨产品可复用的是问题的区别：查到箱子不等于成功扣料、原生时间流逝不等于改日期、鱼卵信息不等于鱼池概率、Zoom 不等于全景拍照、观察状态不等于拥有修改权限。具体产品知识与现行 API owner 已分别维护；这里不再复制所有候选接口表，也不因旧候选清单启动新功能。

## 捕获、比较、准入是不同结果

[便携捕获交付](../../archive/updates/2026/20260720-0006-portable-full-reverse-capture-package.md)记录的 `InventoryOnly` 曾运行 495.2 秒：它实际重算快照/导出清单并核对安装源，不是轻量状态查询。后续工具将状态读取与阶段复用核验分开。旧快照在 Steam 更新后不能再要求与新的安装源一致；复用应绑定它自身保存的输入身份。

[测试分支基线审查](../../archive/reviews/code/2026/20260721-0001-pre-release-reverse-baseline-audit.md)发现 ZIP 文件名未设 Unicode 标志、实际使用 GBK，错误解码导致路径碰撞；生成摘要又把 `test` 写成 `public`。接受后的原始收据没有倒改，纠正在该基线说明中。后续[挂载分支捕获](../../archive/updates/2026/20260806-0001-current-running-10002-full-reverse-capture.md)要求明确区分 requested 与 mounted；[1.00.05 捕获](../../archive/updates/2026/20260823-0002-public-10005-full-reverse-capture.md)还记录 Steam 退出时会改写 manifest 元数据。捕获前后的 payload/manifest 身份必须在同一个明确窗口核对。

从 [1.00.00](../../archive/reviews/code/2026/20260801-0002-current-game-vs-24256979-reverse-baseline-audit.md) 到 [1.00.07](../../updates/2026/20260907-0006-public-10007-full-reverse-capture.md)，导出层数万项 changed 经常来自 GUID/fileID 重写。稳定 scene identity、配置自然键、资源引用关系和原始程序集/音频包能区分真正变化。不能删除所有 GUID 后就宣布场景相同；1.00.06 字体修复恰恰发生在 fallback 引用，1.00.07 则有局部碰撞和位置变化。

已知 AssetRipper 设置对象/Cubemap 限制随原始快照保存；“完成捕获”不意味着恢复作者原工程，也不等于产品兼容准入。当前观察头和已有 Author exact policy 可以不同。希望返种、节日等修复的静态实现与玩家验收仍分别由相关 Review/Issue 维护，本页不把官方公告当游戏 PASS。

## 小版本漂移不必引发全产品重签

`24456188_test → 24567135_test` 的 raw/AssetRipper 差异虽大，同工具 ILSpy 实际只变两个方法体：Workshop 更新不重写 tags、version patch 先排序。生产消费的是未变 ResolveUploadPlan，排序属 QA 观察面，产品文件无变化，因此不新建 policy 或批量重签。`24585411_test` 又仅改 RefreshView 清尾/渲染顺序；间接 MoreSaves/Animal 路径已有同 build 证据。拒绝 drift hash 的旧 exact trace 是正确行为，不能伪造 policy 让它通过。

8 月 11 日 `24650773_public/1.00.03` 的完整捕获关闭 public 字节身份与静态差异未知，不是十一产品玩家回归。六个 managed 文件变化涉及 LocalSave 序列化失败、设备移除空房间/异常、鱼亚种与成就；无直接产品消费者便不要求批量改包。`UserConfig=public`、`MountedConfig=test` 是待切状态，同 build ID 也不证明 public payload。

该次 45,861 个文本/YAML changed 中，45,707 在 GUID/时间归一后相同；剩余仍要按 stable ID、引用关系、路径和值多重集判别对象顺序和真正改动，不能只删所有 GUID。最终才收敛场景/布局、三份变异鱼烘干配方、语言和既有剧情命令；Wwise raw 未变不支持新增音效推断。先绑定 start/frozen/end 身份、分 raw/managed/config/资源层，再映射当前消费者，复用未变 owner 的分析链。

来源：[24567135](../../archive/reviews/code/2026/20260805-0005-current-test-24567135-compatibility-audit.md)、[24585411](../../archive/reviews/code/2026/20260806-0003-current-test-24585411-compatibility-audit.md)、[public 完整比较](../../archive/reviews/code/2026/20260811-0001-public-10003-baseline-difference-audit.md)。
