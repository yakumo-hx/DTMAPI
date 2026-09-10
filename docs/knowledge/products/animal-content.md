# 动物内容：原生状态、产品身份与经济

本页保留历史研究脉络，不赋予素材再分发权，也不将旧待办视为当前未实现事实。现行身份查 [Catalog](../../../tools/release/dtmapi-product-catalog.json)，官方与外部材料边界查 [references](../../../references/README.md)。

## 统一包的历史决定

2026-07 选择四种 PNG/WAV 原型作为统一内容产品输入，Shell Crab 的 AssetBundle 路线及 Lightning Chicken 退役研究不随之加入。新包有独立产品身份；原生动物 protoName、袋子、产物、动画及声音标识是保存/引用兼容问题，不能靠文件重命名解决。旧输入与新包同时启用可能在原生 JSON 导入时已碰撞，不能默认“新包赢”。

当时提出普通/隐藏自定义产物经官方 JSON 加工回原生资源的经济路线；具体概率、价格、加工结果由产品数据拥有，不能固化在 GameBridge。素材来源、JSON 与代码分开，使替换美术不自动成为保存迁移。7 月的私有素材和经济 pending 必须与后续产品实现/发行记录核对，不能原样覆盖现行结果。

来源：[睡眠诊断及根因修正](../../archive/reviews/code/2026/20260630-0003-custom-animal-sleep-state-log-audit.md)、[统一动物包早期边界](../../archive/reviews/code/2026/20260713-0009-first-party-animal-pack-product-boundary-review.md)。

8 月统一 AnimalPack 设计最终覆盖多轮 V0：Hatch/Drecko/Mole/Oilfloater 普通定位为矿/毛/肉/燃料，周期最终 1/3/1/1 天；Drecko 隐藏直接羊毛脂，无隐藏蛋；数值与后续实现应链接对应产品 owner，不能从长 Review 前半模拟表抽值。单包必须保留 species、袋、animator/音频身份，并与旧四包互斥；四 owner 共存证据不自动证明一 owner 四定义。

小动物照料站采用独立原生 GarbageShredder 组避免共享整行覆盖，仍继承可回收设备行为，纯 JSON 没有“只许七种蛋”过滤。特殊喂食需物品 ID 进入 Animal.Eat，普通饲料只补能量；隐藏贡献阈值不是天数。PNG 像素/PPU、sprite_size 碰撞、size 地面宽度、space 畜棚容量是四个不同事实。frame_manifest 当前不是 Runtime 枚举帧权威；旧 movementMultiplier/metabolismMultiplier/packageItemId/shopItemListId 不驱动产品。教程应以仓库验证包派生短主线与字段参考，不能默认从可变 LocalLow 生成。

ONI 照料站 411 张提取序列当时实际仅三种唯一图，不能称为烘焙动画；本地视觉原型不赋予公开再分发许可。8 月 28 日 Workshop 审计只证明 211 文件订阅字节与候选一致，上传后本地 workshop.json 的差异合理，未扩大成全经济链实机通过。该 OfficialJsonContentPack 不应套 Runtime 安装 BAT/PowerShell 压力矩阵。

来源：[经济、单包与教程多轮决定](../../archive/reviews/code/2026/20260827-0001-animalpack-economy-tutorial-unification-preimplementation-review.md)、[Workshop 订阅一致性](../../archive/reviews/code/2026/20260828-0001-animalpack-workshop-subscription-parity-audit.md)。

## 已关闭的声音、图标和照料站误判

原版 marsh_pangolin 与自定义 mole 共用声音事件名，不代表共用替换作用域；2026-08-27 原版控制组没有任何替换命中，路由以当前实例 protoName 精确限定物种。听感不确定不能升级为串音，也不能把零替换日志说成已录音识别原版声音。七种蛋显示成年动物则是 AnimalPack 的 ui_sprite_asset 占位键，控制台忠实显示原生物品图，没有按蛋名猜图的责任。来源：[声音与蛋图 Review](../../archive/reviews/manual-qa/2026/20260827-0002-animalpack-native-voice-and-egg-icon-review.md)。

该 Review 后续确认了两套尺度：世界物品精灵受 PPU 影响，而 Equipment.PositionCenter 使用 Sprite.rect 的原始像素高度，不读取 PPU。249 像素设备把产物中心推到 +15.3125，提高 PPU 不能修正；需裁透明边并降到原生设备像素规格。蛋也需原生物品尺度或等价 sprite 元数据。逻辑占地与可见美术体量仍分别验证，不能仅改 cover_size 宣称落地/掉落坐标完成。

“第二枚蛋显示完成却没有产物”的调查最终关闭为原生酸雨暂停：包声明 WDP_AcidRain_Worker，原生天气装饰器在酸雨期间接管 Update/UpdateNoRender 并不推进 worker；结束后按 durationTu 恢复。用户最终确认普通连续加工与 Y 跳时都正常。早先“第二枚未入槽”“只有第二枚经过跳时”“Y 跳时不支持设备”的解释先后被用户证据否定，不应再触发配方、容量或 Runtime 特判。DropAsync 每单位 200 ms 的出料延迟是另一现象，不能替代天气判别。此项正常不等于当时全部七条配方、隐藏贡献与商店经济都已验收。来源：[同一 Review 的逐次纠正与酸雨关闭](../../archive/reviews/manual-qa/2026/20260827-0002-animalpack-native-voice-and-egg-icon-review.md)。

