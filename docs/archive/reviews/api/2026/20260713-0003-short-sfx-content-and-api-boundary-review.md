# 20260713-0003 Short-SFX Content And API Boundary Review

Status: recorded / fifth-round decisions closed / implementation pending
Date: 2026-07-13
Scope: declarative short-SFX author route, Manbo data migration, public C# API retirement, playback-backend maturity, and recurring Runtime cost
Related decision docket: `docs/reviews/code/2026/20260713-0010-major-update-fifth-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0004-fourth-round-closure-fifth-decision-docket.md`
Closure Update: `docs/updates/2026/20260713-0005-fifth-round-closure-sixth-decision-docket.md`
Related research: `docs/reviews/api/2026/20260617-0001-audio-replacement-native-owner-review.md`, `docs/reviews/api/2026/20260617-0002-audio-replacement-event-map-backend-research.md`

## Source Request

The user wants DTMAPI to provide a generic audio bridge instead of adding one test function per successful replacement. BGM remains a future platform item. This review separates the already working reviewed short-SFX content route from broad Wwise/music promises.

This is a source/API review. It does not change audio playback, content schema, Manbo, public API status/signatures, Hooks, packages, or game files.

## Current Two Entry Paths

`AudioReplacementService` already accepts `Content/DTMAPI/audio-replacements.json` entries for:

- `SimpleSfx`, currently allowlisting `PLAY_RESOURCE_PAPER_BOX`;
- `AnimalVoice`, currently allowlisting reviewed animal events and adding `speciesId + stage + nativeSoundEvent` context.

Manbo is the only source product consumer of `IAudioReplacementApi`. Its DLL registers one static paper-box replacement and exposes a read-only status paragraph. It has no player setting or dynamic policy that requires code. Hatch/Shell Crab and the other current animal packages already exercise the declarative JSON+WAV model.

## Q - Formal Short-SFX Author Route

### Q1 - JSON plus reviewed event catalog

Make `audio-replacements.json` the one recommended author route. DTMAPI owns category/event review, scope matching, conflict arbitration, preload/playback, fail-open suppression, owner cleanup, and diagnostics. Author SDK owns schema, relative path, WAV/PCM, allowlist, scope, and conflict validation.

The first formal scope is deliberately narrow:

- short 2D one-shot WAV;
- no emitter, callback, loop, STOP, 3D, RTPC/state, or bus semantics;
- reviewed `SimpleSfx` event names;
- `AnimalVoice` scoped by species/stage/native event;
- one deterministic suppressing replacement per effective scope; duplicate suppressors are visible errors;
- suppress native only after replacement readiness/playback success; otherwise fail open.

Final decision: **Q1**. It replaces per-gameplay-function product APIs with one policy-gated event route without pretending every Wwise event is safe. The first reviewed `SoundKey` catalog stays around 5-15 proved targets: verified AnimalVoice contexts, Manbo paper-box, and a few discovered simple UI 2D one-shots with no STOP/loop/callback/3D semantics.

Author SDK exposes key search, category/maturity, WAV/path/context validation, duplicate-replacement conflicts and unsupported reasons. Authors own only the stable SoundKey, WAV and JSON declaration; DTMAPI owns changing native event names, suppression timing, playback and Hook/native adaptation.

### Q2 - JSON and C# are equal formal routes

Keep expanding the current C# API beside content JSON.

This preserves dynamic registration but commits to absolute paths, broad public options, and a consumer model not demonstrated by current products.

### Q3 - block all author audio until Wwise is solved

Retain only internal testing until a native bank backend exists.

This is safest for mixer fidelity but unnecessarily withdraws already protected AnimalVoice/Manbo one-shot behavior.

## R - Manbo And The C# Compatibility Surface

### R1 - data-only Manbo 1.0.0; I1 C# retirement

Preserve the existing product identity and Workshop item:

```text
UniqueID: Yuuka.DTMAPI.ManboCardboardAudio
Workshop: 3746319981
Version: 1.0.0
MinimumDTMApiVersion: 0.5.5
Type: ContentPack
EntryDll: none
ModConfigMenu dependency: none
Content: audio-replacements.json + manbo.wav
```

