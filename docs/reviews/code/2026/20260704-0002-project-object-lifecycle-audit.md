# 20260704-0002 Project Object Lifecycle Audit

## Scope

This audit follows the 2026-07-04 `/goal` request to scan DTMAPI broadly for object graph retention, duplicate ownership, cross-save/title pollution, GC mark-stack pressure, and long-run instability risks. It is intentionally wider than ISSUE-010 while still preserving existing behavior.

Hard boundaries:

- Do not copy or imitate old private predecessor implementations.
- Treat Doloc Town reverse data and third-party mods as reference only.
- Preserve CustomAnimals support for official JSON plus player PNG/WAV plus DTMAPI JSON that reuses native animal AI/animators.
- Do not destroy or unload unknown/native-owned Unity objects without evidence.
- Prefer small DTMAPI-owned cleanup fixes and diagnostics.

Required context read:

- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/reviews/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/hook-map/README.md`
- `docs/reviews/code/2026/20260704-0001-issue010-title-return-next-load-object-graph-audit.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- Recent phase records under `docs/updates/2026/`: `20260704-0003`, `20260704-0002`, `20260704-0001`, `20260703-0009`, and `20260703-0008`.

## Scan Method

Source scans covered static roots, events/delegates, timers/tasks, Unity object references, resource caches, save/title boundaries, feature services, registries, content packs, Bootstrap, GameBridge, and the named features from the request.

Representative commands:

- `rg -n "static|event|Action<|Func<|UnityAction|AddListener|RemoveListener|Timer|Task|Thread|UnityWebRequest|AssetBundle|Texture2D|AudioClip|RuntimeAnimatorController|GameObject|Component|DontDestroyOnLoad" src testmods tools`
- `rg -n "SaveLoaded|ReturnedToTitle|Shutdown|OnApplicationQuit|Dispose|Clear|ResetForTitle|ResetForSave" src`
- Targeted reads of Bootstrap UI, Debug Console UI, GameBridge hook scheduling, AudioReplacement, CustomAnimals, AutoFishing, AnimalViewer, SaveSlots, EquipmentSlots, registry, and content pack services.

## External Checks

