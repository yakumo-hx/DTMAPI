# 20260713-0003 Mod Management And Source Ownership Boundary

Status: recorded / source and lifecycle gaps open
Date: 2026-07-13
Scope: DTMAPI CodeMod enablement, official/Workshop source ownership, unsubscribe deletion, local sources, and external BepInEx plugins
Related decision docket: `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0001-major-update-second-decision-docket.md`

Third-round source decision: `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md` selects Workshop as the player authority for an enabled subscribed first-party product, permits a sole valid OfficialLocal source when no valid Workshop source exists, and moves per-Mod local override plus Workshop-validation/player-reproduction modes to Author SDK. Source changes after DLL load remain restart-required; no override grants deletion/overwrite ownership.

## Source Request

The user asked why DTMAPI can manage Mod enable/disable state, why unsubscribing an official Workshop Mod can remove its files, and why a Mod installed directly under `BepInEx/plugins` is outside DTMAPI management.

## Three Different Owners

“Mod management” currently combines three distinct kinds of ownership:

| Source/type | Who downloads/removes files | Who owns enablement | Who loads executable code |
| --- | --- | --- | --- |
| Workshop official content or DTMAPI CodeMod | Steam owns the subscription directory | Doloc Town `ModManager` / official UI | game loads official JSON; DTMAPI loads the CodeMod DLL |
| local DTMAPI Mod under game `Mods` | user/DTMAPI development tools | `dtmapi.disabled` or `.disabled` marker | DTMAPI |
| official-local package under persistent `MODS` | user/installer owns files | Doloc Town `ModManager` | game loads JSON; DTMAPI loads a CodeMod when present |
| external plugin under `BepInEx/plugins` | user/third-party installer | BepInEx/plugin configuration | BepInEx Chainloader |

The current DTMAPI Manager is status/config oriented. Its own UI text says that official enable/disable and ordering remain with Doloc Town or Steam. Although local `game/Mods` entries are marked as DTMAPI-toggleable internally, the current player UI does not implement a complete source-toggle workflow for them.

## Why DTMAPI Can Stop A DTMAPI CodeMod

DTMAPI owns the CodeMod loading transaction:

```text
scan a known source root
  -> read DTMAPI manifest
  -> check source enablement
  -> check MinimumDTMApiVersion and dependencies
  -> begin owner transaction
  -> Assembly.LoadFrom
  -> instantiate DtmMod
  -> Entry(owner-bound helper)
  -> publish owner resources and commit
```

Because event, input, config, API, content, CustomEntity, and GameBridge registrations are owner-bound by `UniqueID`, DTMAPI can remove those known roots when a source disappears or becomes disabled and cascade required-dependency deactivation.

This is not complete DLL unloading. Once Unity Mono loads an assembly, DTMAPI cannot unload it from the process or prove removal of arbitrary static state, Harmony patches, native callbacks, or Unity objects. Current same-process deactivation therefore means:

```text
remove DTMAPI-owned roots
+ stop further managed participation where supported
+ mark restart-required after a code assembly was loaded
```

## Why Workshop Unsubscribe Removes Files

A Workshop functional Mod is read in place from its Steam subscription folder. Steam owns the mapping from Workshop item id to that dedicated directory. The official native `ModManager` asks Steam UGC for the current subscribed ids and their install directories.

- **Disable**: official enablement changes; files remain.
- **Unsubscribe**: Steam removes the subscription from its current set and normally reclaims that item's directory.

The file deletion is performed by Steam, not DTMAPI. On a later scan DTMAPI observes that the source no longer exists and deactivates the owner.

DTMAPI Runtime is deliberately different. Its Workshop item is an installer source. The player runs a BAT/PowerShell installer which copies the five runtime DLLs to `BepInEx/plugins/DTMAPI`. Steam updates or deletes the subscription package, but it does not own the copied destination. Therefore neither update nor unsubscribe changes the actually installed Runtime until the installer/uninstaller runs.

## Why A Direct BepInEx Plugin Is Outside DTMAPI

BepInEx scans `BepInEx/plugins` and invokes classes bearing its plugin metadata. That happens at the chainloader layer, outside the DTMAPI manifest scanner and owner transaction. DTMAPI has no reliable `UniqueID`, dependency declaration, registration ledger, install receipt, or cleanup contract for the foreign plugin.

