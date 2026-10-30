# DTMAPI

DTMAPI provides a Mod runtime and optional authoring tools for Doloc Town. Game assemblies target `netstandard2.0`; source tools and tests use the .NET 8 SDK pinned by this repository.

**This is the `0.7.5-review` source review branch.** It is not being merged into the mainline. The default `main` branch and [Releases](https://github.com/yakumo-hx/DTMAPI/releases) remain at 0.7.0. This branch contains implemented 0.7.5 candidate source, not a completed release acceptance or an installable release. See the [changelog](CHANGELOG.md) for changes and migration boundaries.

Start with the [Chinese guide](README.md), [author guide](author-sdk/README.md), [migration guide](author-sdk/MIGRATION.md), or [contribution guide](CONTRIBUTING.md). Ordinary projects can reference the API directly. The SDK is optional, does not issue Runtime admission proofs, and packages existing directories without rebuilding them.

## 0.7.5 review scope

The candidate includes loading based on actual dependencies, an optional SDK, built-in demand-driven animal, monster and audio modules, audio preparation tools, configuration and diagnostics improvements, installer/uninstaller tools, and independent Mod source.

This version removes legacy public APIs and diagnostic DTOs. `ManifestVersion=2` requires the matching Runtime. Update the Y console and current animal, monster and audio consumers together with the Runtime; retired standalone Hosts and replaced content formats have no compatibility layer. Follow the migration guide and validate the scenarios your Mod uses.

## Source support

Windows supports the declared `PublicWindows` source profile. Linux/WSL supports `Generic`, using native Linux .NET 8 and PowerShell 7. These are executable source tests; game installation, native input and saving require separate acceptance. Game DLLs, internal evidence and unlicensed optional media are excluded.

Focused tests and Windows game checks do not establish acceptance for every Mod feature. MES legacy equipment choice dialogs, some monster player checks and recovery after an animal pack is removed still have validation gaps. Proton, Wine and CrossOver game execution is not covered by these results.

```powershell
./tools/scripts/test-public-source.ps1 -Profile Generic -Configuration Release
```

Use `-List` to inspect the exact selection, or `-BuildOnly` to build it. Windows also supports `-Profile PublicWindows`. Do not build the entire solution as a substitute for these profiles. See [NOTICE](NOTICE.md) for the licensed BepInEx fixture and its complete corresponding source. Source code is licensed under [MIT](LICENSE).