Hatch 的 loose PNG 路线继承鸡的 native AI/controller，并按已登记的 renderer 实例映射帧；回收 renderer 时清上下文，防止池化后把鸡改成 Hatch。独立 ShellCrab AssetBundle 路线不因此获得验证或被替代。初版只证明注册与 Hook ready，没有释放动物，不能声称真实帧/产物通过；包根缺官方 info.json 时，手改 mod_infos 的启用会在官方刷新中消失，补齐 package metadata 才稳定。来源：[Hatch PNG 原型实现](../../archive/updates/2026/20260630-0001-hatch-png-custom-animal.md)。

睡眠诊断不能只看首次 FixedUpdate：初次采样从 pooled idle 变为 sleep，只证明那个采样点。用户持续站立/移动的反证随后定位到午夜重建 controller 后 FreeTime 首帧产生 AnimalMove，而进入 Sleep 状态不会自动取消任务。历史修正仅约束已登记自定义物种、Night 且 isSleep 的决策/运动任务，保留原版与正常早晨/工具唤醒；五夜用户手测通过后，稳定 wait+sleep 的诊断立即结束。签名剔除 normalizedTime，否则每帧都会被误作状态变化。来源：[长时序诊断](../../archive/updates/2026/20260630-0004-custom-animal-sleep-render-root-cause-diagnostics.md)、[任务边界](../../archive/updates/2026/20260630-0005-custom-animal-sleep-task-boundary.md)、[验收后裁减诊断](../../archive/updates/2026/20260630-0006-custom-animal-diagnostic-followup-log-trim.md)。

AnimalVoice 初版要求先成功启动 WAV 才抑制原生音频；无上下文、预加载未完成或播放失败仍保留原生声音。初版把冷却也视作 fail-open，随后 [冷却修正](../../archive/updates/2026/20260701-0015-animalvoice-cooldown-native-suppression.md) 证明这会泄漏模板叫声：ready 且 suppressNativeWhenReady 的匹配项在冷却中继续抑制原声，只跳过重复 WAV。非叠放 smoke 没复现该触发，最终由用户多动物叠放手测接受。Hatch 首次 smoke 每帧强刷内容包，反复拆掉注册使 preload 无法稳定，改成一次刷新后等待才通过。自动运行缺少原版鸡时 negative 为 not-applicable，后续用户人工确认原版不串音，不能把前者改记自动通过。ShellCrab 第二消费包只改 JSON/WAV 并人工接受，无需重新扩建共享运行时。来源：[Hatch 声音基础设施](../../archive/updates/2026/20260701-0001-hatch-animal-voice-audio-replacement.md)、[ShellCrab JSON 复用](../../archive/updates/2026/20260701-0002-shell-crab-animalvoice-json.md)。

AnimalPack 的官方表还提供了两个容易混淆的 owner：animal_shop 没有随机货架，spawn_weight≤0 才是固定条目；完整动物袋回售价来自被捕获动物的 child/adult levels[].price，而不是袋物品 selling_price。照料站 Item 的 electric_energy=0 也不是设备功耗，功耗归 equipment 电气组件，负 selling_price 则由原生制作材料求和。上述源码/静态表值不等于已完成商店购买、全部配方和隐藏产物实机验收，未完项仍查 [统一包实施 Update](../../updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md)。

第七档‘大型畜棚’依赖 MoreSaves 开放 UI 槽位；禁用全部其它产品时误去掉这个访问依赖会让测试根本无法进入。四包 WAV ready 只证明预加载，8 月 27 日单独事件 gate 只实际证明 Hatch 成年/幼体；双 chicken 模板探针仅验证独立 species 上下文，不自动扩大为全畜牧生命周期。统一包后用户已正常等待/Y跳时复测，重复第二枚蛋的同天气假设应停止。来源：[第七档四动物及双模板](../../archive/updates/2026/20260827-0001-custom-animal-10005-slot7-runtime-acceptance.md)、[统一包实施](../../updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md)。

早期 Hatch 吃饭回鸡图的首因是源包 44 PNG/eat 0–6，现场仍 38 PNG/eat 0–3；缺帧 fallback 不是模板状态设计错。同步确切内容后，用户与 adult eat 4–6 日志确认修复。午夜站起另有 PNG/AssetBundle 共通的 AI task 机制，不能由第一次 FixedUpdate 已回 sleep 推成单帧残影。两类现场/行为问题应分别取证。Shell Crab 的独立 bundle、成年/繁殖边界见[原型知识](retired-experiments.md)，不由统一四物种包代验。来源：[吃饭与睡眠完整手测](../../archive/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md)。
