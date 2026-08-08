# 20260801-0004: Final Test Build Full Reverse Baseline

## Metadata

- Update ID: `20260801-0004`
- Date: `2026-08-01`
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: User request to fully freeze, unpack, and decompile the locally installed last test-branch build, retain it as the future comparison baseline, and quantify its changes from `24256979_test_7A1907`.

## Scope

- Freeze only the official Doloc Town player files selected by the tracked clean-room capture workflow.
- Preserve the exact Steam manifest identity and source/frozen/end parity receipt.
- Run pinned AssetRipper and ILSpy through the verified portable full-capture wrapper.
- Compare raw player files, recovered assets/config/scenes, and decompiled managed source with `24256979_test_7A1907`.
- Record derived counts and limitations without committing or redistributing official files, extracted assets, or decompiled source.
- Do not launch the game, install DTMAPI, modify saves, or mutate the shared Runtime.

## Preflight

- Resolved local game: `D:\steam\steamapps\common\Doloc Town` through the tracked Steam-library resolver; no hard-coded path is added to project source.
- Steam app `2285550`, build `24456188`, branch `test`.
- Live appmanifest SHA-256: `E2C5D50E6172C962CB02F58BC0E9931279FAEBC12EBFAFBD386309ECFFD85D67`.
- `Assembly-CSharp.dll`: 6,384,128 bytes, SHA-256 `E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923`.
- Managed assembly/file/product versions are all `0.0.0.0`. The same raw PlayerSettings version slot in `globalgamemanagers` changed from `0.99.08` to `1.00.00`; Steam still identifies the package as branch `test`, so the accepted identity is the 1.00.00 final-test baseline rather than a captured public-branch release.
- New canonical local-only target: `references/doloc-town/reverse/builds/24456188_test_E861E0`.
- Output drive had approximately 401.64 GiB free; no `DolocTown.exe` process was running; the DTMAPI Runtime lock was free.

## Changed Files

- This Update and `docs/updates/INDEX-2026-08.md`.
- `docs/reviews/code/2026/20260801-0002-current-game-vs-24256979-reverse-baseline-audit.md`.
- Current-reference pointers in `docs/reviews/api/native-owner-domains/INDEX.md` and `SOURCE-INDEX.md`.
- Local-only ignored baseline under `references/doloc-town/reverse/builds/24456188_test_E861E0/`.

## Validation

- Preflight identity/path/process/capacity checks passed.
- Capture source parity passed for 452 game files plus the Steam manifest; source start/end and frozen files are exact.
- The frozen raw tree contains no BepInEx/Mods/Saves/player-log pollution and no zero-byte files.
- AssetRipper 1.3.14 completed 51,988 exported files; ILSpy 9.1.0.7988 completed 3,666 main inventory files and 29 firstpass files.
- A second full validation re-hashed all four inventories and checked actual path sets: missing 0, extra 0, mismatch 0 in every set.
- Layered comparison passed: 1/181 managed assembly changed; ILSpy has 1 add, 1 removal, 178 same-path changes and `+1,738/-615` lines; scene identity remains 90 with 0 add/remove/move; 44/195 config tables and 19/133 Yarn files changed.
- The exact disposable package root under `tmp/test-runs/portable-full-capture-run-20260801` was path-checked and removed after successful capture and receipt verification.
- An independent final record-consistency pass rechecked the raw/export/ILSpy/scene/config/Yarn counts, version/branch boundary, and links; after correcting the settings-key and verification wording, it found no remaining P0/P1 contradiction.
- `tools/scripts/check-doc-governance.ps1` and final targeted link/status checks passed.
- No game/runtime launch is required for this source/reference task.

## Evidence

- Previous comparison baseline: `references/doloc-town/reverse/builds/24256979_test_7A1907` (local-only).
- Accepted current baseline: `references/doloc-town/reverse/builds/24456188_test_E861E0` (local-only).
- Local capture receipt: `full-baseline-inventory/portable-full-capture-summary.json`.
- Local independent verification: `full-baseline-inventory/independent-inventory-verification.json`.
- Local layered comparison: `full-baseline-inventory/comparison-to-24256979.json`.
- Tracked comparison Review: `docs/reviews/code/2026/20260801-0002-current-game-vs-24256979-reverse-baseline-audit.md`.
- Capture/package authority: `docs/updates/2026/20260720-0006-portable-full-reverse-capture-package.md`.
- Previous accepted-baseline audit: `docs/reviews/code/2026/20260721-0001-pre-release-reverse-baseline-audit.md`.

## Rollback Notes

- If capture or parity validation fails, retain logs needed to diagnose the failed attempt but do not admit the partial directory as a baseline.
- A clean rollback removes only `24456188_test_E861E0` and this task's records; it must not alter any accepted older baseline or the unrelated pre-existing untracked Review.

## Follow-Up

- Use `24456188_test_E861E0` for future task-specific native-owner/body/config rechecks. Keep generated Native Function Map data on its recorded historical baseline until it is deliberately regenerated.
- If the Steam public branch later differs from build 24456188, preserve this directory and compare that public build against it instead of overwriting it.
