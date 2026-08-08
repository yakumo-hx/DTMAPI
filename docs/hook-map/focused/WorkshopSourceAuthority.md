# Workshop Source Authority Hook Map

Last updated: 2026-08-05

## Scope

This focused map owns the native Steam Workshop subscription snapshot used by
DTMAPI source selection. It covers the one-shot native-ready startup capture,
generic non-UI reloads and the official Mod-page preview/commit transaction. It
does not grant directory ownership, create or upload Workshop items, expose a
public API, or authorize package mutation.

## Native Owners And Signatures

The signatures are anchored to `references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/methods.csv`:

- `System.Collections.Generic.List<Steamworks.PublishedFileId_t> DolocTown.Config.ModManager::GetSubscribedMods()` owns the current subscribed-item set consumed by the game. Its body delegates to the Steam subscription APIs; DTMAPI calls the `ModManager` owner instead of calling `SteamUGC` directly.
- `System.Boolean DolocTown.Config.ModManager::GetSubscribedModDirectory(Steamworks.PublishedFileId_t item, System.String& dir)` resolves the native installed root for a subscribed item. A numeric Workshop directory without this exact native root is not source authorization.
- `System.Collections.Generic.List<DolocTown.Config.ModInfo> DolocTown.Config.ModManager::GetAllValidModInfos()` provides optional current native enablement, priority, official ID and root enrichment. It is not a substitute for `GetSubscribedMods`.
- `System.Void DolocTown.Config.ModManager::ReloadMods()` owns the native reload boundary. DTMAPI observes completion through a Postfix and never replaces the native reload body.
- `System.Void DolocTown.ModUiState::Register()` owns official Mod-page opening.
  Its Prefix starts a preview transaction before the page's first
  `ReloadMods()`.
- `System.Void DolocTown.ModUiState::Hide()` owns the request to close the page.
  Its Prefix marks subsequent native rows as a close candidate.
- `System.Boolean DolocTown.GameData.DataPersistenceManager::SaveModManager(DolocTown.Config.ModManager)`
  is the exact persistence result. Only `true` for the transaction's exact
  manager can authorize promotion.
- `System.Void DolocTown.ModUiState::<Hide>b__27_1()` is the generated delayed
  close transaction in build `23762374`. Its Postfix proves that native
  reload, save, config/cache/language reload and pending-box dismissal all
  returned normally before DTMAPI schedules a commit.

`DolocAPI.dataPersistenceManager` must exist before the `DolocAPI.modManager` getter is safe. The game's `GameManager.Awake` initializes that persistence owner; accessing the getter earlier can throw before any subscription method runs.

## Initial Native-Ready Capture

- Current status: verified.
- Patch kind: none. Initial capture is a one-shot Bootstrap lifecycle boundary, not a Harmony Hook.
- DTMAPI owner: `BootstrapPlugin` prepares Core, UI and GameBridge during its BepInEx `Awake`, installs the already-owned Unity PlayerLoop driver, and defers source capture and Runtime start. On the first native-ready PlayerLoop frame, `TryStartRuntimeOnce("PlayerLoop.FirstFrame")` calls `WorkshopSubscriptionSnapshotBridge.CaptureNativeWorkshopSubscriptions`, then `DtmApiRuntime.Start`, then GameBridge Harmony initialization.
- Ordering contract: the native snapshot exists before Core manifest discovery and duplicate-source selection. Harmony initialization remains after Core startup. The one-shot guard refuses a partial retry and does not poll for native readiness.
- State projection: GameBridge converts native rows into scalar Workshop ID, installed root, nullable enablement, priority and official-ID records. Core receives no raw game or Steamworks type. The SDK-readable snapshot written outside packages is a report/validation projection, never an ownership receipt.
- Lifecycle: initial capture runs once per process. Save load and title return
  do not rescan Steam. A generic native reload outside the official Mod page is
  an immediate recapture; official-page reloads follow the transaction below.

## Hook group: Workshop.ModUiCommit

- Current status: verified for the 0.5.5 frozen player candidate.
- Patch kind: exact Harmony Prefixes on `ModUiState.Register/Hide`; Postfixes
  on `ModManager.ReloadMods`, `DataPersistenceManager.SaveModManager` and the
  generated `<Hide>b__27_1` close callback.
- DTMAPI owner: `DolocTownGameBridge` installs all five process-lifetime
  observations. Core readiness remains pending unless every target is present.
- Opening contract: Register binds one UI transaction. Its reload may build a
  preview candidate, but cannot publish source authority, rediscover, activate,
  deactivate or notify QA.
- Closing contract: Hide clears any preview and arms one close candidate. The
  matching manager's reload stages immutable scalar rows; a different manager
  cannot authorize them. `SaveModManager=false`, capture failure, missing save
  observation or an interrupted close callback discards the candidate.
