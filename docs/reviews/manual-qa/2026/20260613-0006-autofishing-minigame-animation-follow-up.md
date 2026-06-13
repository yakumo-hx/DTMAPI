# 20260613-0006 AutoFishing Minigame And Animation Follow-Up

- Date: 2026-06-13
- Scope: AutoFishingMod, `IFishingAutomationApi`, `FishingAutomationService`, fishing Harmony hooks, fifth-save smoke
- Source: user follow-up after native-loop AutoFishing rewrite testing.
- Status: implemented and verified for fifth-save DefaultLoop/Ready-Cast-Pull FastAnimations and configured charge smokes plus manual red/green/yellow minigame QA; API remains Experimental.

## 1. AutoFishing Should Play The Visible Minigame, Not Directly Finish It

User issue:

> 并非自动小游戏，而是0.75s左右直接完成小游戏，不是绿条长按、红条松手、奖励黄条短按。

Code facts:

- The prior `AutoCompleteVisibleMiniGame` route waited until a visible `FishingGameScrollBar` had existed for about `0.75s`, then directly wrote `currentGameStatus = Success`.
- The native owner is `FishingGameScrollBar.UpdateGame(float dt)`. It reads `DolocAPI.UserInput.NormalUseTool`, `NormalUseToolInProgress`, `NormalFishing`, `NormalFishingInProgress`, `NormalUseItem`, and `NormalUseItemInProgress` while advancing note/bar state.
- Native note data distinguishes stable/green, bonus/yellow, and delay/off-note intervals through `FishingNoteData.NoteType`.

Analysis:

- The direct status write proved a result, not the player-visible operation the user requested.
- The fix should leave scoring, result transition, and panel animation in `FishingGameScrollBar.UpdateGame`, and only supply the per-frame input decision that a player would make.
- `Stable` note frames should hold the in-progress input, `Bonus` note entry should send a short pressed edge, and red/off-note or delay frames should release.

Implemented root fix:

- Added a `FishingGameScrollBar.UpdateGame` prefix that prepares one-frame minigame input decisions from native note/current-time data.
- Added scoped prefixes for the six native `DolocUserInput` fishing/tool/item getters, active only during that native `UpdateGame` call.
- Removed the old delayed direct-success route from normal AutoFishing completion; the postfix now observes native `Success`/`Failed` and publishes smoke evidence with input-frame counters.
- Unit coverage verifies stable hold, delay release, first bonus tap, and bonus no-repeat behavior.

## 2. Ready/Cast/Pull Animation Speed Needs Real Native Owners And A 1-4 Multiplier Setting

User issue:

> 收竿抛竿动画加速没有实现。而且要加个倍率（1-4）设置。

Follow-up issue:

> 还有一个动画：点击渔竿后有一个蓄力过程，这个也要加速。

Code facts:

- The click/hold charge phase is `AgentStateFishingReady`: its constructor creates `RedSaw.CastTimer(CastDuration, PerfectCastDuration)`, `OnPlay` calls `_castTimer.Tick(Time.fixedDeltaTime)`, updates `_powerBar.Progress` / `_powerBar.Color`, and `OnExit` writes `_castTimer.Progress` into `FishRodRenderer.SetPower`.
- Cast enters through `AgentStateFishingCast.OnEnter`, which plays body/fishing-rod animation paths before the native hook flight event.
- Visible cast travel starts later in `FishRodRenderer.CastHook()`, which resets/shows `FishRodHook`, calls `FishRodHook.AddForce(...)`, and then lets the hook move under `FishRodHook.Velocity` / Rigidbody2D gravity until native collision.
- Pull enters through `AgentStateFishingPull.OnEnter`, which calls `FishRodRenderer.Pull` or `FishRodRenderer.PullCancel`, stores the returned `_pullDuration`, and then waits for both animation and pull duration in `OnPlay`.
- The previous implementation mainly wrote reachable animator speeds and could miss the native pull duration owner.
- Old configs lacked `AnimationMultiplier`; `DataContractSerializer` deserialized the missing double as `0`, and the first clamp path normalized that to `1`, so `FastAnimations=true` could still be effectively normal speed.

Analysis:

- Ready/Cast/Pull fast animation must combine animator speed writes, Ready `CastTimer` acceleration, hook-flight physics, and pull-duration scaling, because each visible segment has a different native owner.
- The Ready charge cannot be proven by Cast/Pull animator samples; the smoke must observe `FastReadyCharge` separately from `FastCastHookPhysics` / `FastPullDuration`.
- Cast visual speed cannot be proven by animator speed alone: the visible hook flight is native physics after `FishRodRenderer.CastHook`, so the smoke must observe hook velocity/gravity or pull-duration evidence rather than accept animator-only samples.
- The player-facing setting belongs in AutoFishingMod config as a bounded `1` to `4` multiplier.
- Missing old multiplier values should migrate to the intended default multiplier instead of clamping to `1`.

