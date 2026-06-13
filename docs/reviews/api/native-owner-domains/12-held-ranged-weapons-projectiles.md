# 12 - Held Ranged Weapons Projectiles

Status: Partial for projectile/damage; Blocked for stable handheld ranged weapon
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Original game content that does not currently exist: handheld items and ranged weapons with held animation, flight animation, damage, and attachments.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Partial for player/tool textures | `006_*`, `018_*` | Existing player/tool visuals only |
| Base content mod | Yes for items and recipes | `046_*`, `049_*` | Data items, not arbitrary held weapon state |
| Advanced content mod | Partial for drone/resource/platform content | relevant content docs | Drone weapons are not player-held weapons |
| Runtime behavior mutation | Not public | No official handheld ranged weapon API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Held item/use route | `DolocAPI.SelectedItem`, `DolocAPI.QueryItemProto`, `DolocAPI.GenerateItem`, `AgentControllerState.UseTool`, `AgentControllerState.UseItem`, `BodyController.UseTool`, `BodyController.LoadCurrentTool`, `ToolRenderer` | `DolocAPI.cs`; `AgentControllerState.cs`; `BodyController.cs`; `ToolRenderer.cs`; `Items_Inventory.md` | Inventory selection, agent controller, body/tool renderers | Known tool/item use and held visuals | Medium/high known-family coupling | `ICustomItemDefinition`; experimental `IHeldItemVisual` | Partial |
| Throwable/missile-like items | `ItemMissile`, `ItemFunctionMissile`, `AgentSkillManager.UseSkillFromItemName`, `HandleBomb`, `HandleTimeBomb`, `HandleFireCracker` | `ItemMissile.cs`; `AgentSkillManager.cs`; `Config/Item/ItemFunctionMissile.cs` | Native missile item and skill manager | Existing bomb/time-bomb/firecracker archetypes | Medium name/archetype constraint | `ICustomUsableItemEffect` constrained to native archetypes | Partial |
| Projectile runtime | `BattleSystem.CreateBulletManager`, `BulletFactory.AddBulletManager`, `BulletManager.InvokeBullet`, `Bullet`, `BulletEntity`, `BulletMoverLinear`, `BulletMoverDrop`, `BulletMoverParabolic`, `BulletMoverRing`, `BulletMoverTracking`, `DolocBundleManager.bullets`, `bulletMovers` | `BattleSystem.cs`; `Bullet*.cs`; `DolocBundleManager.cs`; `Assets_Content.md` | Battle/bullet manager and bullet mover configs | Bullet pooling, proto selection, movement, hit callbacks | Medium for existing bullets; high for custom bullet DB injection | Partial/experimental GameBridge candidate for existing bullet ids | Found for projectile runtime |
| Projectile damage | `BattleUtils.OnBulletHitEnemy`, `BattleUtils.OnBulletHitPlayer`, `BattleUtils.CalcDamage`, `Monster.Damage`, `BodyController.OnAttacked`, `PhysicalDamageBox` | damage/battle classes | Battle utility and target damage methods | Damage calculation and application to targets | Medium/high faction/collider risk | Partial/experimental `DamageProfile`, `ProjectileHitPolicy` | Partial |
| Drone weapons/attachments as reference | `Drone`, `DroneController.SetDroneFollower`, `DroneStruct.GetWeaponProto`, `DroneWeapon.CreateWeapon`, `CreateGun`, `DroneWeaponGun`, `DroneWeaponGunDragon`, `DroneWeaponInfo`, `DronePatch.GetDroneWeaponParams`, `DroneChipInfo`, `DroneSlotInfo`, `ItemDroneWeapon` | drone classes/config | Native drone equipment system | Drone weapons, bullets, chips/slots | Medium/high but drone-specific | `ICustomDroneWeaponDefinition` registry-only | Not handheld owner |
| Player-held ranged weapon with attachments | No stable owner found | searched item/tool/drone/bullet/monster systems | None | No general player-held ranged weapon state found | Blocking | DTMAPI-owned experimental bridge around held visual plus projectile spawner | Blocked |

## Rejected Terms For Player-Held Weapon Evidence

| Term family | Reason rejected |
| --- | --- |
| `NormalFire`, `NormalFireInProgress`, `NormalSwitchAutoFire`, `GunReloadTip`, drone gun UI | Drone weapon/input/UI vocabulary, not player handheld weapon ownership. |
| `ItemFarmingGun` | Farming/tool-specific path, not combat ranged weapon proof. |
| `DroneWeaponInfo`, `WeaponFunctionGun*`, `DroneWeaponGun*`, drone slots/chips | Native drone weapon/equipment system only. |
| `WeaponDebuggerSO`, debug assets | Debug/editor evidence, not stable runtime API. |
| `MonsterAttackBehaviourBullet*` | Monster AI reference path, not player-held weapon ownership. |

## API Translation Notes

- Existing projectile flight/damage can support only experimental GameBridge study of known bullet ids after collision, target, room-transition, and cleanup policy.
- Drone weapon attachments are drone-slot/chip owned and must not be presented as handheld weapon attachments.
- Handheld ranged weapon authoring needs a new DTMAPI-owned GameBridge design for input, held animation, fire cadence, projectile spawn, damage owner, attachments, save/load, and cleanup.
- Player-held ranged weapon, held/fire animation, attachments, and custom bullet/proto injection are blocked for stable API until native load-time table behavior and GameBridge cleanup evidence exist.
- Round 3 confidence: projectile runtime owner 92; damage/projectile study path 82; no stable native player-held ranged weapon owner 84; drone weapon/UI terms not handheld 91; `ItemFarmingGun` farming-specific 88; custom bullet/proto injection not stable 86; stable public handheld ranged weapon API blocked 89.

## Blockers And Follow-Up

- No native general handheld ranged weapon owner was found.
- Custom projectile definitions require proving bullet/bulletMover asset DB injection.
- Damage policy must cover target filtering, friendly fire, save/transition cleanup, and multi-mod ownership.

## Evidence Checked

Maps: `Action_Interaction.md`, `Items_Inventory.md`, `Resource_Gathering.md`, `Assets_Content.md`, `UI.md`, `NPC_Dialogue.md`, `Motor.md`.
Classes/symbols: `DolocAPI`, `AgentControllerState`, `BodyController`, `ToolRenderer`, `ItemMissile`, `ItemFunctionMissile`, `AgentSkillManager`, `BattleSystem`, `BulletFactory`, `BulletManager`, `Bullet*`, `BattleUtils`, `Monster`, `PhysicalDamageBox`, `DroneWeapon*`, `DroneSlotInfo`.
