# 20260713-0008 Major Update Fourth Decision Docket

Status: recorded / fourth-round decisions closed
Date: 2026-07-13
Scope: player Manager information architecture, MoreSaves 1.0.0/API scope, and Y-console product/UI ownership
Related Update: `docs/updates/2026/20260713-0003-third-round-closure-fourth-decision-docket.md`
Follows: `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md`

## Source Request

The user supplied `D:/下载/第三轮.md`, accepted E1/F1/I1, refined G1 with explicit Author-SDK source modes, refined H1 with a low-risk Canary, and asked to continue a fourth round.

The user then supplied `D:/下载/第四轮.md`, selected J1/K1/L1/M1/N1, added a registered-keybind aggregation page to J1, corrected K1 with long-term MoreSaves manual evidence, clarified that L1 follows the full I1 retirement lifecycle rather than permanent retention, and made Y console explicitly never built into the base Runtime.

This round is deliberately coherent around three UI-heavy ownership problems. Animal-pack economy and short-SFX/BGM product boundaries move to the fifth round instead of mixing content/economy/audio research into UI decisions.

This is docs/source analysis. It does not implement any option.

## Closed Inputs, Not Fourth-Round Choices

- current Manager/config pagination exists and should be retained initially;
- official Mod enable/disable/order stays with Doloc Town/Steam;
- G1 local source override remains Author SDK state, not a Manager player toggle;
- Bootstrap should become a thin platform entry rather than a permanent owner of optional product UIs;
- MoreSaves does not own official save files; GameBridge/native owners remain responsible as recorded;
- 0.5.5 cannot delete `ISaveSlotsApi`, `IDebugConsoleApi`, or existing provider identities;
- UI ownership extraction and visual/interaction redesign use separate Updates;
- no source reduction or UI extraction is called an ISSUE-010 GC fix.

## J - Manager Audience And Navigation

Focused review: `docs/reviews/code/2026/20260713-0006-manager-player-information-boundary-review.md`.

Current pagination is real. Current audience separation is not: a default Config page can be mistaken for all detected Mods, technical Hook/Feature/path/install fields appear in ordinary pages, tooltips are not rendered, disabled Mods can make the whole product look unhealthy, and long rows are character-truncated without details.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| J1 - player center + advanced diagnostics | One title entry; default Mods page; ordinary `Mods / Settings / Help`; selected row gives friendly reason/action; raw ids/source/path/minimum/shadowed/hooks/features/errors live in Advanced. | One coherent support flow; requires a new internal player presentation model after Batch 2. |
| J2 - separate player and support UIs | Mods/settings and DTMAPI support use separate entry points/windows. | Strong audience split, but duplicate state/navigation and more title lifecycle. |
| J3 - polish current technical tabs | Keep current structure and only fix sizes/localization/colors. | Lowest cost, but preserves the ordinary-player mismatch. |

Final decision: **J1**, with the registered-keybind aggregation view recorded below.

J1 retains page buttons first. Layout uses common tokens, fixed columns, rendered-width ellipsis, detail/help, and visible config tooltips. It does not force every control to the same width. UI implementation follows Catalog/source/version authority and occurs as behavior-equivalent host extraction, then information/visual rewrite.

## K - MoreSaves 1.0.0 Scope

Focused review: `docs/reviews/api/2026/20260713-0002-saveslots-fixed12-product-boundary-review.md`.

The current product and GameBridge normalize every enabled request to twelve. The broad `SlotCount` DTO and retained >12 paging code do not make arbitrary counts a product contract.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| K1 - fixed 12 first | MoreSaves 1.0.0 fully validates slots 7-12; naming, configurable higher counts, and true scrolling become later 1.x projects. | Small trustworthy rebuild, later feature milestone. |
| K2 - all desired features in 1.0.0 | Naming, variable count, scroll UI, sidecar and all lifecycle paths block 1.0.0. | One complete launch, much larger save/UI/migration risk. |
| K3 - fixed 12 forever | Remove later naming/scroll goals. | Smallest, but contradicts stated direction. |

Final decision: **K1 revised**. Default twelve is a protected current product baseline backed by long-term player feedback; 1.0.0 converts that behavior into formal regression evidence and ownership separation rather than treating twelve slots as unfinished experimentation.

## L - Public SaveSlots Promise

| Option | Meaning | Trade-off |
| --- | --- | --- |
| L1 - fixed-12 compatibility facade | Preserve old ABI/provider in 0.5.5; freeze/obsolete `SlotCount`; new MoreSaves uses first-party internal capability; redesign public API only after a second real consumer. | Truthful and compatible with the smallest platform surface. |
| L2 - public 6/12 mode | Keep an Experimental ordinary API explicitly limited to vanilla/expanded mode. | Honest but permanently exposes a single-product abstraction. |
| L3 - arbitrary-count API now | Implement the apparent DTO promise, multi-owner policy, lifecycle and scrolling. | Becomes a broad save platform project. |

