# DTMAPI

> **Runtime 0.7.0 已发布，包含 UTF-8 BOM 兼容修复。**
> 本分支汇总 0.7.0 的源码、标准 MSBuild SDK 和相关验收记录。Windows 与多平台玩家包已在 Steam 创意工坊更新；Author SDK D7 已完成 Windows 范围验收，尚未作为 GitHub Release 附件发布。准确发布记录见 [0.7.0 补丁发布确认](docs/updates/2026/20260911-0011-runtime-y-hotfix-publication.md)，SDK 交付范围见 [候选说明](docs/planning/platform-next/release-candidate.md)。

DTMAPI is a Doloc Town Modding API. BepInEx starts DTMAPI Core, which provides managed Mod loading, author services and diagnostics. Strict CodeMods use the public contracts according to their individual status and stability; a public assembly does not make every member stable. The current 0.7.0 candidate also provides a self-service Advanced path for any valid author ID through explicit local native references. Older receipt-based Advanced packages retain their registry and compatibility rules. Native ownership and supported Mod identities are defined in [PROJECT.md](PROJECT.md); current publication facts remain in the Product Catalog.

Players use the appropriate Runtime installation package. Mod authors use the separate [Author SDK](author-sdk/README.md), distributed as a GitHub Release attachment rather than included in the player installer. See [SDK distribution and release](docs/workflows/author-sdk-release.md) for the packaging boundary; candidate availability does not mean a release has been published.

## Repository Layout

- `src/`: DTMAPI runtime, public abstractions, bootstrap, GameBridge, and config menu projects.
- `products/first-party/`: current first-party product sources; the [Product Catalog](tools/release/dtmapi-product-catalog.json) owns each product's type and build route.
- `author-sdk/examples/` and `author-sdk/samples/`: author examples and API-demand samples.
- `tests/`: source tests, compatibility harnesses and fixtures; `tests/mod-fixtures/qa/` contains game QA Mods.
- `tools/scripts/`: build, test, install, smoke, hook-probe, and evidence collection scripts.
- `tools/release/runtime-workshop/`: four thin player BAT source entries plus their shared CMD dispatcher; the current boundary is defined in [Runtime Workshop Installer Boundary](docs/architecture/runtime-workshop-installer-boundary.md).
- `docs/`: [current documentation](docs/README.md), on-demand [research knowledge](docs/knowledge/README.md), active records and a separate [history archive](docs/archive/README.md).
- `references/`: public reference docs plus local-only ignored research folders.

## Build and validation

```powershell
tools/scripts/test.ps1 -Configuration Release
```

The command above is the complete Release validation boundary and includes its own build. Do not precede it with another full build/test cycle. For compile-only work use `tools/scripts/build.ps1 -Configuration Release -SkipTests`; for an ordinary fix build/run the selected project using [focused validation](tools/scripts/README.md#choose-validation). Documentation-only changes use document checks.

Tracked scripts select the compatible SDK and .NET 8 host through `Get-DotNetExe` in `tools/scripts/common.ps1`; PATH SDK presence alone is insufficient. Complete test entrypoints reject ambient focus filters so a subset cannot be reported as a full PASS.

[DTMAPI.sln](DTMAPI.sln) owns the ordinary framework/example/test build graph used by `build.ps1`. Admitted Advanced products retain their Catalog/Author SDK build route. `tools/scripts/status.ps1` only reports existing local tools, paths and runtime state; it does not install a missing SDK.

## Local Game Paths

Do not hard-code a local Steam or game install path in repository files. Use one of these local-only mechanisms instead:

- `DTMAPI_GAME_DIR`
- `local.settings.json`
- script path resolution based on the resolved game directory

`local.settings.json` is intentionally ignored by Git.

## Source Boundaries

DTMAPI is a clean rebuild. Doloc Town reverse data, official Workshop docs, third-party mods, and SMAPI are reference material only. Do not publish official game DLLs, copied decompiled source, private debug evidence, or third-party mod binaries as DTMAPI source.
