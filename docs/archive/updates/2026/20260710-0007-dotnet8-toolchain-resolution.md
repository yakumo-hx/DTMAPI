# 20260710-0007 .NET 8 Toolchain Resolution

## Status

- `source-and-unit-verified / tooling-only / no-game-runtime-change`
- No Doloc Town process was launched and no game-runtime validation is claimed by this update.

## Source Request

- Goal: `docs/goals/2026/20260710-0007-dotnet8-toolchain-resolution.md`
- User direction: determine why Codex repeatedly reported that only .NET 6/9 were installed despite a repository-local .NET 8, choose the correct version boundary for Doloc Town, fix it consistently, and run a small test.

## Known Facts and Rejected Causes

- Doloc Town is Unity `2021.3.16f1` Mono; DTMAPI game-loaded projects remain `netstandard2.0`. The `net8.0` target belongs only to the console unit runner.
- `.tools\dotnet` is ignored local state but is readable and executable by Codex. Filesystem permission was not the cause.
- The previous resolver returned PATH `dotnet` whenever it exposed any SDK. After the system .NET 9 SDK was installed, that early return prevented discovery of the existing local .NET 8 and prevented the local channel-8 provisioner from running in fresh worktrees.
- `DOTNET_ROLL_FORWARD=Major` was a successful historical workaround, but it did not repair host selection and is no longer the normal validation path.

## Changes

- Added `Test-DtmApiDotNet8Toolchain`, which requires both an SDK and `Microsoft.NETCore.App 8.x`.
- Changed `Get-DotNetExe` to prefer a compatible repository-local `.tools\dotnet\dotnet.exe` before considering PATH `dotnet`.
- PATH `dotnet` is now accepted only when it also satisfies the current .NET 8 runtime requirement; a system SDK alone is insufficient.
- Kept the existing local channel-8 installer fallback and added post-install validation before returning the executable.
- Added an `AGENTS.md` toolchain rule separating Unity Mono `netstandard2.0` game assemblies from the .NET 8 build/test host and rejecting routine major-version roll-forward.

## Changed Files

- `tools/scripts/common.ps1`
- `AGENTS.md`
- `docs/goals/2026/20260710-0007-dotnet8-toolchain-resolution.md`
- `docs/goals/2026/20260710-0007-dotnet8-toolchain-resolution.goal.txt`
- `docs/updates/2026/20260710-0007-dotnet8-toolchain-resolution.md`
- `docs/updates/INDEX.md`

## Validation

- PowerShell parser check for `tools/scripts/common.ps1`: passed.
- Resolver assertion with `DOTNET_ROLL_FORWARD` absent: passed.
  - repository-local host compatible: `True`;
  - PATH system host compatible with the required .NET 8 runtime: `False`;
  - selected host: `E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe`.
- `tools/scripts/test.ps1 -Configuration Release` with `DOTNET_ROLL_FORWARD` absent: passed in 62.7 seconds.
  - all builds completed with zero warnings and zero errors;
  - `DTMAPI.UnitTests: OK`.
- An initial invocation used a five-second command timeout and was terminated before validation completed; the unchanged command was rerun with a normal build timeout and passed. This was a test-driver timeout, not a build or unit-test failure.
- No game install, launch, smoke, save load, or runtime lock operation was needed or performed.

## Evidence and Related Records

- Current unit-runner target: `tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj` (`net8.0`).
- Game-loaded runtime targets: `src/**` and DTMAPI mods remain `netstandard2.0`.
- Historical workaround record: `docs/updates/2026/20260707-0002-phase821-yconsole-vs-zoom-input-owner-isolation.md`; retained as history and superseded for current validation practice by this update.
- No debug, hook-map, smoke-matrix, or public-API status changed.

## Rollback

- Revert the resolver helper/order and the `AGENTS.md` toolchain rule together.
- Rollback would restore system-SDK-first behavior and may reproduce failure to execute `net8.0` tests on machines with only system .NET 6/9 runtimes.

## Follow-up

- Keep game-loaded assemblies on `netstandard2.0`.
- Treat any future move from the .NET 8 test host to .NET 10 LTS as a separate, explicit toolchain migration with its own goal and validation; do not silently retarget tests during unrelated runtime work.

