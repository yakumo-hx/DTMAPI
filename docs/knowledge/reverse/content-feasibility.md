# 内容扩展的可行性边界

适用资料是 2026-07-17 在 `23762374_public_C416D4` 上的两份只读研究；以下是历史中可复用的责任分析，尚未证明新产品实现或新游戏兼容性。

[装饰动物研究](../../archive/reviews/api/2026/20260717-0005-kenenimuu-decorative-animal-capture-native-owner-review.md)区分了场景池化的 DecorativeAnimal 与存档/房间持有的 Animal。前者重新渲染会重建，不能只改一个“可捕获”标志；若未来实施转换，原生牲畜袋与源位置去重必须同属可恢复事务。该可行性讨论本身不是一项未完成实现授权。

[怪物捕获及生产研究](../../archive/reviews/api/2026/20260717-0006-official-json-monster-capture-production-native-owner-review.md)经用户两次纠正后，把 JSON 能力和缺失行为分开：已验证的内部 JSON 格式不因官方教程没刊载就需要额外 DLL 许可层；活体捕获、按设备限制物品和双向隔离仍需要行为实现。普通 ItemFunction 不会因 subtype=tool 产生工具动作。怪物 Remove 与 Kill 也不同，前者仍可能触发特殊 decorator 的剧情，因此不是任意怪物的安全通用捕获入口。

生产机制需要看实际消费方：生态鱼缸按现有核心数量加权选择产物、核心不消耗；产出的 farm-fish ID 还可能被原生转换成鱼卵。ResinCollector 有容量上限，但加上电力配置不代表其更新函数会消费电力；SynthesizerGenerator 则持续生成地面对象，不自带缓冲上限。当前 Mine 产品的实际设计和验证应从产品 owner 进入，不能照搬这些早期备选。

两份研究中对所有脆弱行为统一置于 GameBridge 的旧物理归属建议已被 PROJECT 取代。可复用的是原生状态持有者、输入/输出和失败边界，不是当时的分层结论或整套第三档门槛。
