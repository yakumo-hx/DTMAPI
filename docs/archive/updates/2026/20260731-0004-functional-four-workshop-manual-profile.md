# Functional Four Workshop Manual Profile

## Metadata

- Update ID: `20260731-0004`
- Date: `2026-07-31`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: workshop/subscriptions/manual-profile/action-speed/manbo-audio/fish-roe/animal-bell/runtime/0.5.5
- Source: User-directed replacement of four subscribed Workshop packages with their new DTMAPI versions and exact enablement for manual testing.

## Source Request

After reporting that the current DTMAPI plus AutoFishing hand test passed, the user requested that the subscribed ActionSpeed, Manbo audio replacement, Fish Roe Info, and Animal Bell Info directories be replaced by their new packages and enabled. Codex was to prepare the player-like state without launching the game so the user could perform the interactive test.

## Scope

- Preserve the installed Runtime `0.5.5` and the already staged AutoFishing package.
- Replace only Workshop subscriptions `3742763309`, `3746319981`, `3742763706`, and `3742763843`.
- Preserve each pre-existing `workshop.json`; do not invent one for the Manbo subscription that did not have one.
- Enable exactly DTMAPI, AutoFishing, ActionSpeed, Manbo audio replacement, Fish Roe Info, and Animal Bell Info in both official Mod profiles, with every other entry disabled.
- Keep direct game `Mods`, persistent official-local `MODS`, Author LocalDevelopment, and non-DTMAPI BepInEx plugin paths empty.
- Retain exact pre-change Workshop directories and profiles plus a checked restore script.
- Do not launch Doloc Town; retain the shared Runtime lock for the user's manual test.

## Changed Files

- `docs/updates/2026/20260731-0001-autofishing-workshop-copy-and-055-manual-retest.md`
- `docs/updates/2026/20260731-0004-functional-four-workshop-manual-profile.md`
- `docs/updates/INDEX-2026-07.md`

Generated staging scripts, package extracts, deployment receipts, live Workshop state, Mod profiles, and restoration material remain under ignored repository temp paths, retained-artifact storage, Steam Workshop state, or the existing manual-test lease. They are not production source files.

## Package Selection

- ActionSpeed uses the admitted Advanced package with ZIP SHA-256 `431627407B1883E02BB20DCF2B37E86E3EE22D6C2A382010AE60C44B79F2666F` and DLL SHA-256 `1C115CBAA92EF2AA5DBB9DF509A216CFCD195B7B5E860C563452E6DB727798D5`.
- Fish Roe Info uses the admitted Advanced package with ZIP SHA-256 `774CD3CEA4F14E220D6FC1A54B5CCF14EAD0DDBC55395315C59437D3C8A59E77` and DLL SHA-256 `16A8EAC5AC2BC98CF039F7A5F496C7950FB9A662F62CCDA4DB5FFB7AD150650E`.
- Animal Bell Info uses the admitted Advanced package with ZIP SHA-256 `559CA6B7A793CF6BCB0BD8F6E5FDDDF390F172EEC060840054D29091B12409A2` and DLL SHA-256 `77C026F3F41B2306852E67D7B98D7748C6136FC90C838DA39F922C36B1B240AE`.
- Manbo audio replacement uses the current-source package previously loaded by the all-functional runtime smoke, with DLL SHA-256 `FCB830C45F163DC1746B8CF56F19C427BD3CED81DF795B834E9F8FC591D65F2C`, plus its tracked package marker.
- A fresh Advanced rebuild was intentionally rejected because the current Abstractions projection no longer matches the frozen Author SDK hash. This task did not re-freeze the SDK or substitute unaccepted binaries; it reused unchanged admitted packages whose product source had not changed.

## Validation

- Windows PowerShell 5.1 syntax validation passed for staging, restore, and independent verification scripts.
- The first staging attempt reached profile publication, where Windows PowerShell 5.1 rejected a null backup path passed to `File.Replace`. The transaction restored all four original Workshop directories and both profile hashes; the leftover transaction-only directories were verified and removed before retry. Atomic writes were corrected to use an explicit temporary backup path.
- The second staging transaction completed and produced the retained verification receipt.
- Independent read-only package verification passed:
  - ActionSpeed: 10 live files including the preserved `workshop.json`, Advanced `1.0.0`, exact DLL and receipt.
  - Manbo audio replacement: 7 live files, Strict/legacy-compatible `0.1.0-dtmapi`, exact DLL, no invented `workshop.json`.
  - Fish Roe Info: 8 live files including the preserved `workshop.json`, Advanced `1.0.0`, exact DLL and receipt.
  - Animal Bell Info: 8 live files including the preserved `workshop.json`, Advanced `1.0.0`, exact DLL and receipt.
- Both official profiles enable exactly, in order: `Workshop.3743016467`, `Workshop.3743799721`, `Workshop.3742763309`, `Workshop.3746319981`, `Workshop.3742763706`, and `Workshop.3742763843`.
- The installed Runtime remains provenance `53bc614a89fa`; all five DLL hashes match the pre-deployment candidate.
- Direct game `Mods` and persistent official-local `MODS` contain zero files, the BepInEx plugin root contains only DTMAPI, and the Author installation contains only `workshop-subscriptions.json`.
- The packaged status checker passed. Player Doctor reported five expected Runtime artifacts, zero errors, and zero warnings.
- `DolocTown.exe` remained stopped throughout Codex staging. The user then manually verified AutoFishing, ActionSpeed, Manbo audio replacement, Fish Roe Info and Animal Bell Info individually and in combination; the requested gameplay functions all passed.
- The player run exposed one non-functional diagnostic regression: a later ItemDisplayName demand revisited the already-installed audio Hook, downgraded its status from `verified` to `experimental`, and produced one false duplicate-install warning. This did not affect playback and is separated into [Manual-QA Review 20260731-0002](../../reviews/manual-qa/2026/20260731-0002-audio-hook-idempotent-status-republish.md) and [ISSUE-016](../../../debug/issues/ISSUE-016-20260731-audio-hook-idempotent-status-republish.md).

## Evidence

- Staging and package inventory: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\verification-functional-four-current-53bc614a.json`
- Live lease state: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\lease.json`
- Pre-change Workshop revision: `D:\steam\steamapps\workshop\content\.dtmapi-manual-autofishing-player-20260731-074235\Revisions\before-functional-four-current-53bc614a`
- Pre-change profile revision: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\.dtmapi-manual-autofishing-player-20260731-074235\Revisions\before-functional-four-current-53bc614a`
- Restore script: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\restore-functional-four-current-53bc614a.ps1`
- Final player log/screenshot set: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\manual-qa-20260731-functional-four-audio-hook`

## Rollback

After Doloc Town exits, run the retained restore script from the same locked worktree. It moves the four tested directories into a retained tested-state revision, restores the exact original subscription directories and both original Mod profiles, and returns the lease to its previous state. The installed Runtime and AutoFishing package are outside this four-directory rollback and remain unchanged.

## Follow-up

The six-Mod deployment and functional player gate is closed. The diagnostic-state correction is owned separately by `20260731-0005`/ISSUE-016; it does not reopen the successful gameplay result recorded here.
