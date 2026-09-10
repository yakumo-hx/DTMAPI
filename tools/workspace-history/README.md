# Workspace history construction tool

This tool serves the approved 2026-09-08 migration and full-reading project. It is not a required step for ordinary Mod changes.

- `python tools/workspace-history/history.py check` checks original hashes when local originals are available, current paths, frozen bytes and recorded archived-body hashes.
- `migrate --selection path.json` previews a JSON object mapping original paths to `docs/archive/...` destinations. Add `--apply` to execute. The ignored journal contains before/after bytes and an exact rollback record.
- `rollback --journal path.json` restores one batch only if later edits will not be overwritten.
- `--stable-references path.json` preserves a small old-address route only for verified references from frozen receipts or independent delivery metadata; those source files remain unchanged.
- `relocate-tool --selection path.json` is limited to the approved native-function-map move into `tools/native-function-map`. Its current tool files remain editable while their raw construction originals are preserved.
- `relocate-references --selection path.json` records already moved source/test link targets after verifying the old path in the Git checkpoint and the new file on disk. It journals Markdown-only changes, preserving historical command text; archive verification uses the same mapping. It does not move or rewrite source files.
- `repair-links` previews a link-parser correction against original archived text; `--apply` journals it and refuses any non-link content difference.
- `preserve-originals --selection path.json` copies reviewed baseline bodies to the archive before shortening a current owner. The owner address stays live; only the archived copy is immutable except for mechanical links. Add `--apply` to execute.
- `extend-attachments --selection path.json` checkpoints explicitly inventoried attachments from the same Git baseline. Each entry names `source`, `owner` and `purpose`; attachments are not counted as prose reading. Binary bytes remain exact through migration and rollback.
- `migrate --private --selection path.json` moves fully reviewed local conversations under ignored `docs/archive/conversations/`. It refuses tracked or non-ignored sources/destinations, journals independently, and never writes private coverage into the public manifest.
- `merge-readings` validates explicitly authored full-read decisions. Add `--apply` to import them into the construction manifest. Reading or hashing a file does not count as review.
- To relink knowledge after its writers finish, use an empty selection `{}` with `migrate --selection path.json --include-knowledge --apply`. Earlier location mappings are reused.
- `check --require-full` is a public final coverage gate. Add `--include-private` for local acceptance of the separately ignored private inventory; public CI needs no private originals.

The approved exception for generated inventories is recorded separately as `authored-complete/generated-checked`, never as full-file reading. It requires full reading of authored sections and every distinct judgment, plus generation evidence and explicit inventory checks in `reviewBasis`. Length alone does not qualify; unknown or unreviewed text remains pending.

`python tools/workspace-history/check_links.py` compares local Markdown destinations and anchors with the original Git tree. It reports retained historical defects separately and fails on newly broken links. Its report stays in ignored construction evidence.

New ordinary records continue to be created in their existing active directories. Later monthly archive batches use this same bounded migration mechanism; IDs do not change. Frozen texts remain at their original addresses.
