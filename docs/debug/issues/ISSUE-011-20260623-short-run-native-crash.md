# ISSUE-011 2026-06-23 Short-Run Unity Native Crash And Evidence Gaps

## Status

- State: `open / evidence-improved / pending game validation`
- Severity: high
- Scope: DTMAPI startup/title UI diagnostics, Unity native crash export, third-party mod load failure classification, Y console breadcrumb evidence
- Source package version in player logs: `0.5.2-alpha / 0.5.2.0`

This issue is separate from `ISSUE-010` long-run Mono GC mark-stack crashes. The player packages analyzed here were collected from older distributed builds, before the current lifecycle cleanup/watchdog work was self-tested, uploaded, or distributed. They can show what the old player environment did, but they cannot prove the current lifecycle fixes work or fail.

## Evidence

- Archived player logs: `docs/debug/evidence/player-logs/20260623-download-dtmapi-logs`
- Short-crash analysis: `docs/debug/evidence/player-logs/20260623-download-dtmapi-logs/ANALYSIS-20260623-short-crash.md`
- Key sample: `DTMAPI-logs/20260621-224311`

## Known Facts

1. `20260621-224311` is a real short-session Unity native crash. DTMAPI started normally at about `22:35:49`; the Unity log reached `Crash!!!` at about `22:42:21`, roughly six and a half minutes later.
2. The native stack includes Mono/BDWGC registration frames such as `mono_gc_register_root`. There is no adjacent managed DTMAPI exception proving a direct C# throw caused the crash.
3. The collected package did not include the matching Unity crash dump directory, so the native owner cannot be proven from this package alone.
4. Startup logs repeatedly showed `Input System not yet initialized` while DTMAPI title settings UI was creating an EventSystem fallback through `InputSystemUIInputModule`.
5. A third-party mod `com.user.dolocnoweeds` failed Harmony patching during startup with `Owner can't be an array or an interface`. DTMAPI continued loading, so this should be reported as a mod load/patch failure, not a DTMAPI install failure.
6. The 2026-06-21 crash sample had recent Y-console give-item activity, including right-click x10 gives. This is useful context only; the log does not prove Y console caused the native crash.

## Rejected Or Unproven Hypotheses

- Not proven: current lifecycle cleanup caused or failed to fix this crash. The player package predates current lifecycle changes.
- Not proven: `com.user.dolocnoweeds` caused the native crash. Its Harmony failure is real, but DTMAPI survived startup and the crash occurred minutes later.
- Not proven: Y console give-item caused the native crash. The old logs lacked low-frequency breadcrumb context and Unity dump evidence.

## Current Fix Attempt

- Title DTMAPI settings and in-save Y console EventSystem fallback now reuse an existing EventSystem first and defer `InputSystemUIInputModule` creation until Unity Input System appears ready. If only the new Input System module is available but not initialized, DTMAPI records `UI.TitleSettingsEventSystem=pending` or `UI.DebugConsoleEventSystem=pending` instead of throwing and retrying noisily. DTMAPI-owned fallback EventSystems are no longer `DontDestroyOnLoad`; they are destroyed when title UI / debug console ownership ends or when a native EventSystem appears.
- In-game report export now includes runtime context when available: loaded/failed mod status, mod failure lines, current runtime version, and whether `OnApplicationQuit` was observed.
- Offline `collect-logs.ps1` now adds `support-context.txt` with recent DTMAPI/BepInEx failure tails, loaded/failed-mod clues, startup/quit markers, and Steam-tail evidence when available.
- Unity native crash collection covers both `%TEMP%\RedSawGames\DolocTown\Crashes` and `%TEMP%\RedSawGames\Doloc Town\Crashes`; duplicate crash-folder leaf names are separated by a short path hash. Directory selection keeps the newest directory from each Temp root and the newest crash facts first, then fills the remaining bounded budget with recent directories containing `crash.dmp`, so an older dump cannot crowd out the latest no-dump crash evidence. Skipped or missing dump evidence must write `Unity-Crashes/MISSING-CRASH-DUMP-README.txt` and `Unity-Crashes/summary.txt`.
- Crash-dump handling now lets the latest `crash.dmp` raise the per-report budget before copying adjacent small text evidence such as `error.log`, so a present dump does not crowd out the readable Unity error tail.
- Y console item give records a bounded `DebugConsole.LastGive` hook-status breadcrumb and `debug-console-last-give.txt` support file with item id, requested count, given count, right-click flag, and failure reason.
- Failed code-mod loads are classified as mod load failures and owner-bound DTMAPI events, API registrations, config pages, and custom entity registrations are cleaned up where possible. Loaded mods that catch their own exceptions but log through `Monitor.Error` are now surfaced as `MOD-RUNTIME-DIAGNOSTIC`, not DTMAPI install failures. This does not clean third-party Harmony patches or native side effects, so the report marks code-load failures as partial-side-effect risks and recommends restart/isolation.

## Acceptance Criteria

- Source checks pass: `git diff --check`, Release tests, Windows PowerShell 5.1 parser check for `tools/scripts/collect-logs.ps1`, and fake crash-tree collector verification.
- Next player or self-test report includes either the relevant `crash.dmp` or an explicit `MISSING-CRASH-DUMP-README.txt` reason.
- Startup logs no longer show repeated `Input System not yet initialized` exceptions from title settings or Y-console EventSystem creation.
- Y console can still open/search/give items, and report context shows bounded last-give breadcrumbs without per-frame noise.
- Do not mark solved until a clean game smoke and a fresh crash/no-crash package from the fixed build prove whether the native crash class still exists.
