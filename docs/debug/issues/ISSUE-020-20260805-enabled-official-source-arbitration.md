# ISSUE-020: Enabled official source loses to disabled Workshop duplicate

- State: `open`
- Current boundary: Enabled-first official Local selection is source-verified. The old Y-console cold-start subcondition was superseded by later exact 1.1.1 publication/acceptance; this issue remains open only for the broader multi-product source-arbitration closeout.

## Status

- Opened: `2026-08-05`
- Severity: high
- Area: Core / Mod loading / official Local / Workshop / duplicate UniqueID
- Related review: `docs/reviews/manual-qa/2026/20260805-0002-enabled-official-source-arbitration.md`
- Owning update: `docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`

## Symptom

A DTMAPI-managed package enabled as `Local.<folder>` in Doloc Town's official
Mod state is not loaded when a duplicate `Workshop.<id>` directory is present
but officially disabled.

## Known facts

- Doloc Town's player ModManager has two physical sources: persistent `MODS`
  (`Local.*`) and current Steam subscription install roots (`Workshop.*`).
- `<game>\Mods` is a retired DTMAPI deployment root retained only for old
  recovery/withdraw and test diagnostics; it is not enumerated by the native
  ModManager or ordinary DTMAPI player discovery.
- At issue opening, the Core scanner scanned all three roots and assigned
  native-verified Workshop candidates numeric weights before every Local
  candidate.
- That weighting lets disabled Workshop defeat enabled official Local and is
  asserted by the current Unit suite.
- The official state already supplies enabled and priority for Local rows; the
  native Workshop snapshot supplies current install-path proof plus enabled and
  priority when available.
- Native 1.00 order is descending priority. A unique greatest priority is the
  only official-order duplicate winner DTMAPI can infer.

## Root cause

The 2026-07-15 source-authority implementation expanded a development/session
safety policy into ordinary player selection. It treated subscription proof as
Workshop selection authority, discarded official load-order values, and kept a
third non-native player source. This superseded the earlier enabled-first rule.

## Rejected hypotheses and approaches

- The symptom is not caused by version precedence; current duplicate selection
  does not use manifest version.
- A present or subscribed Workshop directory is not proof that the player
  enabled it.
- More source weights cannot model the official enabled-state boundary.
- The post-successful-close hot lifecycle is not this defect's root cause and
  remains a separate post-0.6 design item under ISSUE-017.

## Acceptance criteria

- Player discovery admits official Local plus only native-verified current
  Workshop install roots; `<game>\Mods` has no selection effect.
- Local enabled / Workshop disabled selects Local; the inverse selects
  Workshop; both disabled select nothing.
- When both are enabled, one unique greatest official priority selects that
  candidate. A tie or missing unique winner blocks the duplicate and emits a
  bounded instruction to enable only one copy.
- Stale numeric Workshop directories outside the current native subscription
  snapshot do not block or defeat an enabled Local candidate.
- Manifest version and internal source label never override disabled state.
- The focused one-off correction matrix passes and is not wired into the
  recurring Release suite.
- Final player acceptance later proves exact candidate root/bytes from official
  `MODS`, clean cold start/exit and restoration; until then the issue remains
  open or source-verified rather than player-verified.

## 2026-08-05 source correction evidence

- Core now admits player candidates only from official `MODS` (`Local`) and
  current native-verified Workshop install roots. Legacy `<game>\Mods` is
  excluded from production discovery.
- Official enabled state is applied before duplicate selection. A legacy
  DTMAPI disable marker cannot override an official Local/Workshop enabled row.
  Multiple enabled copies require one unique greatest official priority;
  missing or tied priority selects nothing and emits one-copy guidance.
- The requested one-off eight-case matrix (including separate tied and omitted
  priority negatives) passed, followed by the complete
  `DTMAPI.UnitTests` runner. Release build completed with zero errors and the
  ten pre-existing DebugConsole nullable warnings. Candidate11 transaction,
  save-mode, no-QA deadline, Batch 5 no-demand profile, PowerShell 7 and
  Windows PowerShell 5.1 parser checks also passed.
- The game was not launched, and no Runtime, Steam subscription, official live
  `MODS`, or player save state was changed. ISSUE-020 remains `open` until the
  final exact Local candidate cold-start/root/byte/restoration acceptance.
- The adjacent 0.6 deployment debt is now bounded: public Author SDK `deploy`,
  `update`, `install-local`, and `source local select` return `SDK003` before
  package reads or state mutation, and `install-to-game.ps1` no longer builds
  or invokes the retired install-local transaction. Existing journal status,
  `install-local-status`, `recover`, `withdraw`, and stale local-selection clear
  remain available solely for old-deployment recovery. This does not close the
  issue's pending exact official-Local player acceptance.

## 2026-08-05 User Cold-Start Source Evidence

