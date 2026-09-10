# Goal: AutoFishing smoke 与架构边界隔离

Status: complete; source/unit boundary verified, no game runtime run.

Source review: `docs/reviews/api/2026/20260711-0005-autofishing-smoke-architecture-boundary-review.md`.

## Task 1 — split smoke ownership

- Make the normal AutoFishing smoke path primitive-only.
- Add `AutoFishingPrimitiveSmokeCase` and `LegacyFishingAutomationCompatibilitySmokeCase` as separately named cases.
- Primitive smoke must not reference `IFishingAutomationApi`, `LegacyFishingAutomationService`, or `FishingAutomationFeature.Service`.
- Share neutral telemetry/result DTOs, performance probe integration, and cleanup assertions.

## Task 2 — architecture gates

- Gate the AutoFishing product against Harmony, reflection, native fishing state types, and legacy API references.
- Gate DTMAPI Core against fishing product names.
- Gate internal primitive API visibility so ordinary mods cannot consume it.
- Gate primitive activation against legacy construction/access.
- Gate legacy construction and activation to the obsolete compatibility `SetEnabled(true)` route.

## Task 3 — input specification

- Document local `DtmKeybindList.JustPressed(helper.Input)` as the ordinary-mod default.
- Document AutoFishing's single owner-bound Gameplay toggle as the deliberate reliable-first-edge exception.
- Add a source test that preserves exactly one AutoFishing registration and keeps Zoom on local queries.

## Task 4 — source validation only

- Release build, complete unit tests, architecture scans, JSON/PowerShell parser checks where touched, and `git diff --check`.
- Do not acquire the runtime lock, install to the game, launch Doloc Town, run inactive memory profiles, run platform layers, fish, or soak.
- Update the smoke matrix, public API matrix, input contract, update index, Debug Index, and ISSUE-010.

## Deferred

- one approximately 10-minute or 10–20-fish active trend at formal-release readiness;
- 100/500-fish matrices;
- version/DeveloperOnly/manifest alignment and the legacy removal window.

## Completed evidence

- Normal smoke dispatch is primitive-only; the separately named compatibility case is opt-in and mutually exclusive in the smoke script.
- Product/Core/internal primitive/legacy activation/input lifetime source gates pass in the complete unit runner.
- Release builds completed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`; PowerShell parser and `git diff --check` pass.
- No runtime lock, install, Doloc Town process, memory baseline, platform baseline, fish, or soak was used.
