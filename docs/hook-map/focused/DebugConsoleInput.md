# DebugConsole Input Isolation Hook Map

Last updated: 2026-08-01

## Scope

This focused map owns the three native input-isolation Prefixes used while a
DTMAPI DebugConsole owner holds the in-save modal input cycle and the guarded
final-player-speed Postfix used by its movement diagnostic. The current
ProductNative `DTMAPI.DebugConsoleMod` owns the normal route. The exact retained
0.3.1 ABI may install the same three guards under a different owner only through
the optional Compatibility component. Neither route gives Bootstrap or
mandatory GameBridge authority over ordinary Mod hotkeys.

## Native Owners And Signatures

- `System.Boolean DolocTown.AgentControllerState::EnterUICheck(System.Single dt, DolocTown.AgentBehaviorSettings settings)` is the native global-menu transition owner. Its Prefix returns `false` with `__result=true` while isolation is active so the Escape that closed DebugConsole cannot continue into `MainMenuUiState`.
- `System.Void DolocTown.AgentControllerState::UseTool(System.Boolean force)` is the native tool-entry owner. Its Prefix skips the native method while isolation is active.
- `System.Void DolocTown.AgentControllerState::UseItem(System.Boolean force)` is the native item-entry owner. Its Prefix skips the native method while isolation is active.
- `System.Single DolocTown.BodyController::get_MoveSpeed()` is the final
  ordinary movement-speed owner after `MotionAbility` native modifiers,
  terrain moderation and equipment addition. Its Postfix multiplies the result
  only when `__instance` is the currently bound `DolocAPI.agent`; NPC and stale
  player bodies pass through unchanged. DebugConsole never reads, writes,
  snapshots or restores native `MotionAbility.MoveScaler`.

The signatures are anchored to `references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/methods.csv`. No game type is exposed through a public DTMAPI API.

## Patch Ownership And Lifecycle

- Current status: ProductNative `verified` in the frozen existing-Workshop
  update candidate; Compatibility resident-dormant until a real frozen-ABI
  consumer.
- Product owner: `dtmapi.mod.dtmapi.debugconsolemod`. Its atomic installer
  owns these three Prefixes, fifteen separate bounded creative/action patches
  and one final-speed Postfix. The input callbacks and active gate live in the Advanced product,
  not mandatory GameBridge.
- Product install lifecycle: Entry resolves all nineteen targets and installs
  one exact owner atomically. Before SaveLoaded and after title the three input
  Prefixes remain installed but gated inactive. The speed Postfix is also
  installed but has a `1x`/null-player fast pass until demanded. Loader
  deactivation unpatches all nineteen and must prove product
  instance/callback/patch/Core roots zero.
- Compatibility owner:
  `dtmapi.compatibility.debugconsole.legacy`. It demand-installs only the three
  input Prefixes while an old ABI modal or Escape drain is active. It separately
  demand-installs the one speed Postfix while a frozen movement consumer holds
  a non-1x factor, and removes that Postfix at 1x/save/title/shutdown cleanup.
  The Compatibility service is lazy and uses no product owner.
- Compatibility demand and physical topology are separate state. Repeating an
  unchanged modal/drain value is a warmed-frame no-op; moving from modal-open
  to Escape drain retains the same three Prefixes. Only a real zero/nonzero
  edge patches/unpatches targets or publishes Hook status.
- Compatibility input, creative and movement group transitions are
  transactional. A partial install is
  removed before the error propagates; a partial removal reconstructs the
  exact prior three-Prefix or fifteen-creative topology when possible. A UI
  open whose input acquisition fails releases its owner-bound modal and
  desired drain state.
- A combined transition and rollback-unpatch failure retains an explicit
  cleanup tombstone. The next reconcile or shutdown reduces the exact owner to
  zero before accepting a new desired topology; unchanged warmed frames still
  perform no Harmony observation.
- Compatibility Close is one UI/Hook/Core-modal transaction. Failed release
  restores the complete visible/open state and Core token for retry instead of
  publishing an invisible closed UI.
- The nested old action executor has one broker-visible lifecycle alias.
  `DebugConsole`-first and `DebugActions`-first construction therefore share
  the same exactly-once update/save/title/shutdown owner.
- Active condition in either route: its own `ModalOpen ||
  NativeInputDrainActive`. Outside that condition every Prefix returns control
  to the native method unchanged.
- Movement condition is independent of modal visibility. Closing Y restores
  time-scale/creative modal state but retains the selected player-speed factor;
  explicit 1x, SaveLoaded, ReturnedToTitle, disable, owner removal or shutdown
  clears it. Player replacement rebinds the same in-memory factor without
  touching either body's native fields.
- Product and old source cannot be selected as the same `UniqueID` owner in one
  Loader generation, but frozen Diagnostic consumers can still demand the old
  Host. Each physical installer therefore checks the other exact owner before
  touching any target:
  `dtmapi.mod.dtmapi.debugconsolemod` and
  `dtmapi.compatibility.debugconsole.legacy` are explicitly mutually
  exclusive in both installation orders.
- Escape close lifecycle: the owning UI starts the drain only after Escape
  closes its own modal. It continues reading the combined input backends while
  the console is closed, resets on another observed Escape edge, and ends only
  after two consecutive clean frames. This prevents a delayed copy of the same
  physical Escape from reaching the native menu owner.
