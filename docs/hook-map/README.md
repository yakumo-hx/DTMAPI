# DTMAPI Hook Map

This root file is a compact router. Hook ownership, signatures, lifecycle, and evidence should be split into focused domain files.

## Active Focused Maps

- [Camera](focused/Camera.md)
- [ActionCompletion and retired Oil route](focused/ActionCompletion.md)
- [ActionSpeed](focused/ActionSpeed.md)
- [DebugConsole input isolation](focused/DebugConsoleInput.md)
- [Experimental controller input and title config navigation](focused/ControllerInput.md)
- [AnimalViewer progress rendering](focused/AnimalViewer.md)
- [Fish roe title decoration](focused/FishRoeTitle.md)
- [ChestLocatorEnhancer inventory widening](focused/ChestLocatorEnhancer.md)
- [MoreEquipmentSlots protected equipment](focused/MoreEquipmentSlots.md)
- [StrongPlantingGun fixed-three ProductNative](focused/StrongPlantingGun.md)
- [Mine ProductNative production and visual lifecycle](focused/Mine.md)
- [Frozen Fishing automation compatibility](focused/FishingAutomationCompatibility.md)
- [Workshop source authority](focused/WorkshopSourceAuthority.md)
- [Optional-QA native load continuation](focused/NativeLoadContinuationQa.md)
- [Runtime lifecycle observations](focused/RuntimeLifecycle.md)
- [Native title and pause-menu layout repair](focused/NativeUiLayout.md)
- [Advanced synthetic fixture](focused/AdvancedFixture.md)

Create `focused/<Domain>.md` when a new or changed Hook boundary needs durable ownership or evidence. Do not rebuild a monolithic root table.

## Historical Snapshot

- [Full Hook Map through 2026-07-11](../archive/hook-map/2026/README-history-through-20260711.md)

The snapshot preserves all earlier Hook rows and evidence. It is a cutoff record, not the write target for new work.

## Hook Record Minimum

- native type and method signature;
- patch kind and DTMAPI owner;
- install/uninstall and save/title lifecycle;
- current status: `planned`, `installed`, `verified`, `degraded`, `blocked`, or `retired`;
- latest runtime evidence or explicit evidence gap;
- related issue, review, API row, and Update links where applicable.

Only update a focused Hook map when Hook facts changed. Source-only refactors that preserve the Hook boundary belong in the Update record alone.
