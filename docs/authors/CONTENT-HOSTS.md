# 可选 Content Host

内容包是目录，无需 SDK、编译或生成 marker。官方 Content 仍由原生系统读取。需要 DTMAPI Host 的内容放 PackData，manifest 给出 ContentPackFor：

```json
{"ManifestVersion":2,"UniqueID":"Author.BadgesPack","Type":"ContentPack","MinimumDTMApiVersion":"0.7.5","ContentPackFor":{"HostId":"Author.BadgesHost","Schema":"author.badges/1","ContractVersionRange":{"minimumInclusive":"1.0.0","maximumExclusive":"2.0.0"}}}
```

绑定隐含必需 Host 依赖。Core 管来源、依赖排序和 Host/Pack 两个 Owner，Host 管实际 schema、准备与效果。未被代码使用的 DLL 文件不因出现在内容目录而触发整包拒载。

第三方 Host 是普通 CodeMod，提供声明的 schema 和激活处理器。构建 Host、编译内容、压缩目录、部署和诊断彼此独立，不要求 SDK catalog 或 proof hash。

Runtime 内置的 Animals、Monsters、Audio 使用 `PackData/content.json` 和内部贡献宿主，不填写上述绑定。PackData 在包首次加载时读取。标题页勾选新包并关闭官方页面后当场激活；取消勾选和已加载文件的修改在重启后应用。它们的注册自动持有需求，查询 API 不创建后端。每个注册独立撤回，Owner 退出撤回其全部贡献；清理成功后可在同进程重新注册，旧句柄不能操作新注册。具体入口见[畜牧](ANIMAL-CONTENT.md)、[怪物](MONSTERS.md)和[音频](AUDIO-CONTENT.md)。

原生 Content 与 Host 的 PackData 可以共存，分别由各自加载者接纳和报告。省略 MinimumDTMApiVersion 不构成 reader 下限拒载；有显式下限时才比较。实际文件读取保留路径边界，未使用的文件不触发整包扫描。

通用制作源整理保持 Content 和 PackData 原路径，不根据 ContentPackFor 转换目录。私有制作源到 PackData 的转换由产品自己的组装步骤完成；纯目录整理不需要 .NET SDK。成品包直接 pack/deploy，不再次搬运文件。
