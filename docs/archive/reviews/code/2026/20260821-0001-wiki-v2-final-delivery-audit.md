# Wiki V2.3 final delivery audit

- Date: 2026-08-21
- Review Status: `recorded`
- Scope: `wiki/v2` 30-page English content candidate, protected infrastructure, upload handoff, and final local delivery package
- Owning Update: [20260821-0001](../../../updates/2026/20260821-0001-wiki-v2-thirty-page-source-translation-audit.md)
- Public Wiki writes: none

## Review outcome

The candidate is acceptable for administrator handoff and server-side **unsaved preview** after packaging. It is not authorized for public publication. Five server gates remain: policy approval, the authorized Bot mapper, desktop/mobile unsaved preview, Scribunto/search acceptance, and saved revision receipts.

No blocking defect was found in the seven English pages changed by the source-level recheck. The changes restore omitted source meaning or correct terminology without changing dynamic component calls:

1. `Seed Compressor`: says the machine produces seeds from gene-free Eden Fruit.
2. `Kasia`: uses a natural section heading and repairs one whispered line.
3. `Pike`: uses official `Doloc Commercial Street` terminology.
4. `Environmental Restoration`: restores the Ponka interaction and closes the Phosphor Bloom link into the packaged English set.
5. `Archives`: restores the source's central-Doloc-Center location detail.
6. `Fishing Rods`: labels the hardcoded price column as `Cost (G)`.
7. `Friendship`: corrects the notification threshold to half a heart and normalizes `Hated gifts`.

The other 23 pages have current source-level records and did not require another prose change. The audit does not convert agent review into community acceptance; all 30 ledger entries retain `human-community-acceptance=false`.

## Structural findings

- Current manifest: 54 targets, comprising 3 Data, 7 Module, 2 Template, 12 Category, and 30 Article targets.
- Permission split: one Bot-generated Data target, 11 administrator-installed local targets, and 42 editor targets.
- Redirects, English alias pages, `/en` routes, and `Fish Generator` are absent from the active V2 config, manifest, upload artifacts, ordered targets, and rollback manifest.
- The 30 canonical titles are the approved formal English titles. Twenty entity/location titles have exact official TextMapper hits; the remaining guide/index titles are page-concept titles, not alternate aliases or routing fallbacks.
- Dynamic infobox, recipe, store, task, electricity, archive, and calculator output remains component/Data-owned. The English articles translate hardcoded source text instead of copying visual output.
- The package excludes the production English mapper because Qiuzy bot must generate it from its separately authorized, same-build source. Local reverse fixtures are test evidence only and are not distributable upload inputs.

## Packaging finding and correction

The first audited archive mixed the actual upload payload with developer-facing manifests, reports, translation reviews,
receipt forms, hashes, and the audit itself. Those materials were valid evidence but made the handoff harder to
understand. The user subsequently narrowed the delivery boundary: keep evidence in the repository and give Wiki users
only the files required to install, generate, copy, preview, and maintain the English pilot.

The current archive is therefore `英文wiki尝试.zip` and has exactly four Chinese root folders:

- `组件/`: 2 local Data files, 7 Lua modules, and 2 templates;
- `bot工具/`: the two mapper preparation/validation scripts, without production mapper data;
- `页面示例/`: 30 English articles and 12 English categories;
- `说明/`: four concise Chinese documents, including an automatically generated 54-target file map.

Every one of the 44 `.wiki` files has a byte-identical `.txt` companion for direct opening. The archive deliberately
omits audit records, manifests, package checksum files, reverse captures, source baselines, historical V1 artifacts,
test fixtures, and the production Bot mapper. Those omissions do not discard evidence: the repository remains its
canonical owner.

The generator still refuses a candidate unless local validation is 26 passed / 0 failed / 5 server gates pending, the
manifest is 54/12/42 with zero redirects, all 30 article configs have no alias field, and the source payload contains
exactly 11 administrator targets, 30 articles, and 12 categories. It additionally verifies the four-folder root, all
44 Wiki/text pairs, Unicode ZIP paths, the extracted file set, and every extracted file hash.

## Non-blocking content follow-up

The per-page reviews retain genuine source/community questions, including Kasia's upstream heart-icon inconsistency, four Environmental Restoration quotations without exact official dialogue assets, the source-side proofreading markers for Doloc Guard Drone and Electricity, and the contaminated Chinese Fishing Probability Calculator source. These do not justify silent invention or an alias; Wiki maintainers should resolve them during community and server review.

## Acceptance checks

- Run `npm test` from `wiki/v2` and require 26 passed, 0 failed.
- Run `npm run package:delivery` and require a 103-file extracted set, four Chinese root folders, and 44 identical
  `.wiki/.txt` pairs.
- Run `tools/scripts/check-doc-governance.ps1` for the owning Update and this Review.
- Confirm the ZIP contains no audit/manifest/checksum payload, reverse/unpacked asset, production mapper candidate,
  redirect page, alias page, or `/en` target.

Passing these checks admits the bytes only to local handoff and unsaved server preview. Public saves remain governed by `wiki/v2/docs/upload-runbook.md` and `wiki/v2/docs/server-acceptance.md`.
