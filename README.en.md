# DTMAPI

DTMAPI provides a Mod runtime and optional authoring tools for Doloc Town. Game assemblies target `netstandard2.0`; source tools and tests use the .NET 8 SDK pinned by this repository.

The current development version is `0.7.5`. This directory contains the public source; installable packages are available from [Releases](https://github.com/yakumo-hx/DTMAPI/releases). See the [changelog](CHANGELOG.md) for changes and migration guidance.

Start with the [Chinese guide](README.md), [author guide](docs/authors/README.md), [migration guide](docs/authors/MIGRATION.md), or [contribution guide](CONTRIBUTING.md). Ordinary projects can reference the API directly. The SDK is optional, does not issue Runtime admission proofs, and packages existing directories without rebuilding them.

## 0.7.5 review scope

The candidate includes loading based on actual dependencies, an optional SDK, built-in demand-driven animal, monster and audio modules, audio preparation tools, configuration and diagnostics improvements, installer/uninstaller tools, and independent Mod source.

This version removes legacy public APIs and diagnostic DTOs. `ManifestVersion=2` requires the matching Runtime. Update the Y console and current animal, monster and audio consumers together with the Runtime; retired standalone Hosts and replaced content formats have no compatibility layer. Follow the migration guide and validate the scenarios your Mod uses.

## Build and test

Install the .NET SDK pinned by `global.json` and run from the source root:

```powershell
./build/build.ps1 -Configuration Release -SkipTests
./build/test-unit.ps1 -Configuration Release
./build/test-architecture.ps1 -Configuration Release
```

Use `test-unit.ps1 -List` to list existing default suites, or `-Focus <name>` to run one. Built-in game modules and first-party Mods need references generated from a local game installation. See the [build guide](build/README.md), [NOTICE](NOTICE.md), and [MIT license](LICENSE).
