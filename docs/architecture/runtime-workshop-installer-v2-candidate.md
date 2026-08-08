# Runtime Workshop Installer V2 Candidate

Status: isolated design candidate. The current canonical source boundary remains
`runtime-workshop-installer-boundary.md` until this candidate is independently
accepted and promoted.

## Why This Candidate Is Isolated

Historical player hotfixes on 2026-06-18, 2026-07-03 and 2026-08-07 copied a
known Runtime package into a new `dist/player-hotfix-*` tree and changed only
that copy. This candidate preserves that safety property while adding a tracked
overlay and builder under `tools/release/runtime-workshop-v2-candidate`.

The builder copies `dist/workshop-packages-0.6.1/DTMAPI` into a new ignored
candidate directory, removes the old installer tools from the copy, and applies
the V2 overlay. It does not edit the current installer source, current 0.6.1
candidate, local official upload directory, Steam subscription, or real game.

## Product Boundary

V2 is a normal-state convergence installer, not a general package manager.

```text
five thin BAT entries
  -> one CMD dispatcher
  -> nonce-bound PowerShell host probe
  -> one PowerShell action, executed once

install        = validate -> lock -> converge known BepInEx files -> replace DTMAPI-owned Runtime -> existence check
uninstall      = lock -> remove only the DTMAPI Runtime allowlist
full-uninstall = explicit confirmation -> lock -> remove exact DTMAPI/BepInEx roots and Doorstop files
check          = read-only best-effort classification
collect        = read-only, ten complete logs, publish only after every selected copy verifies
```

`check` reports static file presence only and says so explicitly. It never
claims that the current game process loaded Doorstop, BepInEx or DTMAPI; that
requires a fresh runtime log or game-start capture outside this lightweight
installer check.

The candidate does not promise to repair arbitrary manual edits. Re-running the
installer after its own interrupted copy must nevertheless converge because it
removes and rewrites the fixed DTMAPI Runtime directories and overwrites only
the fixed files contained in the bundled BepInEx archive.

## Player Assumptions And Stable Guidance

- Spaces, parentheses, Chinese text, ampersand, semicolon and square brackets
  are ordinary supported path characters.
- An uncommon source-package path failure may show one stable suggestion:
  `Copy the whole DTMAPI folder to a path containing only English letters and numbers, then try again.`
  No fixed destination such as `C:\DTMAPI` is recommended.
- The candidate does not tell players to move their Steam library. A valid game
  path is treated as usable by definition.
- Missing package payload produces one stable resubscribe suggestion. The
  installer does not attempt to diagnose or repair player-modified package
  bytes.
- Windows PowerShell 5.1 is the first candidate. PowerShell 7 is a fallback.
  All scripts use the PowerShell 5.1 language surface.

## Path And Deletion Safety

There is no generic "too shallow" rule. A top-level path such as `D:\Game` is
valid when it contains both `DolocTown.exe` and `DolocTown_Data`.

The explicit safety rules are:

1. reject a drive root even if marker files are present;
2. require the two Doloc Town markers;
3. construct every deletion target from the normalized game root plus one
   compiled relative allowlist entry; complete uninstall additionally accepts
   only the historical direct-child name grammar
   `.dtmapi-runtime-install-YYYYMMDD-HHMMSS-fff-xxxxxxxx`;
4. require the resulting full target path to equal that exact construction;
5. reject reparse points in any target ancestor below the game root;
6. never accept a receipt, manifest or environment value as a deletion target.

`-LiteralPath` is a PowerShell cmdlet parameter, not a project function. The
installer history contains no defect attributed to `-LiteralPath`; the recorded
path regressions were CMD compound-block expansion, ambient command extensions,
and a native trailing-backslash argument boundary. `-LiteralPath` remains the
correct wildcard-safe operation and is covered with literal square-bracket and
punctuation paths in the V2 matrix.

## BepInEx Ownership

Install has one player byte source: the bundled
`BepInEx_win_x64_5.4.23.5.zip`. Missing or unreadable bytes fail with the
resubscribe message. There is no network fallback.

Normal uninstall (`2_`) preserves BepInEx and its unknown plugins/configuration.
Complete uninstall (`9_`) is a separate destructive action. After an explicit
`REMOVE` confirmation it removes exactly:

- `<game>\BepInEx`;
- `<game>\DTMAPI`, including DTMAPI logs and residual state;
- `.doorstop_version`, `changelog.txt`, `doorstop_config.ini`, `winhttp.dll`
  and `doorstop_log.txt` at the game root.
- production-named `.dtmapi-runtime-install-*` directories left by historical
  Runtime transactions, after the same exact-child and reparse checks.

It warns that all BepInEx DLL mods and BepInEx configuration will be removed.
It does not delete Unity `Player.log`, the Steam library, Workshop subscriptions,
game saves, or any other game-root content.

## Host Boundary

The CMD dispatcher never accepts an exit code alone as proof of PowerShell.
For each candidate it creates a new nonce/result path, runs the tiny probe, and
accepts the candidate only when the result file contains the exact nonce and
action written by that invocation. A non-PowerShell executable returning zero
cannot pass.

The root BAT and dispatcher both retain an ASCII-only fallback message. If no
PowerShell host can run, the player still receives the English-path and
resubscribe guidance without depending on a PowerShell script.

## Log Export Boundary

- require Doloc Town to be closed;
- select `latest.log` plus `latest-*.log` by last-write time, newest ten total;
- copy every selected log in full with no byte cap or tail truncation;
- verify stable source length/time and equal source/destination SHA-256;
- build in a millisecond-plus-GUID staging directory;
- delete staging on any failure and publish by a final same-parent rename only
  after all selected files and the summary are complete.

Missing optional BepInEx/Unity logs are recorded in the summary. A selected file
that cannot be copied completely blocks publication of that support bundle.

## Promotion Gate

This document does not change the current canonical installer. Promotion needs:

- the [historical installer regression index](../workflows/runtime-installer-history-regression-index.md)
  and every mandatory V2 case green;
- Windows PowerShell 5.1 and PowerShell 7 parser/action evidence;
- exact special-path, false-host, concurrent mutation, log consistency and
  destructive-boundary stress evidence;
- independent review of the complete-uninstall allowlist;
- a separately authorized player candidate/package decision.
