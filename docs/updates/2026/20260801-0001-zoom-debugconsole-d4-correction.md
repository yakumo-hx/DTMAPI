# Zoom Z1、DebugConsole 与 D4 输入所有权修正

- Update ID: `20260801-0001`
- Date: `2026-08-01`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime, player`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: `core/input/events/modal/zoom/camera/debugconsole/ui/productnative/workshop/manual-profile/runtime/0.5.5`
- Source Request: implement the approved Zoom Z1, D4 input-audience and DebugConsole hiding plan, then stage the exact twelve-item player profile while retaining the Runtime lock
- Root-cause Review: [20260801-0001 Zoom、Y 键控制台与 D4 输入所有权复核](../../reviews/manual-qa/2026/20260801-0001-zoom-debugconsole-d4-input-review.md)

## Result

The bounded implementation and final player acceptance are complete; this
Update is `verified`:

1. Zoom now owns only the live camera's `orthographicSize`. Product scale,
   restore, disable and cleanup paths never call native resolution refresh or
   write follow, enabled, camSize, range or position. A product-owned Prefix and
   Finalizer let a genuine `CameraController.RefreshResolution()` run against
   the retained 1x baseline and reapply the selected multiplier without
   swallowing native exceptions.
2. Core no longer pauses Mod `UpdateTicked` while a modal is open. It freezes a
   per-frame Normal/owner-modal/platform-modal input audience, targets modal
   input to the owner, settles Releases to exact Pressed recipients, retains old
   bindings until settlement, requires neutral rearm after eligibility loss and
   separates physical input from current-frame suppression.
3. DebugConsole no longer constructs the unfinished generator, monster or
   resource buttons or their adjacent owner-status text. Their ProductNative
   executors, translation keys, frozen ABI and Compatibility paths remain.
   Ordinary D4 `UpdateTicked` delivery now drives the existing dirty redraw path;
   no product-private polling loop was introduced.

Public ABI and the retained old MoreEquipmentSlots `0.3.1-dtmapi` package are
unchanged. DTMAPI input isolation still does not promise to suppress direct
Unity reads, third-party Harmony patches or native game actions.

## 2026-08-01 second hand-test corrections

The first combined hand test accepted Zoom Z1, D4 redraw/input behavior, the
hidden world actions and the remaining Mod combination, then exposed two
separate boundaries. This Update has returned to `in-progress` until the new
candidate is rebuilt and handed back for player verification.

1. The official Mod page no longer treats both native `ReloadMods()` calls as
   committed source changes. `Register()` begins a preview-only transaction;
   `Hide()` stages the close candidate; only the exact manager's successful
   native `SaveModManager()` plus completion of the generated close callback
   queues one DTMAPI publication/rediscovery on the next GameBridge frame.
   Failed persistence, missing capture, wrong manager identity or interrupted
   close retains the last committed authority.
2. DebugConsole has retired all production reflection of
   `MotionAbility.MoveScaler/SetMoveScaler`. ProductNative now owns one guarded
   `BodyController.get_MoveSpeed` Postfix and multiplies only the current
   `DolocAPI.agent` result after native motion, terrain and equipment
   calculation. Closing Y keeps the factor; `1x`, SaveLoaded, title, disable,
   Loader and owner cleanup clear only the product factor. The frozen
   Compatibility backend uses the same implementation under its existing
   mutually exclusive owner.

No public API, save data, sidecar schema, native Buff field or Workshop package
identity is changed by either correction.

### Second-correction automated validation before packaging

- Complete Release `DTMAPI.UnitTests` and `DTMAPI.QaUnitTests` pass. The
  official-page fixture proves opening preview non-publication, failed-save and
  wrong-manager rejection, next-frame successful-close publication and exact
  committed-source replacement.
- The physical DebugConsole Harmony fixture proves the 19-patch Product owner,
  the four-patch on-demand Compatibility route, exact mutual exclusion and
  owner-zero cleanup. Unit coverage proves a later native/Buff speed change is
  multiplied, console close retains the factor, player-body replacement moves
  the factor without touching the retired body, and 1x clears only product
  state while both test `MoveScaler` sentinels remain unchanged.
- Release production build, Catalog (`27/11/22/48`), Batch 4 semantic boundary
  (`283` production source files), tracked synthetic ABI, test-artifact
  governance and document governance pass. The Catalog count changed only for
  the new Workshop transaction source and final-speed Hook source. The public
  API row set and frozen Abstractions hash remain unchanged.
- Catalog-driven Author SDK build/pack for `y-console` passes and emits
  `DTMAPI-YKeyConsole-advanced-pilot.zip` with SHA-256
  `DB67579E37AB3D899A36164C0085D14AF762FBF0AF20FCF539C91275D25CA733`.
  Runtime packaging and player-directory validation remain pending the clean
  implementation commit required by the package provenance gate.

## Changed authorities

- Core D4: `DtmApiRuntime`, `EventManager`, `WorkshopContentInputUi` and focused
  Unit coverage.
- Zoom Z1: ProductNative runtime/access/callback/Hook transaction and installer,
  physical Harmony fixture, Product tests and game fixture.
- DebugConsole: player UI construction plus
  [the deferred world-action route](../../planning/20260801-debugconsole-world-actions-roadmap.md).
- Workshop authority: the official `ModUiState` preview/save/close transaction,
  native subscription candidate capture and deferred committed publication.
- Contracts: public API matrix, input-lifetime design contract, Camera Hook Map,
  Batch 6 identity note and frozen Author SDK reference receipt.
- QA/deployment: owner-deactivation and Zoom fixtures, smoke runner ownership
  oracle, exact package/profile checks and this Update.

## Implementation commits

- `d389da0f`: commit the official Mod UI successful-close-save source
  transaction and the DebugConsole final player-speed factor.
- `772409eb`: Core D4 modal input ownership without pausing Mod updates.
- `af7a752a`: Zoom Z1 orthographic-only ownership.
- `31f5f02a`: hide unfinished DebugConsole world actions and record their future
  route.
- `0324c705`: keep the legacy sampling route allocation-free under D4.
- `a13f1597`: rebind the frozen `0.5.5` Abstractions payload after the contract
  projection changed.
- `80ae07c8`, `3d647f97`, `c633d01b`: correct the Zoom QA native type, exact
  Prefix/Finalizer count and three-patch owner-cleanup topology.
- `4eb2fbe2`: bind DebugConsole title cleanup to stable Advanced receipt,
  expected Harmony owner and `product-native` host evidence instead of the
  removed incidental `nativeOwner=ProductNative` action token.

## Source and unit validation

- Release production build passed with zero production warnings/errors. The
  Unit fixture compilation still reports ten pre-existing nullable warnings in
  old DebugConsole reflection helpers; this Update neither introduced nor hid
  them.
- Complete `DTMAPI.UnitTests` and `DTMAPI.QaUnitTests` passed after the final QA
  correction. Focused `phase1-core-cleanup`, `zoom-product`,
  `zoom-acceptance-routing` and `debugconsole-product` routes also passed.
- D4 cross-state tests cover continuous updates under modal, immutable
  same-frame audience, targeted Pressed/Released settlement, held-key neutral
  rearm across modal/official UI/scope/save/title/config boundaries, old chord
  retention, suppression isolation and Normal/owner-modal 10,000-frame
  zero-allocation steady state.
- Zoom Unit/fixture coverage proves three all-or-none patches, no
  product-initiated refresh, 1x native-refresh observation, normal/exception
  Finalizer restoration, `4x -> refresh -> 2x`, scene/title/disable/Loader
  restoration and ProductNative/Compatibility mutual exclusion.
- Catalog passed at 27 products, 11 public products, 22 Workshop items and 48
  API rows. Exact retained ABI, Advanced owner/policy, package layout, Author
  SDK, zero-leftover and document-governance gates passed. The frozen
  Abstractions SHA-256 is
  `D04D34CD756C18189314E68891E933CEB5C060A79BA1EB54AF21832E5F3F2BF8`.

## Runtime evidence

`GAME-SMOKE/20260801-033908` is the final valid third-save `NoNativeSave`
focus:

The smoke-loaded Runtime reports `BuildCommit=80ae07c85ec5`. From that commit
through the frozen package provenance `4eb2fbe26f25`, the only tracked changes
are optional QA fixture expectations, Unit source and the smoke runner; no
player-shipped Runtime or product source changed. The final provenance rebuild
was installed and Doctor-checked after the smoke, but deliberately not launched
again before the user's hand test.

- Zoom reports 2x `33.75`, 1x `16.875`, 4x `67.5`, then
  `4x -> RefreshResolution -> 2x = 33.75`; both `SetEnvCamera` transitions
  reapply `67.5`. Exact Hook inventory is three patches over two native targets.
- The real Y/Escape matrix passes eight opens, one Escape close, six Y closes,
  ten short taps and the held-Y no-flicker gate. DebugConsole receives its
  ordinary update stream, returns to title with every Canvas/EventSystem/
  Button/InputField/ScrollRect/listener/binder/root count at zero, and Loader
  deactivation reaches zero leases, instances, Hooks, input, UI and owner roots.
- Zoom Loader cleanup reaches zero instance, actual/derived patches, callback
  and roots. The game exits without a fatal window or remaining process.
- The selected player archive and committed sidecars are hash/length/mtime
  unchanged before cleanup; no player archive restoration or writeback was
  required.
- `Unity-Player.log` is 262,145 bytes, `DTMAPI-latest.log` is 213,901 bytes and
  `BepInEx-LogOutput.log` is 232,297 bytes. All three contain zero logged Error
  and zero logged Warning records; the two text occurrences of `Exception` are
  `exceptionType=none` continuation fields.

Earlier `024118`, `030534`, `030825`, `032628`, `032919` and `033146` runs are
preserved non-acceptance diagnostics. They respectively exposed an invalid
HookProbe route, an invalid Compatibility/Product double route, a namespaced
native target error, obsolete one-callback expectations, obsolete one-target
owner cleanup and the removed incidental owner token. The final run supersedes
only those QA/oracle failures; it does not rewrite their evidence.

## Frozen packages and installed player state

- Final Runtime source/QA provenance is `4eb2fbe26f25`. Both builds contain
  exactly 30 files and 71,587,452 bytes. Their only byte difference is the
  release-manifest `BuildTime`; after replacing only that value with the fixed
  `<normalized>` token and applying the path/NUL/length/SHA-256 aggregate, both
  tree hashes are
  `DD2290D09FFDF775DD983B6C72CE5F5869A428429F35FBDE9E4CB4A493C86083`.
- Deterministic Zoom ZIP A/B SHA-256 is
  `AE5220A51B80ECF98604B010C75F9DDB48F0E722B004DD970479C41FF9DE5740`;
  DebugConsole ZIP A/B SHA-256 is
  `841C9F0C08D4D5C54D876EF3ADB92CF02DD19FA51A95A40FA295FB812ECA17C0`.
- The actual Runtime subscription contains those exact 30 files plus the
  original byte-preserved `workshop.json` SHA-256
  `D6D9206A4A58B88CC985EE72832D57F226FF58767EF6E606A2731B0D604EF98D`.
  It has no reparse entry. The final actual-directory PowerShell 5.1 player
  package matrix at
  `temp/20260801-z1-d4-final/actual-final-subscription-audit/DTMAPI Workshop Audit 20260801-034654`
  reports zero blockers.
- The installed five Runtime assemblies are byte-identical to the final
  package and their installed provenance is `4eb2fbe26f25`. Status plus the
  read-only Player Doctor report exactly five expected Runtime artifacts with
  zero errors and zero warnings.
- Root and `SAVE` `mod_infos.json` both contain exactly the requested twelve
  enabled Workshop entries at priorities 0 through 11: Runtime, AutoFishing,
  ActionSpeed, Manbo, FishRoe, AnimalHusbandryProgress, Zoom, DebugConsole,
  retained MoreEquipmentSlots `0.3.1`, MoreSaves, OneActionComplete and
  ChestLocatorEnhancer. All other entries remain disabled.
- The pre-change Runtime/Zoom/DebugConsole state remains recoverable through
  revision `before-z1-d4-a13f1597`; Manbo's prior tree remains under
  `before-functional-four-current-53bc614a`. Every pre-existing `workshop.json`
  is byte-preserved:
  Runtime and the other ten product directories contain theirs. The exact
  seven-file Manbo subscription and its retained pre-replacement revision never
  contained `workshop.json`, so no synthetic file was invented. Manbo source is
  present at `products/first-party/ManboCardboardAudio`. At that handoff the game
  was stopped and the shared Runtime lock remained held for the user's test.

### Second-correction package and installed state

The preceding `4eb2fbe2` package paragraph remains the accepted first Z1/D4
candidate record. It is superseded for the new movement/official-page hand test
by implementation commit `d389da0fe89b`:

- the rebuilt Runtime stage has exactly 30 files, 71,593,596 bytes, zero
  reparse entries and release-manifest `BuildCommit=d389da0fe89b`;
- the actual Runtime subscription has those exact 30 package files plus the
  byte-preserved original `workshop.json` SHA-256
  `D6D9206A4A58B88CC985EE72832D57F226FF58767EF6E606A2731B0D604EF98D`;
- the rebuilt DebugConsole package has 10 files and 478,455 bytes. Its ZIP
  SHA-256 is
  `DB67579E37AB3D899A36164C0085D14AF762FBF0AF20FCF539C91275D25CA733`,
  and the installed `DTMAPI.DebugConsole.dll` SHA-256 is
  `102E616C598BD4576C37014E7974C6934AD552CF2208B1B7D3B49D24FC6180FF`;
- the DebugConsole subscription retains its original `workshop.json` SHA-256
  `514C3829DA8FABDA78EC0B3934408F9B73D3D40089FAEC7DB6E6B011A953FA6F`;
- the pre-replacement Runtime and DebugConsole trees are recoverable at
  `D:/steam/steamapps/common/Doloc Town/DTMAPI/backups/workshop-subscriptions/before-official-ui-final-speed-d389da0f`;
- the actual subscription player-package matrix reports zero blockers at
  `temp/20260801-official-ui-final-speed-final/actual-subscription-audit/DTMAPI Workshop Audit 20260801-152416`;
- the packaged installer reports exact five Runtime assemblies and matching
  provenance `d389da0fe89b`; read-only Player Doctor reports five expected
  artifacts, zero errors and zero warnings;
- root and `SAVE` `mod_infos.json` still contain exactly the requested twelve
  enabled Workshop entries at priorities `0..11`; no other Mod was enabled and
  retained MoreEquipmentSlots remains Workshop `3744059735` version `0.3.1`;
- At that handoff Doloc Town was stopped and the shared Runtime lock remained
  held. No automated game smoke was launched for this hand-test candidate;
  final Runtime validation comes from the user's accepted run and its clean
  DTMAPI/BepInEx/Unity logs.

## Final manual acceptance

The first combined test confirmed moving-player camera follow without 4x
flicker, immediate DebugConsole source/category/page redraw, the three hidden
world-action controls and the remaining twelve-item combination. The final
focused test then confirmed all three open boundaries: 2x/3x/4x remains active
after closing Y, 1x restores normal movement without removing the native Buff,
and one official Mod-page open/close produces no new DTMAPI warning.

Both latest DTMAPI sessions contain zero Warning and zero Error/Fatal records;
their BepInEx/Unity companions contain no hidden exception or crash record. The
exact logs, accepted package freeze, subscription rollback, upload-tree hashes
and publication authorization are owned by
[Update 20260801-0002](20260801-0002-workshop-upload-release-closeout.md).

The complete Release suite was deliberately not repeated before or after this
narrow hand-test candidate, as requested. Final publication authorization is
therefore exact-tree bounded rather than a reusable rebuild authorization.

## Rollback

- Revert Core D4, Zoom Z1 and DebugConsole UI commits independently if source
  rollback is needed.
- Restore the retained subscription revision rather than reconstructing a
  Workshop directory in place.
- Keep the game stopped and shared Runtime lock held until the user accepts or
  requests rollback. The final closeout released that lock only after exact
  subscription restore and upload-directory validation.

## Follow-up

- Rebuild ContentQuery as a thin official layer, simplify lifecycle phases and
  move Lite/Full logging to the previously selected next-version route.
- Revisit DebugConsole generator/monster/resource UX only through the dedicated
  future route and a new save/world-mutation review.
