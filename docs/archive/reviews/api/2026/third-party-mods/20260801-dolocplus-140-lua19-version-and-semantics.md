# DolocPlus 1.4.0 / Lua CT 1.9 Version And Semantic Compatibility Review

Status: `recorded`

Date: 2026-08-01

Last updated: 2026-08-02

Scope: read-only compatibility study of the current local Qiuzy / 小神增强包 samples, DTMAPI's present Advanced version gate, DolocPlus 1.4.0's player-facing version behavior, Lua CT 1.9's target-resolution behavior, and same-semantic lessons for DTMAPI 0.6.0. This Review is not implementation authority and does not admit or redistribute any third-party binary or source.

## Source Request And Boundary

The user asked whether DTMAPI's exact game-version policy rejects even a small update and therefore forces every Mod to be rebuilt, asked to consolidate the four updated Qiuzy packages in the existing workspace reference area, and authorized local decompilation for compatibility research.

The four source items were copied, not moved, from `D:\下载` into the ignored local reference batch:

`references/third-party-mods/小神增强包/20260801-v1.4.0-ct1.9`

The pre-existing 1.3.2 / 1.8.1 samples under `references/third-party-mods/小神增强包` were not overwritten. The new batch and its `_analysis` directory remain ignored reference material. No third-party binary, archive, Cheat Engine table, or decompiled body is added to Git, DTMAPI Runtime, a managed Mod package, or a release package.

## Copy Receipt

Every source and destination file was compared by relative path, byte length, and SHA-256. All comparisons were exact.

| Copied item | Evidence | Result |
| --- | --- | --- |
| `DolocTownEA_BepInEx_DolocPlusMod` | 6 files, 11,450,207 bytes; includes `Plugins.zip` SHA-256 `DC1701FB7A3A6AD93E97AAD03D67114006BC35E143816D25304A0F31B757DCFC` | exact |
| `DolocTownEA_Lua_CT_1.9` | 1 file, 290,720 bytes; CT SHA-256 `9350C937A6BBA3EFE69A46605424FE37F3B316D8B3D8F8E3CAB2CD372464003C` | exact |
| `DolocTownEA_BepInEx_DolocPlusMod.7z` | 11,369,633 bytes; SHA-256 `AE09C98082E145D1FA560FFAA8FAD57F0ACEA53389EDF73ADC1E1A34051DCD14` | exact |
| `DolocTownEA_Lua_CT_1.9.7z` | 46,018 bytes; SHA-256 `6DCA52C91C95A69A4EB01C3699E743A017C85DCFA82EB19FFC407AFBF5F93DEA` | exact |

The extracted `DolocPlus.dll` identifies itself as 1.4.0, is 129,536 bytes, and has SHA-256 `BEAA313EFC99BC5ADDCC76C8220BFE19CDB479BF9F843005B3F592CB0E056EA1`. It was decompiled locally with ILSpy 9.1.0.7988 for this audit only.

## Direct Answer: Current DTMAPI Policy Is Too Coarse For Routine Updates

The present Advanced classifier verifies all of the following before `Assembly.LoadFrom`:

- Runtime-embedded policy ID, version, and policy hash;
- exact product `UniqueID` binding;
- exact receipt and package-marker bindings;
- exact entry DLL length and SHA-256;
- exact native-reference path, assembly name, byte length, SHA-256, and `copyLocal=false` rows;
- exact `Assembly-CSharp.dll` identity;
- exact Steam build ID.

Consequences:

1. A one-byte change to `Assembly-CSharp.dll` rejects the Advanced product before its Entry loads.
2. A Steam build-ID change also rejects it even if the tracked managed assembly bytes happen to remain identical.
3. There is no current warning-and-continue or user override path.
4. Every newly accepted game build therefore requires a new tracked policy and SDK-generated receipt/package in the current workflow.

