# 20260715-0008 Batch 3 Author SDK Source And Reload Boundary Review

Status: recorded
Date: 2026-07-15
Scope: freeze the implementation boundary for Batch 3 Author SDK 0.1.0, read-only Doctor, receipt-authorized deployment, four source modes, and explicit content reload
Related Update: `docs/updates/2026/20260715-0016-batch3-author-sdk-preview.md`
Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
Decision sources: `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md`, `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md`, `docs/reviews/code/2026/20260715-0006-batch2-closure-and-batch3-predecision-review.md`, and `docs/reviews/code/2026/20260715-0007-decision-escalation-policy-and-product-nodes.md`

## Source Request And Frozen Decisions

The user selected the complete Lamp compatibility shell, required Batch 2 to be committed as one reviewable boundary, and then requested completion of Batch 3. Batch 2 is closed by commit `7ee65e7ce72b8b58f815f700fb18321d977d0a3a`; its final exact-package subscription audit has zero blockers.

Batch 3 proceeds without another ballot under the frozen `SDK-A1` through `SDK-I1` defaults:

- Author SDK `0.1.0` is a Windows x64 self-contained .NET 8 portable CLI named `dtmapi-author`; generated game assemblies remain `netstandard2.0`;
- SDK `0.1.x` targets only Runtime `0.5.5` and bundles a fixed, hashed Abstractions reference, shared build rules, and offline compiler/reference assets rather than reading the player's install;
- `manifest.json` remains Runtime identity/version/minimum/dependency authority; SDK-only build/publish metadata remains separately versioned and explicitly unstable before SDK 1.0;
- there is no force, adopt, Workshop upload, account credential, or ordinary-Mod installation under `BepInEx/plugins`;
- Doctor is always read-only and exports human plus machine-readable findings;
- source override state is external to packages, keyed by canonical game root plus `UniqueID`, and is not an ownership receipt;
- player Runtime remains lifecycle-driven with zero file polling; explicit reload exists only inside an explicitly prepared author session.

QA extraction remains Batch 4, recurring-work removal remains Batch 5, and the independent AutoFishing/ActionSpeed GC ladders remain later release evidence.

## Native Owner And State-Holder Review

### Steam subscription authority

Current Core discovery enumerates Workshop directories and gives `OfficialLocal` higher duplicate priority than `Workshop`. That is incompatible with selected G1 player authority and cannot be corrected honestly by reversing a directory sort alone.

The current reverse baseline `23762374_public_C416D4` identifies the native responsibility chain:

```text
DolocTown.Config.ModManager.ReloadMods
  -> ModManager.GetSubscribedMods
  -> Steamworks.SteamUGC.GetNumSubscribedItems
  -> Steamworks.SteamUGC.GetSubscribedItems
  -> ModManager.GetSubscribedModDirectory
  -> SteamUGC.GetItemInstallInfo
```

The authoritative state holder is Steam's current subscribed-item set as consumed by `ModManager`, not raw directories and not stale `mod_infos.json` rows. The smallest bridge is a metadata-only snapshot captured through GameBridge reflection before Core's initial discovery and again inside the already-owned `ModManager.ReloadMods` postfix. Core then selects sources from that snapshot and the official enablement state; it does not call Steamworks or expose a new public API.

If the native subscription snapshot cannot be captured, Workshop Validation must fail closed and report the missing authority. Player mode may retain a clearly diagnosed compatibility fallback for a sole source, but must never claim a raw directory is a verified subscription.

### Explicit reload authority

Official Doloc Town JSON tables are owned by native `ModManager.ReloadMods` / `DolocConfig.Reload` and table merge state. Batch 3 does not claim a safe per-package rollback owner for those tables. CodeMod assemblies are process-lifetime under Unity Mono. Both classes therefore return `restart-required` and are never hot reloaded.

The current DTMAPI-owned content rebuild candidates were reviewed as:

- `CustomAnimalAnimatorBridgeService` for `Content/DTMAPI/custom-animals.json` and its DTMAPI-owned animator/PNG registration generation;
- `AudioReplacementService` for `Content/DTMAPI/audio-replacements.json` and its DTMAPI-owned WAV replacement generation.

Both existing refresh paths currently rebuild from live files and can replace a valid generation with an empty generation after a parse failure. Only Audio has a truthful Batch 3 atomic boundary: the service can parse one owner's complete replacement set, validate every WAV payload, and swap that owner's next generation only after all work succeeds. Invalid or partial Audio input retains the last-good generation and returns bounded diagnostics.

Custom Animals is not a Batch 3 reload format. Its same-path AssetBundle/controller cache, native `ModManager.CachedSprites`, live animal instances and save-lifetime contexts can retain old Unity objects after a file replacement. `custom-animals.json`, PNG and AssetBundle changes therefore return `restart-required` in Author SDK 0.1.0. Rebuilding that format requires a separate native-cache/lifetime owner review; directory/file presence is not sufficient authority.

No new gameplay Hook is required. The existing content Hooks remain unchanged; the new author-session transport only requests a DTMAPI-owned generation rebuild on the Runtime thread.

Required safety clause:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The owner was found for Steam subscription enumeration and the DTMAPI Audio replacement generation. A complete safe owner was not found for Custom Animals or transactional native official-table hot reload, so those branches are explicitly restart-only.

## 3.1 CLI, Templates, Packager, And Doctor Boundary

The author artifact contains two minimal templates: CodeMod and ContentPack. A clean CodeMod path must compile offline against the fixed `0.5.5` Abstractions reference and .NET Standard 2.0 references without inspecting the game install, PATH `dotnet`, or public NuGet. Packaging uses normalized relative paths, fixed ZIP timestamps/attributes/order, SHA-256 inventory and a machine report so output bytes are stable across source roots and wall-clock time.

