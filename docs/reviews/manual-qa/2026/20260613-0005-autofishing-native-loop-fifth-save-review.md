# 20260613-0005 AutoFishing Native Loop Fifth-Save Review

- Date: 2026-06-13
- Scope: AutoFishingMod, `IFishingAutomationApi`, `FishingAutomationService`, AutoFishing smoke harness
- Source: user feedback after real playtesting the previous AutoFishing fixes.
- Status: implemented; fifth-save real-loop verification passed after fixture was updated to hold a fishing rod.

## 1. Default AutoFishing Should Be A Full Loop

User issue:

> 需要 F6 开启，默认自动抛竿、小游戏自动完成、循环；完整收鱼。

Code facts:

- The old migrated option model exposed multiple partial toggles and allowed tests to prove pieces in isolation.
- Player expectation is now one default automation loop after F6: cast, wait, reel, visible minigame complete, collect, recast.

Analysis:

- The public experimental API should stop modeling old mod UI toggles as primary behavior and should instead model native fishing stages.
- AutoFishingMod should not require players to enable several toggles to get the normal automated loop.

## 2. Extra Options Are Stage Overrides Only

User issue:

> 额外配置是：立即上钩：跳过等鱼过程；跳过小游戏：跳过小游戏直接收鱼，最好是渔竿等级不够就走游戏默认失败路径；动画加速：抛竿、收竿两个动画。

Code facts:

- `InstantBite` previously affected bite timing but still needed correct post-bite routing.
- `SkipMiniGame` must not set Pull success directly.
- Animation speed previously had broader/best-effort paths that could be mistaken for verified cast/pull-only behavior.

Analysis:

- `InstantBite` should select wait strategy only.
- `SkipMiniGame` should route through native `skipFishingGame` / Pull result behavior and preserve native failure/success.
- `FastAnimations` should be constrained to `AgentStateFishingCast.OnEnter` and `AgentStateFishingPull.OnEnter`.

## 3. Fifth Save Replaces Synthetic Smoke Fixtures

User issue:

> 自动钓鱼mod基于第五存档。现在的第五存档进入后角色就站在一个池塘前面。直接F6、抛竿就行。之前的测试脚本我看加载的位置都不是在水的位置。

Code facts:

- The old smoke could create or activate a fishing pool, seed `FishingCache`, generate a rod, and overwrite into `AgentStateFishingWait`.
- That proof could pass without validating the real player start position, selected rod, pond, wait state, bite, and loop.

Analysis:

- AutoFishing phase smokes must explicitly require `-SaveSlot 5`.
- The smoke should fail or block if the fifth-save fixture is not fish-ready: no selected rod, no fishable water/pool, not normal gameplay, or insufficient native progress.
- The global game-smoke default stays third save for unrelated hooks.

## 4. Acceptance Evidence Must Be Real Loop Evidence

User issue:

> 按照重做这个mod来实现。api要尽可能贴近原生责任函数建立。

Code facts:

- Native responsibility owners are `BodyController.UseFishRod`, `AgentStateFishingWait`, `AgentStateFishingWait.NextState`, `FishingGameScrollBar`, and `AgentStateFishingPull`.

Analysis:

- Passing evidence should show `AutoCast -> Wait -> BiteReady -> Battle/Pull -> PullExit -> next AutoCast`.
- Scenario names should reflect real semantics: `DefaultLoop`, `InstantBite`, `SkipMiniGame`, `FastAnimations`, `CombinedInstantSkip`, and `CombinedInstantComplete`.
- `IFishingAutomationApi` remains Experimental because this is a breaking cleanup of experimental DTO semantics, not a stable public contract.

## 5. Current Validation Facts

Analysis:

- Release build/test and unit coverage passed after the native-stage rewrite.
- The migrated fifth-save smoke now rejects synthetic helper paths and requires explicit `-SaveSlot 5`.
- Early local fifth-save smoke `GAME-SMOKE/20260613-094928` loaded slot 5/index 4 and dispatched F6 through the DTMAPI input path, then correctly failed the fixture precondition because the selected item was `DolocTown.ItemTool`, not `DolocTown.ItemFishingRod`.
- After the fifth-save fixture was saved with `carbon_fishrod` selected, real fifth-save smokes passed:
  - `DefaultLoop`: `GAME-SMOKE/20260613-102412`
  - `InstantBite`: `GAME-SMOKE/20260613-103216`
  - `SkipMiniGame`: `GAME-SMOKE/20260613-103328`
  - `FastAnimations`: `GAME-SMOKE/20260613-103427`
  - `CombinedInstantSkip`: `GAME-SMOKE/20260613-103531`
  - `CombinedInstantComplete`: `GAME-SMOKE/20260613-103621`
- The real DefaultLoop log shows `BodyController.UseFishRod` with `rod=carbon_fishrod`, native `AgentStateFishingWait` bite-ready routing to `AgentStateFishingBattle`, visible `FishingGameScrollBar` success after about `0.76s`, `AgentStateFishingPull`, and a subsequent AutoCast, proving collect/recast loop evidence.
- The post-feedback bug was not fixture-related: native `AgentStateFishingWait.NextState()` requires `NormalUseTool`, `NormalUseItem`, or `NormalFishing`. AutoFishing was calling `NextState()` without an input edge, so it returned `this`. The fix keeps `NextState()` as the first choice and mirrors the same native branch only when it returns the Wait state.