That does **not** mean every update necessarily requires source edits. It means every accepted update currently requires re-attestation and reissuing the package. Source changes and a true recompilation are required only when the product's referenced surface, Hook target, reflection target, or native behavior contract changed; however, the current SDK release path normally rebuilds while generating that new package.

The 1.00.00 root-cause review shows why the exact guard was useful but also why it is over-broad: all nine published Advanced products were rejected by the same old whole-assembly identity before Entry, while focused inspection found source changes definitely required in only a subset. See [Doloc Town 1.00.00 / DTMAPI 0.6.0 Compatibility Root-Cause Review](../../../code/2026/20260801-0003-doloctown-100-dtmapi-060-compatibility-root-cause.md).

## Recommended 0.6.0 Compatibility Decision

Do not replace the present guard with an unconditional global “version mismatch, continue anyway.” DolocPlus 1.4.0 itself demonstrates why: most targets survive the current formal build, but several surviving features still bind removed state owners or missing methods. A warning alone cannot prove save or gameplay safety.

For 0.6.0, separate **identity evidence** from the **load decision**:

| Tier | Condition | Player behavior | Rebuild implication |
| --- | --- | --- | --- |
| Verified exact | Existing exact build/policy match | load normally | none |
| Compatible drift | Build or whole-assembly hash changed, but this exact product/DLL passes its required capability contract for the installed build | show one compatibility notice, then load; report capability status | no source rebuild; compatibility attestation/package metadata may be refreshed independently |
| Partial drift | Required product capability fails but optional capabilities survive | load the product shell only when it can disable the affected feature cleanly; show exact disabled features | change only affected feature before full verification |
| Unknown unsafe | No adequate product preflight exists, or a save/native invariant cannot be checked safely | do not silently auto-load; an explicit one-session developer override may be offered with a strong warning | audit or product update required for normal verified use |

The minimum Advanced capability contract should be product-specific and distinguish required from optional dependencies:

- direct native type/member references;
- exact Harmony owner, method name, parameter and return shapes;
- reflection field/property/method shapes;
- state-owner and lifecycle invariants where an unchanged signature is insufficient;
- a feature-local failure action and cleanup route.

This makes the exact build/hash an audit identity rather than the sole compatibility oracle. It also makes the humane case concrete: a tiny update whose relevant product capabilities still pass should produce a notice and continue, not force unrelated Mod source changes. A compatibility record can bind the already shipped entry-DLL hash to an additional audited game identity; rebuilding every unchanged Mod DLL should not be the only way to express that fact.

Strict CodeMods that consume only stable DTMAPI public APIs should remain insulated from this Advanced native-reference gate. DTMAPI Core/GameBridge owns their game-version adaptation.

## DolocPlus 1.4.0 Version Strategy

DolocPlus 1.4.0 declares game compatibility `0.96.06` and parses `Application.version` at plugin startup. It creates a warning controller only when:

- the running game's major version is greater; or
- the major version is equal and the running game's minor version is greater.

It does not compare the revision/build component. Therefore `0.96.07` and `0.96.99` do not trigger the warning, while `1.00.00` does.

Most importantly, the warning path does **not** return, disable the plugin, or skip feature initialization. All 26 ordinary controllers are initialized after the warning controller. At the home page, the warning logs current and expected versions, waits two seconds, and shows a dialog that recommends backing up saves, exiting without saving if a serious bug appears, uninstalling, or waiting for an update.

The remembered behavior “version mismatch shows a popup and does not load” is therefore not true for the inspected 1.4.0 DLL. Its behavior is **popup plus continue**.

Version 1.3.2 compared only `Application.version.Minor` with compatible version `0.95.12`. That would miss a transition from 0.x to 1.00 because `1.00` has minor component zero. Version 1.4.0 fixes that major-version hole, but its compatibility test remains deliberately coarse. Parsing is also unguarded, so a future non-numeric/pre-release `Application.version` string could fail plugin startup before the dialog.

### Feature Failure Isolation

