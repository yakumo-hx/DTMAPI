# 20260608-0028 Public Source Hygiene

## Metadata

- Update ID: 20260608-0028
- Date: 2026-06-08
- Status: verified
- Source: Active goal to fix public source hygiene without changing GameBridge, Hook, CameraView, or Smoke behavior.
- Owner: Codex

## Summary

- Removed the public build dependency on local-only `references/doloc-town/own-mod-sources` by adding an in-tree placeholder `FishBreedingLookup.g.cs`.
- Re-added `FishBreedingAssistantMod` to `DTMAPI.sln` and `tools/scripts/build.ps1` now that it no longer requires private generated source.
- Removed hard-coded `D:\Steam` / `D:\steam` Workshop candidates from `tools/scripts/install-to-game.ps1`; the script still derives Workshop paths from the resolved game directory.
- Added root `README.md`, MIT `LICENSE`, and `NOTICE.md` for the public source boundary.
- Replaced path-heavy reference copy notes with public-friendly copy categories and exclusions.
- Sanitized `AGENTS.md` so the clean-room rule does not expose the private predecessor workspace path.
- Sanitized README-style research notes so they no longer expose local workstation paths.

## Changed Files

- `README.md`
- `AGENTS.md`
- `LICENSE`
- `NOTICE.md`
- `DTMAPI.sln`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `testmods/README.md`
- `testmods/FishBreedingAssistantMod/FishBreedingAssistantMod.csproj`
- `testmods/FishBreedingAssistantMod/Generated/FishBreedingLookup.g.cs`
- `testmods/FishBreedingAssistantMod/README.md`
- `references/README.md`
- `references/COPY-MANIFEST.md`
- `references/doloc-town/research-notes/README-DolocTown-Modding-API.md`
- `references/doloc-town/research-notes/README-DolocTown-Workshop-Functional-Mods.md`
- `docs/updates/2026/20260608-0028-public-source-hygiene.md`
- `docs/updates/INDEX.md`

## Boundary

- No GameBridge, CameraView, Hooking, or Smoke behavior was changed for this goal.
- The public `FishBreedingLookup.g.cs` is a compile-safe placeholder and intentionally contains no private fish lookup rows.
- The placeholder preserves public buildability only; fish-specific FishBreeding tooltip coverage still requires a public-friendly generated data source or runtime data query.

## Validation

- Passed: no `testmods`, `tools`, or `DTMAPI.sln` references to `references/doloc-town/own-mod-sources` or `own-mod-sources/FishBreedingAssistantMod`.
- Passed: `tools/scripts/install-to-game.ps1` contains no `D:\Steam` or `D:\steam` hard-coded candidates.
- Passed: README-style files outside update/debug records contain no `E:\Python_project` local paths.
- Passed: `AGENTS.md` contains no private predecessor workspace absolute path.
- Passed: `references/README.md`, `references/COPY-MANIFEST.md`, root `README.md`, `LICENSE`, and `NOTICE.md` contain no `E:\Python_project`, `D:\Steam`, or `D:\steam` paths.
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `git diff --check` for the touched hygiene files; only CRLF conversion warnings were reported by Git.
- Game smoke was not run because this goal did not touch runtime, hooks, CameraView, or Smoke behavior.

## Evidence

- `testmods/FishBreedingAssistantMod/Generated/FishBreedingLookup.g.cs` is now the only public FishBreeding lookup source used by the test mod project.
- `FishBreedingAssistantMod` builds in Release through both the build script and test script.
- `tools/scripts/install-to-game.ps1` now relies on configured or resolved game paths instead of fixed local Steam drive candidates.

## Related Records

- Public package creation: `docs/updates/2026/20260608-0021-open-source-audit-package.md`
- Prior FishBreeding public-build exclusion: `docs/updates/2026/20260608-0022-public-build-fishbreeding-exclusion.md`
- Latest package refresh before this hygiene fix: `docs/updates/2026/20260608-0027-open-source-audit-package-refresh.md`

## Rollback Notes

- If a private-only FishBreeding generated lookup must be restored locally, keep that dependency out of the public package or gate it behind a private build path.
- If the placeholder is rejected, remove `FishBreedingAssistantMod` from `DTMAPI.sln` and `tools/scripts/build.ps1` again until a public data source exists.
- If Workshop path probing needs more defaults, add them through local settings or environment configuration rather than hard-coded workstation paths.

## Follow-Up

- Completed by `docs/updates/2026/20260608-0029-open-source-audit-package-hygiene-refresh.md`: the external open-source share package and independent audit package were refreshed after this source hygiene fix.
