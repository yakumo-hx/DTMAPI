# Manual QA Review: player title settings entry missing because the current runtime did not inject

## Review Header

- Time: 2026-07-11
- Status: recorded
- Source: player archives `D:\下载\DTMAPI-logs余余余0711.zip`, `D:\下载\DTMAPI-logs余余余0711_2.zip`, pre-reboot startup trace `D:\下载\DTMAPI-startup-capture-20260711-223703.zip`, and post-reboot cold/warm Steam traces `D:\下载\DTMAPI-startup-capture-20260712-131334.zip` / `D:\下载\DTMAPI-startup-capture-20260712-133519.zip`, plus the user report that installation/status passed but the title-screen DTMAPI settings window was absent after removing the Mxx/Terraria-item-pack DLL.
- Scope: durable log review only; no runtime or installer implementation in this review.
- User constraints: move the supplied archive into the feedback-log area, then analyze it.
- Related review/update/debug records: `docs/debug/issues/ISSUE-012-20260711-player-title-settings-runtime-missing.md`; `docs/updates/2026/20260711-0011-player-title-settings-log-analysis.md`; historical `ISSUE-003-hotkey-openconfig-no-overlay.md` is not the active symptom.
- Files/docs inspected: required project/debug/governance documents; player archive contents; current `check-dtmapi-status.ps1`, `collect-logs.ps1`, and `analyze-startup-evidence.ps1`; DTMAPI 0.5.2-alpha subscription package through the release-audit matrix.
- Not inspected: the player's live game directory after collection; the exact output/time of the player's `3_check_dtmapi_status.bat`; the actual Steam/DolocTown inherited environment blocks; Doorstop's internal IAT-hook return state, because the release binary has no verbose native log.

## Issue Review

### Issue 1: title-screen DTMAPI settings entry is absent

Original feedback:

- 玩家主要问题是：成功安装、检查程序无问题，启动后在标题界面看不到设置小窗。
- 图片转写：无截图。

Screenshot/log transcription:

- The archived install state records DTMAPI `0.5.2-alpha` installed at `2026-07-11 14:43:13 +08:00`.
- `DTMAPI-latest.log:1` proves one DTMAPI startup at `14:47:01`; lines 125 and 127 prove only that the title UI host and EventSystem initialized in that old session. There is no `title settings button visible on HomePageUiState` line in the captured DTMAPI log.
- Steam process evidence splits the apparent `14:47-14:54` interval into two launches. PID `6952` was added at `14:47:11` and removed at `14:47:16`; this short launch owns the only fresh BepInEx/DTMAPI log. PID `7016` was added at `14:47:48` and ran until `14:54:08`, but produced no newer BepInEx/DTMAPI log.
- The title UI implementation logs separate milestones for `title settings button visible on HomePageUiState`, button click, and menu open. None appears in the only successful runtime log. Initialization therefore cannot be promoted to stable player-visible presentation.
- `DTMAPI-latest.log:15` records `com.mxx.doloc.itemlimiter.installer` copying a core BepInEx plugin and requesting a restart. This is a real state-changing third-party action, but the archive does not prove that it caused the later injection loss.
- Steam content lines 220-222 record Doloc Town becoming `Files Missing,Uninstalling` and then `Uninstalled` at `14:54:30`. Lines 223-265 record a full game reinstall/update finishing at `14:56:10` on build `23762374`.
- Steam console lines 481-497 and process evidence prove a fresh Steam launch at `15:56:48` with `DolocTown.exe` PID `12208`. The collector ran at `16:01:37` while that process was still present.
- The collected Unity `Player.log` from the current session reaches `HomePageUiState` and `ModUiState` at lines 140-145, but contains no DTMAPI or BepInEx startup evidence.
- The newest BepInEx and DTMAPI logs stop at approximately `14:47`, about 69 minutes before the `15:56` player session. They are stale evidence from a different run.
- The generated `startup-analysis.md` nevertheless labels the archive `NormalDtmapiStartup` because it reads any captured DTMAPI startup segment without comparing its timestamp to the current process start.
- The archive itself was moved to `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/`; SHA-256 is `C2AF5F08E9632C75F9391404B9A1E167A81F6BF331C5FFDABA2A068D4A966CA8`.
- Follow-up A/B evidence: Steam unsubscribed item `3759797170` at `20:00:03`; DTMAPI uninstalled at `20:00:51`, reinstalled at `20:04:00`, and the game launched as PID `6572` at `20:04:14`. The current Unity log reaches native `HomePageUiState`, but no new BepInEx or DTMAPI log was created by collection at `20:07:25`.
- The follow-up BepInEx and DTMAPI logs are byte-for-byte identical to the first archive, not merely files with stale timestamps. Their SHA-256 values are respectively `1503D52B2F27504609C9C62A31157F5AE7BF274B3681D40C22A814ABCD4DC9D9` and `6653D21EE2D0DBBA99E2207670A28E73364C193C6B17C022FFF875ADD8C71385`.
- The second archive was moved beside the first and extracted under `extracted-2`; its SHA-256 is `57F3EE96D4C92A104768FE2F128DD4E3A4ED21B2F96030EBE9C738FD816DC2D1`.
- Screenshot transcription (`powershell-process-env-check.png`): `Get-Process DolocTown -ErrorAction Stop` reports that no `DolocTown` process exists. `$p` is therefore unset, and the empty `$p.Modules` result is not loaded-module evidence. The same screenshot shows empty `DOORSTOP_DISABLE(User)` and `DOORSTOP_DISABLE(Machine)` values.
- Screenshot transcription (`powershell-loader-hash-check.png`): all five checked files match the trusted subscription payload exactly: `winhttp.dll` 26112 bytes / `8C6CDBC38836DEE87E3368F5DE1994D7C0CCEBF29E4CE7ABA3C0981F9375412C`; `doorstop_config.ini` 1460 / `4D5C6DFA0F771C6A5B1B0C559ACA0BD0ECE7D08B08FFF894708DC3B73CE73CFC`; `.doorstop_version` 5 / `75A2F501000D4FE28F74BC7EE66DAE5581959D28B6C44F0C1B0C05CD5BA261E6`; `BepInEx.dll` 128512 / `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70`; `BepInEx.Preloader.dll` 43008 / `55D3895351A9D16B63B6F35F1C01B44AC650979E853D0BD3A442B92A082AF64F`.
- Screenshot transcription (`powershell-live-winhttp-modules.png`): a live `DolocTown` PID `7924` started at `20:23:18` from the expected Steam game directory. Its module list contains both the game-local `WINHTTP.dll` and `C:\WINDOWS\system32\WINHTTP.dll`. The local proxy is therefore admitted and resident; loading the system library alongside it is normal proxy forwarding, not a duplicate-mod conflict by itself.
- Screenshot transcription (`powershell-full-compare-missing-trusted-extract.png`): `Get-ChildItem` fails because `...\3743016467\Content\.tools\bepinex\extract` does not exist on the player's machine. No trusted files were read and `$results` contains no comparison data.
- Screenshot transcription (`powershell-core-list-part1.png` / `part2`): `$trustedCoreNames` also fails to populate from the missing path. The command therefore prints all game core files as "extra". Their 18 names and lengths match the expected BepInEx `5.4.23.5` core inventory; this output is not proof of extra files. The `BepInEx\patchers` query returns no files.
- Screenshot transcription (`steam-launch-options-empty.png` / `steam-default-public-branch.png`): Steam launch options are blank, and the selected version is the default public/latest release rather than a beta branch.
- Screenshot transcription (`powershell-zip-full-compare-part1.png` / `all-match.png`): the bundled BepInEx ZIP exists and passes expected SHA-256 validation; it expands into a unique `%TEMP%\DTMAPI-BepInEx-compare-*` directory. The comparison returns no `MISSING` or `MISMATCH` objects and prints `全部 22 个 BepInEx 文件完全一致。`

Review record:

- User-confirmed facts: installer was reported successful; the status program was reported as showing no problem; the title-screen settings entry was not visible.
- Screenshot/log observations: the current `15:56` game session has native Unity/title evidence but no fresh BepInEx/DTMAPI evidence; only an earlier `14:47` DTMAPI session exists.
- Screenshot/log observations: no captured session proves stable title-button presentation. The one injected launch exited after only a few Steam-observed seconds and stopped before the visibility milestone; the following six-minute launch was already native-only.
- Code/doc facts inspected: `check-dtmapi-status.ps1` treats the latest runtime log as optional and checks install files/config statically; `analyze-startup-evidence.ps1` classifies any archive containing startup lines and `DTMAPI runtime starting` as `NormalDtmapiStartup` without a freshness correlation.
- Codex inference: at the time of the reported missing window, the DTMAPI bootstrap was not active. Therefore the player-visible symptom is upstream of config-menu rendering.
- Ownership: installer/runtime injection state and diagnostics freshness; not primarily `DTMAPI.ModConfigMenu` or title Canvas layout.
- Root-cause hypotheses:
  - Confirmed symptom-level cause: the current Steam launch did not load BepInEx/DTMAPI, so DTMAPI could not mount its title entry.
  - Strong contributing event: Steam fully uninstalled/reinstalled the game after the one proven DTMAPI launch, invalidating or desynchronizing the installed injection footprint.
  - Earlier boundary: injection loss is already observable on the `14:47:48-14:54:08` launch, before the Steam game reinstall. The reinstall is therefore not the original onset and cannot be treated as the sole root cause.
  - Trigger attribution limit: `Priority User Initiated` at `14:54:14` belongs to the separate Workshop unsubscribe/update that removed 182 files under `steamapps/workshop/content/2285550`. The game-depot transition to `Files Missing,Uninstalling` at `14:54:30` does not name its initiator. The archive therefore cannot distinguish a Steam UI uninstall/reinstall from external deletion/movement of game files followed by Steam recovery.
  - Follow-up suspect only: the third-party `com.mxx.doloc.itemlimiter.installer` writes a BepInEx plugin and asked for a restart immediately before later runs; isolate it only if reinjection still fails after reinstalling DTMAPI.
  - Follow-up A/B result: removing the item and copied DLL did not restore injection after a new DTMAPI install. The Mxx plugin is rejected as the direct cause of the absent BepInEx/DTMAPI startup, though its independent owner-lifecycle and Harmony risks still justify leaving it disabled during diagnosis.
  - Narrowed loader boundary: the `20:04` install state says BepInEx was already detected and was not installed by DTMAPI. Given the player-facing install command includes `-InstallBepInEx`, the static completeness predicate passed and skipped a refresh. The next distinction is whether the process loaded the game-local proxy DLL at all.
  - Screenshot result: checked loader-file replacement/corruption and persistent `DOORSTOP_DISABLE` are now rejected. A generic claim that an installed mod modified these BepInEx/Doorstop files is unsupported.
  - Remaining evidence gap: the player ran the module query after the game had exited. The next run must keep the failing title process alive while enumerating `winhttp.dll` modules.
  - Live follow-up closes that gap: the local UnityDoorstop proxy is loaded. The diagnosis moves downstream to Doorstop target invocation or very-early BepInEx Preloader dependency/startup failure, still before any DTMAPI plugin code.
  - Full comparison attempt does not add a conflict finding because its trusted directory was absent. The next comparison must expand and hash-verify the bundled ZIP into a temporary directory instead of relying on the optional/cache `extract` path.
  - Corrected full comparison closes that gap: all 22 trusted payload files are byte-identical. A mod or local corruption changing the installed BepInEx/Doorstop payload is rejected for this machine state.