Each patch feature owns a separate Harmony ID, defaults to disabled on a fresh config, patches when its config toggle turns on, and unpatches itself on disable or application quit. The shared framework also has a conflict map and input-isolation helpers.

This is useful feature-local lifecycle discipline, but it is not a complete health model. If `PatchAll` throws, the controller logs the error and returns while leaving the config toggle enabled. It does not publish a failed/disabled health state or reconcile the toggle. A 0.6.0 DTMAPI product should disable only the affected feature and expose the failure reason visibly.

## Lua CT 1.9 Version Strategy

Lua CT 1.9 records trainer version 1.9, a creation target of game `0.96.06` Early Access, and a last-update date of 2026-06-11. Its game compatibility gate does not compare `Application.version`.

Instead, initialization attempts to resolve two static-field handle groups and 46 method handles by Mono type/member identity and, for some entries, method offsets or byte-pattern scans. `GameData:CheckStatus` weights the result by the feature Entry IDs attached to each handle:

- failure ratio `< 50%`: root status is usable and initialization continues;
- failure ratio `>= 50%`: it displays an error and deactivates root Entry 9.

The direction matters: fewer than half the weighted targets failing is accepted; half or more is rejected. Individual entries can still be unavailable or fail separately. The table also restores original method bytes/handles during disposal.

This is closer to capability probing and partial degradation than DTMAPI's whole-assembly hash veto, but 50% is a coarse global threshold. Passing 51% of weighted entries does not prove that the remaining method bodies still mean the same thing. Its separate online trainer-version check is an update notification for CT version 1.9, not a game-build compatibility proof.

## DolocPlus 1.4.0 Against Current 1.00.00 Metadata

The DLL was inspected read-only against both the old 23762374 baseline and the current `24456188_test_E861E0` 1.00.00 baseline. This is static evidence, not runtime acceptance.

| Surface | Inspected count | Old baseline | Current baseline |
| --- | ---: | ---: | ---: |
| direct `Assembly-CSharp` type references | 186 | 0 unresolved | 0 unresolved |
| direct `Assembly-CSharp` member references | 313 | 0 unresolved | 3 unresolved |
| constant `AccessTools` lookups | 58 total; 56 target the game assembly | game targets resolve | all 56 game targets resolve |
| Harmony target specifications | 65 | all resolve | 3 missing |

Only six deterministic symbol/target breaks were found across those direct surfaces:

| Removed/changed target | Affected DolocPlus feature | Current semantic owner or consequence |
| --- | --- | --- |
| `TimeArchiveData.weather` | Fish Analyzer | weather is now group/room scoped; use the reviewed local-weather projection rather than a global field |
| `DolocPatch.IsNullOrEmpty(string)` | NPC Contacts | the game extension disappeared; ordinary string emptiness semantics remain available outside that removed extension |
| `AgentPhysicalStatus.HorizontalMoveFactor` | AFK Fishing | movement now lives under `MoveModifier.inputMultiplier` and related setters/clearers; conveyor/offset motion also exists |
| `FishTankElectricEel.Update()` | Automatic Tank Collecting | electric-eel tank behavior was redesigned and no longer exposes the old update hook |
| `FishTankElectricEel.UpdateNoRender()` | Automatic Tank Collecting | same redesign; the feature's patch set cannot be treated as wholly compatible |
| `Synthesizer.GetMaxCraftCount(IRecipe)` | Unlimited Bulk Production | current shape adds a `bool useBuffer` argument, alongside another overload |

Likely player effects from static inspection:

- AFK Fishing's Harmony targets still exist, but its movement-cancel path directly reads the removed property. This is the same owner migration found independently in DTMAPI AutoFishing.
- Fish Analyzer can fail when it reads the removed global weather field. The correct current behavior must use the active room/season group's weather.
- NPC Contacts has a narrow removed helper call rather than a broad feature redesign.
- Automatic Tank Collecting patches a class containing both removed electric-eel targets; enabling that controller is likely to fail at patch application rather than cleanly retaining only the other tank hooks.
- Unlimited Bulk Production likewise has a missing target within its feature patch class and is likely to fail that feature's `PatchAll`.