| Finding or suspect | Search terms | Links | Judgment used in this audit |
| --- | --- | --- | --- |
| Static roots and GameObject hierarchy affect asset reachability. | `Unity Resources.UnloadUnusedAssets static variables GameObject hierarchy documentation` | <https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Resources.UnloadUnusedAssets.html>, <https://discussions.unity.com/t/resources-unloadunusedassets-execution-time-slowly-increases-over-time/920692> | Count static/service roots and persistent GameObject hierarchies. Static dictionaries and `DontDestroyOnLoad` UI roots are relevant evidence, even when they are intentional. |
| Persistent Unity roots survive scene changes with children. | `Unity DontDestroyOnLoad Object.Destroy persistent GameObject documentation` | <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.DontDestroyOnLoad.html>, <https://gamedev.stackexchange.com/questions/189791/how-to-retain-gameobject-in-donotdestory-objects-when-game-is-restart> | Bootstrap UI roots are allowed to persist, but their child panels, dynamic event binders, fallback EventSystem, and title-only state need explicit title/save/shutdown boundaries. |
| Runtime UnityEvent listeners should be removed or bounded when lifetimes differ. | `UnityEvent RemoveListener memory leak listeners documentation` | <https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Events.UnityEvent.RemoveListener.html>, <https://discussions.unity.com/t/possible-memory-leak-or-issue-with-unity-event-implementation-usage-852348/852348>, <https://stackoverflow.com/questions/77444068/unity-can-onclick-addlistener-casue-memory-leak> | Keep counting UI binders and remove the DTMAPI-owned SaveLoaded UnityEvent listener on shutdown. Duplicate listener creation in update loops remains a known risk pattern, but current DTMAPI UI dynamic listeners are rebuilt through cleared rendered objects. |
| AssetBundle unload policy can destroy live objects or leave loaded objects intact depending on the boolean. | `Unity AssetBundle Unload false true loaded objects documentation` | <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetBundle.Unload.html>, <https://docs.unity3d.com/540/Documentation/Manual/LoadingAssetBundles.html>, <https://learn.unity.com/tutorial/assets-resources-and-assetbundles> | Do not unload CustomAnimals controller/bundle caches blindly. Treat them as title/process-lifetime suspects with counts and long-smoke validation. |
| UnityWebRequest audio creates AudioClip-backed Unity objects and request handler state. | `UnityWebRequestMultimedia GetAudioClip Dispose AudioClip memory Unity documentation` | <https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Networking.UnityWebRequestMultimedia.GetAudioClip.html>, <https://discussions.unity.com/t/setting-loadinbackground-with-downloadhandleraudioclip/682595>, <https://discussions.unity.com/t/how-to-release-memory-from-audio-clip/637534> | AudioReplacement pending requests, async operations, loaded clips, callback owners, and platform players are valid snapshot families. Existing short snapshots show pending/request/clip/callback state reaches zero after title return; platform players remain a monitored process-lifetime suspect. |
| Mono/Boehm mark-stack failures are consistent with large or unusual reachable object graphs, but not diagnostic by themselves. | `Mono Boehm GC mark stack overflow Unity Unexpected mark stack overflow` | <https://discussions.unity.com/t/fatal-error-in-gc-unexpected-mark-stack-overflow/760479>, <https://discussions.unity.com/t/il2cpp-crash-at-malloc/849108?page=3>, <https://www.hboehm.info/gc/gcdescr.html> | Keep ISSUE-010 open until long evidence passes or a concrete object family is implicated. The fatal text justifies object-graph telemetry, not speculative asset destruction. |
| BepInEx plugin classes are Unity MonoBehaviours and use Unity lifecycle functions. | `BepInEx BaseUnityPlugin lifecycle Awake OnDestroy documentation` | <https://docs.bepinex.dev/v5.4.16/api/BepInEx.BaseUnityPlugin.html>, <https://docs.bepinex.dev/v5.4.11/articles/dev_guide/plugin_tutorial/2_plugin_start.html>, <https://docs.unity3d.com/560/Documentation/Manual/ExecutionOrder.html> | `BootstrapPlugin.OnApplicationQuit` is a valid final boundary to clear DTMAPI-owned UI roots, retry timers, hook static roots, and transient input state. |
| Harmony patches are process-level method patches unless explicitly unpatched or process exits. | `Harmony patch unpatch all documentation` | <https://harmony.pardeike.net/articles/basics.html>, <https://harmony.pardeike.net/articles/patching.html> | Do not try to unpatch gameplay hooks during normal title/save boundaries. Record static callback roots and release DTMAPI-owned hook callback references on shutdown. |

## Classification

### Long-Lived Owner, Config, Or Service

These may live for the process, provided they do not retain per-save/native transient state indefinitely:

- `DtmApiRuntime`, diagnostics, runtime monitor, lifecycle ledgers, owner ledger, resource ledger, SaveLoad request coordinator, title-return boundary ledger.
- Mod scanning and registry services, manifest/content registry diagnostics, runtime-owned API manifests, config registry/menu definitions, workshop content metadata.
- `DolocTownGameBridge` and feature service instances as process-long owners.
- Hook readiness/status diagnostics and bounded reports.
- CustomAnimals content definitions, AI/template registrations, PNG/animator metadata, AssetBundle/controller caches, and type caches. These remain suspects for long evidence, but current short evidence does not prove stale growth.
- AudioReplacement content definitions and stable platform-player pool. Pending request/clip/callback state must be transient.

### Must Clear On ReturnedToTitle

State that should not survive title return as active save/native UI state:

- SaveSlots pager/UI state, event binders, cloned official UI panels, and temporary direct-load state.
- EquipmentSlots cloned slot UI, binders, per-session storage owner handles, and runtime clone maps.
- AnimalViewer overlay objects, rows, native animal data keys, cached UI elements, and hover state.
- AutoFishing native-object keyed dictionaries, ready-state charge sets, mini-game input overrides, animator/hook physics snapshots, and pending-cast markers.
- CustomAnimals per-save `ConditionalWeakTable` contexts, PNG sprite override runtime context, sleep follow-up diagnostics, and save-lifetime animal renderer state.
- AudioReplacement animal sound context stack and save-scoped request/callback traces.
- Title Settings UI open/panel/rendered objects, dynamic event binders, fallback EventSystem, pending input values, captured keybind state, and uncommitted config edits.
- Debug Console UI open/modal state and fallback EventSystem.
- Win32 fallback key-edge static set.

### Must Rebind Or Re-evaluate On SaveLoaded

These are tied to the native save instance and must either clear or rebind after load:

