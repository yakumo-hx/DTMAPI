# 0.5.5 Prerelease No-Demand Managed-Product Isolation Review

Status: `recorded`

Date: 2026-07-28

Owning Update: [20260727-0001 DTMAPI 0.5.5 prerelease route](../../../updates/2026/20260727-0001-dtmapi-055-prerelease-route.md)

## Trigger

Step 6 ran the formal 300-frame warmup plus 10,000-frame no-demand gate
against the frozen Runtime candidate. `GAME-SMOKE/20260728-043724` reached all
10,000 measured frames without a Fatal window, but the gate correctly failed:

- `ActiveOptionalDemandIdsAtStart/End` contained
  `Event.GameLoop.ReturnedToTitle`, `Event.GameLoop.UpdateTicked`, and
  `Event.Save.SaveLoaded`;
- `EventArgsCreated.Delta` was `10000`;
- the Runtime log attributed the active event handlers to
  `DTMAPI.MineMod` and `DTMAPI.StrongPlantingGunMod`;
- Compatibility remained dormant, audio/content state remained empty, no
  optional GameBridge updater was active, and optional Hook-install,
  reflection, file, directory, projection, retained-callback, and native
  updater deltas were zero.

The smoke result is non-acceptance evidence. It must not be described as a
passing no-demand or GC result merely because it had no Fatal window.

## Root Cause

`CoreOnly` is an official-Mod profile. It isolates Workshop and official-local
entries through `mod_infos.json`. The existing no-demand runner also clears and
restores current Author SDK source selections. Neither operation cold-disables
already deployed receipt-bound Advanced products under `Doloc Town/Mods`.

Mine and StrongPlantingGun remained physically deployed and enabled from prior
bounded product work. Mine's ProductNative `UpdateTicked` subscription accounted
for one event publication and `EventArgs` instance on every measured frame.
Their lifecycle event registrations also made the whole-Runtime optional-demand
set non-empty. The no-demand receipt therefore measured active products rather
than an inactive optional-product boundary.

## Rejected Hypotheses

- Compatibility Host did not self-activate: the log reported
  `Compatibility.Host = dormant`, `loaded=false`, `services=0`, `demand=0`,
  `callbacks=0`, and `hooks=0`.
- Content/audio domains did not perform optional work: their registrations,
  pending work, callbacks, files, directories, reflection and native-updater
  counters stayed empty or zero-delta.
- The QA observer was not counted as a product updater: its explicit
  `GameBridge.QaHost` demand and updater remained separately identified as
  required QA instrumentation. The unexpected demand IDs were Core Event
  capabilities owned by the two deployed products.
- The failure was not caused by failing to reach the frame target:
  `WarmupFrameActual=300` and `MeasuredFrames=10000`.

## Required Correction

The existing no-demand wrapper must add one reversible pre-launch isolation
transaction for physically deployed managed products:

1. run only while holding the shared Runtime lock and with no game process;
2. inspect direct children of `Doloc Town/Mods`;
3. accept a target only when its `.dtmapi-author-receipt.json` parses as an
   Advanced deployment and its receipt destination exactly matches the physical
   directory;
4. preserve any pre-existing `dtmapi.disabled` file unchanged;
5. create a run-unique marker only where the marker was absent;
6. after process exit, remove only markers whose exact length and SHA-256 still
   match the wrapper-owned marker;
7. fail closed and retain recovery instructions if the game is still running,
   a receipt is malformed, a marker is a non-file, or marker bytes changed.

This is test-environment isolation. It must not delete product directories,
modify receipts or packages, alter player save archives, or broaden Runtime
product ownership.

## Acceptance

- Focused source tests cover exact receipt/destination validation, pre-existing
  marker preservation, exact owned-marker cleanup, and tamper refusal.
- The formal no-demand run is repeated once against the same frozen candidate.
- Both boundary snapshots have zero optional product demand and updater IDs.
- `EventArgsCreated`, optional file/directory/projection/reflection/native
  updater/retained callback/Hook-install work, and diagnostic revisions have
  the expected zero deltas.
- Compatibility/content remain inactive; no feature Hook is installed by
  optional demand.
- 300 warmup plus 10,000 measured frames, candidate/installed hashes, QA run
  identity, `NoNativeSave`, title cleanup, profile/source restoration and
  process exit all pass.

The correction does not authorize a full Release, an L0-L5 replay, a long soak,
or any claim that the broader Unity/Mono Fatal class is solved.
