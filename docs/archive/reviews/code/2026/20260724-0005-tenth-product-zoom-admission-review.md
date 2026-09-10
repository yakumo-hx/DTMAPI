# Tenth Product Zoom Admission Review

**Review ID:** `20260724-0005`

**Date:** 2026-07-24

**Status:** recorded — GO for Zoom as the only admitted tenth product;
DebugConsole and Mine remain outside this ProductNative slice

**Scope:** admission-only comparison after the independently reaccepted
nine-product baseline. This Review decides identity, native owner, compatibility
cost, removable default-Runtime code and the minimum focused/runtime gate. It
does not itself claim implementation or acceptance.

**2026-07-24 correction:** the retained-artifact exact set contains 35
CameraView MemberRefs, not 36: the request DTO has one constructor plus eight
setters. Review `20260724-0006` initially reopened implementation acceptance,
then closed it after the corrected exact-final-candidate evidence; neither
correction changes this admission-only GO.

## Verdict

**GO: admit exactly `DTMAPI.ZoomMod` as the tenth Advanced ProductNative
product.**

Do not admit DebugConsole or Mine in this slice:

- DebugConsole's reflected UI currently depends on Core owner-bound modal
  lifetime plus three GameBridge native-input isolation Hooks. Advanced
  products have no existing seam for that boundary. Moving it now would require
  a new public/product-specific protocol or leave the real UI executor outside
  ProductNative.
- Mine remains `split-decided` / `PrototypeBlocked`: its runtime state spans
  native power, archive time, scheduler/RNG, storage, economy, recipe/tech and
  renderer ownership rather than one native owner.

Zoom has no product save data, one real product consumer and a bounded native
state holder. The migration may reuse the existing Advanced SDK, Catalog,
single Compatibility Host, Doctor/Manager and package lanes. It adds no new
Host, public API, receipt family or SharedNative component.

## Candidate Comparison

| Candidate | Gross default-Runtime opportunity | Owner and risk | Decision |
| --- | ---: | --- | --- |
| **Zoom** | Camera feature directory: **1,511 physical / 1,356 non-empty lines** before thin proxy/demand retention | `DolocAPI.mainCamera.orthographicSize`, product scale/vanilla snapshot and one `DolocAPI.SetEnvCamera` postfix; no product save data; medium visual/lifecycle risk | **GO; sole tenth product** |
| DebugConsole | Reflected Bootstrap UI: **2,335 physical / 2,123 non-empty lines** | Core modal owner, GameBridge input-isolation Hooks and eight diagnostic APIs cross the current Advanced boundary; retained ABI cost is high | **NO-GO; keep optional diagnostic-host track** |
| Mine | Machine feature: **1,468 physical / 1,315 non-empty lines** | Multiple state holders and unresolved economy/power/restoration blockers | **NO-GO; remain PrototypeBlocked** |

These are gross physical source opportunities, not promised net, repository,
download, install or total-shipped reductions.

## Frozen Identity

| Fact | Value |
| --- | --- |
| UniqueID | `DTMAPI.ZoomMod` |
| Catalog ID | `zoom` |
| Workshop ID | `3742717440` |
| Retained published version | `0.4.2-dtmapi` |
| Target | `1.0.0`, minimum DTMAPI `0.5.5` |
| Official folder | `DTMAPI_Zoom` |
| Package DLL | `DTMAPI.Zoom.dll` |
| Canonical config | `DTMAPI/config/DTMAPI.ZoomMod.json` |
| Canonical Harmony owner | `dtmapi.mod.dtmapi.zoommod` |
| Product save sidecar | none |

The tracked Author SDK must generate the Advanced manifest, reference receipt
and package. No hand-authored manifest or package is authorized.

## Native Owner And Product Boundary

- The game owns the live camera object, native follow/clamp behavior,
  `SetEnvCamera`, background/fog/panorama rendering and environment state.
- ProductNative owns the configured `1x..4x` scale, the exact vanilla
  orthographic-size snapshot, direct orthographic-size application/restoration,
  keybind/config/lifecycle and one exact non-suppressing
  `DolocAPI.SetEnvCamera(Vector2,Vector2,bool,bool,bool)` postfix that reapplies
  the active scale after the native reset.
- The product must not call `CameraController.RefreshResolution`,
  `CameraController.SetPosition` or `DolocAPI.RefreshScanner`, and must not claim
  background/fog/panorama synchronization.
- The existing ItemDisplayName environment-reset Hook remains a separate
  SharedNative responsibility. Similar native timing does not merge the two
  owners or authorize a new SharedNative fanout.

## Compatibility Disposition

The retained published `0.4.2-dtmapi` Zoom binary is the one real consumer of
the current Experimental `ICameraViewApi`. The exact retained-artifact scan
locks all 35 observed MemberRefs: `ICameraViewApi.AcquireLease/GetState`,
`ICameraViewLease.SetViewScale/Update/GetState/IsReleased/LastResult`, the
`CameraViewRequest` constructor and eight setters, the five
`CameraViewResult` getters, and the fourteen `CameraViewState` getters. This
larger set is produced by the real retained DLL (SHA-256
`DFA74BDD3561A9E9647FEC01AB9B48F095826D2A752AE920566BE2C5063971B3`);
the earlier three-member summary was a review error and is not an authority for
removing any DTO or state member.

The obsolete `ICameraZoomApi` family must also retain its exact binary member
shape. The new Advanced product must not consume either API.

The old CameraView/CameraZoom executors therefore move behind the existing
dormant-shipped Compatibility Host. Mandatory GameBridge may retain only thin
provider proxies, demand/owner coordination and the already-shared
ItemDisplayName reset route. Both activation orders must fail closed before
the product and compatibility service can own the same camera state.

No stability promotion or API expansion is authorized. CameraView remains
Experimental compatibility; CameraZoom remains obsolete compatibility.

## Minimum Acceptance

Focused checks must cover:

1. Product scale normalization, native read/write failure rollback, exact
   vanilla restoration and environment-reset reapplication.
2. One exact Harmony postfix installed all-or-none; target-resolution failure,
   install rollback, residual/mixed owner and exact-owner unpatch failure are
   fail-closed.
3. Config disable/re-enable, title/re-entry and real Loader owner deactivation
   clear the product instance, input registrations, callbacks, cached native
   state, Hook and Core roots.
4. Compatibility-first and product-first activation, retained MemberRefs,
   frozen API/DTO metadata and old owner lifecycle through the existing Host.
5. Catalog, Author SDK/package, Doctor/Manager and live zero-leftover checks
   over the exact ten-product candidate.
6. One third-save `NoNativeSave` short smoke: current DLL/HookProbe, vanilla to
   2x/4x, real `SetEnvCamera` reapply, disable/title restoration and clean
   exit. Archive and committed sidecars must remain unchanged before cleanup.

No complete Release, L0-L5, GC gradient or long test is authorized. A failed
bounded smoke may be repaired and rerun only at the smallest affected boundary.

## Admission Boundary

This GO authorizes one Zoom implementation Update only. It does not admit
DebugConsole, Mine, an eleventh product, G7, general Advanced authoring or the
0.5.5 release. It does not add SharedNative or permit claims about repository,
download, install or all-products-enabled total weight.

## Inspected Authorities

- `PROJECT.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/api/public-api-matrix.md`
- `tools/release/dtmapi-product-catalog.json`
- current Zoom, CameraView/CameraZoom and environment-reset sources
- retained published Zoom identity and member references
- reverse build `23762374_public_C416D4` camera/environment methods

This admission Review ran no build, Unit, package, game or Release validation.