This is the central compatibility lesson: the formal build is not “everything broken,” but neither is “most symbols still exist” enough to declare the whole Mod safe. A per-feature capability result would be more useful than both a whole-DLL veto and a single global warning.

## 1.3.2 To 1.4.0 Functional Delta

The controller set remains 26 features. Meaningful changes include:

- plugin version `1.3.2 -> 1.4.0` and declared compatible game `0.95.12 -> 0.96.06`;
- the corrected major/minor warning comparison described above;
- AFK Fishing now requires normal gameplay state and idle state before recasting, checks native fishing energy first, stops when energy is insufficient, and uses the named fishing-note enum rather than a raw numeric discriminator;
- Panorama Camera adds a first-use warning and a feature-local high-resolution screenshot patch that scales the requested capture resolution by the room-fit factor, then removes that patch on exit;
- other changes include adaptations to the 0.96.06 native surface plus decompiler-output differences; line-count churn alone is not treated as a semantic change.

The AFK Fishing additions are especially relevant to DTMAPI: preflight native energy and game/UI state before driving the state machine, and make stop/restore behavior explicit.

## Focused Same-Semantic Implementation Comparison

The user selected six behaviors for a deeper comparison on 2026-08-02. The comparison below follows `trigger -> native owner -> mutation -> restore/failure path -> current-build compatibility`. It describes behavior and member identity only; no third-party method body is copied into this Review.

| Requested comparison | Semantic relationship | Current 1.00.00 static result | 0.6.0 consequence |
| --- | --- | --- | --- |
| DolocPlus AFK Fishing vs AutoFishing | Same native fishing lifecycle, but different session and success policy | both still find the fishing states; both reference the removed `HorizontalMoveFactor` movement projection | migrate AutoFishing's movement owner; keep DTMAPI's transactional lifecycle rather than porting the compact patch set |
| DolocPlus Time Speed vs DebugConsole time scale | Similar player label, different game meaning | both native targets remain available | keep them separate; a game-clock cadence feature would be a new ProductNative behavior, not a replacement for global simulation scale |
| DolocPlus Located Chests vs ChestLocatorEnhancer | Same locator/shared-container eligibility and cross-room equipment-consumption goal | the relevant inventory and room surfaces remain available | retain DTMAPI traversal/cleanup, but narrow or explicitly approve its currently broader caller scope |
| CT Easy Fishing vs AutoFishing instant bite/skip | Similar quick-success outcome, different transition route | named types/fields still appear, but CT's machine-code hook remains runtime-unverified | no direct port is required; preserve native transition and rollback paths |
| CT tech points vs DebugConsole `AddTechPoint` | Same `AvailablePoints` state | owner and layout appear stable statically | keep the native mutation; fix the DebugConsole UI so the player selects the point type |
| CT current weather vs DebugConsole weather | Same intended live-weather mutation | both still implement the removed global-weather contract and are incompatible | mandatory room/season-group weather migration before 0.6.0 verification |

### 1. AFK Fishing: Same State Chain, Different Ownership Discipline

DolocPlus starts its AFK session when the player first uses a fishing rod. Its patches then:

- complete the ready-state cast timer at full/perfect charge;
- stop if the cast transition does not reach a fishing state;
- synthesize the ordinary fishing input when the wait state exposes a bite;
- press only during non-delay note windows in the visible fishing minigame;
- once per second, recast only from normal gameplay and native idle state, after a native fishing-energy preflight;
- stop when the character's horizontal movement projection becomes non-zero.

This is a small and intelligible implementation. It also keeps all session state in static fields, does not expose an explicit save/title boundary reset in the feature itself, and has no transactional snapshot for native fields changed during a fishing transition. Feature-level Harmony ownership gives it patch/unpatch cleanup, but not DTMAPI's session settlement guarantees.

