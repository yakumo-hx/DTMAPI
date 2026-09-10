# Y 键控制台语义目录、ProductNative 召唤与技术债复核

- Review ID: `20260812-0001`
- Date: `2026-08-12`
- Status: `recorded`
- Scope: Y-console semantic catalog, teleport/weather stability, ProductNative monster/animal spawning, responsive UI, localization, fuel content and recurring-work cost
- Source: user comparison screenshots and the approved Y-console 1.1.0 implementation plan
- Owning Update: [20260812-0001-y-console-semantic-ui-productnative](../../../updates/2026/20260812-0001-y-console-semantic-ui-productnative.md)

This Review freezes the user-visible observations, exact semantic choices and native-owner findings before implementation. Implementation and validation facts belong to the owning Update.

## 1. The current console is not yet a stable semantic directory

### Feedback and screenshot facts

- The reference T console groups item classes into a compact semantic directory and keeps world actions in stable positions.
- The current Y console exposes a fixed `1500x900` panel, raw source/subtype-shaped category lists, a dynamic weather row, generated teleport rows and incomplete English-heavy Advanced buttons.
- The requested first release uses the native ten main item categories, followed by virtual Monster and Animal categories. Subtype remains search/detail metadata rather than primary navigation.

### Code facts and root cause

- `DebugConsoleUi` treats raw subtype identities as categories, rebuilds the complete screen for most state changes and creates callbacks/reflection delegates while building item cells.
- Item data, display projection, input bindings and layout are coupled inside one large reflected-Unity class. The coupling makes semantic changes expensive and causes avoidable object/listener churn.
- The correct ProductNative boundary is one stable catalog model with locale-dependent projections, pooled cells and region dirty flags. It does not require a new public UI API.

### Acceptance boundary

- Category order is exactly `tool, material, farm, husbandry, product, food, kit, equipment, construction, special, Monster, Animal`.
- “All items” includes the ten item categories only. Monster and Animal use the same search/icon/page surface but never mix with inventory results.
- Left click means one and right click means ten for item, monster and animal cards.

## 2. Teleport must use an exact semantic registry

### Feedback

Use the native thirteen transport stations, then only explicitly mapped landmarks. Deduplicate by exact `MarkPointId`, show a disabled reason when the native target is unavailable, and remove substring or coordinate guessing.

### Exact landmark correction

`鹿神池塘` is in the suburban deep woodland and maps to `林地深处-右端`. `后山悬崖-奥兰多` is the separate 庞卡雕像 location and must not appear as 鹿神池塘 or as an additional target.

### Root cause and rejected shortcuts

- The current discovery/export route scans native labels and can project unstable or internal names.
- A substring match is not a semantic identity and can silently retarget after game content changes.
- Old-city beds have no exact bed-specific MarkPoint and therefore remain excluded. Raw coordinates, duplicated valley outpost rows and arbitrary scene-boundary marks are also excluded.

### Acceptance boundary

The product owns one ordered, translated `display key -> exact MarkPointId` registry shared by the UI, execution and CSV export. Exact-id lookup failure is visible and fail-closed.

## 3. Weather controls must not move when weather state changes

### Feedback

All seven real weathers fit in one stable control set. Current and forecast status may change highlight/badges, but must not sort, remove or paginate buttons.

### Code facts and root cause

The UI currently reads weather state and available weather data separately, then sorts current/forecast values ahead of other rows. This duplicates table work and makes action location state-dependent.

### Acceptance boundary

One `WeatherPanelSnapshot` projects IDs `1..7` in the fixed order 晴、多云、雨、雷雨、大风、酸雨、烈日. Current and forecast badges can coexist; unavailable native rows stay in place and are disabled.

## 4. Monster and animal creation belong to their native hosts

### Monster findings

- The existing executor relies on the obsolete two-argument `DolocAPI.Command_GenerateMonster` shape.
- Current build `24650773` exposes `IMonsterHost.GenerateMonster(MonsterProto, Vector2, bool)`, `MonsterCount` and `RemoveMonster`.
- The relevant `IMonsterHost` and `MonsterProto` source bytes are identical to the admitted `24456188` policy build.

