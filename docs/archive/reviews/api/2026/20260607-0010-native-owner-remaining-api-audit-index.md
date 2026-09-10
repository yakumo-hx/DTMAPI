# DTMAPI Native Owner Remaining API Closure Audit

Date: 2026-06-07
Record: 20260607-0010
Mode: docs-only code-level review

This pass closes the public API areas not already covered by:

- `20260607-0008-native-owner-special-audits`: `IInputHelper.Suppress`, `ICameraZoomApi`, `IMachineProductionApi`, and custom entity runtime verbs.
- `20260607-0009-native-owner-special-audits`: Save/Time/Teleport/Inventory/EquipmentSlots/Vehicle/Workshop/Content index.

This round only audits and records. Even where a severe gap is found, this record does not create an implementation goal and does not change runtime/API/mod/game/Workshop files.

## Volumes

| Volume | Area | Verdict |
| --- | --- | --- |
| 01 | [Framework/GameLoop/Event/Input remaining APIs](20260607-0010-native-owner-remaining-api-audit/01-framework-gameloop-event-input.md) | `OK/Watch`: DTMAPI runtime events and input polling are usable for ordinary mod lifecycle/hotkeys, but not native gameplay action interception. |
| 02 | [Config/ConfigMenu/Localization/Logging/ModRegistry](20260607-0010-native-owner-remaining-api-audit/02-config-localization-logging-registry.md) | `OK/Watch`: mostly DTMAPI-owned stable infrastructure; ConfigMenu remains title-UI experimental and ModRegistry is registry-only, not official mod-manager truth. |
| 03 | [DTMAPI UI/Diagnostics/Report APIs](20260607-0010-native-owner-remaining-api-audit/03-ui-diagnostics-report.md) | `OK/Watch`: DTMAPI UI and report helpers are ordinary-mod usable as DTMAPI surfaces, not as native Doloc Town UI/report ownership. |
| 04 | [Migrated gameplay APIs](20260607-0010-native-owner-remaining-api-audit/04-migrated-gameplay-apis.md) | `Partial/Watch`: ActionCompletion, ActionSpeed, Fishing reach important native owners but remain policy-driven adapters for DTMAPI migrated mods; Tooltip/AnimalViewer are display-only safer surfaces. |
| 05 | [Debug/Y Console remaining APIs](20260607-0010-native-owner-remaining-api-audit/05-debug-yconsole-remaining-apis.md) | `debug-only`: IDebugConsole, Weather, Movement, Mail, and AdvancedDebug are native-backed debug wrappers but must not be presented as ordinary gameplay APIs. |
| 06 | [0.3.0 Chest Locator Enhancer and Strong Planting Gun APIs](20260607-0010-native-owner-remaining-api-audit/06-030-chest-strongplanting-apis.md) | `experimental/internal`: both reach real native hooks, but DTMAPI owns fragile traversal, policy arbitration, and UI/storage expansion side effects. |
| 07 | [All API Risk Closure Table](20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md) | Cross-pass table integrating 0008, 0009, and 0010 into public/open, restricted, debug-only, registry-only, and rebuild categories. |

## Final Closure Table