DTMAPI AutoFishing is a managed ProductNative state machine rather than a short input loop. It owns explicit phases and sequence IDs; exact hook installation and collision checks; scoped native input; energy gating, retry/backoff and session leases; two separate success routes (visible native minigame automation or a temporary native `skipFishingGame` transition); and save/title/owner-deactivation cleanup. Its instant-bite path validates the native wait state, prepares the native roll, applies the bite fields as a bounded transaction, refreshes native presentation, and can settle or restore the transaction.

The reusable DolocPlus lesson is the early normal-game/idle/energy preflight, which AutoFishing already largely implements. The reusable lesson is not the static patch shape. Both products' current movement-cancel logic is stale because 1.00.00 removed `AgentPhysicalStatus.HorizontalMoveFactor`; current movement ownership is under `MoveModifier.inputMultiplier` plus velocity/conveyor/offset motion. AutoFishing must migrate and re-test that boundary. DolocPlus 1.4.0 is not compatible merely because all of its fishing Harmony method names still resolve.

### 2. Time Speed: Game Clock Cadence Is Not Unity Simulation Scale

DolocPlus patches `GameLoop.FixedUpdate` and changes the private `_secondTimer` interval. That timer gates `ArchiveDataHandle.Update()`. At multiplier zero it repeatedly resets the timer; at other multipliers it uses an interval of one second or `1 / multiplier`. Character movement, animation, physics, and ordinary Unity frame timing are not directly scaled. The feature is therefore a **game-clock/archive-update cadence** control.

Disabling the feature first requests multiplier one, waits briefly so a `FixedUpdate` can restore the timer interval, and then unpatches. This is pragmatic, but it restores an assumed interval rather than an exact captured original and does not detect another owner's timer mutation. DolocPlus also declares AFK Fishing and Time Speed mutually conflicting.

DebugConsole `SetTimeScale` instead calls the game's public `DolocAPI.SetTimeScale(float, bool)`, which delegates to the native time-scale manager and ultimately changes Unity's global simulation scale. DTMAPI clamps the requested value to `0.1..16`, snapshots the exact pre-existing native value, detects foreign mutation, verifies the applied value, and restores the lease on modal/title/owner cleanup. It deliberately cannot express DolocPlus's zero-speed game-clock pause, and at `4x` it accelerates far more than the archive clock.

The two behaviors must not be merged under one API or silently substituted for each other. If a future product needs DolocPlus-style clock cadence, it should receive its own ProductNative owner review, conflict policy, exact restore contract and game acceptance. The current `GameLoop` timer path and the public global time-scale path both survive 1.00.00 statically, so this is a semantic boundary rather than a formal-version repair.

### 3. Locator Chests: DTMAPI Traversal Is Stronger, But Its Hook Scope Is Broader

The native contract is already useful: installing a chest locator makes the host container's `ILocatable.IsShared` true. `ArchiveDataHandle.GetAvailableInventories` always begins with the backpack, then admits a `Case` when it is nearby or shared. It admits an `IsShared` storage shelf under the native `useBox`/`autoUseBox` policy and then exposes the inventories of boxes stored inside that shelf.

DolocPlus preserves this native filtering. A prefix marks only calls originating at `DolocAPI.GetInventoriesAroundEquipment`; a transpiler substitutes the equipment enumeration inside `GetAvailableInventories`; and the substitute returns the main farm's equipment plus equipment in each directly owned building room. Because remote equipment is outside the caller's area, the unchanged native filter admits only its `IsShared` containers. The good property is exact **equipment-consumption caller scope**. The weak properties are a thread-static flag that can leak if the expected replacement path is not reached, one-level building traversal, no explicit reference deduplication, and a fragile transpiler dependency.

ChestLocatorEnhancer keeps the native result and post-appends only shared cases and, when the native box policy permits, boxes inside shared shelves. It recursively walks room/building graphs, guards visited rooms, deduplicates inventories by reference identity, caches reflection metadata, records observations, owns its Harmony patch atomically, and fails closed to the untouched native result.

