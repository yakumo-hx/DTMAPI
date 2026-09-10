# Runtime 测量与验收范围

本页的历史证据来自 [ISSUE-010](../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)；现行测试触发和结束条件由 [产品验证流程](../../workflows/product-change-validation.md)负责。历史 Release、长测和隔离参数不自动成为新任务要求。

7 月 .NET 8 的 warmed 输入/钓鱼单元测试证明了特定线程和代码路径零分配，不能外推 Unity Mono。Mono 虽暴露分配 API，但保活同线程 4,096 字节数组后仍读 0→0；因此旧 AllocatedBytes=0 结论被撤回，缺失/失效指标须 Blocked/null。Process private/working 也曾是 stub 0，后以 Win32 GetProcessMemoryInfo 获取真实值。能力检测需要行为校准，不能只检查方法存在。

分配子探针失效不必使独立趋势测量停止。600 秒 quiet baseline 各涨约 28–30 MB，仅是那个窗口的观测，不能充当泄漏阈值。1,800 秒 inactive 样本保留 61 点、零结构增长、尾部平稳及下降的 Gen2 后低点；private 中途波浪造成 OLS 正斜率，终值却低于尾窗起点。结论只清除 inactive AutoFishing 的持续持有嫌疑，随后明确停止补跑同类 inactive/platform baseline。

活跃 AutoFishing 与 ActionSpeed 加速各有责任路径。单位真实分钟和单位鱼/动作需要同时观察，避免只看吞吐更高带来的绝对分配；没有观测到同 Animator 就不自动增加跨产品仲裁。早期 100/500 鱼候选后来被当前 preview 的约 10 分钟/10–20 鱼范围替代，不能继续把旧候选列为必过门。

Batch 5 的正式历史验收后来完成：ActionSpeed 四类动作各 L0–L5、每阶段 600 秒；AutoFishing L0–L5 独立完成。最初高倍数能量 fixture 每秒检查错过消耗而停止，改成 250ms 后复跑；那是 QA 维护失败，未见 Fatal。父 runner 后续失败不抹去已经完整的 ActionSpeed 子阶段收据。

7 月 28 日候选验证缩成明确选择的当前行为，12 次游戏都 NoNativeSave、退出前证明 archive 和 committed sidecar 未变，没有玩家存档回写。之后发现 AutoFishing 原 stage 仍写旧最大 600 秒；对未变 raw/stage hash 重新运行当前最大 190 秒的 validator，实测末点约 22/10/13/13 秒均通过，未重跑游戏，也未改写旧收据。只有判据解释变化时，可保留原事实另附重评结果。

7 月 31 日 frozen 0.5.5 一小时标题样本通过，OS sampler 120 点、尾半小时近乎平稳；仍不建立全游戏内存预算。后续协程退休、shutdown guard 和相对 frame-health 修正只有源代码/Unit 验证，记录明确保留新二进制的游戏证据缺口。源码修正、有限场景 PASS 和长期风险结论必须分别表达。