### Animal findings

- Current `IAnimalHost` owns capacity, walkability, `DM_animal`, global `AnimalSystem`, creation and removal.
- The four native species are `slime`, `chicken`, `goat` and `marsh_pangolin`.
- `DEBUG_SetAdult` switches the native mature state/renderer. `DEBUG_SetHusbandryValue` writes the native husbandry contribution map; thresholds must be read from `TbHusbandry`, not copied into product constants.
- The relevant host, entity, proto and husbandry sources are byte-identical between builds `24456188` and `24650773`, so the admitted policy remains valid.

### Transaction boundary

- Monster creation uses exact `DolocAPI.AgentPosition`; animal creation uses deterministic valid cells around `AgentRoomCellPosition`.
- Every batch validates returned entities plus host/global manager counts. Any partial failure rolls back the whole batch through the native host.
- A failed rollback disables further batch mutation for the active save and records a hard diagnostic. No product sidecar or immediate save is introduced.

## 5. Responsive layout and localization are platform/product responsibilities

### Code facts

- The current CanvasScaler remains in constant-pixel mode and the panel is hard-coded to `1500x900`, making 4K extremely small and low resolutions clip.
- Runtime `TranslationService` normalizes only English versus Simplified Chinese and fixes its two dictionaries at construction time.
- DebugConsole adds a second language selection/config layer, so game language does not own the final product locale.

### Decision boundary

- Runtime resolves full locale, then base language, then English; GameBridge supplies cached `DolocAPI.CurrentL10nId` without adding a public API.
- The product removes its language config and supplies all nine current game languages. Native content names remain native-localized; semantic labels use translation keys.
- UI uses `Scale With Screen Size`, safe area, responsive breakpoints, wrapping and a physical 44-pixel minimum target.

## 6. “Creative generator” is an official-content fuel item, not a generator action

### Native/content facts

- `Content/item_tbitem.json` is the product's official `TbItem` sidecar route.
- Native combustible machines accept ordinary items with positive `ElectricEnergy`; the three current rates/efficiencies remain finite at a one-billion input.
- The ID `dtmapi_creative_generator` is already save-visible and must remain stable.

### Decision boundary

Rename the localized item to “无限燃料”, retain the coal sprite and ordinary `ItemFunction`, set `electric_energy=1000000000`, add all nine native localization sidecars, and expose it only through the item browser. The frozen compatibility method remains binary-compatible but has no UI button.

## 7. Recurring work and dead-code audit

### Findings

- `ModEntry` subscribes `UpdateTicked` for the entire loaded session even though UI work is needed only while open/input-draining and action work only while a movement lease is active.
- `ApplyVisibilityState` initializes the Canvas before checking closed state.
- Full rebuilds repeatedly read catalogs, reflect members, compile delegates and create grid/event objects.
- `AddPointerClickListener`, `CreateRow`, `TryGetScreenSize`, private first-item Advanced handlers, duplicate `modItemsOnly`, product language fields and duplicate weather reads are removable.
- The title displays Runtime API version as though it were the product version.

### Acceptance boundary

The closed idle product has no update subscription and creates no Canvas. A movement lease may keep only the native action updater alive. Input release/drain retains the verified ISSUE-014 safeguards. Cell/listener counts remain stable after pool warm-up.

## Rejected shortcuts

- Do not move ProductNative monster/animal state into mandatory Runtime or a new public GameBridge surface.
- Do not refresh the Advanced reference policy merely because the observed game build number changed.
- Do not replace native animal state with fabricated drops, custom ages or product persistence.
- Do not retain a hidden language override or restore the unfinished Generator/Monster/Resource quick buttons.
- Do not claim save semantics from direct callback invocation or post-run restoration of a live Steam save.

## Resolution

Implementation and evidence are owned by [Update 20260812-0001](../../../updates/2026/20260812-0001-y-console-semantic-ui-productnative.md). The Update remains `implemented` after author validation until independent player acceptance completes.
