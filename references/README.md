# References

This directory contains reference material only. DTMAPI is rebuilt from zero; these files inform design and tests but are not source code to copy into the runtime.

See `COPY-MANIFEST.md` for exact local source paths and explicit exclusions.

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

- `doloc-town/official-workshop-docs/pdf`: copied official Workshop/modding PDFs from `E:\Python_project\DLK\research\创意工坊说明`.
- `doloc-town/official-workshop-docs/feishu-crawl-20260517`: latest extracted official Workshop documentation crawl.
- `doloc-town/official-workshop-docs/update-notes`: local notes for Doloc Town Workshop updates `0.96.05` and `0.96.06`.
- `doloc-town/research-notes`: selected factual notes about Doloc APIs, official Workshop behavior, and vehicle research. These are not old runtime source.
- `doloc-town/reverse/builds`: copied decompiled build research for `23249387_workshop_247ACD` and `23465763_workshop_38581E`.
- `doloc-town/own-mod-sources`: selected source-only copies of the user's five old Doloc Town mods, kept as migration inputs and compatibility references.

The reverse build folders include `input/Assembly-CSharp.dll` and decompiled game code for local research. Do not publish or package those files.

## Stardew Valley SMAPI

- `stardew-smapi/installed`: installed SMAPI runtime copied from `D:\Steam\steamapps\common\Stardew Valley`.
- `stardew-smapi/bundled-mods`: only SMAPI bundled `ConsoleCommands` and `SaveBackup` were copied. Other Stardew Valley user mods were not copied.

Use this as ecosystem/API/diagnostic reference, not as code to paste.

## Third-Party Doloc Town Mods

- `third-party-mods`: existing third-party mod archives/extracted packages that were already in this workspace.

These are compatibility samples. Do not modify, redistribute, or fold them into DTMAPI unless license/author permission is explicitly confirmed.
