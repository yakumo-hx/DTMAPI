# Runtime-only no-Player-Doctor standalone hotfix package

- Update ID: `20260807-0001`
- Date: `2026-08-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: `release/workshop/installer/player-hotfix/runtime-only/no-exe/player-doctor/access-denied`
- Source Request: 先制作一个类似历史定制安装包的无 EXE 小包，用于绕开并隔离部分玩家在 `candidate\tools` 提交时的访问拒绝
- Root-cause Review: [20260807-0001](../../reviews/manual-qa/2026/20260807-0001-runtime-tools-move-access-denied.md)
- Debug issue: [ISSUE-022](../../debug/issues/ISSUE-022-20260807-runtime-tools-move-access-denied.md)

## Scope

This Update owns one standalone player hotfix artifact based on the current `0.6.0` Runtime candidate. The artifact:

- retains the four BAT entry points, shared CMD dispatcher, Windows PowerShell 5.1 compatibility, offline BepInEx payload, five Runtime DLLs, optional framework component, Runtime transaction, status, collection and Runtime-only uninstall;
- deliberately omits the self-contained Player Doctor executable and its two redistribution notice files;
- adjusts only the copied hotfix-package scripts so intentional Doctor omission is not classified as Runtime corruption;
- does not replace the normal Workshop package, change game-loaded assemblies, sync the local upload tree, update Steam, or touch the real Doloc Town directory.

## Implementation

- Base payload: `dist/dtmapi-060-candidate-566467f0-source9/DTMAPI`, Runtime `0.6.0`, binary version `0.6.0.0`, source receipt `566467f08193`.
- Output tree: `dist/player-hotfix-20260807-runtime-only-no-doctor/DTMAPI`.
- Final ZIP: `dist/DTMAPI-0.6.0-runtime-only-no-exe-hotfix-20260807.zip`.
- The copied package removes only the exact three-file `Content/DTMAPIInstaller/tools/player-doctor` tree, changes the copied install/status/collect/preflight scripts, changes the copied release package kind, and adds one Chinese usage readme. All Runtime DLLs, the dormant compatibility component, BepInEx ZIP, BAT/CMD entries and remaining helpers are byte-identical to the base candidate.
- The copied installer accepts only the normal Runtime package kind or the explicit `workshop-runtime-no-player-doctor-hotfix` kind. The latter stages no Doctor files or receipts, rejects an accidentally present Doctor payload, verifies none was committed, and writes installed release kind `workshop-runtime-install-no-player-doctor-hotfix`.
- The copied status and collector scripts report intentional Doctor omission as informational rather than a missing required Runtime file. The in-game Core already treats a missing Doctor helper as bounded, non-fatal `unavailable`; no game-loaded assembly was changed.

## Changed Files

- generated standalone package tree and ZIP under `dist/` as listed above;
- copied package files:
  - `Content/DTMAPI/release-manifest.json`;
  - `Content/DTMAPIInstaller/tools/install-to-game.ps1`;
  - `Content/DTMAPIInstaller/tools/check-dtmapi-status.ps1`;
  - `Content/DTMAPIInstaller/tools/collect-logs.ps1`;
  - `Content/DTMAPIInstaller/tools/probe-install-preflight.ps1`;
  - `README-无EXE临时包.txt`;
- Review `20260807-0001`, ISSUE-022, this Update, the issue ledger and the August Update ledger.

The canonical source scripts and normal Workshop package were deliberately not changed by this bounded hotfix.

## Validation

- Final ZIP size: `1,687,351` bytes.
- Final ZIP SHA-256: `31E1868D13168C5C2694899F7B1A1357E16E440184BE71F4B863B364D96B970B`.
- ZIP inspection: `38` archive entries, exactly four root BAT launchers, zero `.exe` entries and zero `player-doctor` entries. Extracted file count is `29`; no alternate data streams were found before compression.
- Windows PowerShell `5.1.26100.8875` parsed all ten packaged PowerShell scripts with zero errors. The packaged scripts contain no direct `Get-FileHash` or `Expand-Archive` command references.
- Final extracted-ZIP subscription audit passed with `Blockers: 0`: missing target `1`, invalid/empty target install `1`, empty status `1`, valid install `0`, post-install status `0`, collection `0`, uninstall `0`, and expected post-uninstall status `1`. Evidence: `tmp/test-runs/runtime-no-doctor-hotfix-final-zip-audit/DTMAPI Workshop Audit 20260807-002845/Results/stress-summary.md`.
- Exact path test copied the package under `Program Files (x86)/steam/steamapps/workshop/content/2285550/3743016467`, forced outer `cmd.exe /d /e:off /v:off` and Windows PowerShell 5.1, and targeted `Fake 游戏 (x86) & Ampersand/Doloc Town`. Public BAT install/status/uninstall and direct broad collection returned `0/0/0/0`; post-uninstall status returned expected `1`. Before uninstall, installed `DTMAPI/tools/player-doctor` was absent and installed `DTMAPI` contained zero EXEs. Evidence root: `tmp/test-runs/runtime-no-doctor-hotfix-exact-path-20260807-0031`.
- The copied non-mutating preflight ran against that exact path and returned `OK=11 WARN=0 FAIL=0 SKIPPED=0`, explicitly reporting that Runtime payload was complete and Doctor was intentionally omitted. Report: `tmp/test-runs/runtime-no-doctor-hotfix-exact-path-20260807-0031/Preflight Report/dtmapi-install-preflight-probe-20260807-002751.json`.
- No real game directory, local official upload tree, Steam subscription tree or player save was modified.

## Not Run

- No live Doloc Town launch: the five game-loaded DLLs are unchanged and this artifact changes only installer/support delivery.
- No affected-player run yet. Local validation proves package semantics but does not prove which external process denied the two players' directory move.
- No normal Workshop package rebuild, local-upload sync or Steam upload.

## Rollback

Delete the standalone hotfix output. No installed game, upload tree or Steam subscription rollback is required because project validation is confined to disposable temporary game/package roots.

## Follow-up

Send the ZIP to one affected player. Success supports the Doctor/EXE-lock class and unblocks installation while permanent optional-diagnostic and bounded move-retry design is reviewed. The same access denial with this zero-EXE package instead routes ISSUE-022 to ACL, Controlled Folder Access and handle-owner collection.
