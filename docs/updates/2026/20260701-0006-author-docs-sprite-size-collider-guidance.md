# 20260701-0006 Author Docs Sprite Size Collider Guidance

## Source

User shared sprite-size research and asked whether DTMAPI treats `sprite_size` as the animal hit/interaction size. The requested author guidance is two-track: either keep the native template animal sizing, or change animal sizing through small in-game feel adjustments.

## Changed Files

- `author-docs/content-packs/custom-animal-json-png-wav.md`
- `docs/updates/2026/20260701-0006-author-docs-sprite-size-collider-guidance.md`
- `docs/updates/INDEX.md`

## Summary

- Clarified that DTMAPI's `pngSpriteOverride` path does not read `sprite_size` to choose or validate PNG frames.
- Documented the native responsibility path: `AnimalLevelData` reads `sprite_size`, `AnimalInfo.PostResolve()` derives child/adult collider sizes and emotion offsets, and `Animal.OnRender()` assigns the collider size to `AnimalRenderer.BoxColliderSize`.
- Reframed `sprite_size` for authors as an interaction/contact core size rather than PNG canvas or automatic visual bbox.
- Split sizing guidance into two recommended routes:
  - keep the native template `sprite_size`, `size`, and `space` for the most stable first pass;
  - only change `sprite_size` in small increments after game-feel testing if the custom animal's body needs a different interaction box.
- Updated preflight and release checks so authors do not automatically copy PNG canvas or bbox dimensions into `sprite_size`.

## Validation

- Searched DTMAPI source and tests for `sprite_size` / `SpriteSize`; no runtime DTMAPI parser or PNG bridge code reads the field.
- Inspected reverse build `23762374_public_C416D4`:
  - `DolocTown.Config.Animal.AnimalLevelData` parses JSON `sprite_size` into `SpriteSize`.
  - `DolocTown.Config.Animal.AnimalInfo.PostResolve()` derives `ColliderSizeAdult`, `ColliderSizeChild`, and emotion offsets from stage `SpriteSize`.
  - `DolocTown.Animal.OnRender()` assigns the stage collider to `Renderer.BoxColliderSize`.
  - `DolocTown.AnimalRenderer.BoxColliderSize` writes `BoxCollider2D.size` and vertical offset.
- Parsed all 11 `json` code blocks in `author-docs/content-packs/custom-animal-json-png-wav.md` with PowerShell `ConvertFrom-Json`.
- Ran `git diff --check`; it reported only existing LF/CRLF normalization warnings.
- Not run: build/test/game smoke, because this is a docs-only correction based on static code review.

## Related Records

- Author guide creation: `docs/updates/2026/20260701-0003-author-docs-custom-animal-guide.md`
- Oilfloater author-docs closure: `docs/updates/2026/20260701-0005-author-docs-oilfloater-review-closure.md`
- Hatch PNG route: `docs/updates/2026/20260630-0001-hatch-png-custom-animal.md`

## Rollback

Remove the `sprite_size` guidance section and restore the previous PNG-canvas-oriented checklist wording, then delete this update record and remove the `20260701-0006` row from `docs/updates/INDEX.md`.
