# 迁移到可选 SDK

本说明面向 `0.7.5-review` 候选源码，包含破坏性变更；源码评审不代表完整发行验收。新 `ManifestVersion=2` 包需要本次 Runtime reader，不能只更新 manifest 而继续使用旧 Runtime。当前工具入口见[作者指南](README.md)。

1. 使用普通 `Microsoft.NET.Sdk` 工程，目标 `netstandard2.0`。删除强制 Author props/targets、`NoStdLib`、SDK 编译器检查和 msbuild-prepare/check target。
2. API、Unity、Harmony、BepInEx、游戏原生程序集用普通 Reference，设置 `Private=false`。需要公开原生内部类型时，运行独立引用转换器，将结果置于可重建目录。
3. 删除 `dtmapi.author.json`。旧生成的 native/dependency/receipt 文件不再生成；留在旧包中也不影响准入。
4. 新 manifest 使用 `ManifestVersion=2`，展示信息与版本移到根 `info.json`；删除 `CodeModKind`、NativeContractVersion、DependencyContractVersion。Harmony Owner 可显式给出；新格式默认 `<UniqueID>.Native`。
5. 先普通编译，再整理 package 目录。`pack` 只消费已有目录，不再替作者编译；旧 `--build-output`、`--offline`、policy flags 不属于新 pack 接口。
6. 使用目录或 ZIP 部署。普通 deploy 覆盖本次产物，不自动备份；删除或改名的旧文件会留下，需要干净环境时明确使用 `--clean`。旧 --replace 仅为冗余覆盖确认。目标 workshop.json 保留，同名包内设置由新产物覆盖；包外配置不动。清空失败后的 staging 是待安装新包，不是旧安装备份；按错误提示保留 `--target` 原安装路径重试，避免旧 manifest 丢失后改变目录身份。旧树摘要、构建成功记录、Doctor 成功均不是部署前提。

支持范围内的旧包保留原格式读取：缺失、损坏或过期的生成证明不会拒载；真实源码版本、依赖缺失、实际引用身份冲突仍按运行事实报告。历史冻结包不重封，pre-0.6 支持不扩展。
