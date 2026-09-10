# 20260607-0008 - DTMAPI Native Owner Special Audits

Status: complete
Scope: docs-only code-level audit for the four highest-risk API domains called out by the 0007 review.
Rule: this round only reviews and records. It does not modify runtime, public API, mods, game files, Workshop files, or official/decompiled source, and it does not create an implementation goal.

## Purpose

The 0007 review established broad native-responsibility risk. This 0008 review checks whether the highest-risk conclusions survive code-level reading of the public interface, Core/GameBridge implementation, callers, callees, hook/update/debug evidence, and reverse/research native-owner candidates.

This is not a coverage task and intentionally avoids a full symbol inventory. Each volume focuses on call paths and failure modes that decide whether ordinary mods can safely depend on the API today.

## Volumes

| Volume | Area | Verdict |
| --- | --- | --- |
| 01 | [IInputHelper.Suppress](20260607-0008-native-owner-special-audits/01-input-helper-suppress.md) | `Blocked`: Core stores a suppressed set, but no input sampler, runtime event path, or GameBridge hook consumes it. |
| 02 | [ICameraZoomApi](20260607-0008-native-owner-special-audits/02-camera-zoom.md) | `Partial`: the API reaches `DolocAPI.mainCamera.orthographicSize`, but not the background/fog/room/camera-controller owners required for stable large view. |
| 03 | [IMachineProductionApi](20260607-0008-native-owner-special-audits/03-machine-production.md) | `Partial/Gap`: recipe, tech, electric, inventory, and preview slices are native-backed; production scheduling and fuel/output state are DTMAPI sidecar loop state. |
| 04 | [CustomEntity runtime verbs](20260607-0008-native-owner-special-audits/04-custom-entity-runtime-verbs.md) | `Blocked`: registration/status are Core registry contracts; spawn/summon/execute/equip/mode verbs return `runtime-creation-blocked` before any native runtime object exists. |

## Final Decision Table

| API / Field Group | Result | Native owner reached | Ordinary mod usability | Recommendation |
| --- | --- | --- | --- | --- |
| `IInputHelper.Suppress(string)` | Blocked | None. `InputService.suppressed` is Core-only state with no consumer. Y-console isolation reaches native `AgentControllerState` methods, but through `DebugConsoleModalOpen`, not through `Suppress`. | 禁止依赖 | Rename as non-native metadata or implement a real action-suppression adapter. Ordinary mods calling `Suppress("B")` can still leak backpack/tool/item actions because no native input owner reads the set. |
| `ICameraZoomApi.Register/SetViewScale/ResetViewScale` | Gap | Partial: `DolocAPI.mainCamera.orthographicSize` / `UnityEngine.Camera.main.orthographicSize`. | 仅 DTMAPI 自家 mod 可用 | Split camera size, background scale, depth fog, room render range/scanner, parallax, and UI/input coordinate owners. Ordinary mods risk the known background-small-frame failure and room/input mismatch. |
| `IMachineProductionApi.RegisterMachine/GetState` | Gap | Partial: `TbRecipe`, tech-tree/TbTechNode, `IElectronicComponent.Launch()`, `LinearInventory.PlaceItemAt`, and visual-preview hooks. Production loop owner remains DTMAPI. | 仅 DTMAPI 自家 mod 可用 | Split native content/table registration from the experimental DTMAPI runtime loop. Ordinary mods risk sidecar state loss, cross-room miss, save/sleep time drift, shared-table pollution, and visual scale contamination. |
| `ICustomAnimalApi.RequestSpawn` | Blocked | Intended `AnimalManager`/animal lifecycle owners are not connected. | 禁止依赖 | Keep stable registry wording separate from runtime creation. Ordinary mods get no native animal, room/home/feed/excrement/breeding/produce/save state. |
| `ICustomMonsterApi.RequestSpawn` / `RegisterSpawnTable` | Blocked/Gap | Intended `MonsterController`, spawn group, AI, attack, damage, and drop owners are not connected. | 禁止依赖 for runtime verbs; registry metadata only is ordinary-mod usable when documented as registry-only | Do not describe spawn tables as native room spawn-table mutation. Ordinary mods get no monster instance, AI, combat, loot, or save lifecycle. |
| `ICustomAttackApi.SpawnProjectile/ExecuteAttack` | Blocked | Intended `BulletFactory`, `BulletManager`, hitbox/collision/damage owners are not connected. | 禁止依赖 | Treat attack definitions as registry contract only. Ordinary mods get no projectile, barrage, collision, damage, or expiry runtime. |
| `ICustomDroneApi.RequestSummon/Equip/SetMode` | Blocked | Intended `DroneController`, `DroneWeapon`, equipment/movement/save owners are not connected. | 禁止依赖 | Runtime adapter must be built before public docs imply summon/equip/mode support. Ordinary mods mutate no native drone equipment or mode state. |
| CustomEntity DTO/result fields: `Succeeded`, `FailureReason`, `RuntimeStatus`, `Handle`, `Snapshot`, `SpawnRules`, `Summon`, `Execute`, `ActiveRuntimeInstanceCount`, `SaveStateRecordCount` | Watch/semantic risk | DTMAPI Core registry/status only unless a runtime verb explicitly returns blocked. | 普通 mod 可用 only as registry/status metadata; 禁止依赖 as native runtime proof | Document these as DTMAPI registry/status fields, not evidence that native runtime exists. Current blocked results deliberately set `Succeeded=false` and `RuntimeStatus=RuntimeCreationBlocked`. |

## Evidence Rules Applied

- Code paths are cited by path and line number.
- Existing game smoke evidence was read, but no new game smoke was run.
- Reverse/decompiled material was used only as local research/candidate naming. This review does not copy or distribute decompiled source or official DLL content.
- Native-owner uncertainty is recorded as searched candidates plus excluded paths, not as a generic "needs more validation."

## No Implementation Goal

This review intentionally stops at documentation. The next implementation goal, if any, must be requested separately by the user after choosing which owner map to rebuild first.
