# 20260715-0013 Batch 2 Steam Player Input And Public Product Gates

## Metadata

- Update ID: `20260715-0013`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: smoke/steam/input/workshop/products/lifecycle/recovery
- Source: user requested the Batch 2 normal-Steam, no-HookProbe short-tap focus regression and the exact eleven-public-product combination/disabled matrices, then confirmed the three Smoke P1 corrections; related reviews: `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md` and `docs/reviews/code/2026/20260715-0006-batch2-closure-and-batch3-predecision-review.md`; continuing lifecycle owner: `docs/updates/2026/20260714-0004-batch2-version-release-authority.md`

## Scope

- add an explicit Steam subscription smoke profile containing exactly the eleven public Product Catalog identities;
- fail before launch if any retained Steam-managed product tree differs from its Catalog file-count, byte-count or normalized tree digest;
- validate enabled and disabled cold-load product counts, dependency compatibility, no-op refresh, title cleanup and final GameBridge health;
- add a no-HookProbe external short-tap gate that uses foreground-window `SendInput` only, never the internal smoke driver or `PostMessage` fallback;
- require independent continuous observation windows for READY Gameplay and every post-`ReturnHome` HomePage gate;
- preserve and byte-verify the original `mod_infos.json` plus all three files belonging to the selected player save slot;
- prove `DolocTown.exe` has exited before restoring player-owned state; when bounded exit confirmation fails, retain every backup and emit an exact manual-recovery receipt without writing the saves, enablement file, smoke settings, or product configs.

This slice does not close `ISSUE-010` or `ISSUE-011`, does not count as an AutoFishing or ActionSpeed GC ladder, and does not mutate a Workshop subscription tree.

## Known Facts And Rejected Paths

- `GAME-SMOKE/20260714-034644` and `GAME-SMOKE/20260714-034949` each contain one missing-frame fallback and two frame-driver stall warnings. Both recovered and passed. The earlier statement that no stall occurred was corrected by `20260715-0008`; the warning pattern alone is not a new Debug issue.
- A normal-Steam input check cannot be represented by HookProbe or the in-game smoke driver. The gate therefore disables the internal DebugConsole smoke exercise, requires the game window to be foreground at send time, requires successful Win32 `SendInput`, and forbids the pre-existing `PostMessage` compatibility fallback.
- A mutable Steam subscription directory is not a retained-release baseline. The eleven-product gates stop before launch on any Catalog digest mismatch instead of silently testing unknown updated files.
- A successful game exit is insufficient recovery evidence. The runner restores and hashes the original enablement file and the selected slot's current, previous and backup save files independently.
- Elapsed time since a `ReturnHome` request is not title-stability evidence. Product refresh cleanup, external-player cleanup, title-button lifecycle, and the existing save/load-cycle route now start a separate timestamp on the first observed expected state, reset it when that state is lost, and pass only after a continuous interval.
- A `finally` block is not authorized to race a live game process. The runner first requests graceful close and waits for a bounded exit; a remaining process converts restoration into a fail-closed manual-recovery receipt under the evidence directory.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
  - preserves the raw Escape compatibility fallback with a bounded release drain, keeps native menu/use isolation active through that drain, and routes old-DLL modal Y only through the owner-targeted Core compatibility dispatch.
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
  - closes only its own manager-page modal instead of clearing another owner's active custom menu.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - records the active custom-menu owner, exposes the guarded legacy-modal dispatch boundary, and emits a stable pre-`Assembly.LoadFrom` CodeMod source/root/DLL provenance record for the external package gate.
- `src/DTMAPI.Core/Services/EventManager.cs`
  - supports an owner-filtered legacy `ButtonPressed` dispatch without widening normal broadcast semantics.
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
  - tracks the active owner-bound custom modal and clears that identity at the same lifecycle boundaries as the menu.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
  - owns separate normal-Gameplay and HomePage observation timestamps for external input plus dedicated HomePage timestamps for product-refresh and title-button cleanup.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
  - keeps native `EnterUICheck`, tool and item isolation active while the console-close Escape drain is pending.
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
  - publishes the modal/drain boundary used by the native-owner guards, clears it on title/shutdown cleanup, and reports synchronous `ModManager.ReloadMods` postfix completion so dependent lifecycle work cannot overtake the Workshop refresh dispatch.
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
  - orders Published11 reload completion before title lifecycle work and implements the external READY/terminal-marker/ReturnHome handshake;
  - uses one focused continuous-observation primitive for independent READY Gameplay and post-ReturnHome HomePage windows, including product-refresh, external-input, title-button, and save/load-cycle cleanup.
- `tools/scripts/run-game-smoke.ps1`
  - adds `Published11`, exact enabled/disabled product assertions, `SkipInstall`, package-payload forwarding and a player-like external input gate;
  - snapshots all eleven retained subscription artifacts against Product Catalog digests before launch;
  - records per-owner Entry/Begin/Commit/failure counts, dependency and hot-refresh outcomes, title cleanup, HookProbe absence and final health;
  - records every external key attempt with hold duration, log offset, foreground PID, `SendInput` result and fallback state, and records missing-frame/stall/resubscribe/lifecycle-refresh counts without treating a recovered warning as a new Debug issue by itself;
  - restores and byte-verifies `mod_infos.json` and the selected save-slot files, attempting both recovery families even if one fails;
  - performs a bounded process-exit check before any player-state restoration and, if the game remains alive, preserves backups and writes `manual-recovery-required.json` plus human-readable recovery instructions instead of touching player files.
