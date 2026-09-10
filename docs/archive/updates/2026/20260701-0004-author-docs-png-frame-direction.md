# 20260701-0004 Author Docs PNG Frame Direction Clarification

## Source

User asked whether extra PNG frames are meaningful, whether they report errors, and whether the author docs explain left/right PNG direction behavior. The user then noted Hatch may have had left/right source resources and asked whether the real mod package uses them.

## Changed Files

- `author-docs/content-packs/custom-animal-json-png-wav.md`
- `docs/updates/2026/20260701-0004-author-docs-png-frame-direction.md`
- `docs/updates/INDEX.md`

## Summary

- Clarified that extra PNG frames beyond the template-requested frame names are normally unused rather than errors.
- Added a dedicated section explaining that current `pngSpriteOverride` maps only requested template sprite names to custom sprite names.
- Documented that current Hatch packages use one numbered PNG set, not separate left/right direction PNG files.
- Added author guidance that left/right direction suffixes are not used by the current route and that visible asymmetry may be mirrored by native animal renderer direction handling.

## Validation

- Inspected source and local runtime Hatch package `Content/Sprites` lists: both contain the same 44 numbered Hatch PNG files and no left/right direction files.
- Inspected `CustomAnimalAnimatorBridgeService.MapPngSpriteName`, which only does template-prefix replacement, `child_` to `young_`, and `jump_ready` to `jump_0`.
- Ran `git diff --check`; it reported only existing CRLF warnings.
- Not run: build/test/game smoke, because this is a docs-only clarification.

## Related Records

- Parent author-docs guide update: `docs/updates/2026/20260701-0003-author-docs-custom-animal-guide.md`
- Hatch PNG route: `docs/updates/2026/20260630-0001-hatch-png-custom-animal.md`

## Rollback

Remove the new left/right PNG clarification from the author guide, delete this update record, and remove the `20260701-0004` row from `docs/updates/INDEX.md`.

