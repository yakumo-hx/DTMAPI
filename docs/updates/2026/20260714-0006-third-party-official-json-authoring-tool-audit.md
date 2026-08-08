# 20260714-0006 Third-Party Official JSON Authoring Tool Audit

## Metadata

- Update ID: `20260714-0006`
- Date: 2026-07-14
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: review/third-party/official-json/author-tool/security/import-export/schema
- Source: user requested a read-only sweep of Workshop item `3755367789`, a third-party browser helper for authoring official JSON Mods

## Scope

This documentation-only Update records a static source and official-schema audit of the installed Workshop copy of “更好上手的模组制作工具” version `0.6`.

It does not run the HTML page, download or execute its remote libraries, import or generate a Mod ZIP, modify the third-party Workshop item, launch the game, or change DTMAPI.

## Changed Files

- `docs/reviews/code/2026/20260714-0003-third-party-official-json-authoring-tool-audit.md`
  - records the screenshot, artifact identity, security boundary, six generator verdicts, import/re-export data-loss risks, official-ID drift, safe-use boundary, and author fix order.
- `docs/updates/2026/20260714-0006-third-party-official-json-authoring-tool-audit.md`
  - owns the documentation lifecycle and validation boundary for this audit.
- `docs/updates/INDEX-2026-07.md`
  - routes this Update from the July ledger.

## Validation

- The four Workshop files were enumerated and SHA-256 hashed without modification.
- `info.json` parsed successfully; both PNGs decoded and their dimensions were checked.
- The 90,630-byte HTML was scanned for external resources, upload/network APIs, browser file APIs, dynamic-code execution, obfuscation indicators, DOM injection sinks, and archive import/export paths.
- No direct inline upload API or deliberate obfuscation was found. Two remote CDN scripts without SRI/CSP and multiple unescaped `innerHTML` sinks were confirmed.
- The inline JavaScript passed a syntax-only check with the bundled Node.js parser; the page code was not executed.
- All six advertised generator paths and the import path were compared with the installed official example Mod and public build `23762374` table/constructor behavior.
- Selectable map values were compared with current official subtype, source, function, recipe subtype/group, normal/exchange store, gene, and item IDs. Invalid values and explicitly supported store aliases were separated.
- Documentation governance and diff checks are the final repository validation gates.

No browser or game runtime validation is required to record these static defects. The related review explicitly avoids claiming anything about the live runtime behavior of the remote CDN scripts.

## Evidence

The inspected third-party Workshop files remain at their installed path and were not copied into the repository. Durable findings are summarized in the related review without redistributing the HTML source.

## Rollback

Remove this Update, its July ledger row, and the related review together. There is no runtime, game-file, API, package, or third-party artifact rollback.

## Follow-Up

Do not adopt version 0.6 as an official-JSON validation or round-trip workflow. Reassess a future version only after it removes imported-data HTML injection, localizes/verifies its dependencies, fixes the four blocked generator categories, preserves imported content, and adds exact current-schema/package validation.
