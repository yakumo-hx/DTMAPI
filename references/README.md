# References

This directory contains reference material only. DTMAPI is rebuilt from zero; these files inform design and tests but are not source code to copy into the runtime.

See `COPY-MANIFEST.md` for copy categories and explicit exclusions.

## Git Scope

Git should track only public reference documentation and DTMAPI-authored research notes:

- `COPY-MANIFEST.md`
- `README.md`
- `doloc-town/official-workshop-docs`
- `doloc-town/research-notes`

The following folders are local-only and ignored by Git:

- `doloc-town/reverse`
- `doloc-town/own-mod-sources`
- `third-party-mods`
- `stardew-smapi`

Those local-only folders may exist in a developer workspace, but they must not be published or required to build DTMAPI.

## Doloc Town

- `doloc-town/official-workshop-docs/pdf`: copied official Workshop/modding PDFs from the private predecessor research workspace.
- `doloc-town/official-workshop-docs/feishu-crawl-20260715`: latest complete browser-rendered official Workshop documentation crawl, including real embedded-sheet TSV/screenshot exports and a SHA-256 inventory.
- `doloc-town/official-workshop-docs/feishu-crawl-20260517`: preserved historical predecessor-workflow crawl; useful for token/history comparison, but not the current sheet/text authority.
- `doloc-town/official-workshop-docs/update-notes`: local notes for Doloc Town Workshop updates `0.96.05` and `0.96.06`.
- `doloc-town/research-notes`: selected factual notes about Doloc APIs, official Workshop behavior, and vehicle research. These are not old runtime source.
- `doloc-town/reverse/builds`: local-only decompiled/unpacked build research. A build may include frozen official player bytes, a recovered Unity project, SHA-256 inventories, and structural diffs; none of those private artifacts may be committed, published, or required for a normal DTMAPI build.
- `doloc-town/own-mod-sources`: selected source-only copies of the user's five old Doloc Town mods, kept as migration inputs and compatibility references.

The reverse build folders may include `input/Assembly-CSharp.dll`, full official player snapshots, decompiled game code, and extracted Unity assets for local research. Do not publish or package those files.

Use [the capture workflow](../tools/portable-reverse-capture/README.zh-CN.md): `Status` reads existing stage summaries, `CodeOnly` omits resource export, and full capture runs only the missing or invalid stages. `Resume` binds the retained snapshot, tools and outputs before reuse. A symbol lookup uses existing matching knowledge; [baseline comparison](../tools/reverse-capture/README.md) reads the selected captures without starting unpacking tools.

Official bytes and extracted output stay in the ignored reverse tree. Tracked documentation may record hashes, limitations and derived conclusions, not copied official content. Build-policy reference identity, latest research observation and actual test-game identity are separate facts.

DTMAPI-authored native-owner summaries derived from these local references belong in `../docs/reviews/api/native-owner-domains/INDEX.md`. That library may name classes, members, maps, and official docs, but it must not copy decompiled method bodies or distribute official binaries.

## Stardew Valley SMAPI

- `stardew-smapi/installed`: installed SMAPI runtime copied from a local Stardew Valley install.
- `stardew-smapi/bundled-mods`: only SMAPI bundled `ConsoleCommands` and `SaveBackup` were copied. Other Stardew Valley user mods were not copied.

Use this as ecosystem/API/diagnostic reference, not as code to paste.

## Third-Party Doloc Town Mods

- `third-party-mods`: existing third-party mod archives/extracted packages that were already in this workspace.

These are compatibility samples. Do not modify, redistribute, or fold them into DTMAPI unless license/author permission is explicitly confirmed.
