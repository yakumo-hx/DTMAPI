# 20260530-0001: Branding Assets And Update Record System

## Metadata

- Update ID: 20260530-0001
- Date: 2026-05-30
- Status: implemented
- Source: user requested DTMAPI-branded images and a traceable update-record system.
- Owner: Codex

## Summary

- Found the old DLKsmapi Workshop/runtime image style in `E:\Python_project\DLK`.
- Created DTMAPI-branded replacements in the DTMAPI workspace.
- Added this `docs/updates` ledger so future updates can be traced to goals, changed files, validation, evidence, and follow-up.
- Added project rules requiring update records for non-trivial implementation, runtime, hook, UI, mod loading, installer, asset, and direction changes.

## User-Visible Impact

- DTMAPI can now use project-local branding assets instead of old `SMAPI` images.
- Future Codex sessions have a single place to find what changed, why it changed, and how it was validated.

## Changed Files

- `assets/branding/dtmapi-icon.png`
- `assets/branding/dtmapi-preview.png`
- `docs/updates/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260530-0001-branding-and-update-record-system.md`
- `AGENTS.md`
- `PROJECT.md`

## Validation

- Visual inspection performed for:
  - `assets/branding/dtmapi-icon.png`
  - `assets/branding/dtmapi-preview.png`
- No build or game smoke test was run because this update only adds docs and branding assets.

## Evidence

- Source image style was found in:
  - `E:\Python_project\DLK\packages\workshop\WorkshopPackages\DLK_Functional_Mod_Loader\icon.png`
  - `E:\Python_project\DLK\packages\workshop\WorkshopPackages\DLK_Functional_Mod_Loader\preview.png`
- New assets:
  - `assets/branding/dtmapi-icon.png`
  - `assets/branding/dtmapi-preview.png`

## Related Records

- Debug: none.
- Hook map: none.
- Smoke matrix: none.
- API matrix: none.

## Rollback Notes

- Revert by removing `assets/branding` and `docs/updates`, then removing the update-ledger mentions from `AGENTS.md` and `PROJECT.md`.
- No runtime behavior depends on these assets yet.

## Follow-Up

- The next UI/main-menu goal should use `assets/branding/dtmapi-icon.png` or a DTMAPI-named runtime asset derived from it.
- Each subsequent goal should create a new update record before final handoff.