- Title/owner/shutdown cleanup: title boundary reset, missing owner, UI
  disposal and Loader cleanup clear modal/drain state. The drain is bounded
  state only; it installs no additional target and performs no per-file
  polling.
- The separate old-DLL modal-Y compatibility lane is a Core owner-targeted event dispatch, not a Harmony Hook. Normal `Gameplay` scope and ordinary legacy broadcasts remain blocked while the modal is open.

## Validation

- `GAME-SMOKE/20260715-143510` first proved that a delayed Escape edge is drained before the following Y reopens the console, with `NativeMenuLeakDetected=false`.
- `GAME-SMOKE/20260715-145818` passed the complete normal-Steam, no-HookProbe external-input matrix: two Escape closes, ten 40 ms Y taps, a 1,800 ms hold, six owner-targeted retained-DLL Y closes, no `PostMessage` fallback, no native-menu leak, successful title cleanup and clean process exit.
- `GAME-SMOKE/20260715-153336` repeated that matrix after final provenance hardening and is the current acceptance result. It also proved the exact retained Workshop tree/DLL hash and exactly one Core load-source record for Workshop `3742714442`; all input and cleanup counts remained green.
- `GAME-SMOKE/20260727-083919` supersedes the normal-route ownership result for
  the Advanced product. It executes the current actions through
  `nativeOwner=ProductNative`, proves exact transient restoration, and records
  the complete `DTMAPI.DebugConsoleMod` title UI graph at zero before exact
  Loader cleanup.
- `GAME-SMOKE/20260727-085929` loads the exact retained 0.3.1 DLL against the
  then-current optional Host, passes the old modal/input matrix with strict 120 ms
  single-send taps, executes all seven action groups through
  `nativeOwner=Compatibility`, and records the old owner title UI graph at
  zero. It also contains 804 old exact-owner cleanup entries in about 28
  seconds and is therefore the reproduction for ISSUE-015, not a corrected
  steady-state result. The old action bodies and their creative/input Hooks
  are physically Host-owned; mandatory GameBridge is only a reflected broker.
- Isolated `NativeSaveExpected` `GAME-SMOKE/20260727-090702` proves the product
  Save here first click cannot call native save, an injected native rejection
  propagates and clears confirmation, and a newly confirmed retry performs one
  successful native save.
- Focused Unit covers 240 warmed frames with zero additional patch operations,
  topology transitions or status publications; modal-to-drain handoff;
  partial input/creative acquisition rollback; failed-removal prior-topology
  reconstruction; and failed-open modal/UI release.
- A focused real-Harmony metadata fixture proves ProductNative-first and
  Compatibility-first arbitration, with zero losing-owner patches. The updated
  fixture proves 19 Product patches, a four-patch Compatibility topology when
  input plus movement are demanded, current-player-only multiplication,
  composition with a later native speed change, untouched native MoveScaler
  and exact zero cleanup.
- Focused `debugconsole-product` Unit on 2026-08-01 proves the selected factor
  survives ordinary Y close, transfers to a replacement player body, and is
  removed at explicit reset without changing pre-existing or later native Buff
  aggregate values. The final player test proves 2x/3x/4x remains effective
  after Y-console close and explicit 1x restores normal displacement while the
  native movement Buff remains present.
- `GAME-SMOKE/20260727-104118`/`104214` prove failed-save Working mutation
  cold-reloads the prior committed money. `104302`/`104359` prove a successful
  save cold-reloads the exact new money. All four use a disposable fixture;
  both cold observers are `NoNativeSave`.
- `104513` and `104654` are non-acceptance old-route attempts and do not close
  current-byte Compatibility player reacceptance. The latter timed out in the
  runner after external-input readiness; it exited normally and its
  archive/profile/QA/source cleanup was proven exact.
- `GAME-SMOKE/20260727-132103` is the corrected exact-current retained result.
  Runtime provenance is `d7db257747a2`; the optional Host SHA-256 is
  `9920410DCF153D6FACB287A9F48E159FC8A9E4BE2D08159AB5352F454612DCE9`
  and the retained DLL SHA-256 is
  `E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E`.
  All external input and seven Compatibility action gates pass. Eight UI
  open/close cycles produce sixteen input topology edges, the later creative
  cleanup produces one edge, and 4,886 lifecycle updates produce zero
  owner-wide cleanup entries. ReturnedToTitle reports the entire console UI
  graph and exact patches at zero; NoNativeSave, QA/profile/source/Loader and
  process cleanup gates pass.
- Recovered missing-frame/frame-driver warnings in these minute-scale runs do
  not change Hook status and are not GC evidence.

## Rollback

Revert the Advanced product, lazy proxy and optional Compatibility rehome as one
atomic unit. Do not restore only the mandatory GameBridge Prefixes or only the
Bootstrap UI: either partial rollback creates dual/missing owners. If changing
the Escape drain, change the owning UI state and its same-owner Prefix
condition together; otherwise the guard can stay active incorrectly or leak
the close Escape into the native pause/menu owner.

## Related Records

- `docs/updates/2026/20260715-0013-batch2-steam-player-input-and-public-product-gates.md`
- `docs/updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md`
- `docs/reviews/manual-qa/2026/20260712-0002-y-console-close-double-toggle.md`
- `docs/debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md`
- `docs/debug/issues/ISSUE-015-20260727-debugconsole-hook-transactions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/api/public-api-matrix.md` Input row