However, its Postfix is installed on `ArchiveDataHandle.GetAvailableInventories` itself. In the current game that method is also called by both `GetInventoriesAroundAgent` overloads and by `GetBackpackWithInsideBoxes`, not only by `GetInventoriesAroundEquipment`. Consequently, the current product can append all-farm shared inventories to player-nearby and backpack-with-boxes queries as well. That is broader than the feature promise “设备消耗材料时” and broader than DolocPlus's implementation.

For 0.6.0, preserve DTMAPI's recursive traversal, deduplication, native `IsShared` eligibility, native `autoUseBox` respect, failure isolation and lifecycle ownership. Before repackaging, either narrow augmentation to the equipment-origin path or deliberately approve and document the broader behavior with focused tests. Do not copy the unbalanced thread-static sentinel; prefer a hook boundary or scoped origin token whose cleanup is guaranteed on every return and exception path.

### 4. CT Easy Fishing: Forced Battle Success Versus Native Skip Transaction

CT Easy Fishing modifies the entry of `AgentStateFishingWait.HandleFishOnHook` so bite probability becomes one and the wait counter interval becomes zero. It separately modifies `AgentStateFishingBattle.NextState` so the minigame status is `Success` before the method chooses its next state. The player still initiates fishing and reaches native wait/battle transitions; once enabled, every affected fishing battle is forced to succeed. Disabling restores the saved method bytes.

The nearest AutoFishing combination is `InstantBite + SkipMiniGame`, but it is not the same implementation. AutoFishing prepares the bite through the native fish-roll owner and temporarily enables the game's own `skipFishingGame` route while invoking the native wait transition; it restores that flag in a `finally` path. Its default visible-minigame mode instead plays the real minigame using scoped input. DTMAPI therefore bypasses the battle through an official native route or automates it visibly; it does not enter the battle and overwrite its status globally.

The CT member names can still be found by its substring-based lookup in the current decompile, but the relevant fishing method bodies changed and its injected machine-code offsets are not accepted by static name resolution. No DTMAPI feature should copy that method-entry overwrite. If an exact “enter battle then immediately succeed” presentation is ever requested, it is a separate optional product behavior with its own native-transition and cleanup proof, not evidence for broadening the stable API.

### 5. Tech Points: Same Derived Value, Native Mutation Is Safer

The four editable CT entries target Nature, Science, Operate and Animal tech-point records through dictionary internals and write the four-byte backing storage corresponding to `TechLevelData.AvailablePoints`. They do not cover the Battle and Fishing point types. The write bypasses `TechLevelManager.ChangeTechPoint`, enum validation, non-negative clamping and before/after verification.

The current `TechLevelData`/`TechLevelManager` source is statically unchanged from the inspected earlier build, so the CT layout still appears plausible; this is not runtime pointer-chain proof. More importantly, `AvailablePoints` is derived state rather than a JSON-persisted field. On load, `TechLevelManager.AfterLoadData` rebuilds available points from earned level rewards and subtracts costs of unlocked nodes. Unspent injected points therefore reconcile away on reload, while nodes bought with those points may persist through the native unlocked-node data.

DebugConsole enumerates all six native `TechPointType` values, reads current points/level through native archive operations, clamps an addition to `1..1000`, calls `DolocAPI.AddTechPoint`, and verifies the observed delta. This is the correct owner route and shares the same derived-state persistence semantics.

The present UI hides that strength: its single `+tech` button sorts options alphabetically, takes `FirstOrDefault`, and adds 100 points. With the current enum names this always selects `ANIMAL`. For 0.6.0 the UI should require a point-type selection or expose six explicit choices and should explain that unspent debug points are recalculated on reload. The native action should not be replaced by CT-style memory editing.

### 6. Current Weather: Both Implementations Bind The Removed Global Contract