- `tests/DTMAPI.UnitTests/Program.cs`
  - adds Batch 2 ordering/recovery contracts and a focused logic test proving first observation, full interval, state-loss reset, and independent Gameplay/HomePage stability windows.
- `tools/scripts/test.ps1`
  - adds the smoke runner to the Windows PowerShell 5.1 compatibility parse gate.
- `docs/hook-map/focused/DebugConsoleInput.md` and `docs/hook-map/README.md`
  - own and route the changed native Prefix lifecycle, including the two-clean-frame Escape drain.
- `docs/api/public-api-matrix.md`, `docs/debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md`, `docs/debug/issues/README.md`, `docs/debug/regressions/smoke-matrix.md`, and `docs/reviews/manual-qa/2026/20260712-0002-y-console-close-double-toggle.md`
  - synchronize the narrow owner-targeted legacy input exception, current Issue boundary and actual runtime evidence without changing ISSUE-010/011.
- `docs/updates/INDEX-2026-07.md`
  - routes this Update from the monthly ledger.

## Validation

- PowerShell 7 parsed the modified smoke runner successfully.
- The shared compatibility parser accepted the runner through the Windows PowerShell compatibility route.
- A read-only digest replay against the current Steam subscription found exactly eleven public products and matched every Catalog file count, byte count and normalized tree SHA-256; no subscription file was changed.
- `GAME-SMOKE/20260715-131727` passed the enabled Published11 lane: all eleven exact retained roots/hashes loaded, every owner Entry/Begin/Commit count was `1/1/1`, failures were zero, dependencies/provenance/artifacts/cleanup passed, hot refresh loaded zero, HookProbe was absent, all selected-save and enablement hashes restored, and no process remained. It recorded one recovered missing frame and two recovered frame-driver stalls.
- `GAME-SMOKE/20260715-132321` passed the disabled Published11 lane: all eleven owner Entry/Begin/Commit counts were `0/0/0`, the observed owner set was empty, dependencies/artifacts/cleanup and all restoration/process gates passed, and HookProbe was absent. It recorded one recovered missing frame and one recovered stall.
- `GAME-SMOKE/20260715-145818` first passed the ordinary-player input sequence under normal Steam launch with HookProbe absent: Y open, Escape close, reopen, ten 40 ms Y taps, one 1,800 ms hold and cleanup all used foreground `SendInput` with no `PostMessage` fallback. Six modal Y closes produced six owner-targeted legacy dispatches, six retained-DLL toggle closes and zero typed modal-Y dispatches; no native menu leaked. Enablement and all three slot-3 files restored byte-for-byte, one missing frame and two stalls recovered, and no process remained.
- Final review then found that this first runner could accept zero duplicate-source diagnostics without independently proving the selected load root. The gate was tightened to require the exact retained Catalog tree, nonempty DLL/hash and one unique pre-`Assembly.LoadFrom` Runtime load-source record pointing to Workshop `3742714442` and the expected DLL.
- `GAME-SMOKE/20260715-153336` replayed the complete input matrix with that final runner. The tree matched `10` files / `303759` bytes / digest `c4e6eda7f131fcffebe14b1784b9b487f3ff07edb8ad7f600fea0e32063b7d60`; the DLL matched SHA-256 `E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E`; exactly one load-source record matched Workshop ID/root/DLL. All prior 16 attempt-one input, `6/6/6` legacy owner/toggle, zero typed-modal-Y, two Escape, no-native-menu-leak, restoration, no-fatal and process-exit assertions remained green. Warning counts remained one missing frame, two stalls, two resubscribes and four player-loop lifecycle refreshes.
- Focused UnitTests passed after the strict provenance addition, and PowerShell 7 plus Windows PowerShell 5.1 parsed the final runner. These live runs used the final-freeze working-tree candidate; the separate exact-commit full Release suite remains Update `0011`'s source/package gate.

## Rollback

Revert `ReflectedDebugConsoleUi.cs`, `ReflectedTitleMenuSettingsUi.cs`, `DtmApiRuntime.cs`, `EventManager.cs`, `WorkshopContentInputUi.cs`, `DolocTownGameBridge.cs`, `DolocTownGameBridge.Hooks.cs`, `DolocTownHookCallbacks.cs`, `SmokeHarness.cs`, the smoke runner, focused UnitTests, `test.ps1`, the focused Hook map/router, API/Input row, ISSUE-014/Smoke/Review evidence updates, this Update, and its July ledger row as one scoped set. Reverting only the runner would leave half of the reload/handshake state machine behind; reverting only Core/GameBridge/Bootstrap would leave gates that can never complete or a stale Hook ownership record. Rollback does not change Product Catalog retained artifacts, the existing manual enablement file, player saves, subscribed packages, or ISSUE-010/011 state.

## Follow-Up

- Keep `ISSUE-010` and `ISSUE-011` open; run the separate AutoFishing and ActionSpeed speed/disable/title GC ladders after Batch 2.
