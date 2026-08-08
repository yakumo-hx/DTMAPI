# AutoFishing Workshop Copy and Runtime 0.5.5 Manual Retest

## Metadata

- Update ID: `20260731-0001`
- Date: `2026-07-31`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: autofishing/workshop/copy/author-sdk/runtime/0.5.5/manual-retest
- Source: User-directed three-language Workshop copy replacement followed by a two-Mod Runtime 0.5.5 manual retest.

## Source Request

The user reviewed the real subscribed Runtime `0.5.2-alpha` rejection of the new AutoFishing product, found the Workshop description ambiguous, supplied replacement copy including the `0731` crash-risk note, and requested a new manual test with Runtime `0.5.5` while keeping only DTMAPI and AutoFishing enabled.

## Scope

- Replace the Simplified Chinese, Traditional Chinese, and English AutoFishing Workshop descriptions at their two tracked authorities.
- Keep the product version, Runtime minimum, code, configuration, input, lifecycle, and native behavior unchanged.
- Regenerate the admitted Advanced package through the Catalog-driven Author SDK route.
- In the existing recoverable manual-test lease, stage the exact frozen 30-file Runtime `0.5.5` player candidate into the DTMAPI Workshop subscription directory while preserving its original `workshop.json`.
- Install from that actual staged subscription directory and leave only the DTMAPI and AutoFishing Workshop identities enabled for user manual testing.

## Changed Files

- `products/first-party/AutoFishing/official-info.json`
- `products/first-party/AutoFishing/dtmapi.author.json`
- `docs/updates/2026/20260731-0001-autofishing-workshop-copy-and-055-manual-retest.md`
- `docs/updates/INDEX-2026-07.md`

Generated package, live deployment, logs, receipts, and restoration scripts remain under ignored `temp`, `dist`, the retained-artifact root, Steam Workshop state, or the leased game/runtime state; they are not production-source files.

## Validation

- Both tracked JSON authorities parse successfully; their three localized descriptions are exact-equal, and the top-level official description matches Simplified Chinese.
- Catalog-driven frozen Author SDK validate/build/pack: PASS.
- Generated AutoFishing ZIP: 9 files, SHA-256 `2FE1872406A2D6089963ED846B47E0171A989D2BD275DAAE6DAEF12EDFD5CC2C`.
- Product DLL remained byte-identical at SHA-256 `DB7EAE628E97B46047EF888F5FBD48EDF86A969CC4FD4421C918FDBBD6A2B267`; this change alters publication copy only.
- Actual DTMAPI Workshop subscription staging: PASS at 31 files, consisting of the exact frozen 30-file Runtime candidate plus the original `workshop.json`; root BAT set is exactly `1` through `4`, with no `0_probe`.
- Actual AutoFishing Workshop subscription staging: PASS at 10 files, consisting of the exact regenerated 9-file package plus the original `workshop.json`.
- Windows PowerShell 5.1 Runtime install: exit `0`.
- Packaged 0.5.5 status checker and read-only Player Doctor: exit `0`; exact five Runtime assemblies, binary version `0.5.5.0`, dormant Compatibility Host, Player Doctor footprint, and provenance `f96c9cc61bf2` passed.
- Pre-launch player-state verification: PASS. Both official profiles enable exactly the DTMAPI and AutoFishing Workshop identities; direct game `Mods`, official-local `MODS`, Author LocalDevelopment state, and non-DTMAPI BepInEx plugins are absent.
- No new game process was launched by Codex during staging. The user later completed the interactive fifth-save gate against the current `0.5.5` Runtime and reported the AutoFishing manual test passed.

## Player Acceptance

On `2026-07-31`, after the current Runtime candidate with provenance `53bc614a89fa` was loaded, the user reported `手测通过`. This closes the user-owned interactive gate for the staged AutoFishing Workshop package. The result is player evidence for the two-Mod DTMAPI plus AutoFishing profile; it is not reassigned to the later six-Mod profile or to a broad release matrix.

## Evidence

Package and live-install receipts are under the existing retained manual-test lease:

- `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\install-dtmapi-055-subscription.log`
- `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\status-dtmapi-055-subscription.log`
- `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\verification-055.json`
- `E:\Python_project\DTMAPI\temp\autofishing-workshop-copy-20260731-055-manual\summary.json`

## Rollback

Revert the two tracked AutoFishing metadata files and this Update/index entry. The manual-test lease preserves the original Workshop subscription directories, game Runtime, local official packages, profiles, and Author SDK installation state for exact restoration after the user finishes testing.

## Follow-up

The interactive fifth-save behavior observation is complete. Any later multi-Mod composition test is owned by its own Update and does not reopen this two-Mod acceptance result.