CT lists weather IDs from `TbWeather`, then calls `ArchiveDataHandle.SetWeather` and `PatchWeather` using the old calling convention: instance plus a global `WeatherType`, with an optional render flag. It resolves `SetWeather` without an exact signature and emits a raw register-level call. In 1.00.00, `SetWeather` requires `(string seasonGroupId, WeatherType type, bool shouldRender)` and `PatchWeather` requires `(string seasonGroupId, WeatherType type, Vector2Int weatherKey)`. Passing the old register layout no longer merely means “method not found”; if a current overload is selected, the string/enum arguments occupy incompatible positions.

DebugConsole is safer in failure behavior but currently binds the same obsolete model. It reads removed `CurrentWeatherType` and `TimeArchiveData.SeasonProto`, searches for `SetWeather(WeatherType, bool)`, and optionally searches for `PatchWeather(WeatherType)`. The current UI always requests `patchCurrentPeriod: true`, so the action now fails at the missing old `SetWeather` signature before it can change or verify weather.

The authoritative 1.00.00 command path establishes the new semantics:

1. obtain the active room's `SeasonGroupId` (or the reviewed equivalent current season-group owner);
2. read/verify the room-local weather projection `LocalWeatherType`;
3. call `SetWeather(seasonGroupId, type, true)`;
4. when the current forecast period should persist, call `PatchWeather(seasonGroupId, type, DateNow.CurrentWeatherKey)`;
5. verify the resulting local weather and bounded current-period forecast.

This migration is mandatory for DebugConsole 0.6.0. CT 1.9's weather command is incompatible with the current build and should not be used as a fallback. DTMAPI should retain its `TbWeather` allowlist, structured failure result and observed-state verification while moving them to the new group-scoped owner.

### Focused 0.6.0 Disposition

1. **Required compatibility repair:** migrate AutoFishing movement detection away from removed `HorizontalMoveFactor` and rerun its existing fishing/GC acceptance boundary.
2. **Required compatibility repair:** migrate DebugConsole state/list/set-weather behavior to room/season-group weather and the current weather key.
3. **Required semantic review before release:** constrain or explicitly approve ChestLocatorEnhancer's all-caller `GetAvailableInventories` widening; its current scope exceeds the equipment-consumption feature statement.
4. **Bounded product UX correction:** let DebugConsole users select one of all six tech-point types and disclose reload reconciliation.
5. **No compatibility merge:** retain global simulation scale and game-clock cadence as separate behaviors; do not port CT's raw fishing-success or weather mutation paths.

## Same-Semantic DTMAPI Mapping

| DolocPlus / CT behavior | Nearest DTMAPI product/domain | What is actually shared | What must remain distinct |
| --- | --- | --- | --- |
| AFK Fishing | AutoFishing | fishing ready/cast/wait/minigame/input state machine; movement cancellation; energy and lifecycle preflight | DTMAPI keeps stronger session owner, settlement, restore, and fifth-save acceptance; do not copy direct patch logic |
| Fish Analyzer / fish-tank helpers | FishBreedingAssistant and future fish query work | fish condition/progress presentation and room-scoped weather dependency | pool probability analysis, roe tooltip, instant breeding, and automatic collection are different intents; no 0.6.0 public API is justified by this sample alone |
| Use All Chests / Located Chests / EZ Sort | ChestLocatorEnhancer and inventory transaction domain | inventory discovery, equipment consumption, cross-room located storage | locator-assisted equipment consumption is not the same as arbitrary backpack sorting; keep mutations in reviewed native transactions |
| One-Click Planting | StrongPlantingGun / planting automation | batch planting, compatible basin selection, seed/material consumption | one intercepts seed use across a room; the other enhances the official planting tool; keep separate ProductNative owners |
| Game Console / CT commands | DebugConsole | official command/native-owner delegation, input isolation, exact action preflight | raw command execution and DTMAPI's typed safe actions have different safety boundaries |
| Panorama / Super-Resolution Photo | Zoom / camera domain | camera scale, background/depth-fog compensation, restoration | full-room photo state and high-resolution capture are not ordinary playable zoom and should not be absorbed into Zoom |
| Automatic animal/fish/crop mutation | AnimalHusbandryProgress and other read-only progress UI | observation of native production state | automatic collection/growth/petting changes gameplay and ownership; read-only display is not permission to add mutation APIs |
| PatchController / HotkeyController | managed ProductNative lifecycle | feature owner, conflict checks, input isolation, patch/unpatch cleanup | DTMAPI also needs explicit health, failure reconciliation, save/title/session cleanup, and shared-owner arbitration |