Its old read-only status page moves to J1 advanced details/Doctor. DTMAPI 0.5.5 marks `IAudioReplacementApi` and related DTOs Deprecated/Frozen with `[Obsolete(..., false)]`, retains provider/ABI/owner facade/old behavior, warns per owner, and adds no events/templates/adopters. Conditional removal follows I1 only after Manbo migration, one published warning cycle, renewed consumer scans, migration guidance, and old-DLL regression.

Final decision: **R1 revised**.

Manbo first ships the data-only migration as a real-player Canary. Besides package identity and ordinary behavior, its release gate covers Workshop update, JSON/WAV loading, native suppression and fail-open fallback, disable recovery, long runtime, old-Runtime blocking and player feedback. After that evidence, a separate product-lifecycle decision either retains it as a small content product or publishes retirement guidance and keeps it as a historical compatibility sample. Manbo is not the long-term architecture owner.

The C# API retirement boundary remains unchanged: 0.5.5 preserves ABI/provider/behavior, freezes and warns it, and new author docs/templates recommend JSON only. No physical removal occurs before the complete I1 warning, consumer-scan, migration and explicit breaking-version gates.

### R2 - keep Manbo as a CodeMod

Retain the DLL and C# API indefinitely for its status page.

This preserves implementation shape but keeps a static data product and broad API for no product-policy benefit.

### R3 - create a new Manbo content product

Publish a new content item/UniqueID while leaving the old Workshop product behind.

Rejected: it breaks the selected factual product identity/update model without need.

## S - Contract And Backend Maturity

### S1 - classify layers separately

| Layer | Selected target status if S1 is chosen |
| --- | --- |
| reviewed JSON schema/validator | formal author route / StableCandidate |
| verified Manbo/AnimalVoice product behavior | ProtectedCurrent per category |
| `IAudioReplacementApi` | Deprecated/Frozen compatibility |
| current Windows WAV playback backend | narrow Experimental implementation |
| arbitrary Wwise events | unsupported |
| BGM/loop/STOP/callback/3D | separate Proposed project |

Final decision: **S1**. It lets authors use a truthful narrow contract without claiming the current playback engine has native mixer semantics. A reviewed content declaration can become the recommended author route while a category remains ProtectedCurrent and the backend remains narrow Experimental.

### S2 - wait for a native mixer backend before the route is formal

Keep schema and products Experimental until a Wwise/Unity backend exists.

This creates a higher fidelity bar but delays documentation/validation of behavior already used by real products.

### S3 - call the current backend stable audio replacement

Treat current successful WAV playback as broad completion.

Rejected. The current verified fallback is `System.Media.SoundPlayer`: it bypasses Wwise/Unity mixer, game SFX volume, pause, bus, 3D, and callbacks. Current platform playback does not apply the public `Volume` option, so volume cannot be a formal v1 semantic.

## Non-Optional Runtime Corrections

Current `AudioReplacementService` remains an EveryFrame feature and performs recurring work before its nominal unchanged/interval decisions:

- snapshots/sorts LoadedMods;
- constructs paths/signatures;
- calls `File.Exists` and `GetLastWriteTimeUtc`;
- uses LINQ and multiple `ToArray()` copies;
- publishes skipped refresh ledger state;
- scans/sorts/copies entries on sound events and owner-state updates.

The required endpoint is content-generation/official-lifecycle refresh plus Author-SDK B1 explicit reload, a temporary pump only while a decode/retry is pending, pre-indexed event/scope lookup, and no Hook/feature activation with no JSON or legacy C# consumer.

`DungeonResourceModelPaperBox.OnInteract` Postfix exists to prove the native paper-box smoke path; real replacement uses the shared Wwise event Hook. The product-specific postfix belongs in optional QA. `Animal.PlayAnimalSound` context Hooks are generic AnimalVoice scope support and activate only on AnimalVoice demand.

Removing this recurring work is not evidence that audio caused ISSUE-010 or that GC is solved.
