# Audio Hook Idempotent Status Republish Manual-QA Review

## Review Boundary

- Review ID: `20260731-0002`
- Date: `2026-07-31`
- Kind: manual feedback and root-cause review
- Source: the user's combined ActionSpeed, Manbo audio, Fish Roe Info and Animal Bell Info hand test, the attached Manager screenshot, and the retained final process logs
- Implementation owner: [`20260731-0005 Update`](../../../updates/2026/20260731-0005-audio-hook-idempotent-status-and-all-mod-profile.md)
- Current conclusion: one false lifecycle warning and one real diagnostic-state regression are confirmed; no duplicate Harmony patch or Manbo playback failure occurred

This Review owns the pre-implementation reasoning only. Validation and deployment results belong to the owning Update and the related Debug issue.

## 1. Player result and visible warning

### Original feedback

The user confirmed that the newly staged AutoFishing, ActionSpeed, Manbo audio replacement, Fish Roe Info and Animal Bell Info functions all passed manual testing. The remaining request is to explain and minimally remove the warning shown whenever the combined feature set is used.

### Screenshot transcription

The screenshot shows the DTMAPI `0.5.5` Manager Error page with `Errors (0)` and `Warnings (1)`. The only row is timestamped `22:33:27`, owned by `DTMAPI.RefactorScaffold`, and begins `Lifecycle boundary contract diagnostic ... Hook install signal...`.

### Retained evidence

- `dtmapi-latest.log`: SHA-256 `411A5F367CAF3AA5EE95E725539F33A9FD398BF27F4DE6E332FFBF9FE5E14403`
- `bepinex-logoutput.log`: SHA-256 `C93A2F4609E0259B507005176C6DFB3DDC5D299C40A1F2ED1D4E78C3759A983E`
- screenshot: SHA-256 `95AF9EADF6823369C310FEB16616532A6AB4E22D59F3F38F2AEDBF35AC519944`
- retained directory: `E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\manual-qa-20260731-functional-four-audio-hook`

## 2. Why Animal Bell Info revisited the audio Hook

Animal Bell Info has no audio dependency. Its first localized item-name lookup activates the demand-driven `ItemDisplayName.EnvironmentReset` route so the shared name cache can be cleared after content, save or Workshop environment changes. A new physical-Hook demand asks the current scheduler to process a batch. The scheduler intentionally calls `InstallHooks()` for every currently demanded feature, not only the newly demanded route, so the already-active audio feature is revisited in the same batch.

This is an at-least-once scheduler contract. A feature installer must therefore be idempotent. The audio bridge's four physical booleans correctly prevented a second Harmony patch; the coupling is scheduler breadth, not a gameplay dependency between the two Mods.

## 3. Why only audio produced the warning

The retained sequence is exact:

1. At `22:29:17`, the Wwise event Hook is installed and `Audio.SoundEventReplacement` is published as `experimental`.
2. At `22:31:46`, a Manbo paper-box replacement plays successfully with `played=True` and `suppressed=True`; the same Hook status advances to `verified`.
3. At `22:33:27`, `DemandActivated:ItemDisplayName.EnvironmentReset` starts a scheduler batch. The audio `InstallHooks()` call observes all physical patches already installed and does not invoke Harmony again, but unconditionally republishes the status as `experimental`.
4. `LifecycleBoundaryContractService` infers an install signal from status/source/detail English text. It therefore treats the second status publication as a second install and emits `Hook install signal repeated for the same hook id`.

Other ProductNative features do not traverse this legacy GameBridge installer. `Feature.NativeUiLayoutDiagnostics` is revisited but remains `ready -> ready`, and `Feature.*` summary statuses are excluded from this string-based install heuristic. Audio is uniquely visible because it regresses `verified -> experimental` and its detail contains the install-looking Harmony text.

## 4. Confirmed defects and rejected hypotheses

Confirmed defects:

- `AudioReplacementHookBridge.InstallHooks()` republishes physical state even when none of its four physical Hook flags changed, erasing behavioral `verified` evidence.
- the lifecycle diagnostic infers installation from English status text and cannot distinguish an idempotent review from a new physical installation.

Rejected hypotheses:

- Manbo failed to load or play: rejected by the successful native event replacement evidence.
- Animal Bell Info depends on or modifies Manbo audio: rejected by the demand route and feature ownership chain.
- Harmony installed the Wwise patch twice: rejected by the bridge's physical guards and the absence of a second patch operation/failure.
- the warning represents a new error on every save exit: rejected; Manager retains the first warning, while the recorded warning count remains one.

## 5. Authorized minimal correction

The current release correction is limited to the audio bridge:

- publish the initial physical state once;
- publish again only when any of the four physical Hook flags differs from the last published snapshot;
- keep retrying an unavailable target so `pending -> experimental` remains observable when it later becomes patchable;
- when the physical state is unchanged, do not call `SetHookInstalled` or overwrite `Audio.SoundEventReplacement`.

The lifecycle diagnostic's general string heuristic and the global scheduler breadth are not changed in this release. True repeated install-looking publications must remain diagnosable.

## 6. Deferred unified-audio route

The next-version route is frozen in [`20260731 audio replacement bridge roadmap`](../../../planning/20260731-audio-replacement-bridge-roadmap.md). It separates physical installation from behavioral verification, replaces status-string inference with a structured install result, and discusses scheduler granularity together with the already reviewed audio backend/event policy. It does not expand BGM, loops, STOP/callback ownership or the public Experimental API without new native-owner evidence.

## Acceptance Boundary

Source acceptance for the minimum correction requires:

- an initial physical publication followed by `verified` behavioral state, then an unchanged review that retains `verified` and adds no duplicate-install diagnostic;
- an initial unavailable state followed by a later available state that can still publish `experimental`;
- focused Unit/source/governance checks.

Player acceptance remains open until the same Manbo plus delayed item-name-demand sequence is rerun on the corrected Runtime with no new warning. No complete Release, long GC run or unrelated gameplay matrix is required for this bounded fix.
