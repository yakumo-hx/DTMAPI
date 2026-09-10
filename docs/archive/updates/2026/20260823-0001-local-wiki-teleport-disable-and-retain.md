# 20260823-0001：本地 Wiki 传送辅助停用与保留

## Metadata

- Update ID: `20260823-0001`
- Date: `2026-08-23`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: 用户确认 Wiki 图片补充已经提交，并要求移除、禁用但保留刚才的本地传送小 Mod；后续可另行整理为只定位非剧情隐藏房间伊甸果的简单传送附属 Mod。

## Scope

- 从 Doloc Town 官方本地 `MODS` 发现根移出 `Codex_LocalWikiTeleport`，使其不再作为本地官方 Mod 被发现或加载。
- 不删除已部署包；将整个三文件包移动到游戏目录的禁用备份区，并在移动前后校验逐文件 SHA-256。
- 保留工作区内两个实验实现：早期外部 BepInEx 插件原型和当前 DTMAPI CodeMod 原型及其构建输出。
- 不修改 DTMAPI Runtime、产品目录、准入注册表、公开 API、Hook、存档或游戏文件；不在本任务内修复或继续测试传送行为。

## Changed Files

- `docs/reviews/manual-qa/2026/20260823-0001-local-wiki-teleport-hotkeys-no-dispatch.md`
- `docs/updates/2026/20260823-0001-local-wiki-teleport-disable-and-retain.md`
- `docs/updates/INDEX-2026-08.md`

## Local Runtime Change

- Active source removed from:
  `C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/MODS/Codex_LocalWikiTeleport`
- Retained package moved to:
  `D:/Steam/steamapps/common/Doloc Town/DTMAPI/backups/disabled-localmods-20260823-wiki-teleport/Codex_LocalWikiTeleport`
- Preserved prototypes remain under ignored local workspace paths:
  `temp/local-wiki-teleport/` and `temp/local-wiki-teleport-dtmapi/`.

## Validation

- Acquired the shared DTMAPI runtime lock before changing the official local `MODS` tree and released it immediately afterward.
- Confirmed `DolocTown.exe` was not running before the move.
- Confirmed the active source directory is absent and the retained backup directory exists after the move.
- Verified all three package files by relative path and SHA-256 before and after the move; comparison returned zero differences.
- Confirmed no `LocalWikiTeleport` deployment was present under `BepInEx/plugins` before the move.
- No game process was launched. Runtime load/non-load smoke was not run because this task only removes the package from the authoritative discovery root; a future gameplay use requires a separate implementation and acceptance task.

## Evidence

- Retained package file count: `3`.
- `info.json`: SHA-256 `8D2D7C134A88A4E1E576E5995D069E70666024104B51B38EAEE7C3D622F51CA2`.
- `Content/DTMAPI/Codex.LocalWikiTeleport.dll`: SHA-256 `CB99237A484613074A6DADBEA59A824F64630C9410B07E2388F4A3623BDE80D1`.
- `Content/DTMAPI/manifest.json`: SHA-256 `DD0F7D210A3C18E40FFABF24984C5D0F6CFE85A4500F51B2E36D957F99665F22`.
- Final read-only state check: active official-local directory absent, retained backup present, zero BepInEx plugin hits, zero `DolocTown.exe` processes, and runtime lock free.

## Related Records

- [本地 Wiki 传送快捷键无动作排障审查](../../reviews/manual-qa/2026/20260823-0001-local-wiki-teleport-hotkeys-no-dispatch.md)
- [HookProbe left in normal play blocks hotkeys](../../../debug/issues/ISSUE-002-hookprobe-blocks-hotkeys.md)

## Rollback Notes

- 不要从备份目录自动恢复或启用该包。只有用户明确要求重新启用时，才在持有 runtime lock、游戏进程已退出且重新核对包身份后，把精确的 `Codex_LocalWikiTeleport` 目录移回官方本地 `MODS` 根。
- 源码原型和禁用备份均保留，因此本次停用可恢复；没有删除玩家数据或修改存档。

## Follow-Up

- 当前 `0.2.0` 原型不视为已修复、已验收、已准入或可发布产品。
- 若后续实现简单传送附属 Mod，范围应重新限定为已确认不触发剧情、仅用于到达隐藏房间并定位伊甸果的位置；不得自动领取物品、推进任务或调用原生保存。
- 后续实现必须单独裁决 Strict/Advanced/ProductNative 归属、输入入口、原生传送责任函数、离开/返回与过渡清理，并通过新的玩家验收；本 Update 不预授权这些工作。