- Official state for MoreSaves was Local enabled/priority `8` and Workshop
  disabled/priority `-1`. The Runtime log selected the exact official Local
  directory and ignored the subscribed duplicate, directly confirming the
  enabled-first decision in a normal player cold start.
- Both physical packages were the same stale MoreSaves `1.0.0` bytes. The run
  therefore confirms source arbitration but cannot serve as the issue's final
  exact current-package acceptance; `1.0.1` still needs to be installed and
  observed on the user's next cold start.
- Ordinary Core startup no longer creates or logs an absent `<game>/Mods` root.
  The explicitly named legacy path remains only for tests and old-deployment
  diagnostics. ISSUE-020 remains open until exact current Local root/bytes,
  startup and restoration evidence are complete.

## 2026-08-05 Exact Local Package Prepared

- Under the shared Runtime lock, the stale official Local MoreSaves `1.0.0`
  package was recoverably replaced with the SDK-generated seven-file `1.0.1`
  package SHA-256
  `3021E7340C30BC71727301E61FE02D0DC7E7070782740AD7EBF6A760C853F571`;
  its entry DLL SHA-256 is
  `90EBC85A2B8154F8A73D3C62DCFCCA06FC5338D90581E80EB5DEDDB9C20C0EC2`.
- Both official state files remained byte- and timestamp-exact, so Local remains
  enabled at priority `8` and Workshop remains disabled at priority `-1`.
  Workshop item `3742763050`, every other Local product, the full player SAVE
  tree and legacy `<game>/Mods` also remained exact.
- Runtime `0.6.0` from clean commit `0328978c021f` is installed and passes the
  read-only status/Doctor check with zero errors and warnings. Codex did not
  launch the game, so the next user cold start still owns final observation of
  the exact selected root/bytes and this issue remains open.

## 2026-08-06 Y Console Exact Local Package Prepared

- A user cold start on game build `24585411` proved source arbitration was
  still correct: Core selected enabled `Local.DTMAPI_YKeyConsole` at priority
  `7` and ignored disabled Workshop `3742714442`. The selected Local tree was
  nevertheless the stale `23762374` package. Its entry DLL SHA-256 was
  `102E616C...6180FF`, and `DebugConsoleHookInstaller.ResolveTargets()` failed
  with `Sequence contains no matching element`; owner rollback completed and
  the process required a restart.
- Candidate11 r6 had temporarily exercised the current package and then
  restored every original Local tree by design. The persistent hand-test setup
  had updated MoreSaves separately but had not published the Y console
  candidate, so this observation is a package-deployment omission rather than
  a recurrence of Workshop-over-Local selection.
- Under the shared Runtime lock and with no game process, the exact frozen
  `566467f0-local11/DTMAPI-YKeyConsole` tree was copied to same-volume staging,
  verified, and atomically published as `MODS/DTMAPI_YKeyConsole`. The result is
  `10` files / `479,345` bytes with tree SHA-256
  `B81988BB...59B02`, entry DLL SHA-256 `2869BFD9...A252F`, minimum Runtime
  `0.6.0`, and policy `doloctown-24456188-debugconsole-v1`; the Local tree no
  longer contains Workshop-only `workshop.json`.
- The prior `11`-file tree remains intact at
  `DTMAPI/backups/ykeyconsole-local-1.0.0-before-b0aa552e-20260806-140145925`
  with tree SHA-256 `A99AC5F2...D9BD3`. Candidate source, the complete SAVE
  projection, every other official Local product and Workshop item
  `3742714442` were exact before/after. Official state remains Local enabled at
  priority `7` and Workshop disabled. Runtime status and Player Doctor passed
  with `5 / 0 / 0`; staging is empty, no game process remained and the lock was
  released.
- Codex did not launch the game. Build `24585411` is newer than the candidate's
  tracked `24456188` reference, so ISSUE-020 remains open until the next user
  cold start proves the exact Local DLL completes `Entry`/Hook installation and
  the in-save Y console opens and closes without the old `ResolveTargets`
  exception.

## 2026-08-30 Y Console Subcondition Superseded

- The pending 2026-08-06 Y-console Local observation is no longer a current
  issue gate. Subsequent focused/player acceptance corrected the text-focus
  route, and Update `20260823-0003` closed exact DebugConsole `1.1.1`
  publication against Steam manifest `6693520158465470410` with matching
  subscription bytes.
- On 2026-08-30 the user also confirmed that the exact old `0.3.1-dtmapi`
  product has no remaining users and released its Compatibility retention
  prerequisite. This does not delete the compatibility code in this issue.
- ISSUE-020 stays `open` only because its broader enabled-first Local/Workshop
  source-arbitration lifecycle covers more than Y-console. It must no longer be
  summarized as waiting for the stale `24585411` Y-console cold start.