- Rejected/unproven hypotheses:
  - Not supported as a title-UI drawing defect in the reported session: DTMAPI was not running in that session.
  - Not caused by a missing bundled title icon in the successful `14:47` run: `IconLoad ... result=loaded` is present.
  - Not a general 0.5.2-alpha subscription package structure failure: the temporary package audit passed all required cases with zero blockers.
  - Not attributable to the DTMAPI uninstaller in the captured evidence: its last run was at `14:40:28`, recorded `RemoveBepInExRequested=false`, removed only DTMAPI-owned plugin/tools/state paths, and completed without errors about 14 minutes before Steam's game-level uninstall.
  - Not attributable to Workshop unsubscribe alone: Steam completed the Workshop deletion at `14:54:14`; the independent game app entered `Uninstalling` 16 seconds later.
  - The archive cannot prove which exact root injection file or loader state became ineffective; it did not include current root-file hashes or the actual status-check output.
- Required downstream updates: keep `ISSUE-012` open; future implementation should make status/collection distinguish static install presence from fresh runtime injection.
- Implemented diagnostic follow-up: Update `20260711-0012` adds a double-click Workshop/player helper which automates the remaining Process Monitor startup capture, validates Microsoft Procmon identity, filters destructively to `DolocTown.exe`, embeds ordinary logs, and returns one ZIP. This changes diagnostics only; it does not claim the player loader issue fixed.
- First-helper feedback and analysis: the window closed before launching the game because the packaged UTF-8 BAT used LF-only line endings and `cmd.exe` resumed label jumps inside command text. The exact helper was replaced with a CRLF-normalized build, BAT-level elevation, a persistent fallback error file, and a zero-blocker post-fix package audit. No new conclusion about Doorstop/BepInEx is drawn until the player returns the corrected capture.
- Maintainer self-test follow-up: the probe captured fresh DTMAPI runtime/UI/button-visible milestones, proving the evidence set is sufficient, but the pre-fix classifier preferred an unreadable locked BepInEx snapshot over the fresh DTMAPI log. The correction prioritizes fresh DTMAPI, automatically closes the verified captured PID after collection, removes UTF-8 BAT labels, and adds explicit post-return cleanup. This run validates the probe only and remains separate from the affected player's failure.
- Packaging decision: startup capture is a standalone player probe, not a Runtime Workshop feature. The formal Runtime package contains neither root `5` nor the capture script; standalone root `6` removes only probe-created Desktop output/error and the Procmon cache after exact confirmation.
- Probe v2 decision: add automatic bounded launch/security context rather than another player procedure. The returned ZIP now includes parent-chain/protected command-line facts, known Loader variables, compatibility/mitigation state, antivirus/selected Defender state, relevant security events, critical Loader ACL/Zone facts, and Procmon result aggregation. Full target-process environment, Defender exclusions, full event logs, and Zone URLs remain outside collection for privacy and false-positive control.
- Acceptance checks:
  - After any Steam verify/reinstall/update, rerun `1_install_dtmapi.bat`, then run `3_check_dtmapi_status.bat` and retain its full timestamped output.
  - Launch through Steam and require new BepInEx/DTMAPI log timestamps later than the process start.
  - Require `DTMAPI runtime starting`, `DTMAPI reflected title settings UI initialized`, and `DTMAPI title settings button visible on HomePageUiState`, plus the player-visible title icon/window.
  - A collector run with a live game process and only pre-process-start runtime logs must not classify the run as `NormalDtmapiStartup`.
  - If injection still fails, retest once with Workshop item `3759797170` disabled/unsubscribed and a clean DTMAPI reinstall; do not modify that third-party package.
- Follow-up diagnostic request:
  - Loaded `winhttp.dll` module paths are now captured and show both the trusted local proxy and system library.
  - User/machine `DOORSTOP_DISABLE` values and trusted-payload hash comparisons are complete and clean.
  - Full payload, patchers, Steam launch options, and branch selection are clean. Capture a Process Monitor startup trace to determine whether the Preloader is never requested, cannot open a dependency, or starts and fails before logging.
- Blocker conditions: the exact loader failure remains open until a fresh-Steam A/B run or bounded actual-process environment/IAT-hook evidence distinguishes the two remaining native Doorstop branches.

### Returned startup-capture follow-up

