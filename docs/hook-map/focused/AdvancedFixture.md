# Advanced Synthetic Fixture

Status: `verified` / synthetic G2 only

## Native Boundary

- Game build: `23762374_public_C416D4`; `Assembly-CSharp.dll` SHA-256 `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404`.
- Target: `System.Boolean DolocAPI::Has087DemoData()`, metadata token `0x060000A0`.
- The target delegates a fixed `doloc-archive-` prefix to `DataPersistenceManager.HasDataStartWith`. The fixture observes the returned Boolean only; it does not change the return value, archive state, save data or Unity state.
- Signature authority: `references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/methods.csv` and the matching reverse-build metadata.

This is a synthetic ProductNative proof target, not a stable public API or SharedNative GameBridge adapter. It has one synthetic consumer and establishes no future game-build compatibility.

## Owner and Lifecycle

- Managed identity: `DTMAPI.AdvancedFixture`; canonical Harmony owner: `dtmapi.mod.dtmapi.advancedfixture`.
- A verified Advanced reference receipt, exact game build/reference hashes, explicit package identity and the canonical owner must pass before Entry or patch publication.
- Entry installs one Harmony Postfix and calls the same native query once. The Postfix only records the observed result; its result must equal the direct query.
- Wrong owner, duplicate patch, Entry failure and attributable late-owner drift fail closed with owner-scoped cleanup diagnostics and restart-required health. Sibling-owned late patches are not attributed to the fixture.
- Disable after load removes only managed roots and the canonical Harmony owner where provable. Mono assembly unload is never claimed; Advanced changes remain restart-required until a clean cold start.
- SDK session update from v1 to v2 preserves one exact deployment identity, reports restart-required on reload, clears credentials, and is accepted only after a final clean v2 cold run.

## Evidence and Relations

- Accepted cold/runtime roots: `GAME-SMOKE/20260720-211547`, `211632`, `211713`, `211754`, `211835`, `212010`, `212235`, `213129`, and `213231`.
- The committed runtime receipt binds each result, Runtime/report log, Player Doctor projection, package ZIP tree, orchestration artifact, cleanup receipt and process check: `tools/release/baselines/batch6-g2-runtime-matrix-receipt.json`.
- The Strict sibling is `DTMAPI.HookProbeMod`; all nine cases completed its Entry, GameLaunched, OneSecond and third-save SaveLoaded observations.
- AutoFishing load-source and Entry counts are exactly zero in the receipt. This record does not authorize a real product migration.
- Owning Update: `docs/updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md`.
- Prerequisite Review: `docs/reviews/code/2026/20260720-0004-batch6-g2-advanced-synthetic-vertical-slice-prerequisite.md`.
- Root-cause Review: `docs/reviews/code/2026/20260720-0005-batch6-g2-first-runtime-matrix-root-cause.md`.

## Rollback

Withdraw the exact receipt-bound fixture deployment after proving the game process is absent, then revert the G2 implementation and closure boundaries together. Never use global Harmony unpatch or remove an unreceipted sibling.
