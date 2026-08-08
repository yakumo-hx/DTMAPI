# ISSUE-012: Player title settings entry missing while current DTMAPI runtime is absent

## Status

- State: `mitigated / restart-resolved`
- Opened: 2026-07-11
- Severity: high
- Area: installer / BepInEx-Doorstop injection / title UI / diagnostics freshness
- Related review: `docs/reviews/manual-qa/2026/20260711-0001-player-title-settings-runtime-missing.md`
- Evidence: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/`

## Symptom

The player reports a successful DTMAPI install and clean status check, but the title-screen DTMAPI settings entry is not visible.

## Current Classification

The captured failing session is not a config-menu render failure. Steam launched `DolocTown.exe` at `15:56:48`, and the current Unity log reached the title UI, but the newest BepInEx/DTMAPI logs end around `14:47`. DTMAPI was not active in the reported session.

The one injected launch does not prove player-visible UI. Its logs contain EventSystem creation and reflected title UI initialization, but not the distinct `title settings button visible on HomePageUiState`, click, or menu-open milestones. Steam observed PID `6952` for only `14:47:11-14:47:16`. A separate PID `7016` then ran from `14:47:48` to `14:54:08` without producing any fresh BepInEx/DTMAPI log. Runtime injection had therefore already disappeared before Steam reinstalled the game.

Steam also recorded a full Doloc Town uninstall/reinstall from `14:54:30` through `14:56:10`, after the one proven DTMAPI startup. This is a strong environment-changing event. The archive does not identify the exact ineffective loader file after that event.

The initiating actor is not present in the captured Steam logs. `Priority User Initiated` at `14:54:14` applies to a separate Workshop unsubscribe/update which removed 182 Workshop files. The game app independently entered `Files Missing,Uninstalling` at `14:54:30`. This is consistent with either a Steam UI uninstall/reinstall or game files being removed/moved externally and Steam rebuilding them; it is not sufficient to choose between those paths.

A clean follow-up isolation run at `20:04` reproduces the same boundary after Workshop item `3759797170` and its copied `Mxx_DolocTownMod_Plugins.dll` were removed. Steam records the item unsubscribe at `20:00:03`; DTMAPI was uninstalled at `20:00:51`, reinstalled at `20:04:00`, and launched as PID `6572` at `20:04:14`. The current Unity log reaches `HomePageUiState`, while the collected BepInEx and DTMAPI logs are byte-for-byte identical to the old `14:47` files. The Mxx plugin is therefore rejected as the direct cause of the missing current-session injection, although its separate lifecycle/compatibility problems remain out of scope for this loader diagnosis.

Compatibility note: `com.mxx.doloc.itemlimiter.installer` directly places `Mxx_DolocTownMod_Plugins.dll` under the shared `BepInEx\plugins` tree rather than a DTMAPI-owned ordinary-Mod directory. That binary is outside DTMAPI's ownership, update, compatibility, and uninstall lifecycle; DTMAPI must not silently delete or manage it. This is a general conflict surface, but the removal/reproduction A/B and later restart recovery provide no evidence that it affected this incident.

The follow-up install state records `BepInExDetectedBeforeInstall=true` and `BepInExInstalledByDTMAPI=false`. Because the player-facing installer uses `-InstallBepInEx`, this combination means the installer's static completeness test passed before the `20:04` install and it intentionally did not refresh Doorstop/BepInEx. Static completeness checks only require non-empty expected files and an enabled target in `doorstop_config.ini`; they do not verify file hashes, the `DOORSTOP_DISABLE` environment switch, or whether the running process actually loaded the local proxy `winhttp.dll`.

The live module check now proves PID `7924` (started `20:23:18`) loads both `E:\Program Files (x86)\Steam\steamapps\common\Doloc Town\WINHTTP.dll` and `C:\WINDOWS\system32\WINHTTP.dll`. This is consistent with the game-local UnityDoorstop proxy loading and forwarding to the real Windows WinHTTP library. The remaining failure boundary is after native proxy admission but before a successful BepInEx Preloader/Chainloader startup and DTMAPI plugin load.

Update `20260711-0012` now provides a double-click capture route for this exact remaining boundary. It downloads Process Monitor from Microsoft at runtime, verifies Microsoft Authenticode plus Procmon product identity, destructively filters to `DolocTown.exe`, launches the game automatically, records Doorstop/Preloader file activity and module/log freshness, runs the bounded collector, classifies the result, and produces one support ZIP. Process Monitor is not redistributed in the DTMAPI package.

The player's first helper double-click did not reach game launch. This was reproduced as a BAT packaging defect: LF-only/UTF-8 label seeking caused `cmd.exe` to resume inside command text; a later self-test also exposed a malformed trailing partial command. The corrected standalone probe uses label-free ASCII BAT wrappers, keeps failures visible, writes a Desktop fallback error file, and closes only its verified captured game PID after collection. This probe defect is separate from the still-open game injection failure.

A maintainer self-test at `21:35` confirms evidence coverage but is not affected-player proof: it captured successful local Doorstop/Preloader access, fresh DTMAPI startup, reflected title UI initialization, and `title settings button visible on HomePageUiState`. The original summary misclassified that success because BepInEx held its log open; Update `20260711-0012` now prioritizes fresh DTMAPI and retries after automatic game close. The probe is standalone and absent from the Runtime Workshop package; root `6` removes only probe output/cache after return.

Probe v2 adds bounded automatic evidence for the leading process-launch/security hypotheses without extra player actions: parent chain/command line, known Loader variables at probe/user/machine scope, IFEO/AppCompat/mitigation context, registered antivirus and selected Defender state, relevant capture-window Defender/Code Integrity/AppLocker events, critical Loader ACL/Zone facts, and aggregated Procmon results. It intentionally does not claim access to Steam/DolocTown's complete inherited environment and excludes full environment/event/security-exclusion data.

The affected player's returned v2 trace at `22:37` closes the native-proxy-to-Preloader ambiguity. The game-local UnityDoorstop `WINHTTP.dll` is live, `doorstop_config.ini` is opened and read successfully, and `BepInEx.Preloader.dll` receives exactly one successful `QueryOpen` metadata request. There is no Preloader `CreateFile`, `ReadFile`, mapping, BepInEx core dependency access, or `LogOutput.log` activity. Native Mono then loads normally and the title session remains native-only. The automatic label `PreloaderReadNoFreshBepInExLog` is therefore semantically too strong for this trace: the target file was found, but the managed Preloader assembly was not read.

UnityDoorstop's source performs the target-assembly existence check before injection, honors the actual process's `DOORSTOP_DISABLE` value, installs an IAT hook around `GetProcAddress`, and only opens the target assembly later from the Mono bootstrap. The returned sequence narrows the remaining branches to an actual-process disable/duplicate-load state or a failed/ineffective IAT hook. Probe/user/machine variables are clean, but v2 deliberately does not read the environment block inherited by the already-running Steam process, so the first branch is still untested rather than rejected.

The July 12 reboot A/B shows recovery without a file reinstall. When Steam was initially closed, the probe's 60-second PID wait expired, but DolocTown appeared 71 seconds after capture began and subsequently produced a clean DTMAPI startup at `13:15:03` plus `title settings button visible on HomePageUiState` at `13:15:47`. With Steam pre-opened, PID `6368` was captured normally: the Preloader progressed from `QueryOpen` to `CreateFile` and a complete `43,008`-byte `ReadFile`, BepInEx and DTMAPI logs were fresh, Chainloader completed, and the title-button-visible milestone appeared at `13:37:14`. Thus pre-opening Steam is not the recovery condition; resetting the Windows/Steam process session is the material difference.

The player now confirms that the DTMAPI settings button was actually visible after reboot. This closes the possible gap between the runtime visibility milestone and the rendered screen. For this incident, the operational resolution is recorded simply as `restart resolved`; the exact native producer is not pursued because reboot removed the failing process environment and hook state. If the symptom recurs, preserve the failing Steam session and collect actual-process Doorstop state before any restart.

## Diagnostic Gap

- `check-dtmapi-status.ps1` validates static install files and Doorstop configuration, but does not prove that the latest/current launch injected DTMAPI.
- `analyze-startup-evidence.ps1` classifies an archive as `NormalDtmapiStartup` whenever captured logs contain startup segments, even if those logs predate the live game process by more than an hour.
- The collector did not preserve the player's status transcript or a timestamped/hash inventory of `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, BepInEx core, and DTMAPI plugin files.
- The collector does not record the loaded `winhttp.dll` module path or the user/machine `DOORSTOP_DISABLE` environment values, so it cannot distinguish a local proxy that was ignored/blocked from a preloader failure after proxy load.

