# 20260704-0001 Content / Manifest Registry Authoritative Index

## Summary

Implement DTMAPI phase 8A: an internal-only Content / Manifest Registry authoritative index for diagnostics and ownership attribution. The registry records mod/content-pack identity, version, source, enablement, dependency compatibility, minimum DTMAPI version, capabilities, owner, and registry diffs.

This phase must not enable registry takeover. `RegistryTakesOver=false` remains the required runtime posture. The new registry may become the authoritative diagnostic index, but it must not change the actual content application result, CustomAnimals behavior, AnimalVoice behavior, AutoFishing behavior, manifest/JSON semantics, content-pack paths, Hook targets, or public APIs.

## Scope

- Add internal manifest/content registry snapshot rows for the existing `ModScanner` output.
- Compare the new index against the legacy discovered/loaded result and report diffs.
- Keep old loader and existing GameBridge features as the runtime authority for behavior.
- Expose diagnostics through existing feature/hook status and runtime report context.
- Add smoke/result fields:
  - `ContentRegistry`
  - `ManifestRegistry`
  - `DependencyCompatibility`
  - `ContentPackOwnership`
  - `RegistryDiffs`

## Non-Goals

- Do not set `RegistryTakesOver=true`.
- Do not rewrite `LoadMods`, `OrderMods`, `CanLoadDependencies`, CustomAnimals, AnimalVoice, AutoFishing, AnimalViewer, or content pack paths.
- Do not change `IDtmHelper`, `IManifest`, `IWorkshopHelper`, content-pack JSON fields, `custom-animals.json`, or `audio-replacements.json`.
- Do not expand stable public API.

## Acceptance

- Unit tests cover flag defaults/overrides, bad manifest scanner diagnostics, missing dependency, too-new DTMAPI version, duplicate UniqueID diagnostics, capabilities/ownership summary, and registry diff formatting.
- `tools/scripts/test.ps1 -Configuration Release` passes.
- `git diff --check` passes with only existing line-ending warnings.
- PowerShell parser check for changed smoke scripts passes.
- Short smoke gate passes:
  - slot 3 lifecycle + HookProbe + title button lifecycle
  - slot 7 Hatch AnimalVoice
  - slot 4 AnimalViewer UI hidden-product fixture
  - slot 5 AutoFishing short soak
- Smoke results show `RegistryTakesOver=false`, new registry fields passed, no unexplained registry diffs, no Fatal GC, no duplicate LoadGame, and no leftover `DolocTown.exe`.

## Rollback

- Disable the new diagnostic index through `DTMAPI/config/refactor-scaffold.json` with `ContentManifestRegistry=false`.
- Disabling the flag must leave the legacy manifest scanner, loader, content pack behavior, and shadow registry behavior unchanged.