Validation rejects malformed manifests, duplicate/empty identities, invalid versions/minimums, absolute or escaping `EntryDll`, missing entry assemblies, path traversal, ContentPack DLL/code payloads, forbidden game/Unity/Harmony/BepInEx references, and ordinary Mod targets below `BepInEx/plugins`.

Doctor inspects manifests, directory layout and PE metadata with `PEReader`/metadata readers. It never calls `Assembly.Load`, never executes a scanned module, and never mutates the tree. It classifies exact Runtime files, DTMAPI CodeMods, ContentPacks, external `BaseUnityPlugin`/`BepInPlugin` assemblies, native/bad/unknown DLLs, misplaced manifest-driven CodeMods and retained deprecated/Lamp references. A before/after tree digest is part of Doctor tests.

## 3.2 Receipt Deployment And Source Modes Boundary

`dtmapi-package.json` remains non-authoritative. Destructive authority requires both:

1. a package-local SDK receipt created before directory publication; and
2. a matching package-external journal keyed by canonical game root, `UniqueID`, destination, transaction ID, package kind and exact payload inventory/hash.

Deploy/update/withdraw use same-volume staging and recovery. Unknown destinations, missing/forged receipts, journal mismatch, identity/kind/path/hash drift, unknown additions, concurrent ownership, or incomplete recovery refuse without overwrite. Withdrawal first moves the verified directory to recovery and commits external state only after the move; any injected failure restores it. A prepared transaction remains recoverable after process termination.

The four source modes are:

| Mode | Selection contract |
| --- | --- |
| Player / Workshop | An enabled native-subscribed Workshop source wins; an unverified local copy is shadowed. A valid sole local/OfficialLocal source may load only when no subscribed duplicate exists. |
| Local Development | One `UniqueID` selects one exact local source path recorded by the SDK. Directory presence alone grants no priority or mutation authority. |
| Workshop Validation | Requires the native subscription snapshot, selects its installed Workshop source, and reports version/path/tree hash. |
| Player Reproduction | Snapshots and suppresses all overrides for the game root, uses Player / Workshop selection, and restores only through an explicit matching snapshot operation. |

Every mode reports selected and shadowed candidates with reasons. A loaded CodeMod source change remains restart-required and never re-enters in the same process.

## 3.3 Explicit Author Session Boundary

Ordinary Runtime must create no pipe, watcher, timer or file poll for authoring. The SDK prepares a short-lived random session descriptor before game startup. Runtime reads it once during startup, validates game-root/runtime identity and expiry, and only then opens a game-root/session-specific named pipe. Requests carry the random token, Runtime version, `UniqueID`, selected source path and expected tree hash. Wrong root/version/token/id/path/hash, replay, concurrency and unsupported content are rejected.

The pipe thread never mutates Runtime content directly. It enqueues a bounded request for the Runtime thread and returns that result. The first native startup `ReturnedToTitle` may retain the explicitly prepared session exactly once only when no save load, current loading slot or parsed request has been observed. A parsed request cancels that one-time exception, so the same or any later `ReturnedToTitle` closes the session; expiry or shutdown also closes it and releases the pipe. CodeMod DLLs, Custom Animals, native official JSON and unknown formats return `restart-required`; only Audio replacements may produce a new generation in SDK 0.1.0.

## Rejected Directions

- Reversing directory priority without a native subscription snapshot is not Workshop authority.
- Treating `mod_infos.json` or a numeric Workshop folder as proof of a current subscription is rejected.
- Runtime/player file polling, `FileSystemWatcher`, implicit listener startup and automatic development watching are rejected for Batch 3.
- `Assembly.Load`-based Doctor inspection is rejected because it executes loader behavior and can lock or initialize unknown code.
- DLL unload/reload, native official-table partial reload, package force/adopt and external BepInEx mutation are rejected.
- A receipt by itself, an external journal by itself, or author metadata by itself never authorizes update/withdrawal.

## Acceptance And Blocker Gates

Batch 3 may close only after automated evidence proves:

- both templates pass new -> validate -> offline build where applicable -> pack; packs are byte-identical across roots/times;
- the portable win-x64 artifact runs with PATH `dotnet` absent, empty NuGet state and no player install reference;
- fixed Runtime/Abstractions/compiler/reference hashes and third-party notices are present;
- Doctor safely classifies damaged/native/unknown/misplaced/deprecated inputs and preserves an exact tree digest;
- receipt deployment, update, withdrawal, fault recovery, concurrency and multi-game-root matrices fail closed;
- all four source modes expose exact selected/shadowed results from the native subscription snapshot plus external override state;
- valid -> invalid -> valid explicit Audio reload retains last-good then atomically swaps the next generation, while Custom Animals/DLL/native JSON/unknown return restart-required;
- normal Runtime creates no author listener/poll; an explicit session survives at most the one no-save/no-load/no-request startup-title exception, a parsed request cancels that exception so the same or any later title closes it, and expiry or shutdown also closes it; owner isolation holds and no public API/Hook target changes;
- Release tests, document governance and relevant third-save Runtime smoke pass with restoration and no residual process.

If native subscription capture is unavailable in the real Runtime, if offline compilation needs an unbundled SDK/network/player DLL, or if DTMAPI content cannot retain last-good state without changing player behavior, the affected claim remains blocked and the recorded reversal trigger returns to the decision-escalation policy rather than broadening authority.

## Validation Boundary

This Review cross-checked the current Core scanner/loader/reconciliation, `ModManager` reverse call graph, ContentManifest/Shadow registries, CustomAnimals and AudioReplacement refresh paths, public API matrix, ISSUE-013 and the frozen decision records. It introduces no API, Hook, Runtime, package, game, Workshop or save mutation. Implementation and final evidence belong to Update `20260715-0016`.