Implemented root fix:

- Added AutoFishingMod inline bool+number config for `FastAnimations` and `AnimationMultiplier` with range `1` to `4`.
- Normalized missing/invalid old config multiplier values to default `3`, then clamps valid values to `1..4`.
- Added an `AgentStateFishingReady.OnPlay` postfix that applies the configured multiplier to native `_castTimer` by ticking the extra `(multiplier-1) * Time.fixedDeltaTime`, then refreshes the native progress circle.
- Added postfixes for `FishRodRenderer.Pull` and `FishRodRenderer.PullCancel` that divide native returned duration by the configured multiplier.
- Added a `FishRodRenderer.CastHook` postfix that scales hook `Velocity` by the configured multiplier and hook Rigidbody2D `gravityScale` by multiplier squared, then restores hook physics on fishing state/lifecycle exit.
- Kept Ready/Cast/Pull animator speed writes on `AgentStateFishingReady.OnEnter` / `AgentStateFishingCast.OnEnter` / `AgentStateFishingPull.OnEnter`, added broader native candidate discovery, and retained restore on Pull/base exit plus save/title/environment reset.
- Added a fallback `_pullDuration` scale in Pull phase only when the `FishRodRenderer` return hook has not recently fired.
- Tightened the FastAnimations smoke gate so animator-only samples no longer satisfy visible animation speed; accepted evidence must include Ready `FastReadyCharge` plus `FastCastHookPhysics` or `FastPullDuration`.

## 3. Configurable Cast Charge Should Default To No Charge

User issue:

> 还要增加一个设置：蓄力程度，默认就是不蓄力，可调节到蓄满力。开启动画加速也加速蓄力过程。

Code facts:

- Native charge is not a standalone fish-rod setter. `AgentStateFishingReady.NextState` keeps the state in Ready while `DolocUserInput.NormalUseToolInProgress` is true, and `OnExit` later transfers `_castTimer.Progress` to `FishRodRenderer.SetPower`.
- The existing animation fix already accelerates `AgentStateFishingReady._castTimer` in `OnPlay`, so a target progress can share the same native timer owner.
- The smoke script initially clamped `-AutoFishingCastChargeRatio 0.5` through integer `[Math]::Min/Max` overloads, truncating it to `0`; that made the first passing smoke a default no-charge false positive.

Analysis:

- The setting should be a native Ready input policy: hold `NormalUseToolInProgress` until native progress reaches the target, then release and let `AgentStateFishingReady.NextState` enter Cast.
- Default `0` should release immediately, preserving the requested no-charge default.
- The smoke must check the requested target in the log line, otherwise a no-charge release can accidentally satisfy a configured-charge case.

Implemented root fix:

- Added experimental `FishingAutomationOptions.CastChargeRatio` and AutoFishingMod config labels/tooltips, normalized to `0..1`.
- Added Ready-phase input override for `DolocUserInput.NormalUseToolInProgress`, with `0` releasing immediately and `1` holding until near-full charge.
- Added unit coverage for a `0.5` target hold/release path and the default immediate-release path.
- Added `-AutoFishingCastChargeRatio` smoke support plus `AutoFishingCastCharge` result field, and fixed the script clamp to use floating-point `0.0/1.0`.

## 4. Toggle Hotkey Should Be Rebindable

User issue:

> 自动钓鱼还要给出热键替换功能。其他功能手测均无问题。

Code facts:

- `AutoFishingConfig` already had a `ToggleKey` field with default `F6`, and `RegisterInputKeys()` registered that field.
- `NormalizeConfig()` was still forcing the field back through the old fixed-F6 assumption by not distinguishing custom, missing, and disabled keybind cases in the menu/smoke path.
- The config menu exposed behavior switches, animation multiplier, and cast charge, but did not expose the toggle keybind even though DTMAPI's config menu already supports keybind controls and conflict reporting.
- The smoke script always wrote `ToggleKey = F6`, sent F6, and waited for F6-specific log lines, so it could not verify a rebound hotkey.

Analysis:

- This is an AutoFishingMod configuration ownership bug, not a new GameBridge fishing-state bug.
- The native fishing API should remain unchanged; ordinary key replacement belongs at the mod/config/input helper layer.
- Missing old config should keep the default F6 behavior, but an explicit `None` selection should disable the hotkey.

Implemented root fix:

- Added a player-facing AutoFishing keybind option to the DTMAPI config menu.
- Preserved custom keys during config normalization, migrated missing/blank keys to default `F6`, and kept explicit `None` disabled.
- Re-registers the active toggle key after config saves; manual movement cancel keys remain registered separately.
- Extended fifth-save AutoFishing smoke support with `-AutoFishingToggleKey`, dynamic key injection, and key-specific log matching so a rebound key can be validated.
- Added unit coverage that custom `F7` survives normalization, missing key migrates to `F6`, explicit `None` remains disabled, and existing animation/charge normalization still applies.

## 5. Current Validation Facts

Analysis:

- Release build and Release unit tests passed after the new input and animation-owner changes.
- Fifth-save `DefaultLoop` smoke `GAME-SMOKE/20260613-153632` passed with `AutoFishingMiniGameComplete=Passed`; logs show `behavior=AutoPlayVisibleMiniGame`, `status=Success`, `stableHoldFrames=82`, `releaseFrames=49`, and no delayed `visibleSeconds=0.76` direct-success evidence.
- The first `FastAnimations` re-smoke `GAME-SMOKE/20260613-153850` failed `AutoFishingAnimationSpeed` because an old config with missing `AnimationMultiplier` migrated to `1`; that is retained as the rejected migration bug.
- After fixing migration and smoke config generation, fifth-save `FastAnimations` smoke `GAME-SMOKE/20260613-154252` passed with `AutoFishingAnimationSpeed=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, report `dtmapi-report-20260613-154345.zip`, Cast `1->3`, Pull `1->3`, and `FishRodRenderer.Pull` duration `0.104->0.035`.
- User manual QA on 2026-06-13 confirmed red/green/yellow minigame handling works.
- User manual QA then found animation was still not visually accelerated; the retained `GAME-SMOKE/20260613-154252` evidence is treated as incomplete for cast visibility because it only proved animator and Pull-duration owners.
- After adding the `FishRodRenderer.CastHook` physics owner, fifth-save `FastAnimations` smoke `GAME-SMOKE/20260613-160354` passed with `FastCastHookPhysics`, `hook.Velocity:(18.55,29.68)->(55.65,89.04)`, `hook.gravityScale:12->108`, Pull duration `0.147->0.049`, report `dtmapi-report-20260613-160443.zip`, clean process/fatal checks, and hook physics restore evidence.
- The first Ready-charge fifth-save re-smoke `GAME-SMOKE/20260613-163257` is retained as a smoke-harness false negative: runtime logs already showed `FastReadyCharge`, but the harness only sampled the last animation summary after Cast/Pull overwrote the Ready summary.
- After adding independent Ready-charge smoke tracking, fifth-save `FastAnimations` smoke `GAME-SMOKE/20260613-163812` passed with `AutoFishingAnimationSpeed=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, report `dtmapi-report-20260613-163900.zip`, `FastReadyCharge` samples such as `castTimer.Progress:0->0.04;extraDt=0.04;powerBar.updated=2`, `FastCastHookPhysics` with `hook.Velocity:(22.366,35.786)->(67.098,107.357)` and `hook.gravityScale:12->108`, `FishRodRenderer.Pull` duration `0.117->0.039`, and final loop summary `readyCharge=True, fastAnimation=True`.
- The first configured-charge smoke `GAME-SMOKE/20260613-170514` is retained as a false positive because the script wrote `CastChargeRatio=0` after integer `[Math]::Min/Max` truncation; `GAME-SMOKE/20260613-171104` is retained as the stricter-gate failure that exposed the clamp bug.
- After fixing the script clamp and requiring `target=0.5`, fifth-save `FastAnimations` smoke `GAME-SMOKE/20260613-171405` passed with `AutoFishingCastCharge=Passed`, `AutoFishingAnimationSpeed=Passed`, clean process/fatal checks, report `dtmapi-report-20260613-171455.zip`, `Smoke.AutoFishingCastCharge` release at `target=0.5, progress=0.54`, Ready charge speed samples up to `0.44->0.48`, `FastCastHookPhysics`, Pull duration scaling, and final `readyCharge=True, fastAnimation=True`.
- User manual QA on 2026-06-13 confirmed the other AutoFishing functions are OK after the minigame, animation, and cast-charge fixes; the remaining requested behavior is hotkey replacement.
- Fifth-save `DefaultLoop` smoke `GAME-SMOKE/20260613-175055` passed the hotkey replacement path with `AutoFishingToggleKey=F7`, `AutoFishing native loop policy registered. Toggle=F7`, `Input F7 pressed dispatched to DTMAPI mods`, `AutoFishing automation enabled reason=hotkey F7`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, and report `dtmapi-report-20260613-175205.zip`.