## Known Facts

- Install state: DTMAPI `0.5.2-alpha`, installed at `14:43:13`.
- Proven DTMAPI startup: `14:47:01`; title UI host initialized and icon asset loaded.
- Player-visible title UI: not proven; visibility/click/open milestones are absent.
- First proven native-only follow-up: PID `7016`, `14:47:48-14:54:08`, no fresh BepInEx/DTMAPI log.
- Third-party state change: `com.mxx.doloc.itemlimiter.installer` copied a BepInEx plugin and requested restart during the proven run.
- Steam full game reinstall: `14:54:30` to `14:56:10`, build `23762374`.
- Failing/current launch: Steam process at `15:56:48`; collected at `16:01:37`; native title UI present; no fresh BepInEx/DTMAPI log.
- Subscription package audit: the local Steam `0.5.2-alpha` package passed the required PowerShell 5.1/install/check/collect/uninstall matrix with zero blockers.
- Clean isolation follow-up: item `3759797170` unsubscribed at `20:00:03`; DTMAPI reinstalled at `20:04:00`; PID `6572` launched at `20:04:14` and reached native `HomePageUiState`; collection ran at `20:07:25` while the process was alive.
- Follow-up runtime freshness: BepInEx log SHA-256 remained `1503D52B2F27504609C9C62A31157F5AE7BF274B3681D40C22A814ABCD4DC9D9`; DTMAPI log SHA-256 remained `6653D21EE2D0DBBA99E2207670A28E73364C193C6B17C022FFF875ADD8C71385`. Both are identical to the first archive and stop at `14:47:16`.
- Player screenshot hash check: `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `BepInEx.dll`, and `BepInEx.Preloader.dll` match the trusted subscription payload in both length and SHA-256. There is no evidence that a mod replaced or corrupted these five loader files.
- Player screenshot environment check: both user- and machine-scope `DOORSTOP_DISABLE` values are empty.
- Loaded-module check: not collected. `Get-Process DolocTown -ErrorAction Stop` failed with `NoProcessFoundForGivenName`, so the later `$p.Modules` command operated on no process and cannot prove which `winhttp.dll` is selected during a failing launch.
- Live loaded-module follow-up: PID `7924`, start `20:23:18`, game executable at the expected Steam path; both the game-local UnityDoorstop proxy and system WinHTTP are loaded.
- Full-payload comparison attempt: invalid/incomplete. The chosen trusted path `...\3743016467\Content\.tools\bepinex\extract` does not exist on the player's machine, so `$results` was never populated and `$trustedCoreNames` was empty. The subsequent listing of every core file under "extra" is a command artifact, not evidence that those files are unexpected.
- Core/patcher observations: the displayed 18 core filenames and byte lengths match the expected BepInEx `5.4.23.5` payload inventory; `BepInEx\patchers` contains no files. Full hashes beyond the five already checked remain uncollected.
- Steam configuration screenshots: launch options are empty and the game is on the default public branch, not a beta branch.
- Subscription comparison source: the local official subscription for the same Workshop manifest `6297788246671130629` contains the trusted BepInEx ZIP with SHA-256 `82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4`. The `extract` directory is a cache/source convenience and should not be assumed present; the ZIP is the canonical comparison input for the next player check.
- Corrected full-payload comparison: the player's bundled ZIP passes the required SHA-256 check, expands successfully to a unique temp directory, and all 22 extracted BepInEx/Doorstop files match the game-directory copies byte-for-byte. No missing or mismatched bundled loader file remains.
- Returned v2 capture: original ZIP `D:\下载\DTMAPI-startup-capture-20260711-223703.zip` was moved to `player-captures/DTMAPI-startup-capture-20260711-223703.zip`; SHA-256 `AED5A9B1B0A1C0ACF34A15CE4C484614E8D61BC5F8E888E0FE95AB8FB9C280FF`, 37 extracted files.
- Affected process: PID `10316`, Steam-parented, started `22:37:58`; the probe closed it with `GracefulExit` after capture.
- Affected Procmon boundary: `208,933` total events; `77` Doorstop-config events; repeated successful 1,460-byte configuration reads; exactly one Preloader `QueryOpen / SUCCESS` at `22:37:58.9122437`; no Preloader file open/read or fresh BepInEx/DTMAPI log.
- Affected launch/security context: expected Steam command line; no selected Loader variable at probe/user/machine scope; no IFEO/AppCompat override; Windows Defender only; zero bounded Defender/Code Integrity/AppLocker matches; ten critical Loader paths present with no Zone stream, explicit deny ACE, or ACL error.
- Actual-process environment remains uncollected. Empty probe/user/machine `DOORSTOP_DISABLE` does not prove that the long-running Steam process and its child lack an inherited process-only value.
- Reboot cold-Steam return: ZIP SHA-256 `3684901A6A939EBE07EEDB892FD6B405B0616C078C5B1A99F1164AB48BA14F07`; probe start `13:13:38`; Steam start `13:13:44`; game PID `20420` start `13:14:49`; DTMAPI start `13:15:03`; title button visible `13:15:47`; no DTMAPI error/fatal/warning line.
- Cold-start classifier limitation: `GameProcessNotObserved` is technically true only for the 60-second wait. PID `20420` appeared 71 seconds after capture began; the collector saw it at `13:14:49`, and the next archive preserved its successful `13:15` DTMAPI log. The probe stopped Procmon too early and did not own/close this late process.
- Reboot warm-Steam return: ZIP SHA-256 `6B72D8A1005FCED5CA6D5ECA09757DE6E75BF1C2E9EB31EB7CA33E80D567B98D`; Steam PID `18972` start `13:34:18`; game PID `6368` start `13:36:08`; Preloader full read at `13:36:09`; DTMAPI start `13:36:25`; title button visible `13:37:14`; zero DTMAPI error/fatal/warning lines; probe records `GracefulExit`.
- Post-reboot result: both cold- and warm-Steam game launches inject normally. No reinstall or payload change was reported between the pre-reboot failure and these recovery runs.
- Player visual acceptance: the player confirms the post-reboot DTMAPI settings button was visible. Runtime, log, and player-visible acceptance now agree.
- Final operational disposition: restart resolved. No DTMAPI runtime/UI or payload change is required for this incident.

## Rejected Or Unproven Hypotheses

- Rejected for this session: a DTMAPI config page/layout bug is not sufficient to explain the symptom because DTMAPI was not loaded.
- Rejected as a general package blocker: the matching-version subscription package passes the release audit matrix.
- Rejected as the direct source based on this archive: DTMAPI's last uninstaller run at `14:40:28` removed only DTMAPI-owned plugin/tools/state paths with `RemoveBepInExRequested=false`; it did not request or record deletion of the game depot.
- Rejected as sufficient by itself: unsubscribing Workshop item `3759797170` removed files only from the Workshop content depot and completed before the separate game-level uninstall.
- Rejected: Steam reinstall alone caused the first missed injection. The `14:47:48` launch already has no fresh loader/runtime log, before the `14:54:30` uninstall.
- Rejected as the direct injection-loss cause: Workshop item `3759797170` and its copied DLL were removed before a clean DTMAPI reinstall and the `20:04` reproduction. Keep it disabled while diagnosing because it has separate compatibility risks, but it no longer explains the absence of a fresh BepInEx header.
- Rejected from the supplied screenshot: replacement/corruption of the five checked Doorstop/BepInEx files. Their hashes exactly match the trusted `0.5.2-alpha` subscription payload.
- Rejected from the supplied screenshot: persistent user- or machine-scope `DOORSTOP_DISABLE` disabling Doorstop.
- Rejected from the live module screenshot: Windows completely refusing or bypassing the game-local `winhttp.dll`. The exact trusted proxy is resident in the failing game process.
- Rejected from the Steam screenshots: custom launch options or a selected beta branch causing this run.
- Not established by the failed comparison: extra or modified BepInEx core files. The command could not load its trusted baseline, and the visible list actually matches expected names/sizes.
- Rejected by the corrected ZIP comparison: a mod, incomplete install, or local corruption changed any of the 22 bundled BepInEx/Doorstop files. This is now a complete payload result, not a five-file sample.
- Rejected by the affected Procmon trace: a managed BepInEx exception after Preloader assembly load. The Preloader assembly is never opened.
- Strongly reduced by the affected v2 context: Loader ACL/Zone denial, Defender/AppLocker/Code Integrity intervention, IFEO/AppCompat override, or an unexpected third-party antivirus/overlay module. No relevant blocking evidence was captured at those boundaries.
- Not rejected: process-only `DOORSTOP_DISABLE` or Doorstop duplicate-load state inherited by Steam/DolocTown. The probe explicitly does not read those actual process environment blocks.
- Not rejected: Doorstop's `GetProcAddress` IAT hook fails to install or take effect after configuration and target-path validation.
- Rejected as the recovery requirement: Steam must already be open before probe/game launch. The cold-Steam launch also injected successfully after its delayed process creation.
- Strongly reduced as a persistent machine or payload defect: reboot alone restored two clean startups with fresh BepInEx/DTMAPI logs and the title-button-visible milestone.
- Still unproven historically: the pre-reboot failure was specifically `DOORSTOP_DISABLE`/duplicate-load state rather than an ineffective IAT hook. Recovery establishes a transient session boundary but cannot reconstruct deleted process state.

## Next Evidence

1. Treat this incident as restart-resolved; no DTMAPI runtime, title UI, BepInEx payload, or game reinstall change is justified.
2. Treat complete Steam exit as the first recovery if the symptom returns and Windows restart as the second. Before either recovery, preserve the failing state with the probe if possible.
3. Prepare a future probe revision with a longer cold-Steam launch wait, verified late-PID handling, and privacy-bounded actual Steam/DolocTown `DOORSTOP_*` inspection. This is a diagnostic reliability follow-up, not evidence that DTMAPI runtime code needs a fix.
4. Do not claim the exact historical native producer unless a recurrence captures process-only environment or explicit Doorstop IAT-hook status before state reset.

## Acceptance Criteria

- A post-install Steam launch produces fresh BepInEx and DTMAPI logs after the process start.
- The runtime logs contain title UI initialization and `title settings button visible on HomePageUiState`.
- The player confirms the title icon/settings window is visible.
- Status/collector output clearly distinguishes static install presence from current-session injection.
- Startup analysis cannot label stale pre-process runtime logs as `NormalDtmapiStartup`.

Do not mark verified or closed from static package checks alone.
