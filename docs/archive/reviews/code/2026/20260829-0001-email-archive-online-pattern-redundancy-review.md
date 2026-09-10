# Email Archive online-pattern and redundancy review

- Date: 2026-08-29
- Review Status: `recorded`
- Scope: `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant` flattened 205-mail implementation, delivery builders, manual data, and current Doloc Town HuijiWiki Lua/Gadget patterns
- Related Update: [20260829-0001](../../../updates/2026/20260829-0001-email-archive-flat-sandbox-delivery.md)
- Public Wiki writes: none

## Review outcome

The two six-file delivery folders are already the smallest sensible administrator handoff. The remaining high-value simplification is inside the Lua-to-JavaScript payload, the manual data source, and one obsolete build entry point. The current implementation does not need another Config, Trace, Schema, Manifest, client-side Data fetcher, or DOM-diff framework.

The present 221,749-byte flattened payload can plausibly fall to about 128 KB without changing the visual contract. The safe route is to remove fields already derivable from canonical fields, generate the search index once in JavaScript initialization, and keep the current two load boundaries and unknown-data fallbacks.

No source or Wiki page was changed during this review.

## Online sources inspected in Chrome

- `Module:Utils`, especially `load_json` and `unity_richtext_to_css`.
- `Module:Arguments`.
- `Module:Fishing/FishingAnalyzer` and `Gadget:FishingAnalyzer.js`.
- `Module:Email/EmailArchive`, `Module:Email/EmailArchiveConfig`, `Module:Email/EmailArchiveTrace`, and `Module:Email/EmailUtils`.
- `Module:Item/ItemUtils`, `Module:Mission/MissionUtils`, and `Module:Recipe/RecipeUtils`.
- `Template:角色肖像`, `Module:Npc/NpcUtils`, and `Module:Showcase`.
- `Gadget:Entrance.js` and `MediaWiki:Common.js`.

The current site pattern is intentionally light at the Lua data boundary: mature modules call `Module:Utils.load_json` directly and perform domain checks while traversing data rather than validating an entire schema first. The Fishing Analyzer moves several Data reads into a bundled client application, but that makes its JavaScript substantially more complex. Email Archive should not copy that client-fetch architecture because it would need to reproduce item titles, mission links, manual conditions, and attachment fallbacks in the browser.

The current portrait chain is `Template:角色肖像 -> Module:Npc/NpcUtils.avatar -> Module:Showcase.portrait -> MediaWiki File markup`. A live parse of Kasia produced one CDN image inside server-generated links and decorative wrappers. This supports simplifying the current recursive avatar sanitizer to one-image extraction, but does not by itself authorize trusting arbitrary HTML.

## High-confidence simplifications

1. Retire the obsolete build entry point.
   - `tools/build_email_wiki_delivery.py` still owns the old Config/Trace/conditions/AUDIT/checksum/ZIP delivery path.
   - Keep the command name only as a thin compatibility wrapper around `build_email_archive_rebuild.py`, or mark it explicitly retired.
   - It must never regenerate the old administrator package as the current delivery.

2. Remove dead and duplicated payload fields.
   - `rewardKinds` is never read by the current JavaScript.
   - `avatarText` is already derivable from `role.name`.
   - Reward `target` and `text` are redundant because Lua always supplies a non-empty `title`, including unknown-attachment fallback records.
   - Per-email `categoryLabel` is derivable from top-level categories.
   - Per-email `searchText` duplicates the full body and other fields; derive one lowercase index during `normalizeData` from ID, title, sender, role, condition, rewards, category, and text extracted from `contentHtml`.
   - Removing the full set is estimated to reduce the current payload from 221,749 bytes to about 128,289 bytes, roughly 42.1%.

3. Make the manual table genuinely optional-field data.
   - All 170 current `contact` values equal the existing sender-alias/original-sender fallback and can be omitted without changing grouping.
   - Eighty-two empty `condition` values, seven `portrait=false` values, all unused `group` fields, and the duplicated `uncategorized` declaration can be omitted.
   - Keep the eight zero-mail contacts because they predeclare future portrait/group behavior.
   - Update the local preview parser at the same time: its fixed-order regular expressions currently contradict the optional-field runtime contract and can silently miss valid records.

4. Remove low-value repeated client work without introducing a framework.
   - Let `visibleRoleCards` test each role's existing email list directly; remove the intermediate full `visibleEmails` list.
   - Reuse the already-filtered email list length in `renderSidebar`.
   - Render static statistics once after shell initialization.
   - Reduce `safeAvatarHtml` to extracting and rebuilding the one image produced by the trusted portrait chain; keep `loading`, `decoding`, `no-referrer`, a text fallback, and an explicit source policy.
   - Delete only confirmed dead CSS declarations/selectors; do not reformat or redesign the stylesheet.

5. Remove output-to-input coupling in the builder.
   - `build_email_archive_rebuild.py` currently reads `dist/email_admin_delivery_preview.html` as a reward-title source and optionally reads avatar metadata from `dist`.
   - `dist` must remain output-only. Move required acceptance fixtures under a data/fixture location, or allow the preview to degrade to IDs when a cache is absent.
   - Share one common four-file delivery list between the Html and Entrance variants.

## Keep unchanged

- The two `pcall` boundaries around main JSON and manual-data loading.
- `Module:Utils.load_json`, including its `{ data = [...] }` unwrapping behavior.
- `id/name` and `$type/＄type` compatibility.
- Unknown sender, category, attachment, item-title, and mission-title fallbacks.
- `mw.text.nowiki` around the embedded JSON payload.
- `escapeHtml`, `safeRichHtml`, and the internal-Wiki reward-link restriction.
- Direct ID routing, `email-` compatibility, unique-title compatibility, and duplicate-title rejection.
- Separate selected role, selected email, and mobile view state.
- The `data-dlk-email-mounted` guard, RLQ wrapper, and exact Entrance page allowlist.
- Both six-file delivery folders as independent alternatives; do not merge them or deploy both loaders.

## Medium-risk items requiring sandbox proof

- Removing `popstate` and relying only on `hashchange` should eliminate duplicate back/forward renders, but must be tested in Chrome across direct hash entry, mail-to-mail navigation, back, forward, and hash clearing.
- Replacing the avatar CDN whitelist with trust in server-parser output would remove platform hardcoding, but should not happen until the exact runtime `frame:expandTemplate` output is inspected in the real sandbox. One-image extraction with a bounded URL policy is the safer first reduction.
- Removing the top-level `meta` counters is possible because the client already recomputes them, but saves almost no bytes and should not be mixed into a higher-risk search rewrite unless it makes the contract materially clearer.

## Acceptance checks for a later implementation

- Rebuild both delivery folders and require exactly six files and zero subdirectories in each.
- Assert that Lua output, the Python preview builder, and the JavaScript fixture expose the same exact field sets.
- Require 205 emails and 205 unique IDs, 32 active roles, all eight category counts, 88 non-empty conditions, and unchanged attachment rendering.
- Re-run direct ID, `email-`, unique-title, duplicate-title rejection, search by title/sender/body/reward/condition/category, category filtering, clear filter, and browser history tests.
- Repeat Chrome 1440x900 and 390x844 visual acceptance, including all current portrait roles and zero console warnings/errors.
- Verify missing manual data, unknown sender, unknown category, unknown attachment type, and main-data failure behavior.
- Run DTMAPI document governance after recording the implementation in a new Update; this Review remains pre-implementation evidence only.