Final decision: **L1**, governed by the complete I1 lifecycle. The facade is frozen compatibility for 0.5.5, not a permanent endpoint; conditional removal may occur only after a published warning cycle, consumer rescan/migration, and an explicit breaking version.

## M - Y-Console Product Identity

Focused review: `docs/reviews/code/2026/20260713-0007-yconsole-bootstrap-product-boundary-review.md`.

Y console is already a published Diagnostic product, but its 2,432-line reflected UI is constructed and updated from Bootstrap for every Runtime.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| M1 - optional published Diagnostic product | Keep Workshop identity; product owns interaction/UI policy; optional first-party UI host loads on demand; old provider remains a lazy compatibility island. | Makes disable/unsubscribe real and removes product UI from the base path. |
| M2 - permanent built-in console | Absorb it into DTMAPI Runtime and sunset/reduce the Workshop product. | Keeps every player coupled to debug UI/actions and conflicts with lightweight ownership. |

Final decision: **M1**. Y console remains a published optional Diagnostic product and is never made a permanent base-Runtime feature.

## N - Y-Console Migration And Redesign

| Option | Meaning | Trade-off |
| --- | --- | --- |
| N1 - equivalent extraction, then rewrite | First remove Bootstrap ownership with behavior-equivalent optional host/compatibility; later rewrite UI and features independently. | Strong regression attribution and rollback. |
| N2 - extract and fully rewrite together | One large migration changes ownership, focus, input, EventSystem, lifecycle, layout and commands. | Fewer intermediate steps, much higher regression ambiguity. |
| N3 - redesign inside Bootstrap | Improve UX without moving product ownership. | Leaves the main boundary problem intact. |

Final decision: **N1**. Ownership extraction and the large UI rewrite are separate Updates.

## Final Answer Set

```text
J = J1  one player center with advanced diagnostics
K = K1  MoreSaves 1.0.0 verifies fixed twelve first
L = L1  old SaveSlots ABI becomes a fixed-12 compatibility facade
M = M1  Y console stays an optional published Diagnostic product
N = N1  behavior-equivalent Bootstrap extraction before a separate UI rewrite
```

## User Decision Resolution - 2026-07-13

This section preserves the supplied fourth-round J/K/L/M/N order.

### 1. J1 - Player Center, Advanced Diagnostics, And Registered Hotkeys

#### User-confirmed direction

The unified title entry becomes:

```text
Mods / Settings / Hotkeys / Help
```

It defaults to Mods. Normal player rows show name/version, Available/Not enabled/Missing prerequisite/Update DTMAPI/Restart required, settings availability, and a concrete next action. UniqueID, source/path/hash/minimum, shadowed sources, Hook/Feature, and raw errors move into selected-Mod advanced details or `Help -> Advanced diagnostics`.

The Hotkeys page aggregates only formally registered ConfigMenu keybind items:

| Column | Meaning |
| --- | --- |
| Mod | Owner display name. |
| Feature | Registered config item label. |
| Current key | The Mod's pending/current transaction value. |
| Default key | The registered first-party/author default when available. |
| Status | Normal, None, or conflict. |

It groups/searches by Mod, filters conflicts, edits/restores/sets `None` through the original Mod's config transaction, and never creates a second hotkey file or scans/hijacks unregistered keyboard input. First-party hard-coded hotkeys should migrate to formal registration.

#### Analysis immediately following issue 1

The aggregate page is a presentation over existing owner-bound ConfigMenu registration and transaction semantics, not a new global input owner. It must preserve per-page save/cancel/validation and disabled/locked state. Default-value display may consume the existing keybind-default capability internally; it does not justify a new public global-hotkey registry.

Existing pagination remains the first navigation baseline. Fixed columns, rendered-width ellipsis, detail/help, visible tooltips, and common layout tokens replace whole-row character truncation.

### 2. K1 Revised - Protect Existing MoreSaves Behavior

#### User-confirmed direction

Long-term player feedback confirms:

- slots 7-12 support create/save/title-return/reload;
- fresh restart recognition works;
- copy/delete work;
- disabling restores six slots without damaging extra saves;
- re-enabling restores extra saves;
- 12 and 16 slots have worked normally;
- 18 slots stack on one page and overflow the screen.

The durable manual-QA record is `docs/reviews/manual-qa/2026/20260713-0001-moresaves-long-term-player-baseline-review.md`.

#### Analysis immediately following issue 2

MoreSaves 1.0.0 keeps default twelve as protected behavior, separates product policy from GameBridge native adaptation, and converts accumulated manual behavior into automated/formal regression cases. It must no longer be described as an unproven twelve-slot experiment.

