# 20260711-0011 Player Title Settings Log Analysis

## Metadata

- Update ID: `20260711-0011`
- Date: 2026-07-11
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-run`
- Related Issue State: `mitigated`
- Source request: move `D:\下载\DTMAPI-logs余余余0711.zip` and follow-up `D:\下载\DTMAPI-logs余余余0711_2.zip` into the player feedback-log folder and analyze why the title-screen DTMAPI settings entry remained absent, including after removing the Mxx/Terraria-item-pack DLL.
- Related review: `docs/reviews/manual-qa/2026/20260711-0001-player-title-settings-runtime-missing.md`
- Related issue: `docs/debug/issues/ISSUE-012-20260711-player-title-settings-runtime-missing.md`

## Scope And Result

- Moved the supplied archive into `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/` and verified the moved SHA-256 as `C2AF5F08E9632C75F9391404B9A1E167A81F6BF331C5FFDABA2A068D4A966CA8`.
- Extracted 20 player evidence files without altering the original archive.
- Established that the captured `15:56` player session had native Unity/title activity but no fresh BepInEx or DTMAPI runtime startup; the only runtime logs were from a `14:47` session.
- Corrected the `14:47` interpretation: the injected PID `6952` only initialized the title UI host and never logged the button-visible milestone, while the separate six-minute PID `7016` was already native-only. Stable UI presentation is not proven, and injection loss predates the Steam reinstall.
- Recorded the intervening Steam full game uninstall/reinstall at `14:54:30-14:56:10` and kept the precise loader-file failure as an evidence gap.
- Clarified that the logs do not identify the game-uninstall initiator: the earlier `Priority User Initiated` marker belongs to a separate Workshop unsubscribe, while the DTMAPI uninstaller had finished at `14:40:28` without requesting BepInEx or game removal.
- Identified a diagnostics defect: the generated startup analysis used stale runtime logs and incorrectly classified the archive as `NormalDtmapiStartup`.
- Kept title/config UI implementation unchanged because the failing session never reached that layer.
- Moved and extracted the follow-up archive, SHA-256 `57F3EE96D4C92A104768FE2F128DD4E3A4ED21B2F96030EBE9C738FD816DC2D1`, containing 22 files.
- Confirmed a clean `20:04` reproduction after item `3759797170` was unsubscribed and its copied DLL removed: DTMAPI was reinstalled, the game reached native `HomePageUiState`, and no new BepInEx/DTMAPI log appeared.
- Final follow-up disposition: Windows restart restored both cold- and warm-Steam DTMAPI injection, and the player visually confirmed the title button. Record the incident as restart-resolved; no runtime/UI fix is attributed.
- Compatibility boundary: the third-party Mxx installer writes its DLL directly into shared `BepInEx\plugins`, outside DTMAPI ownership and uninstall control. Its impact is unproven here; removing it did not restore injection before restart.
- Verified that both collected runtime logs are byte-identical to their first-archive copies, rejecting the Mxx plugin as the direct cause of current-session injection loss.
- Narrowed the remaining boundary to static-versus-live Doorstop state: the installer considered BepInEx complete and skipped refreshing it, but current diagnostics do not capture the loaded `winhttp.dll` path, the `DOORSTOP_DISABLE` switch, or trusted root-file hashes.
- Preserved and transcribed two player PowerShell screenshots. The five loader-file lengths/hashes exactly match the trusted subscription payload and both persistent `DOORSTOP_DISABLE` scopes are empty, rejecting file replacement/corruption and that disable switch. The loaded-module question remains open because no game process existed when the player ran the module query.
- Preserved a follow-up live-module screenshot for PID `7924`. The failing process loads both the trusted game-local UnityDoorstop `WINHTTP.dll` and Windows' system `WINHTTP.dll`, closing the native-proxy-admission gap and moving the boundary to Doorstop target invocation or very-early BepInEx Preloader startup.
- Preserved five further screenshots. The attempted full comparison did not run because the player subscription lacked the assumed `Content\.tools\bepinex\extract` directory; the resulting "extra core" list is a false artifact of an empty trusted-name set. The visible core inventory matches expected names/sizes, patchers are empty, Steam launch options are blank, and the public branch is selected.
- Corrected the comparison baseline to the bundled BepInEx ZIP and recorded its expected SHA-256 `82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4` from the same Workshop manifest `6297788246671130629`.
- Preserved the corrected ZIP comparison screenshots. The trusted ZIP validates and all 22 installed BepInEx/Doorstop files match byte-for-byte, rejecting complete-payload modification, omission, or corruption as the loader failure.

## Changed Files

- `docs/reviews/manual-qa/2026/20260711-0001-player-title-settings-runtime-missing.md`
- `docs/debug/issues/ISSUE-012-20260711-player-title-settings-runtime-missing.md`
- `docs/debug/issues/README.md`
- `docs/updates/2026/20260711-0011-player-title-settings-log-analysis.md`
- `docs/updates/INDEX-2026-07.md`
- local-only player evidence under `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/`

## Validation

- Archive move verified: the source no longer exists and destination SHA-256 matches the pre-move hash.
- Follow-up archive move verified: the second source no longer exists; destination SHA-256 is `57F3EE96D4C92A104768FE2F128DD4E3A4ED21B2F96030EBE9C738FD816DC2D1`; extraction count is 22.
- Follow-up process correlation verified: install `20:04:00`, Steam PID `6572` start `20:04:14`, collector `20:07:25`, native `HomePageUiState`, and no fresh runtime log.
- Cross-archive SHA-256 comparison verified the BepInEx and DTMAPI logs are exact duplicates from `14:47`, not current-session output.
- Screenshot values were compared against `D:\Steam\steamapps\workshop\content\2285550\3743016467\Content\.tools\bepinex\extract`; all five lengths and SHA-256 values match.
- Extraction verified: 20 files.
- Source review verified that `check-dtmapi-status.ps1` checks static presence while `analyze-startup-evidence.ps1` does not correlate log freshness with process start.
- DTMAPI Workshop release audit ran against `D:\Steam\steamapps\workshop\content\2285550\3743016467` (`0.5.2-alpha`) using a fake game directory with spaces and Chinese characters.
- PowerShell 5.1 parser checks passed for all packaged scripts.
- Required matrix exit codes matched expectations: missing install `1`, empty install `1`, empty check `1`, valid install `0`, post-install check `0`, log collection `0`, uninstall `0`, post-uninstall check `1`; blockers `0`.
- No Doloc Town runtime/game smoke was run. The package audit used only temporary fake directories and cannot close ISSUE-012.

## Evidence

- Player evidence root: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/`
- Package audit: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/package-audit/DTMAPI Workshop Audit 20260711-191949/Results/stress-summary.md`
- Canonical interpretation: the related manual-QA review and ISSUE-012.

## Rollback

- The original archive is preserved at the evidence destination with its verified hash.
- Documentation can be reverted independently; no runtime, package, third-party mod, or local game files were changed.

## Follow-up

- Implement process/log freshness and root-loader inventory in status/collector/startup analysis under a separate in-progress Update.
- Obtain a post-Steam-reinstall installer/status transcript and fresh title-session logs from the player before claiming the loader issue fixed.
- This incident requires no further player capture after restart recovery and visual confirmation. If it recurs, capture the failing process session before restarting; do not infer causation from the separately unmanaged Mxx DLL without new evidence.