- Archive transcription: the `10,046,616`-byte ZIP was moved to `player-captures/DTMAPI-startup-capture-20260711-223703.zip`; SHA-256 is `AED5A9B1B0A1C0ACF34A15CE4C484614E8D61BC5F8E888E0FE95AB8FB9C280FF`. Its 37 entries passed rooted/traversal/duplicate-destination validation before extraction; none of its executable content was run.
- Process transcription: Steam launched PID `10316` at `22:37:58` with the expected executable-only child command line. The local UnityDoorstop `WINHTTP.dll` is resident, and the probe later closed the captured game PID gracefully.
- Procmon transcription: `doorstop_config.ini` is opened and read successfully many times. `BepInEx.Preloader.dll` has exactly one `QueryOpen / SUCCESS` at `22:37:58.9122437`, but no `CreateFile`, `ReadFile`, mapping, core-dependency access, or `LogOutput.log` activity follows. Native Mono still loads normally.
- Freshness transcription: BepInEx and DTMAPI logs remain the old `14:47` files. The failing process is once again native-only.
- Security transcription: no relevant Loader `ACCESS DENIED`, `SHARING VIOLATION`, or `BAD IMAGE`; no bounded Defender, Code Integrity, or AppLocker event; all ten critical Loader paths exist without a Zone stream, explicit deny ACE, or ACL read error. No IFEO/AppCompat override is present.
- Classifier correction: `PreloaderReadNoFreshBepInExLog` counts the metadata-only `QueryOpen` as a Preloader event. The durable interpretation is `target exists, Preloader not opened`, not `Preloader read and failed`.
- Code-path finding: UnityDoorstop checks target-assembly existence before injection, can disable itself from the actual process's `DOORSTOP_DISABLE`, installs an IAT `GetProcAddress` hook, and only opens the target assembly inside the later Mono bootstrap. The observed sequence ends before that bootstrap.
- Root-cause narrowing: remaining branch A is an actual-process disable/duplicate-load state inherited from the long-running Steam process; branch B is an IAT hook which failed or did not take effect. Probe/user/machine variables are clean but do not cover the actual Steam/DolocTown process environment, so branch A remains unproven rather than rejected.
- Player next step: reset only process state—close the game, fully exit Steam or restart Windows, start Steam normally, then rerun the same probe once. Do not reinstall files or set `ignore_disable_switch=true` for this comparison.
- If still reproduced: collect only actual-process `DOORSTOP_DISABLE`, `DOORSTOP_INITIALIZED`, and bounded `DOORSTOP_*` names in the next probe revision; exclude the rest of the environment for privacy.
- Updated blocker: static payload, proxy admission, mod `3759797170`, launch options/branch, ACL/Zone, and common security interception have been closed or strongly reduced. The completed reboot A/B restores healthy runtime evidence; only player-visible confirmation remains for recovery, while exact native attribution requires a future recurrence captured before state reset.

### Post-reboot cold/warm Steam A/B

