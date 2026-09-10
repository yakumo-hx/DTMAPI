# 未发布 M2 0.7.0 内部快照

这是 PN-036 前已验收的原始内部输入，不是公开 0.7.0 的接口或发行承诺。当前内部候选为 [0.6.2](../../compatibility/0.6.2/compatibility.contract.json)，公开 0.7.0 保持 planned，须等完整 M3。

`compatibility/0.7.0/` 保持原目录相对布局与全部原字节，`original-inventory.json` 保存搬移前清单。原 contract SHA-256 为 `a9b301778d1909317923ad0d47d3f5ac80846094a3266c3e4f10d341baedb616`；原 Abstractions 为 `69a28c5c8da3a9b19afdff9d7a12c728b44161c31ffc269e790b1b270b16cfd4`。source-build.json 和 source/ 不因新版本安排重写。

原 SDK 0.2.0 ZIP 位于仓库 artifacts/pn007/frozen-sdk-final，SHA-256 `3eeeeda1908735c7b975c9a472e207047c2f1781b131fc98febb58496b8793d1`。原离线 SDK 自带两个原 payload，可独立重放原作者构建；当前 SDK 不再提供该历史 target。原 PN-021 SDK0.3/API0.8 候选保留在 artifacts/pn021/candidate-b，其 recipe、contract、运行 DLL、ZIP 和实机证据不变；0.8 不由此成为正式清退版本。

本目录另外保存原 target-catalog.json 及两个隔离构建脚本。源码重建需要以原 source-build.json 指定的 8.0.421、Release、PathMap 和完整输入布局执行；不能把新源码混入旧 target 来获得相同名字的另一份 DLL。当前 SDK 的包构建只枚举当前 available 目标，本目录不作为兼容载荷进入新 ZIP。

旧内部作者工程迁移：保留原工程/ZIP，复制一份工作工程；将 dtmapi.author.json 的 targetDtmApiVersion 和 manifest.json 的 MinimumDTMApiVersion 明确设为 0.6.2，使用新的 SDK 0.6.2 build/pack。自定义代码中的 GetRequiredService 最低版本也按实际新内部 target 调整。给新包新的 Mod 版本或新输出目录，禁止改名旧 ZIP、手改 marker 或只换 DLL。0.5.5 工程及旧 SDK/marker reader 无需迁移。

相关记录：[PN-007](../../../docs/updates/2026/20260909-0006-platform-sdk-candidate.md)、[PN-021](../../../docs/updates/2026/20260909-0009-platform-public-reflection.md)、[PN-036](../../../docs/updates/2026/20260909-0011-platform-internal-version-realignment.md)。历史 PASS 只绑定原候选，新组合须有新实机抽验。
