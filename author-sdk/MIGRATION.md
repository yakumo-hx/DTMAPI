# 冻结 API 使用者迁移

当前 SDK 和默认 API target 为 0.7.0；Windows 与多平台 Runtime 0.7.0 已发布。SDK 附件的实际发布状态见[仓库发布页面](https://github.com/yakumo-hx/DTMAPI/releases)。既有 0.5.5 载荷和旧 DLL 仍是兼容输入，不要改写 manifest 或替换哈希，把它们伪装成新构建。

随包 [API 状态表](API-STATUS.md#07-candidate-migration-and-removal-preparation)从权威矩阵投影各 API 家族状态、替代方案、最早可能破坏兼容的系列，以及实际公告/移除日期。这项迁移准备没有宣布新的移除日期。矩阵单独记录历史 CSV 物理移除例外；0.7.0 没有恢复该 API，也没有再次删除 ABI。玩家产品提供自己的行为，不是冻结 API 的可编程直接替代品。

1. 重建前保留原包、DLL、manifest、SDK/target 和对应哈希。对准确包或安装目录运行只读 Doctor。有界扫描中未发现引用，不能证明外部作者没有使用某个 API。
2. 同时读取 stability 和 disposition。Disabled/Frozen 契约的 `GetApi` 成功不代表原生行为可用。例如 Lamp 返回禁用外壳，原生自定义实体创建仍被阻止，部分保留接口没有 provider。
3. 选择文档指定的玩家产品、经过独立验证的 Advanced 实现，或明确处理“能力不可用”的分支。不要把 CameraZoom 迁到 CameraView，它们都已冻结。公共 helper/context/owner-data 服务不能代替缺失的玩法能力。
4. 更新包时保留作者 UniqueID 和既有数据。移除 provider 查询不授权删除配置、全局数据、原生存档或对应 reader。保留原包，以便退出游戏后回退。

## 可运行的“能力不可用”迁移示例

用公共 CLI 创建普通工程，将生成的入口源码替换为下面示例，再运行 restore 和 pack。示例不查询 Lamp API，也不提供替代照明行为；所选 API 家族没有替代公共能力时，应明确表现为不可用。

```text
dtmapi-author new codemod LampMigration --id Linden.OldLampConsumer --name "Lamp migration" --author Linden --api-target 0.6.4
dtmapi-author restore LampMigration --json
dtmapi-author pack LampMigration --json
```

```csharp
using DTMAPI.Abstractions;
namespace Linden.OldLampConsumer;
public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        helper.Monitor.Log("Lamp control is unavailable; this version performs no lighting changes.");
    }
}
```

已有工程应保留 ID，并有意识地提高 manifest Version，不覆盖原源码/包。`install-local` 要求包报告中的准确 ID、版本和 SHA-256。更新代码前退出游戏，下一进程中再比较加载来源及版本。回退时可用 `withdraw UniqueID --game-root PATH` 撤回受管包，再以原哈希安装保留的原包。撤回包与删除 owner 数据是两项不同操作。

Runtime 在每次 owner 激活期间，对使用者及其请求的过时契约只提醒一次；重复查询不会重复提醒，诊断输出失败也不改变 API 结果。即使 Mod 没有进入 Entry，Doctor 的元数据警告仍有帮助。这两种警告都不表示所有旧 target Mod 不兼容。

仓库 PN-033 证据保留了一份 0.5.5 Lamp 使用者，以及通过上述公共流程构建的同 ID、0.6.4“能力不可用”迁移包。编译结果、准确二进制和后续 Mono 结果分别记录，构建成功本身不代表 Runtime 验收。
