# 20260911-0010: Y 控制台 1.1.3 保留物品 ID 大小写

## Metadata

- Update ID: `20260911-0010`
- Date: `2026-09-11`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 用户在[机械大剑排查](../../reviews/code/2026/20260911-0005-player-mechanical-sword-item-give.md)后授权尽量简单稳定地修复、做最小测试、升为 1.1.3 并放入上传目录；若需大范围改动则改由第三方作者处理 ID。

## Summary

只调整 Y 产品的物品发放：使用保留原始 ID 的原生生成入口，检查生成结果后交给对象版背包接口。保持原有数量分批、容量检查和不发溢出邮件的行为；不修改 Runtime、全局物品表、第三方 ID 或原生方法。版本为 1.1.3，仍要求 Runtime 0.6.1，保持 netstandard2.0。

## Changed Files

- Y 产品领取方法与版本元数据。
- 对应物品领取回归、QA 指定物品入口，以及必要的包与上传目录记录。
- 本 Update 和月度行；原 Review 仅增加实施入口。

## Validation

- PASS：`test-unit.ps1 -Configuration Release -Focus debugconsole-inventory`。同一回归在改前失败、改后通过；含大写与同名小写键、普通小写领取 10 个、分批、容量不足/部分成功、生成空值及原始异常。
- PASS：原引用基线 24456188 与玩家/本机 25163613 都有相同的公开 `GenerateItem(string,int)`、`CanPlaceItem(Item)`、`TryPlaceInBackpack(Item,bool)` 签名，生成入口不调用转小写。只读取方法元数据/IL，没有复制原生实现。
- PASS：只构建本次 QA 项目，零警告/错误；QA 可通过 `DTMAPI_QA_DEBUG_INVENTORY_ITEM_ID` 指定必须存在的精确大小写 Mod 物品，缺失时失败，不再以可选物品分支跳过本次验证。
- PASS：Catalog 驱动的 SDK validate / offline restore / pack，保留原 Advanced policy。manifest、info、包标记均为 1.1.3，程序集为 1.1.3.0 / netstandard2.0；无原生/Runtime 依赖 DLL 混入。Windows 与其他游戏平台继续共用这一份产品代码包，没有另建平台分支；本次原生运行验证在 Windows 完成。
- 首次运行 `GAME-SMOKE/20260911-224013` 失败：CoreOnly 隔离配置遗漏 MoreSaves，原生只提供 6 槽，自动选择第 10 槽失败，未执行领取。通过 CloseMainWindow 退出；存档/侧文件无变化，保留失败证据。
- PASS：补充已有 MoreSaves 1.0.1 作为第 10 槽的测试前提，使用同一产品包运行 `GAME-SMOKE/20260911-224556`。官方木头 47→48；临时纯原生内容包的大写 ID `DTMAPI_QA_ItemCase` 0→1，`given=1`、`nativeOwner=ProductNative`。正常退出，QA/配置恢复、当前存档及备份/已提交侧文件比较均通过；不保存、不备份或写回玩家存档。
- PASS：取得 Runtime 锁后同步 `MODS/DTMAPI_YKeyConsole`，最终 27 文件 / 629035 字节。除原样保留的 `workshop.json` 外，路径/长度/hash 与 SDK ZIP 全部一致；测试物品移除，`mod_infos.json` 恢复，原 Runtime Core 未变，QA 激活文件移除，锁已释放。临时物品 `info.json` 被游戏补齐空本地化字段的原生规范化已保留现场，再清理该自建目录。
- 文档治理及本次增量检查结果保存在 `artifacts/y-console-113-item-id-case/doc-governance.log` 与 `source-diff-check.log`。

## Evidence

- 根因和玩家原始证据见上列 Review。
- 本次构建、测试和同步证据保留在 `artifacts/y-console-113-item-id-case/`，含红/绿测试、SDK 报告、最终 `upload-final.json`、同步前原包及元数据。
- 实机记录：[首次测试前提失败](../../debug/evidence/GAME-SMOKE/20260911-224013/result.json)、[最终通过](../../debug/evidence/GAME-SMOKE/20260911-224556/result.json)。日志证明来自纯原生内容表的实际大写物品，而不是只在 fake API 中模拟。

## Rollback Notes

仅还原本次 Y 源码和版本增量；上传目录同步前保留准确原包，按本次同步收据回退。保留工作区其他 SDK、地图和 Runtime 修改，不修改玩家物品 ID 或存档。

## Follow-Up

本次实施、必要验证及本地上传目录同步完成。用户随后发布了 1.1.3，公开状态及准确订阅交付已由 [0011](20260911-0011-runtime-y-hotfix-publication.md) 核实。未取得玩家原始机械大剑包，不把自建大写 ID 物品的通过记成该第三方包整包验收。
