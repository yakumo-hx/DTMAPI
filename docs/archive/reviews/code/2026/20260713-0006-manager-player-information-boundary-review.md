# 20260713-0006 Manager Player Information Boundary Review

Status: recorded / J1 selected / implementation open
Date: 2026-07-13
Scope: DTMAPI title Manager/config shell audience, navigation, pagination facts, player versus support information, and Bootstrap UI ownership
Related decision docket: `docs/reviews/code/2026/20260713-0008-major-update-fourth-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0003-third-round-closure-fourth-decision-docket.md`

## Source Request

The user identified the current Mod-management/config UI as a secondary major-update problem: its information is not written for ordinary players, control/row lengths need a coherent layout, and paging/navigation need improvement. After closing the first three boundary rounds, the user asked to continue a fourth round.

This is a source/docs review. It does not change UI, public API, official enablement, layout, localization, package composition, or runtime behavior.

## Pagination Is Already Present

The older finding that Manager pages only show the first N rows has been partially superseded by current source. `ReflectedTitleMenuSettingsUi` now has explicit page indices, page bounds, Previous/Next buttons, range text, and page counts for:

- config Mod list: 14 per page;
- config items: 12 per page;
- Mod rows: 16 per page;
- errors and warnings: 7 each per page;
- hooks and features: 17 each per page.

The 2026-06-12 manual feedback also said configuration paging appeared usable. The fourth-round product problem is therefore not “add any pagination.” It is player information architecture, row/detail presentation, localization, consistent layout tokens, and later optional wheel/scroll behavior.

## Current Player-Facing Mismatch

Current rows are formatted as one long support string inside a roughly 930-pixel area and truncated by character counts such as 120/132/136/140. Character count is not rendered width: Chinese, English, ids, and paths consume different pixels. There is no selected-row detail panel.

The current ordinary title UI exposes support/developer concepts directly:

- overall Hook/Feature counts;
- `pathMatch`, `legacyMoved`, uninstall-helper and full local-path state;
- raw source/status/loaded/reason values;
- Unique IDs and technical provider/feature terminology;
- partially localized tabs/rows and English formatter output.

At the same time:

- configuration tooltips are stored by the config API but never rendered;
- a deliberately disabled Mod contributes to overall warning state even when no player action is required;
- optional/missing Hook and historical degraded Feature state can dominate the player summary;
- default opening on Config can make a player mistake “Mods which registered config pages” for “all discovered/loaded Mods.” This confusion was observed in prior manual feedback.

The actionable player state needed by the new compatibility model is also not present in the current Manager row: actual installed DTMAPI versus subscribed installer package, truthful Mod minimum, selected/shadowed sources, and update/restart next action. UI implementation must follow the Batch 0-2 Catalog/source/version authority rather than inventing these values locally.

## Ownership Map

| Layer | Current responsibility | Boundary judgment |
| --- | --- | --- |
| `DTMAPI.Abstractions/ConfigMenu.cs` | Author declaration contract. | Healthy; do not add player layout details. |
| `DTMAPI.ModConfigMenu` | Registry, page transactions, validation, keybind conflicts, save/reset/cancel, owner cleanup. | Healthy; do not add Unity rendering. |
| `DTMAPI.Core/Manager` | Diagnostic snapshot to support-oriented view model and row formatter. | Keep support model; add a separate internal actionable player presentation model after source/version authority. |
| `DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs` | 1,941 lines of reflected Unity construction, layout, paging, config controls, support Manager, clipboard, and localization. | Too heavy for a bootstrap entry owner. Extract an internal Doloc Town player-UI host or equivalent through a dedicated architecture Update. |

The shared DTMAPI Settings/Manager shell is a legitimate platform UI: every configurable Mod and the player update/support path use it. It is different from the optional Y-console product. The product decision is its audience structure, not whether DTMAPI should have any settings/status UI.

“Extract a UI host” does not by itself authorize a sixth player Runtime DLL. A physical assembly/package change must pass the dedicated architecture/release review required by the current five-DLL invariant; an equivalent internal composition split may be selected if it achieves thin Bootstrap ownership without changing the package shape.

## Enablement And Safety Constraints

These are not product options:

- official Workshop/OfficialLocal enablement and ordering stay with Doloc Town/Steam;
- the Manager may explain/open the official route but does not write official state;
- external BepInEx plugins remain read-only `external / not managed` rows;
- loaded CodeMod source/disable changes report restart-required rather than DLL hot unload;
- half-built `game/Mods` marker toggling is not exposed as a player feature during this redesign;
- G1 developer source overrides remain Author SDK state.

## Options

### J1 - one player center with advanced diagnostics

Use one title entry and shared shell. Default to the Mod list, not Config.

Normal navigation is approximately:

```text
Mods / Settings / Hotkeys / Help
```

The normal Mod row shows display name, product version, friendly player state, and whether settings are available. Selection shows the cause and next action. UniqueID, source/path/hash/minimum, selected/shadowed rows, raw status codes, Hook/Feature rows, and full diagnostics move under an Advanced details/support area. Ordinary support action is “export diagnostics,” not interpreting `pathMatch`.

Recommended.

The user selected J1 and added a Hotkeys aggregation view. It displays only ConfigMenu-registered keybind items, grouped/searchable by Mod with conflict filtering, current/default/status columns, and edit/default/`None` actions through the original Mod config transaction. It never owns a second keybind store or scans unregistered input.

### J2 - separate player and support entry points

One title entry owns Mods/settings and another owns DTMAPI support diagnostics.

This makes audiences explicit, but duplicates Mod state/navigation and makes an ordinary error flow cross two windows. It also adds title-page entry and lifecycle work.

### J3 - retain the current support console and polish visuals only

Keep seven technical tabs and current row model, then improve localization, sizes, and colors.

This is the smallest rewrite but does not solve the default-Config misunderstanding or technical information being presented as ordinary player state. It is not recommended as the formal endpoint.

## J1 Engineering Requirements

- retain the existing page model as the first reliable navigation implementation; wheel/ScrollRect is a later enhancement, not a prerequisite;
- use fixed columns, rendered-width ellipsis, and a detail/help region instead of character-count whole-row truncation;
- define shared layout tokens for margins, row heights, status badges, primary/secondary buttons, and pagers; different control types need coherent sizing, not identical width;
- display config tooltips through hover or the fixed help region;
- prevent section headings from becoming orphaned at the end of a config page;
- treat intentional `disabled` as neutral unless it creates an actionable dependency problem;
- map raw states to player language such as Available, Not enabled, Update DTMAPI, Missing prerequisite, Restart required, Old copy shadows update, and External plugin;
- first extract the UI host without behavior/interaction redesign, then implement the J1 information/visual rewrite in a separate Update.

## Sequence

```text
Batch 0  freeze J direction and player state vocabulary
Batch 2  complete Catalog/source/version/update authority and internal player model
UI A     behavior-equivalent Bootstrap UI-host extraction
UI B     J1 navigation, localization, detail/help, and layout rewrite
QA       1920x1080 + lower resolutions + long Chinese/English + 16+ Mods
         + 12+ config pages + errors/warnings + keyboard/mouse/controller
```

The Manager rewrite does not block the current ownership P0, Oil removal, 0.5.5 compatibility, Author SDK, QA extraction, or hot-path work.
