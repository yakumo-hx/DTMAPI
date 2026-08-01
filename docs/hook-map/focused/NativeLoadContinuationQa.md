# Optional-QA Native Load Continuation

Status: `installed` / source-unit-runtime verified

## Native Boundary

- Game build: `23762374_public_C416D4`.
- `System.Void DolocAPI::AfterLoadArchiveData(System.Boolean isNewGame)`: Harmony Prefix and Postfix.
- `System.Collections.Generic.Dictionary<System.Version,DolocTown.VersionPatch> DolocTown.VersionPatcher::LoadAllVersionPatches()`: Harmony Prefix and Postfix.
- `DolocTown.VersionPatch[] DolocTown.VersionPatcher::LoadAllVersionPatchesBeyond(System.String)`: Harmony Prefix and Postfix.
- `System.Void DolocTown.MapManager::Init(System.Boolean)`: Harmony Prefix and Postfix.
- `DolocTown.TextureUtils.DrawArea` remains deliberately unpatched because it is high frequency.

Current signature authority is `references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/methods.csv`. Historical ISSUE-010 motivation and earlier runtime evidence remain in `docs/reviews/code/2026/20260706-0006-phase816-post-saveload-native-continuation-probe.md`; this focused map owns the current install/close lifecycle.

## Owner and Lifecycle

- The hooks exist only when the receipt-bound optional QA participant selects `G6NativeLoadContinuationProbe=VersionPatcher`. Ordinary player startup installs none of them.
- Each QA run owns a distinct Harmony id: `dtmapi.gamebridge.doloctown.qa.native-continuation.<runId>`. It never reuses production owner `dtmapi.gamebridge.doloctown`.
- `NativeLoadContinuationProbe.Update` retries missing target pairs every two seconds until all eight Prefix/Postfix patches are installed.
- Participant close invokes `NativeLoadContinuationProbe.Close` before committing its own closed state. Cleanup reflects only the exact `UnpatchAll(string ownerId)` Harmony API; zero-argument/global unpatch is forbidden.
- A failed owner-only unpatch publishes `cleanup-failed`, keeps the probe and participant retryable, and prevents `closed=true`. A successful unpatch publishes `closed` with the owner id, all pre-close pair states and `unpatchSucceeded=True`.
- Production retains only the inert callback/breadcrumb endpoints and the neutral owner-aware Harmony primitive. Target selection, retry policy, status composition and hook ownership stay in the optional QA assembly.

## Evidence and Relations

- Targeted QA build/tests verify the unique owner, owner-only unpatch source contract, participant close ordering, and G6 failure-close cleanup.
- `tools/release/batch4-production-qa-semantic-inventory.json` schema 5 expands semantic discovery to native continuation/probe/breadcrumb/unpatch terms and adds explicit lifecycle contracts plus executable negative samples.
- `tools/scripts/run-game-smoke.ps1` requires a final `closed` receipt with the exact staged run owner, all four pairs installed before close, and successful owner-only unpatch. Every staged-QA run also requires the final synthetic-owner cleanup receipt with zero Core roots, owner instances and Camera leases.
- `GAME-SMOKE/20260717-121830` passed that final close gate under normal Steam/slot 3/no HookProbe: owner `dtmapi.gamebridge.doloctown.qa.native-continuation.<runId>` matched the staged run, all four pairs were installed before close, and exact owner-only unpatch returned `True`. The same close published zero synthetic-owner Core roots, owner instances and Camera leases.
- Related Update: `docs/updates/2026/20260715-0019-batch4-qa-host-extraction.md`.
- Related Debug issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md` remains open. This diagnostic hook lifecycle does not establish a GC fix.

## Rollback

Revert the optional-QA probe, its owner-aware neutral unpatch primitive, runner receipt gate, semantic lifecycle contracts and this map as one unit. Do not remove or globally unpatch the production Harmony owner.