| API / domain | Review source | Native owner verdict | Ordinary mod usability | Closure classification | Recommendation |
| --- | --- | --- | --- | --- | --- |
| `IGameLoopEvents.GameLaunched` | 0010-01 | DTMAPI runtime dispatch after mod load, no native owner required. | 普通 mod 可用 | stable 可开放 | Keep stable as DTMAPI lifecycle event, not native scene-ready guarantee. |
| `IGameLoopEvents.UpdateTicked` / `OneSecondUpdateTicked` | 0010-01 | BepInEx/DTMAPI update pump; GameBridge only sets hook status. | 普通 mod 可用 with caution | experimental 可开放 | Keep for lightweight mod polling; document overlay block and non-simulation-tick boundary. |
| `IGameLoopEvents.ReturnedToTitle` | 0010-01 | Partial native reach through `DolocAPI.ReturnHome` postfix. | 普通 mod 可用 for cleanup | experimental 可开放 | Keep experimental until all title-return paths are mapped. |
| `IInputEvents.ButtonPressed/ButtonReleased` and `IInputHelper.IsDown/WasPressed/RegisterButton` | 0010-01 | Unity key polling in bootstrap plus Core input state. | 普通 mod 可用 for hotkeys | experimental 可开放 | Keep hotkey-only wording; do not imply native action suppression. |
| `IInputHelper.Suppress` | 0008 | No consumer of the Core suppressed set. | 禁止依赖 | 必须降级或重做 | Rebuild with a native action-suppression adapter; ordinary mods can still trigger backpack/tool/item actions today. |
| Config helpers | 0010-02 | DTMAPI JSON files and migration callbacks. | 普通 mod 可用 | stable 可开放 | Keep stable. No native owner is expected. |
| ConfigMenu API and DTOs | 0010-02 | DTMAPI title-settings registry and reflected title UI. | 普通 mod 可用 with title-UI limits | experimental 可开放 | Keep experimental; document pending-preview setter side effects and title-only UI boundary. |
| Localization helpers | 0010-02 | DTMAPI `i18n` files with env/culture detection. | 普通 mod 可用 | experimental 可开放 | Keep experimental until language source is aligned with official settings. |
| Logging monitor | 0010-02 | DTMAPI latest log plus host logger. | 普通 mod 可用 | stable 可开放 | Keep stable. |
| ModRegistry | 0010-02 | DTMAPI loaded-mod/API dictionaries. | 普通 mod 可用 as registry-only | registry-only | Document that official enablement and runtime table loading are separate owners. |
| UI helper pages | 0010-03 | DTMAPI UI runtime, title settings UI, reflected overlay. | 普通 mod 可用 as DTMAPI UI | experimental 可开放 | Keep as DTMAPI overlay/status/config pages; not native menu replacement. |
| Diagnostics helper/report | 0010-03 | DTMAPI diagnostics service and report zip writer. | 普通 mod 可用 | stable 可开放 / experimental evidence helper | Keep stable for errors/hooks/log export; `RecordEvidence` remains experimental. |
| Workshop/content helper APIs | 0009 | Read-only scanner plus official reload hook. | 普通 mod 可用 as read-only metadata | experimental 可开放 | Keep read-only; indexed content is not runtime table proof. |
| Save lifecycle events | 0009 | Native save/load hooks reached. | 普通 mod 可用 with caution | experimental 可开放 | Keep lifecycle wording and document ordering limits. |
| InstantSave/Time/Teleport/Inventory debug APIs | 0009 | Native calls reached for debug flows. | debug-only | debug-only | Keep out of ordinary gameplay docs; direct dependence risks user surprise, scene residue, or progression bypass. |
| Mail delivery API | 0010-05 | `DolocAPI.SendItemAsEmail` reached, template behavior only. | debug-only / DTMAPI feature-only | debug-only | Do not present as stable quest/reward mail. Custom title/content/sender are not native-confirmed in current build. |
| Weather debug API | 0010-05 | `TbWeather`, `ArchiveDataHandle.SetWeather`, `PatchWeather` reached. | debug-only | debug-only | Keep debug-only; ordinary mods need a forecast/schedule abstraction. |
| Movement debug API | 0010-05 | `MotionAbility.SetMoveScaler` reached. | debug-only | debug-only | Keep debug-only; ordinary mods need effect-scoped movement modifiers with ownership stacking. |
| AdvancedDebug API | 0010-05 | Multiple native owners reached, plus broad creative Harmony bypass hooks. | debug-only | debug-only | Keep whitelisted Y-console surface only; no ordinary mod dependency. |
| ActionCompletion API | 0010-04 | Partial native resource/fuel/feed owners reached. | 仅 DTMAPI 自家 mod 可用 | experimental 可内用 | Keep restricted until action categories, save/time, and drop semantics become stable contracts. |
| ActionSpeed API | 0010-04 | Partial native animation/state owners reached. | 仅 DTMAPI 自家 mod 可用 | experimental 可内用 | Keep restricted because speed ownership is sidecar/global and can conflict between mods. |
| FishingAutomation API | 0010-04 | Native fishing wait/state/minigame owners reached. | 仅 DTMAPI 自家 mod 可用 | experimental 可内用 | Keep restricted; this is gameplay automation, not a stable fishing state machine API. |
| ItemTooltip fish-roe API | 0010-04 | Native item title/description/detail postfixes reached. | 普通 mod 可用 as display-only | experimental 可开放 | Document display-only behavior; no item data/economy mutation. |
| AnimalViewer API | 0010-04 | Native viewer/data/UI hooks reached for cloned display rows. | 普通 mod 可用 as display-only with caution | experimental 可开放 | Keep display-only; do not promise animal state mutation or produce ownership. |
| EquipmentSlots API | 0009 | Partial native stats/UI reached; DTMAPI owns sidecar slots. | 仅 DTMAPI 自家 mod 可用 | 必须降级或重做 | Split stable attribute adapter from experimental storage/UI. |
| Vehicle/Motor API | 0009 | Original motor partial, second motor DTMAPI clone around singleton native model. | 仅 DTMAPI 自家 mod 可用 / debug-only slices | 必须降级或重做 | Do not publish as general vehicle registry. |
| MachineProduction API | 0008 | Native recipe/tech/electric/storage slices reached; production loop sidecar. | 仅 DTMAPI 自家 mod 可用 | 必须降级或重做 | Split native content/table contract from runtime scheduler. |
| CameraZoom API | 0008 | Camera orthographic size only. | 仅 DTMAPI 自家 mod 可用 | 必须降级或重做 | Split camera size, background, fog, room render range, parallax, UI/input scaling. |
| ChestLocatorEnhancer API | 0010-06 | Native inventory-array postfix reached; DTMAPI owns shared-container traversal. | 仅 DTMAPI 自家 mod 可用 / restricted experimental | experimental 可内用 | Keep for official-local feature only until policy arbitration and room traversal are hardened. |
| StrongPlantingGun API | 0010-06 | Native farming gun use/UI hooks reached; DTMAPI expands storage and routes multi-slot use. | 仅 DTMAPI 自家 mod 可用 / restricted experimental | experimental 可内用 | Keep restricted until farming-gun storage, transfer, save/load, and multi-mod ownership are redesigned. |
| Custom entity stable registration contracts | 0008 | Core registry only; native runtime creation intentionally blocked. | 普通 mod 可用 only as registry/status metadata | registry-only | Split stable contract from blocked runtime adapters in docs. |
| Custom entity runtime verbs | 0008 | Native owners not reached. | 禁止依赖 | 必须降级或重做 | Keep spawn/summon/execute/equip/mode blocked until native adapters exist. |

## Completion Notes

- `public-api-matrix.md` rows already covered by 0008/0009 were imported rather than re-reviewed.
- 0010 adds code-level closure for the remaining matrix rows: framework/game loop/event/input, config/menu/localization/logging/registry, UI/diagnostics/report, migrated gameplay APIs, debug/Y-console remaining APIs, and 0.3.0 Chest Locator/Strong Planting Gun.
- No game smoke was run in this round. Existing smoke/update/hook records were read as evidence only.
