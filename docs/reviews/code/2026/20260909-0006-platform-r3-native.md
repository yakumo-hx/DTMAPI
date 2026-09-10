# R3.native — 任意作者 Advanced 原生契约

- Date: `2026-09-09`
- Status: `accepted with explicit limits`
- Owner: [PN-010 Update](../../../updates/2026/20260909-0013-platform-open-advanced.md)
- Decision: **GO PN-022**。接收 P05 的内部 0.6.4 NativeContractVersion=1；不是完整 M3 或公开发行验收。

## 核验

仓库外 Cedar、Pine、Birch 使用普通 SDK 的 new/build/pack/install-local/withdraw，不增第一方 Catalog ID、不借旧 receipt 白名单。直接游戏引用的 CS1705 由通用 metadata surface 解决；编译器仅见无原方法体/资源/字段数据的本地引用。生成记录覆盖实际辅助解析输入，许可证随工具分发；运行时仍只有原五个强制 DLL，没有 Cecil。

包绑定 manifest、entry、依赖清单、原生来源与 marker；Core/Doctor 共享严格格式校验。Native 显式选择与三种旧 reader 分开，未知/错位/缺失字段不回退。实例成员、重载、静态性、返回/参数类型按完整签名匹配；Runtime 复查实际文件与驻留 identity/MVID，拒绝加载前漂移。泛型等未支持签名有明确诊断。作者动态可选成员由作者自行降级，必需声明不能靠异常回退逃过加载前验证。

Core 154 入口、Native 格式/实际 Runtime Entry 集成、依赖和 pack-build 回归通过。恶意 JSON 重绑外层 hash 的负例仍被内部契约拒绝；测试证明宿主静态构造器不会在 metadata 预检执行。阶段实机发现全包/legacy 摘要不一致，修正为既有 Advanced 全包摘要并补真实 Entry 回归；旧 Advanced 内存编译引用报告空路径也已修复，原旧快照公开 pack 与 Mono Entry 均通过。

[最终三次实机](../../../debug/evidence/GAME-SMOKE/20260909-platform-pn010-final-runtime/README.md) 证明新作者 Mono Entry、只读实例查询、准确 Harmony owner、故意部分 Entry 故障清理和独立作者命令/Hook 存活，旧 Advanced/Strict/legacy 同场运行。原 retained ABI 两 DLL 返回 73/writes=1。测试包撤回、五 DLL 恢复、30+5 文件保护通过。

## 边界

旧真实引用在当前游戏上仅获得所需签名兼容证据；没有第二个旧游戏进程或任意版本承诺。部分 Entry 故障运行的健康状态保持 Failed，不能用行为断言覆盖原始结果。普通 Mono 单 AppDomain 没有通用热卸载或任意 native 副作用恢复。作者自行承担未声明反射/动态原生行为；平台不替作者保证玩法安全。QA 自动退出不是官方 UI、实体输入或 Runtime 安装包验证。

0.6.4 最终 SDK 与其支持 target 摘要固定，A/B/C 内部草稿没有兼容回退；原已冻结 target/SDK 不改字节。继续 P06 多项目、资源、显式 restore，再执行 PN-023 完整组合，不删除验收项或提前宣称 M3 成立。