- SaveLoad request active state and latest request closure.
- Save-owned UI/native object references in SaveSlots, EquipmentSlots, AnimalViewer, AutoFishing, CustomAnimals, custom entities, ActionSpeed, OilCoalDrop, and machine/status helpers.
- AudioReplacement animal context and callback state for native animal sound invocation.
- Smoke and HookProbe per-save evidence state.
- Title Settings UI and Debug Console transient state, because a load from title must not keep a stale title panel or captured keybind open into gameplay.

### Must Clear On Shutdown Or Disable

These are DTMAPI-owned roots that should not be left hanging when Unity is quitting:

- Bootstrap fallback pump timer.
- Bootstrap Title Settings and Debug Console persistent UI roots, dynamic binders, input field maps, fallback EventSystems, and icon Sprite/Texture.
- `ReflectedUnityInput` fallback key-edge static set.
- GameBridge hook retry timer and `AppDomain.AssemblyLoad` subscription.
- DTMAPI-owned `DolocAPI.OnAfterLoadArchiveData` UnityEvent listener.
- `DolocTownHookCallbacks.Runtime`, `DolocTownHookCallbacks.Bridge`, and debug-console modal static flag.

### Suspect But Not Yet Proven

These need evidence before any destructive cleanup:

- CustomAnimals AssetBundle/controller caches. They are intentionally long-lived to preserve custom animal behavior and avoid breaking live controller references.
- AudioReplacement platform players and process-lifetime entries. Short title-return evidence shows no pending request/clip/callback growth, but long-window evidence should continue tracking player counts.
- Harmony patches and static callback methods. They are process-long by design; shutdown now releases DTMAPI static object roots, but normal title/save boundaries should not unpatch.
- Diagnostic ledgers. They are bounded and useful for ISSUE-010, but long runs should keep checking event/snapshot counts.
- Native Doloc Town object graph after title return. Latest fatal samples happen before SaveLoaded; if DTMAPI-owned counts stay bounded, the next step is native crash dump/root-set analysis rather than risky mod-side destruction.

## Fixed In This Pass

1. Title Settings UI now has explicit SaveLoaded, ReturnedToTitle, and Shutdown boundaries.
   - Clears open panel state, rendered objects, dynamic event binders, input values, captured keybinds, page markers, fallback EventSystem, and pending title-only status.
   - Cancels pending config edits at lifecycle boundaries through the existing config menu transaction path.

2. Title Settings icon Sprite and Texture2D are destroyed on UI shutdown/reset instead of only dropping the managed `iconSprite` reference.
   - This is DTMAPI-owned bitmap state loaded from local UI assets, not a borrowed native game asset.

3. Debug Console UI now has explicit Shutdown cleanup.
   - Closes the DTMAPI runtime UI menu if active, clears modal state, destroys the DTMAPI-owned root and fallback EventSystem, clears dynamic binders/input maps, and resets per-open state.

4. GameBridge now has an explicit Shutdown cleanup boundary.
   - Stops hook retry sources, releases `AppDomain.AssemblyLoad`, removes the DTMAPI-owned SaveLoaded UnityEvent listener, clears DTMAPI static hook roots, and publishes `GameBridge.ShutdownCleanup`.

5. Win32 fallback input transient state now clears at SaveLoaded, ReturnedToTitle, and Shutdown.
   - This prevents an edge-state set from crossing save/title/shutdown boundaries.

## Files Changed

- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`

## Validation So Far

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; NuGet vulnerability feed lookup emitted restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- Shared runtime lock was acquired before runtime smoke and released afterward.
- Final process check after smoke found no leftover `DolocTown.exe`.

Short game smoke passed.

Runtime smoke evidence:

- Slot 3 startup/load/title-button lifecycle: `GAME-SMOKE/20260704-151818` passed `RunStatus`, `SaveLoaded`, `TitleButtonLifecycle`, `DuplicateLoadRequests`, `SaveLoadBoundary`, `TitleReturnBoundaryLedger`, `ProcessExited`, and `NoFatalInstanceWindow`. SaveLoad summary: `requests=1`, `duplicateRequests=0`, `nativeEnter=1`, `nativeReturn=1`, `saveLoaded=1`, `fatalWindows=0`.
- Slot 3 load/title/load cycle: `GAME-SMOKE/20260704-151937` passed `SaveLoadCycle` with `requests=2`, `duplicateRequests=0`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `fatalWindows=0`, and title-return ledger `events=24`, `objectSnapshots=10`, `fatalEvents=0`.
- Slot 3 multi-cycle title return: `GAME-SMOKE/20260704-152059` passed `SaveLoadCycle` with `requests=3`, `duplicateRequests=0`, `nativeEnter=3`, `nativeReturn=3`, `saveLoaded=3`, `fatalWindows=0`, and title-return ledger `events=34`, `objectSnapshots=14`, `fatalEvents=0`.
- Slot 7 Hatch CustomAnimals plus AnimalVoice: `GAME-SMOKE/20260704-152238` passed `HatchAnimalVoice`, SaveLoad fields, process exit, and fatal-window checks.
- Slot 5 AutoFishing short soak: `GAME-SMOKE/20260704-152339` passed `AutoFishingPhase`, `AutoFishingSoak`, SaveLoad fields, process exit, and fatal-window checks. Lifecycle summary showed `nativeTransientHandles=0`, `miniGameHandles=0`, `readyChargeStates=0`, `animators=0`, `hookPhysics=0`, `pendingCast=False`, `failures=0`, and `soakLoops=3/3`.
- Slot 4 AnimalViewer UI: `GAME-SMOKE/20260704-152523` passed `AnimalViewerUi`, SaveLoad fields, process exit, and fatal-window checks.
- Slot 3 SaveSlots UI: `GAME-SMOKE/20260704-152629` passed `MoreSavesOfficialSaveUi`, `MoreSavesOfficialSaveUiEvidence`, SaveLoad fields, process exit, and fatal-window checks.

## Remaining Suspects And Next Verification

| Suspect | Current evidence | External check result | Next verification |
| --- | --- | --- | --- |
| CustomAnimals controller/bundle cache | Short 8.5 snapshots showed zero cache growth in the title-return window; behavior requires preserving official JSON plus player PNG/WAV plus DTMAPI JSON reuse of native AI/animators. | Unity AssetBundle docs warn that `Unload(true)` destroys loaded objects and can break live references; `Unload(false)` preserves loaded objects. | Keep counts in title-return snapshots; only consider targeted unload or cache-generation policy if a long fatal run shows cache growth or stale per-save keys. |
| AudioReplacement platform players | Short 8.5 snapshots showed pending requests/async/AudioClips/callback owners/animal contexts at zero after title return, with stable entries/players retained. | Unity audio request docs support counting UnityWebRequest, handler, and AudioClip families; community reports support explicit release when clips are no longer needed. | Verify slot 7 AnimalVoice and slot 5 AutoFishing smoke after shutdown cleanup. Long evidence should compare player count before/after title cycles. |
| Harmony/static hook callback roots | Hooks are process-level by design; static callback roots previously retained `Runtime`/`Bridge` until process exit. | Harmony docs describe patching/unpatching as process-level patch table updates; BepInEx/Unity lifecycle docs support final cleanup on quit. | Shutdown now clears DTMAPI static object roots. Smoke should verify no hook readiness regression. |
| Bootstrap persistent UI roots | Title Settings and Debug Console intentionally use `DontDestroyOnLoad` roots, so they must be inactive/bounded outside title. | Unity docs confirm persistent roots survive scene loads with children; UnityEvent listener docs support explicit binder cleanup. | Slot 3 title-button lifecycle and SaveLoadCycle should show no title UI/fallback EventSystem leak or input capture crossing boundaries. |
| Native Doloc Town title-return object graph | Latest fatal evidence occurs before SaveLoaded after title idle/cycle, but DTMAPI short snapshots have not found a concrete retained family. | Unity/Mono mark-stack reports support object-graph/root-pressure investigation but do not identify a mod-side cause. | If short smoke passes, run a long gate only if needed. If fatal recurs with DTMAPI counts bounded, escalate to native crash/root-set inspection. |

## Done Criteria State

- Full project lifecycle scan: completed for current source tree at code-review depth.
- Definite DTMAPI-owned cleanup fixes: implemented in Bootstrap UI, input, GameBridge shutdown, and UnityEvent listener cleanup.
- Short smoke: passed across title lifecycle, load/title/load, multi-cycle title return, Hatch AnimalVoice, AutoFishing soak, AnimalViewer, and SaveSlots.
- Remaining suspect explanation: documented above. No short smoke identified a new uncleared DTMAPI-owned object family.
