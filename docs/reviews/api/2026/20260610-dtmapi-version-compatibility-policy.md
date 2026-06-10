# DTMAPI Version Compatibility Policy Review - 2026-06-10

## Scope

Public symbol/domain: manifest `MinimumDTMApiVersion`, dependency `MinimumVersion`, runtime `DtmApiRuntime.ApiVersion`, bootstrap `DtmApiRuntime.BinaryVersion`, and packaging/install normalization for official-local DTMAPI mods.

Current matrix status: framework compatibility policy for Stable framework contracts; no new public API surface.

Recommended status after this review: keep the current numeric compatibility policy. Do not add `AllowPrerelease` or strict SemVer prerelease gating in this branch.

This review is docs-only. It does not change runtime, loader behavior, public API members, package layout, game files, Workshop files, official DLLs, or reverse/decompiled reference material.

## Files Read

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `Directory.Build.props`
- `tools/scripts/install-to-game.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260610-0026-api-version-050-alpha-helper-notes.md`

## Current Runtime Policy

`DtmApiRuntime.ApiVersion` is the runtime API compatibility label and is currently:

```text
0.5.0-alpha
```

`DtmApiRuntime.BinaryVersion`, assembly version, and file version stay numeric:

```text
0.5.0.0
```

The loader checks `MinimumDTMApiVersion` and dependency `MinimumVersion` through `IsVersionRequirementSatisfied(...)`. That method calls `TryParseVersion(...)`, which trims any `-` or `+` suffix before using `System.Version.TryParse`. In practice:

```text
0.5.0-alpha -> 0.5.0
0.5.0       -> 0.5.0
0.5.1-alpha -> 0.5.1
```

Then `CompareVersions(...)` compares major, minor, build, and revision numerically. This is intentionally numeric compatibility, not strict SemVer prerelease ordering.

## Policy Decision

Keep `MinimumDTMApiVersion` numeric-only compatibility for the current Refactor/dev baseline.

`0.5.0-alpha` is a dev baseline label for DTMAPI's current API surface. It should not block a mod that declares `MinimumDTMApiVersion=0.5.0`, and it should not require an `AllowPrerelease` manifest property. The controlled API surface is still governed by the public API matrix status rows (`Stable`, `StableCandidate`, `Experimental`, `Diagnostic`, `Proposed`, `Failed`) instead of SemVer prerelease rules alone.

This means:

| Manifest requirement | Runtime `0.5.0-alpha` result | Reason |
| --- | --- | --- |
| empty / omitted | load | no minimum requirement |
| `0.3.1` | load | numeric runtime `0.5.0` is newer |
| `0.4.2` | load | numeric runtime `0.5.0` is newer |
| `0.5.0` | load | same numeric surface |
| `0.5.0-alpha` | load | same numeric surface after suffix trimming |
| `0.5.1-alpha` | block | numeric requirement is newer |
| `99.0.0` | block | numeric requirement is newer |

## BepInEx Metadata Constraint

`[BepInPlugin]` version metadata must stay numeric. The retained failed smoke `GAME-SMOKE/20260610-120641` showed BepInEx rejected `0.5.0-alpha` in plugin metadata with:

```text
Skipping type [DTMAPI.BepInExBootstrap.BootstrapPlugin] because its version is invalid.
```

Therefore the bootstrap attribute uses:

```csharp
[BepInPlugin("dev.dtmapi.bootstrap", "DTMAPI Bootstrap", DtmApiRuntime.BinaryVersion)]
```

and `DtmApiRuntime.BinaryVersion` remains `0.5.0.0` while runtime UI/log/API metadata may report `0.5.0-alpha`.

## Why Not Strict SemVer Now

- Existing manifests and install normalization already use simple minimum-version semantics.
- The current loader has no `AllowPrerelease` public manifest surface.
- The public API matrix is the actual stability gate for child helper surfaces and GameBridge APIs; `0.5.0-alpha` does not make Experimental or Diagnostic surfaces Stable.
- Strict prerelease rules would be a breaking loader-policy change and should be introduced only with a dedicated manifest migration design and compatibility notes.

## Confirmed Evidence

- Unit coverage in `tests/DTMAPI.UnitTests/Program.cs` verifies `MinimumDTMApiVersion=0.5.0-alpha`, legacy `0.4.2`, and legacy `0.3.1` load on the current runtime, while `0.5.1-alpha` and `99.0.0` are blocked with `api-too-new`.
- Passed Camera smoke `GAME-SMOKE/20260610-121420` verifies installed packages load under the alpha dependency-normalized runtime and BepInEx logs `Loading [DTMAPI Bootstrap 0.5.0.0]`.
- Passed ActionSpeed smoke `GAME-SMOKE/20260610-121631` verifies the same runtime/package path for ActionSpeed.
- Failed attempt `GAME-SMOKE/20260610-120641` remains the negative evidence for why BepInEx plugin metadata cannot use `0.5.0-alpha`.

## Future Review Gates

Before switching to strict SemVer prerelease behavior, add a separate API review that covers:

- Whether manifests need an `AllowPrerelease` or channel field.
- Compatibility behavior for already-shipped `MinimumDTMApiVersion` values.
- Installer/package normalization changes.
- Diagnostics wording and `IDtmModStatusInfo.StatusCode` mapping.
- Smoke evidence for ordinary installed mods and legacy package compatibility.