- User test order: after restarting Windows, run probe `5` once with both Steam and the game initially closed; later run it again with Steam open and the game closed.
- Archive transcription: cold return `439,779` bytes / SHA-256 `3684901A6A939EBE07EEDB892FD6B405B0616C078C5B1A99F1164AB48BA14F07`; warm return `9,325,284` bytes / SHA-256 `6B72D8A1005FCED5CA6D5ECA09757DE6E75BF1C2E9EB31EB7CA33E80D567B98D`. Both were moved under `player-captures`, validated, and extracted without execution.
- Cold-run correction: `GameProcessNotObserved` reflects only the probe's 60-second wait. Capture began `13:13:38`; Steam started `13:13:44`, spent `42.23` seconds initializing, handled the URI at `13:14:29`, and created PID `20420` at `13:14:49`, 71 seconds after capture began and outside the wait.
- Cold-run delayed proof: PID `20420` produced `DTMAPI runtime starting` at `13:15:03`, UI initialization at `13:15:04`, and `title settings button visible on HomePageUiState` at `13:15:47`. The successful log is preserved by the later archive as `DTMAPI-log-history/latest-20260712-051557320.log`; it contains no DTMAPI error, fatal, or warning line.
- Cold-run probe consequence: Procmon was stopped before the late game appeared; the collector noticed the process but the probe did not own/close it. Steam records manual/natural removal at `13:26:45`. This is a probe timeout/late-PID handling defect, not a failed DTMAPI startup.
- Warm-run transcription: Steam PID `18972` began at `13:34:18`; URI at `13:36:04`; game PID `6368` at `13:36:08`. Preloader activity progresses from metadata query to successful open and a full 43,008-byte read at `13:36:09`.
- Warm-run runtime proof: fresh BepInEx loads `DTMAPI Bootstrap 0.5.2.0`, Chainloader completes, DTMAPI starts at `13:36:25`, and the title-button-visible milestone occurs at `13:37:14`; no DTMAPI error, fatal, or warning line exists. The probe closes PID `6368` gracefully.
- A/B conclusion: both post-reboot launches injected normally, so pre-opening Steam is not required. The material recovery boundary is the fresh Windows/Steam process session, with no reported reinstall or payload replacement.
- Root-cause limit: the A/B proves transient session state and rejects a persistent payload/UI defect, but it cannot distinguish historical process-only Doorstop disable/duplicate state from ineffective IAT hooking because reboot erased the failing process state.
- Player visual confirmation: the player reports that the settings button was actually visible after reboot. This agrees with both post-reboot `title settings button visible on HomePageUiState` milestones and closes the rendered-visibility question.
- Final incident classification: restart-resolved / mitigated. Confirmed symptom cause was a pre-BepInEx UnityDoorstop invocation failure in the old process session; recovery followed a full Windows/process-session reset without payload replacement, and the player visually confirmed the title button. The exact transient native trigger remains unknowable and is not an active investigation for this incident.
- Third-party ownership note: `com.mxx.doloc.itemlimiter.installer` directly installs `Mxx_DolocTownMod_Plugins.dll` into the shared `BepInEx\plugins` directory. It is not a DTMAPI-owned ordinary Mod, so DTMAPI does not own its update/uninstall lifecycle and should not remove it automatically. Direct shared-loader installation is a potential compatibility surface, but this case's removal/reproduction A/B did not change the failure and no impact from that DLL was found.
- Diagnostic follow-up decision: do not implement from this review-only request. A later authorized probe change should extend cold-launch waiting (for example 180 seconds), verify/close a late requested PID, and preserve the existing one-click player workflow.

## Cross-Issue Summary

- Confirmed user facts: installation/check were perceived as successful, but the title DTMAPI entry was absent.
- Screenshot/log facts: the report mixed a stale successful DTMAPI run with a later native-only game session.
- Code-path findings: current status and startup-analysis scripts can produce a reassuring result without proving that the current session injected DTMAPI.
- Risks: treating stale logs as current can misroute future reports into title-UI code and hide an upstream loader failure.
- Suggested implementation scope: add process/log freshness and root injection inventory to status/collector output; keep the title UI unchanged until a fresh injected session still fails visibly.
- Implemented bounded scope: the new helper handles the immediate native-proxy-to-Preloader evidence gap without changing title UI, Doorstop, BepInEx, bootstrap, or DTMAPI runtime code. Broader stale-log status/analyzer correction remains separate follow-up work.
- Items that should not be carried forward: do not revive the retired in-save IMGUI overlay and do not treat historical `ISSUE-003` as this root cause.

## Implementation Record Decision

- Create/update an implementation update record: yes for this evidence move and durable analysis; no runtime fix is authorized or claimed.
- Additional debug/API/hook/smoke records required: `ISSUE-012` is required; no Hook/API/smoke change because no game/runtime validation was run.
- Suggested task title: make DTMAPI status and collected startup analysis detect stale runtime logs after Steam verify/reinstall.
- Completion standard: fresh-session injection evidence plus player-visible title entry, and diagnostics that no longer classify stale logs as normal startup.
