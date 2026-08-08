# Native UI Layout Repair Hook Map

## Boundary

DTMAPI owns a small platform UI repair for the native title text menu and
pause-menu layout that it extends. The production owner is
`NativeUiLayoutDiagnosticsFeature` plus `NativeUiLayoutRepairService`; the
optional QA facade is observation-only.

## Active Hooks

| Native target | Patch | Owner | Lifecycle | Status |
|---|---|---|---|---|
| `DolocTown.HomePageUiState.RenderTextMenu()` | Postfix | GameBridge native UI layout repair | process install; repair title menu after render | `installed`, runtime recheck pending |
| `DolocTown.UI.MainMenuPanel.OnStartShow()` | Postfix | GameBridge native UI layout repair | process install; refresh active pause menu when shown | `installed`, runtime recheck pending |
| `DolocTown.UI.MenuUI.SetCapacity(int)` | Postfix | GameBridge native UI layout repair | process install; repair exact pause-menu instance after capacity changes | `installed`, runtime recheck pending |
| `DolocTown.UI.GameDataPanel.SetCapacity(int)` | Postfix | GameBridge QA observation | process install; read-only observation | `installed`, runtime recheck pending |

`NativeUiLayoutRepairService.UpdateActiveMenuLayout()` remains a mandatory
active-menu repair reached from the frame dispatch, but the demand scheduler
invokes it at most once per 250 ms. It is not a Hook.

## Lifecycle

- The four Harmony patches are installed once for the process. Save load and
  return-to-title do not uninstall them; each callback and the 250 ms active
  repair act only on the currently observable native UI instance.
- GameBridge shutdown stops Hook retry sources, removes its UnityEvent and
  static callback roots, and clears the callback bridge. Harmony patch removal
  has no independent rollback proof, so the physical patches remain classified
  `ProcessPinnedDormant` until process exit.
- No title/save sidecar, persisted layout state or hot-unload promise exists.

## Retired Attempts

The 2026-07-31 U1 correction retired all six broad generic attempts:

- open `DolocGridUI<T>.ResetLayoutSize(int)` Postfix;
- open `DolocGridUI<T>.SetCapacity(int, int)` Postfix;
- closed `DolocGridUI<TextButton>.ResetLayoutSize(int)` Prefix and Postfix;
- closed `DolocGridUI<MenuButton>.ResetLayoutSize(int)` Prefix and Postfix.

Mono/Harmony rejected those generic targets in the 0.5.5 manual run, while
the exact production hooks above were sufficient for readiness. Their
callbacks, state fields and the patcher's closed-generic helper were removed,
so they are no longer installed or reported as player errors.

No global Unity `GridLayoutGroup.constraintCount` setter Hook is installed.

## Frame Driver

The GameBridge dispatcher continues to be reached through Bootstrap's
PlayerLoop driver, with InputSystem and MonoBehaviour `Update` paths available
as fallbacks. Its own 250 ms Timer remains health/reinstall-only and does not
dispatch Mod or layout updates. The obsolete reflected
`StartCoroutine(IEnumerator)` driver was removed on 2026-07-31; it is no
longer attempted and cannot create a startup Error after PlayerLoop succeeds.

## Evidence

- Source build: Release build passed on 2026-07-31.
- Focused Unit: the mandatory 250 ms route's active updater repaired synthetic
  title/pause drift; exact four-Hook tuple checks passed; source-absence checks
  proved the six generic attempts, their dedicated callbacks, the dormant
  global setter lane, `TryPatchClosedGeneric*`, and coroutine driver are absent.
- QA Unit: exact-hook readiness and title/pause observations passed.
- Runtime evidence gap: no game was launched for this source correction.
  Title layout, settings entry, return-to-title frame continuity and player
  error-count reduction remain pending in the next bounded game/manual run.

## Links

- [Owning Update](../../updates/2026/20260731-0002-autofishing-legacy-native-and-runtime-fallback-closeout.md)
- [Long-run Mono/GC issue](../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)
- [Historical Hook snapshot](../README-history-through-20260711.md)