There is no meaningful DolocPlus DLL equivalent for MoreSaves, MoreEquipmentSlots, Manbo cardboard audio, or several other admitted DTMAPI products. Outcome resemblance must not be used to invent a SharedNative API.

## Adoptable Lessons, Without Copying Code

1. Prefer a feature-level compatibility report over one whole-Mod compatible/incompatible bit.
2. Keep each ProductNative feature's patch owner and cleanup route explicit.
3. Check game state, UI state, energy/resources, and conflicts before beginning automation.
4. Warn once in player language, but pair warnings with exact disabled-feature reasons.
5. Reconcile a failed toggle to a visible failed/disabled state; logging alone is insufficient.
6. Treat method-body/state-owner drift as first-class. Signature presence is only the first gate.
7. Keep photo mode, cheats, read-only analysis, and ordinary quality-of-life APIs as different intent and safety classes.

These are architecture and acceptance lessons only. No third-party method body should be copied or mechanically translated into DTMAPI.

## Validation And Limits

Completed:

- exact source/destination relative-path, length, and SHA-256 comparison for all four copied items;
- archive and extracted-plugin inventory;
- ILSpy 9.1.0.7988 local decompilation of DolocPlus 1.4.0;
- 1.3.2 versus 1.4.0 metadata and focused semantic comparison;
- read-only Mono.Cecil resolution audit against both game baselines;
- direct inspection of Lua CT 1.9 target initialization and root-status branch;
- focused inspection of DolocPlus AFK Fishing, Time Speed and Located Chests trigger/mutation/cleanup paths;
- focused inspection of Lua CT Easy Fishing, four tech-point entries and current-weather mutation;
- current-build call-graph inspection for inventory discovery, game-clock cadence, tech-point reconstruction and room/season-group weather;
- focused comparison with AutoFishing, ChestLocatorEnhancer and DebugConsole native-owner/lifecycle implementations;
- direct inspection of DTMAPI Core's current pre-load Advanced classifier.

Not run:

- Doloc Town launch or gameplay smoke;
- BepInEx installation of DolocPlus;
- Cheat Engine execution;
- DTMAPI build/tests, because no Runtime or source implementation changed.

Static resolution does not prove runtime correctness, Harmony transaction behavior, save safety, or feature cleanup. This Review must not be cited as player acceptance for DolocPlus or DTMAPI 0.6.0.

## Related Evidence

- [Round 1 third-party Mod semantics](ROUND-1-agent-e-mod-semantics.md)
- [DolocPlus function map](../../../../../../references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md)
- [DolocPlus deep dive](../../../../../../references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md)
- [DolocPlus overlap study](../../../../../../references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md)
- [Doloc Town 1.00.00 / DTMAPI 0.6.0 root-cause review](../../../code/2026/20260801-0003-doloctown-100-dtmapi-060-compatibility-root-cause.md)
- [AutoFishing product source](../../../../../../products/first-party/AutoFishing/src/ModEntry.cs)
- [ChestLocatorEnhancer native traversal](../../../../../../products/first-party/ChestLocatorEnhancer/src/Native/ChestLocatorInventoryTraversal.cs)
- [DebugConsole native actions](../../../../../../products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Core.cs)
- `src/DTMAPI.Core/Manifesting/ManagedModClassification.cs`
- `references/doloc-town/reverse/builds/23762374_public_C416D4`
- `references/doloc-town/reverse/builds/24456188_test_E861E0`
