# 20260701-0010 Hatch Feishu Doc Generator

## Source

User requested implementation of the plan to automate the Feishu version of the `新增养殖动物` author document:

- Use the current local Hatch package as the only source, not an abstract template.
- Generate Feishu Docx OpenAPI blocks with native JSON code blocks, tables, image assets, and light-yellow replacement highlights.
- Build five action frame atlas images with per-frame file names.
- Keep local generated payloads/logs so the Feishu write can be repeated after credentials are available.

## Changed Files

- `author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `author-docs/content-packs/new-farm-animal-draft/README.md`
- `author-docs/content-packs/new-farm-animal-draft/generated/feishu-blocks.json`
- `author-docs/content-packs/new-farm-animal-draft/generated/validation-report.json`
- `author-docs/content-packs/new-farm-animal-draft/generated/hatch-feishu-doc.preview.md`
- `author-docs/content-packs/new-farm-animal-draft/generated/frames/hatch_idle_frames.png`
- `author-docs/content-packs/new-farm-animal-draft/generated/frames/hatch_move_frames.png`
- `author-docs/content-packs/new-farm-animal-draft/generated/frames/hatch_eat_frames.png`
- `author-docs/content-packs/new-farm-animal-draft/generated/frames/hatch_jump_frames.png`
- `author-docs/content-packs/new-farm-animal-draft/generated/frames/hatch_sleep_frames.png`
- `docs/updates/2026/20260701-0010-hatch-feishu-doc-generator.md`
- `docs/updates/INDEX.md`

## Summary

- Added a Hatch-specific Feishu document generator under the local `新增养殖动物` draft folder.
- The generator reads `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets` by default.
- The generator validates the current Hatch package before producing output:
  - 11 JSON files parse successfully.
  - 44 PNG files exist.
  - 2 WAV files exist.
  - young frames are all `28x24`; adult frames are all `32x32`.
  - action counts match `eat=7`, `idle=4`, `jump=2`, `move=8`, `sleep=1` for both young and adult.
- The generator emits:
  - `feishu-blocks.json` with Feishu-ready block payloads and the original document model.
  - `validation-report.json`.
  - `hatch-feishu-doc.preview.md`.
  - Five frame atlas PNGs, one per action, with young/adult rows and each frame's full file name below the sprite.
- Feishu upload is implemented behind explicit `--upload`; it requires `FEISHU_ACCESS_TOKEN` plus `FEISHU_DOC_ID` or `--document-url`. Without credentials, the script only writes local output and does not touch Feishu.
- Chrome inspection found the current Feishu debug page URL as `https://kcndb2wpn7uc.feishu.cn/wiki/OLUawqyXViaxdAkr1XVchTKfnYc`.

## Validation

Passed:

- `python -m py_compile author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `python author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- Generated payload summary:
  - top-level blocks: `110`
  - JSON code blocks: `11`
  - JSON-language code blocks: `11`
  - tables: `6`
  - images: `5`
  - highlighted code segments using `background_color: 3`: `110`
  - validation errors: `[]`
- Visual inspection of `generated/frames/hatch_move_frames.png` confirmed young/adult rows, enlarged sprites, and per-frame file-name labels are readable.

Not run:

- Feishu OpenAPI upload, because no `FEISHU_ACCESS_TOKEN` / `FEISHU_DOC_ID` environment variables are configured in the current shell.
- Game/runtime smoke; this is author-document automation only and does not touch runtime, hooks, game files, or local MODS content.

## Rollback

Delete the generator script, generated folder, and this update record, then remove this row from `docs/updates/INDEX.md`. This has no runtime rollback requirement because it does not modify DTMAPI runtime code or Doloc Town content packages.

## Follow-Up

- Provide `FEISHU_ACCESS_TOKEN`, then run the generator with `--upload --document-url https://kcndb2wpn7uc.feishu.cn/wiki/OLUawqyXViaxdAkr1XVchTKfnYc` against the current Feishu debug page.
- After the debug page is visually accepted, rerun the same command against the final Feishu document.