Sixteen-slot evidence indicates that native data capacity is not fixed at twelve. Eighteen-slot evidence points to the UI container/layout boundary. Neither fact commits 1.0.0 to arbitrary counts. Naming, configurable counts above twelve, real pagination/scrolling, and name identity/copy/delete/migration remain later features.

### 3. L1 - I1 Freeze, Warning, Then Conditional Removal

#### User-confirmed direction

In 0.5.5, `ISaveSlotsApi` and related DTO/provider identities become Deprecated/Frozen with `[Obsolete(..., false)]`, no new capabilities/templates/adopters, owner-scoped warnings, and old-DLL compatibility. New MoreSaves 1.0.0 uses a first-party internal archive-count/UI capability.

After at least one published warning-bearing release, removal at an explicit breaking boundary such as 0.6.0 additionally requires:

- completed MoreSaves migration;
- renewed Workshop/OfficialLocal/external consumer scan;
- no new real ordinary consumer or a concrete compatibility/migration plan;
- published migration guidance;
- old-DLL and new-product regression matrices.

If a second real author demand appears during the window, removal pauses and a narrow demand-driven API is redesigned.

#### Analysis immediately following issue 3

This matches the deprecated fishing lifecycle but has an independent migration target and consumer audit. “Useful first-party product” does not imply “permanent ordinary-author public API.” No physical deletion occurs in 0.5.5.

### 4. M1 - Y Console Is Never Built In

#### User-confirmed direction

Y console remains:

```text
PublishedProduct
ProductType = Diagnostic
DistributionState = PublicWorkshop
```

Its clearer graphical operations, unique actions, and DTMAPI development/support value justify the optional product, not permanent Runtime inclusion. With no subscribed/enabled Y product, the base path creates no console UI and enters no console per-frame update. The old API/provider remains a demand-lazy I1 compatibility island in 0.5.5.

#### Analysis immediately following issue 4

This makes Workshop disable/unsubscribe meaningful and keeps high-risk debug operations out of the ordinary platform promise. Overlap with the official console should guide later UX differentiation, not ownership reversal.

### 5. N1 - Equivalent Extraction Before Full Rewrite

#### User-confirmed direction

The first stage changes ownership only: remove Y UI from Bootstrap, add the optional product host, preserve old provider/DLL compatibility, and keep current hotkey/focus/same-key-close/right-click/modal/EventSystem/title/save behavior.

After complete regression, a separate project may rewrite navigation, search/filtering, command history/autocomplete, dedicated item/weather/teleport panels, ordinary-player guidance, and differentiation from the official console.

#### Analysis immediately following issue 5

The two stages may not share one Update. This preserves regression attribution and prevents product-host failures from being hidden inside visual redesign changes.

## Deferred Fifth Round

The next content/audio round will decide, with current preflight facts retained:

- whether the unified first-party animal pack initially contains the four proven JSON+PNG+WAV routes while ShellCrab remains separate and LightningChicken waits for a route rebuild;
- whether animal 1.0.0 starts from vanilla-comparable official-JSON economics before unique products/hidden drops;
- whether declarative `audio-replacements.json` becomes the short-SFX/AnimalVoice author route while the C# API remains an I1 compatibility surface;
- whether BGM is frozen as a separate future Wwise/native-lifecycle project rather than added to the short-SFX schema.

Asset provenance/permission, stable content IDs, old-pack migration/duplicate diagnostics, no per-frame audio/content scans, and BGM loop/stop/fade/callback lifecycle proof are engineering/release gates rather than optional product preferences.

The fifth-round O-T options continue in `docs/reviews/code/2026/20260713-0010-major-update-fifth-decision-docket.md`.

## Work-Order Effect

```text
Batch 0  freeze J-N directions, MoreSaves protected behavior, and compatibility surfaces
Batch 2  build actionable player state after Catalog/source/version authority
Batch 4  demand-activate SaveSlots and remove no-owner Y/Manager recurring work
Batch 5  MoreSaves and Y-console ownership rebuilds as separate products/Updates
UI A1    behavior-equivalent shared Manager-host extraction
UI A2    behavior-equivalent Y optional-host extraction
UI B1    player Manager information rewrite
UI B2    later Y-console UX rewrite
```

These choices do not block or move ahead of the current P0, Oil, 0.5.5 compatibility, Author SDK, QA extraction, or hot-path batches.

## Validation Boundary

This docket cross-checked the third-round feedback, current Manager/config UI source and historical UI records, SaveSlots native-owner review and source behavior, MoreSaves/Y-console products and Workshop identities, public API matrix, Bootstrap UI/update paths, and existing manual QA constraints. It did not launch the game, acquire the runtime lock, change public APIs/UI/products/packages, or mutate save/Workshop files.