Current local BepInEx log evidence illustrates the timing:

```text
2 plugins to load
Loading [连击大剑Mod 1.0.0]
Loading [DTMAPI Bootstrap 0.5.3.0]
```

The external plugin can execute before DTMAPI itself. Even when load order differs, DTMAPI still does not own the assembly/Harmony lifecycle. A DTMAPI CodeMod DLL placed in that directory without valid BepInEx plugin metadata may simply fail to execute; DTMAPI does not scan that root.

Recommended policy:

- never claim DTMAPI can enable, disable, update, or uninstall an external BepInEx plugin;
- add a read-only doctor that lists non-DTMAPI plugin DLLs, plugin id/version/path/hash, DTMAPI.Abstractions references, and adjacent manifests;
- hard-error only on a positively identified misplaced DTMAPI CodeMod;
- label other plugins `external / not managed by DTMAPI`;
- never move or delete external/unknown DLLs automatically.

## Current Source Gaps

### 1. OfficialLocal can shadow a newer Workshop DLL

Duplicate selection first prefers enabled sources, then uses this fixed priority:

```text
OfficialLocal -> Workshop -> game/Mods Local
```

When an old official-local copy and the auto-updated Workshop package share a `UniqueID` and both are enabled, DTMAPI selects the old official-local copy. Current local logs already contain multiple OfficialLocal/Workshop duplicate warnings.

The new “Workshop functional Mods update automatically; Runtime is manually installed” model therefore also requires:

- repair the ownership P0 first;
- safely migrate verified old DTMAPI official-local copies;
- make Workshop the normal player authority for catalogued published products;
- permit local override only through an explicit developer mode;
- report `[SHADOWED]` with both versions/paths instead of silently selecting a stale copy.

Unknown third-party duplicates must not be deleted or reordered through guessed ownership.

### 2. The official UI notification can read pre-save state

Current native `ModUiState.Hide` performs:

```text
ModManager.ReloadMods()
  -> DTMAPI ReloadMods Postfix
  -> DTMAPI rereads disk SAVE/mod_infos.json
SaveModManager(modManager)
DolocConfig.Reload()
```

DTMAPI's Postfix can therefore read the old on-disk enablement state during the first official UI close. Existing source tests which write the file first and then invoke refresh do not prove the real player sequence.

A future fix should refresh after a successful native save or pass a reviewed in-memory ModManager snapshot through GameBridge to Core. It requires player testing of first-close enable, disable, dependency cascade, config-page locking, restart-required state, and the next launch.

### 3. Directory presence is not subscription proof

The native `ModManager` uses Steam's current subscription list. Core currently enumerates every directory under `workshop/content/2285550` and combines it with the persisted enablement file. If Steam cleanup is delayed and the previous `enabled=true` row remains, a residual directory may briefly look like an active source to DTMAPI.

Long term, GameBridge should provide a native subscribed/official Mod snapshot. Core should not treat bare directory presence as authoritative subscription state. Until that owner is rebuilt, status should diagnose residual Workshop directories rather than silently treat them as current subscriptions.

## Compatibility Consequence

Workshop functional Mod DLLs are loaded directly from updated subscription folders, but Runtime DLLs are copied and remain stale. The required status model is therefore:

```text
latest subscribed DTMAPI installer-package version
actual BepInEx-installed/running DTMAPI version and hash
selected functional Mod source/version
functional Mod MinimumDTMApiVersion
shadowed duplicate source/version, if any
```

Current Core source calls `CanLoadApiVersion` before resolving/loading the CodeMod assembly, which is the correct order. The real “new AutoFishing + old installed DTMAPI” path remains unverified and must prove that `Entry()` is never reached, no missing-member/assembly error occurs, and the player receives a manual reinstall instruction.

## Acceptance Gates

- real official UI first-close enable/disable state is observed only after authoritative save/in-memory snapshot;
- Workshop unsubscribe plus delayed/missing directory cleanup cannot load an unsubscribed CodeMod;
- an enabled current Workshop product is not shadowed by a stale verified local copy in player mode;
- local developer override is explicit and clearly displayed;
- deactivation cleans all DTMAPI-owned roots and truthfully reports restart-required;
- external BepInEx plugins remain untouched and are shown in a separate read-only category;
- updated Workshop installer package versus stale installed Runtime reports `[UPDATE]` before support tools say the install is ready.
