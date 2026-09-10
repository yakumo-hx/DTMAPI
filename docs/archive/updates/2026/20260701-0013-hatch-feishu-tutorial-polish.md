# 20260701-0013 Hatch Feishu Tutorial Polish

## Source

User requested a detailed tutorial-readability pass for the Feishu `新增养殖动物` document generator, including a sub-agent review focused on official-style consistency and detailed tutorial suitability.

User requirements:

- Remove "第一轮" style wording and provide the template as a stable authoring example.
- Use simplified Chinese in simplified-Chinese/display text positions.
- Remove per-JSON "核心配置：是/否" labels.
- Give every JSON a meaningful purpose explanation distinct from its name.
- Prefer wide, short tables over narrow, tall tables.
- Put image titles above images, with titles starting with `·`.
- Display this Hatch package as `哈奇`, author `Yuuka`, version `1.0.0`, with a simplified-Chinese default description.
- Check whether `Content/DTMAPI/dtmapi-package.json` is runtime-required; if not, do not include it in the tutorial body.

## Changed Files

- `author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `author-docs/content-packs/new-farm-animal-draft/README.md`
- `author-docs/content-packs/new-farm-animal-draft/generated/feishu-blocks.json`
- `author-docs/content-packs/new-farm-animal-draft/generated/validation-report.json`
- `author-docs/content-packs/new-farm-animal-draft/generated/hatch-feishu-doc.preview.md`
- `author-docs/content-packs/new-farm-animal-draft/generated/hatch-feishu-rich-copy.html`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets\info.json`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets\Content\DTMAPI\manifest.json`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets\Content\animal_tbanimal.json`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets\Content\item_tbitem.json`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets\Content\animal_tbanimaldocument.json`
- `docs/updates/2026/20260701-0013-hatch-feishu-tutorial-polish.md`
- `docs/updates/INDEX.md`

## Summary

- Spawned a review sub-agent for tutorial suitability and official-style consistency. The review confirmed the main issues: stale generated output, English display text in source content JSON, `dtmapi-package.json` in the tutorial body, "核心配置：是/否", "第一轮" wording, narrow tables, below-image captions, and duplicate body title.
- Confirmed `Content/DTMAPI/dtmapi-package.json` is not required for custom-animal runtime loading:
  - `ManifestReader.FindManifest` discovers content packs through `Content/DTMAPI/manifest.json`.
  - `CustomAnimalAnimatorBridgeService` reads `Content/DTMAPI/custom-animals.json` from enabled `ContentPack` mods.
  - `AudioReplacementService` reads `Content/DTMAPI/audio-replacements.json` from enabled `ContentPack` mods.
  - `dtmapi-package.json` is used as a DTMAPI-owned package marker by install, uninstall, upload/display, and tooling paths.
- Removed `dtmapi-package.json` from the generated tutorial body and JSON validation list.
- Changed Hatch display metadata in the local source package:
  - `info.json`: `哈奇`, `Yuuka`, `1.0.0`, simplified-Chinese description.
  - `Content/DTMAPI/manifest.json`: `哈奇`, `Yuuka`, `1.0.0`, simplified-Chinese description, while keeping `UniqueID` unchanged as `DTMAPI.HatchAssets`.
  - `animal_tbanimal.json`, `item_tbitem.json`, and `animal_tbanimaldocument.json`: display text changed to simplified Chinese.
- Removed generated "核心配置：是/否" bullets and table column.
- Removed "第一轮" wording and rewrote template defaults as Hatch template behavior.
- Reworked tables:
  - JSON relationship table now has two columns: `配置文件` and `在新增动物链路中的作用`.
  - Animation requirements now use a wide table with action columns.
  - Image requirements now use a wide single-row table.
- Removed the body H1 so the Feishu document title can be `新增养殖动物` while body content starts at `一、整体说明`.
- Moved generated frame image titles above images and prefixed them with `·`.
- Added README generation conventions so future passes preserve this style.

## Validation

Passed:

- `python -m py_compile author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `python author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- Generated artifact structure check:
  - code blocks: `10`
  - top-level body H1 blocks: `0`
  - JSON files validated: `10`
  - PNG files validated: `44`
  - WAV files validated: `2`
  - validation errors: `0`
  - `dtmapi-package`: absent from generated preview
  - `核心配置`: absent from generated preview
  - `第一轮`: absent from generated preview
  - old `DTMAPI Hatch Assets` display name: absent
  - old English Hatch display text snippets: absent
  - `Yuuka`: present
  - `哈奇动物袋`: present
  - image titles starting with `· 哈奇`: `5`
  - generated rich-copy HTML JSON language markers: `10`
  - inline yellow highlight spans: `120`
- `git diff --check` passed with only existing LF-to-CRLF warnings for `author-docs/content-packs/custom-animal-json-png-wav.md` and `docs/updates/INDEX.md`.

Not run:

- Game/runtime smoke. This change edits local author-document source JSON and generated documentation assets only; no runtime hooks or game launch were exercised.

## Runtime Lock

Acquired and released the shared runtime lock for edits under the local Doloc Town `MODS\DTMAPI_HatchAssets` folder:

- `update Hatch author-doc source metadata`
- `update Hatch Chinese display texts for author docs`

## Rollback

- Revert the generator and README changes.
- Regenerate `generated/` with the previous generator.
- Restore the local Hatch source package display JSON values if the old developer-only metadata is needed for runtime smoke labels.
- Remove this update record and its row from `docs/updates/INDEX.md`.

## Follow-Up

- Paste the regenerated `hatch-feishu-rich-copy.html` into the Feishu debug page and visually check the shorter tables, top-positioned image titles, and simplified-Chinese display text.
