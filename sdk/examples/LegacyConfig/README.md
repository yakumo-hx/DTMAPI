# 同一 ID 的旧配置迁移

本例取自 ActionSpeed 的实际字段改名，只演示总开关、工具和装水两类设置。完整 AS 还处理吃喝、交互、采集、种植、自动装水和旧 Profile 的时间语义；这些产品规则不在本例中重建。

```powershell
dtmapi-author new codemod WORK --id Samples.LegacyConfig --name "Legacy config example" --author Samples --api-target 0.7.0
Copy-Item SDK_ROOT/examples/LegacyConfig/ModEntry.cs WORK/src/ModEntry.cs
dtmapi-author restore WORK --json
dtmapi-author pack WORK --json
```

通过正常作者安装／会话流程加载后，新安装默认三个开关均为 true、倍率为 3。关闭游戏后，可以把本目录 `legacy.json` 作为**该示例 ID** 的旧配置放到 Runtime 的 `DTMAPI/config/Samples.LegacyConfig.json`，再启动；输出应保留 false／2.5／3.2，并写入 SchemaVersion 1。再次启动不会重写已迁移文件。不要用本例的子集 DTO 覆盖真实 ActionSpeed 配置。

| 旧字段 | 新字段 | 冲突时 |
| --- | --- | --- |
| ToolSpeedEnabled | ToolEnabled | 显式新字段优先，false 不能被默认值覆盖 |
| BottleFillSpeedEnabled | FillEnabled | 同上 |
| BottleFillMultiplier | FillMultiplier | 同上，保留 AS 的 1—4 限制 |

有文件时先严格读取和识别 schema；未知 schema、损坏 JSON或访问错误直接传播，不写默认。只有确实不存在文件才调用旧 Config helper 创建默认值。成功转换通过平台原子 writer 提交；写入失败时原文件保留，下次启动仍可重试。

## 为什么保留原配置路径

这次需求是同 ID 的有限字段改名。现有读写接口和一个 schema 字段已能完成，避免再维护两份配置及导入状态。不要把旧 `RegisterMigration(Action<T>)` 当作一次性迁移；它仍遵守每次读取的旧契约。

`IVersionedConfigHelper` 适合后续需要显式多步迁移、校验结果和版本化存储的工程，但使用独立文件。若选用它，只有 `Read` 返回 `Missing` 时才显式读取旧 DTO、校验并 `Write`；其他失败和 `UnsupportedSchema` 不授权默认覆盖。迁移成功后保留旧文件，重复启动只读版本化结果。两条路径都不迁移玩法存档，也不要求新增公共 API。
