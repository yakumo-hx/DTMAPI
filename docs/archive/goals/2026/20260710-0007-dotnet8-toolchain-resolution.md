# 20260710-0007 .NET 8 Toolchain Resolution

## Status

- Implementation: source and unit verified.
- Fixed target version: `0.5.2-alpha`; no version bump.
- Runtime scope: build/test tooling only; no Doloc Town launch or game-runtime change.

## Objective

Make every tracked DTMAPI build/test entry point resolve a host that can build and execute the current `net8.0` console unit runner, prefer the repository-local .NET 8 toolchain, and prevent PATH .NET 9 plus `DOTNET_ROLL_FORWARD=Major` from hiding a missing .NET 8 runtime.

## Required Changes

1. Centralize the fix in `Get-DotNetExe` so `build.ps1`, `test.ps1`, `status.ps1`, install, package, and smoke build paths inherit it.
2. Accept a host only when it exposes at least one SDK and `Microsoft.NETCore.App 8.x`.
3. Prefer `.tools\dotnet\dotnet.exe`; use PATH `dotnet` only when it also satisfies the .NET 8 runtime requirement; otherwise provision the existing local channel-8 toolchain.
4. Validate the local installation before returning it.
5. Record the Unity Mono boundary in `AGENTS.md`: game-loaded projects remain `netstandard2.0`, while .NET 8 is only the current build/test host.

## Validation

- PowerShell parser check for `tools/scripts/common.ps1`.
- Resolver assertion: repository-local host accepted, PATH host rejected on this machine, and `Get-DotNetExe` returns the repository-local executable.
- Run `tools/scripts/test.ps1 -Configuration Release` with `DOTNET_ROLL_FORWARD` absent.
- Do not install to or launch Doloc Town; this change does not touch game runtime behavior.

## Acceptance

- tracked source validation no longer reports that only .NET 6/9 are available while a usable repository-local .NET 8 exists;
- the system .NET 9 SDK is not selected solely because it is an SDK;
- the existing `net8.0` unit runner completes without major-version roll-forward;
- game-loaded DTMAPI assemblies remain `netstandard2.0`.

## Implemented Result

- Added `Test-DtmApiDotNet8Toolchain` and made the common resolver local-first and runtime-aware.
- Added post-install validation for the local channel-8 SDK/runtime.
- Added the durable toolchain/runtime boundary to `AGENTS.md`.
- Resolver assertions passed and the full Release source-test script completed with zero warnings/errors and `DTMAPI.UnitTests: OK` without `DOTNET_ROLL_FORWARD`.