- Commit ordering: the close-callback Postfix queues, but does not execute, the
  commit. On the next GameBridge frame DTMAPI publishes subscription/root
  authority, then runs Core rediscovery/reconciliation and
  `WorkshopModListChanged`, then emits the dependent QA receipt. Native code is
  never re-entered by this commit.
- A generic `ReloadMods()` outside an active official-page transaction retains
  the historical immediate capture -> Core refresh -> QA order.
- Shutdown lifecycle: GameBridge shutdown stops retry sources and releases DTMAPI callback roots. The Harmony patch is process-lifetime; there is no per-save or per-title install/uninstall cycle.

## Failure And Enrichment Semantics

- `ModManager` unavailable, Steam not initialized, required `GetSubscribedMods` missing/throwing, or a non-enumerable required result makes the snapshot unavailable. Workshop Validation and ordinary Workshop player discovery fail closed: a raw numeric directory, even as a sole copy, is not a current subscribed-install source without native root proof. Official Local candidates remain independently selectable from their official state.
- A subscribed ID without a successful `GetSubscribedModDirectory` result remains subscribed-known but installed-root-unverified. Core rejects the enumerated numeric directory as an authorized Workshop source.
- `GetAllValidModInfos` is optional enrichment. If it is missing, throws, or returns a non-enumerable result, the already captured subscribed IDs and installed roots remain available; the bounded failure diagnostic is retained, `NativeEnabled` stays null, and the existing official enablement state remains authoritative for that scan. DTMAPI never invents `true` or `false`.
- If `GetAllValidModInfos` succeeds but omits the matching Workshop row, Core fails that enablement branch closed. Null or malformed optional rows are skipped without poisoning later valid rows.
- Duplicate subscribed IDs are collapsed deterministically. Failure to persist the SDK-readable projection is diagnosed but does not discard the current in-process native snapshot.

## No-Polling Boundary

This domain creates no `FileSystemWatcher`, recurring timer, file poll or Steam
readiness loop. Its triggers are the one-shot native-ready startup capture, a
generic reload completion, or the next frame after a successfully completed
official-page close transaction. Explicit Author sessions do not broaden this
rule, and ordinary-player startup with no session descriptor creates no author
listener or polling facility.

## Validation

- Unit coverage proves startup ordering, one-shot PlayerLoop fallback, required-owner failure, subscribed-without-installed-root rejection, optional enrichment failure without invented enablement, successful-empty enrichment fail-closed behavior, and null-row isolation.
- The 2026-08-01 transaction Unit covers opening preview retention, failed-save
  discard, exact-manager identity, no same-stack publication and one next-frame
  successful commit. The final player run opened and closed the real official
  page once and logged exactly one preview, one close candidate, one successful
  native save/close commit and one deferred DTMAPI refresh, with zero new
  warning or error records.
- The 2026-08-05 ISSUE-020 source correction Unit matrix proves official Local
  enabled/Workshop disabled, the inverse, both disabled, unique-greatest
  priority, tied/missing-priority blocking, stale numeric Workshop rejection and
  `<game>/Mods` exclusion. It also proves a legacy DTMAPI marker cannot silently
  disable an officially enabled Local package. This is source-only evidence;
  final exact-candidate cold-start acceptance remains pending.
- `GAME-SMOKE/20260715-182252` is superseded diagnostic evidence: the generic Steam/third-save lifecycle passed, but pre-native-ready access left the snapshot unavailable and established that BepInEx `Awake` was too early.
- `GAME-SMOKE/20260715-185255` verifies the corrected initial boundary under Steam: capture ran from `PlayerLoop.FirstFrame`, source authority was `nativeSubscriptions=True/43` before discovery, `Bootstrap.StartRuntime totalMs=1128`, third-save/title gates passed, and no `DolocTown.exe` remained.
- `GAME-SMOKE/20260715-191256` is the final normal-player acceptance row: normal Steam launch with no HookProbe or Author descriptor, initial native authority plus `ModManager.ReloadMods.Postfix` recapture, unchanged Core refresh, third-save/title cleanup and process exit all passed.

## Rollback

Revert the Bootstrap native-ready start boundary, GameBridge snapshot bridge, Core source-authority consumption and `ReloadMods` callback ordering as one Batch 3 slice. Removing only capture would leave Core fail-closed against an authority it can never receive; removing only Core consumption would turn a native snapshot into unused diagnostics. Do not replace the owner with direct `SteamUGC` calls or treat directories, `mod_infos.json`, author source state, receipts or journals as subscription proof.

## Related Records

- `docs/updates/2026/20260715-0016-batch3-author-sdk-preview.md`
- `docs/reviews/code/2026/20260715-0008-batch3-author-sdk-source-reload-boundary-review.md`
- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- `docs/reviews/manual-qa/2026/20260805-0002-enabled-official-source-arbitration.md`
- `docs/debug/issues/ISSUE-020-20260805-enabled-official-source-arbitration.md`
- `docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/debug/regressions/smoke-matrix.md`
