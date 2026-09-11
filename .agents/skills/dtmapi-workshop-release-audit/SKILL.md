---
name: dtmapi-workshop-release-audit
description: Audit DTMAPI Runtime Workshop packages, subscription parity, and player installer failures across the published Windows and multi-platform distributions. Use before or after Runtime publication, or for Runtime installer diagnostics; do not use for ordinary product Mod upload-folder syncing.
---

# DTMAPI Runtime Workshop Audit

Route Runtime Workshop audits to the repository's current authorities and scripts. This skill does not maintain a second installer test implementation.

## Start

1. Resolve the repository root; never hard-code this checkout or a Steam library path.
2. Read `docs/architecture/runtime-workshop-installer-boundary.md` and only the relevant lane in `docs/workflows/workshop-package-subscription-test-matrix.md`.
3. For current publication or subscription facts, read `tools/release/dtmapi-product-catalog.json`, `tools/release/current-subscription-manifest.json`, and the Review/Update routed by those authorities. Historical package bytes and Updates are evidence, not current release authority.
4. Classify one primary mode before running anything: published Windows package, multi-platform package, subscription parity, or player installer failure.

## Route

| Mode | Canonical action |
| --- | --- |
| Published Windows package or installer behavior | Run `tools/scripts/test-runtime-workshop-installer-061.ps1 -PackageRoot <package>` against the exact candidate or subscription artifact. The script owns the fake-game, path, wrapper, PowerShell 5.1, install/status/log/uninstall, transaction and cleanup matrix. |
| Multi-platform structure or provenance | Run `tools/scripts/test-dtmapi-multiplatform-package.ps1 -PackageRoot <package>`. Add `-AllowWorkshopControlFile` only for the official local upload leaf; never for a repository candidate or Steam subscription. The observed 0.7.0 subscription uses the separate `-AllowDeliveredWorkshopControlFile`, which requires its exact frozen Catalog content/control receipts. Select schema-2 provenance with `-SourceKind Candidate -AcceptedPackageRoot <exact Windows input>`; publication does not rewrite build provenance. |
| Multi-platform installer lifecycle | After the structural audit, run `tools/scripts/test-multiplatform-runtime-installer.ps1 -PackageRoot <package>` when installer behavior changed or a failure needs reproduction. Report any host lane that was skipped; copied bytes alone do not prove Steam Deck, Proton or CrossOver startup. |
| Post-upload subscription parity | Treat the downloaded subscription as the player artifact. Follow the matrix's Windows file parity or multi-platform normalized-receipt rule, including its local-metadata-successor exception; do not rebuild, sync or upload during a read-only audit. |
| Player installer failure | Read the relevant current Debug issue/protocol and latest installer Review/Update, inspect the player's package/log evidence, then reproduce only the matching package/fake-game lane. Separate a package defect from a host, path, permission, antivirus or game-state failure. |

For an ordinary product Mod upload-folder sync with no Runtime installer concern, leave this skill and follow that product's release authority instead of running Runtime matrices.

## Execution Boundaries

- Prefer package-only and fake-game checks. Do not install into the real Doloc Town directory, launch the game, write local official `MODS`, or prepare a live upload leaf unless the user requested that shared-runtime operation; then follow the repository runtime-lock workflow.
- Do not add custom persistent `%TEMP%\DTMAPI-*` roots. The lifecycle scripts use the managed test root and clean it by default. Use `-KeepTemp` only for a stated evidence need, record the retained path, and clean it under `docs/debug/protocols/test-artifact-retention.md`.
- Keep the canonical syntax checks enabled. If a diagnostic run deliberately skips a host/parser lane, report that omission rather than calling the package accepted.
- Audit does not authorize publication. Do not mutate Catalog authorization, upload folders, Steam state, game files or player saves unless the user separately requests that action.
- Do not infer runtime acceptance from layout, hashes or install success. Fresh game startup/injection evidence and platform-specific player acceptance remain separate gates.

## Report

Return the selected mode, exact source/subscription/candidate paths, authority records consulted, scripts and options run, pass/fail per executed lane, skipped or unproven gates, blockers versus warnings, and retained evidence/cleanup state. If validation was not run, say so explicitly.
