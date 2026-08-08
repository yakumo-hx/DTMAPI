# 官方双来源启用状态仲裁复查

- Review ID: `20260805-0002`
- Date: `2026-08-05`
- Status: `implemented / source verified / player acceptance pending`
- Scope: managed Mod discovery, official Local/Workshop enablement, duplicate UniqueID arbitration, release-candidate source proof, post-0.6 lifecycle boundary
- Source: user feedback and the attached source-history analysis
- Owning Update: [20260802-0001 DTMAPI 0.6.0 authority roadmap](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Debug owner: [ISSUE-020](../../../debug/issues/ISSUE-020-20260805-enabled-official-source-arbitration.md)

This Review preserves the user's issue order and records the root cause before
another Mod-loading implementation attempt. Historical Updates remain audit
evidence; this record does not rewrite their former decisions.

## 1. Enabling a Mod still does not make DTMAPI recognize it

### Feedback

The player enabled the DTMAPI-managed Mod in Doloc Town's official Mod page,
but DTMAPI still did not load it. For managed player Mods there are only two
official runtime roots:

- `LocalLow\RedSawGames\DolocTown\MODS\<folder>`, identified by the game as
  `Local.<folder>`;
- Steam's current subscribed install directory, identified as
  `Workshop.<published-file-id>`.

The repository-created `<game>\Mods` development directory is not an official
player source. A Workshop subscription snapshot may prove that an enumerated
directory is the current subscribed install root, but it must not grant that
candidate selection priority. Enabled state comes first. If more than one
authorized candidate with the same `UniqueID` is enabled, official ModManager
priority/load order may disambiguate only when it has one unique winner;
otherwise DTMAPI must block the duplicate and tell the player to enable only
one copy.

The official-page lifecycle simplification is a separate, post-0.6 item:
after a successful native save, compare the newly committed functional-Mod plan
with the process startup plan and show one restart-required prompt when they
differ. Do not implement same-process DLL load, owner deactivation, dependency
reconciliation or config-page rebuild as part of that future design. The
ISSUE-017 successful-commit discrimination remains necessary.

### Analysis

- Commit `7984deec` and Update `20260628-0001` established enabled-first
  duplicate selection. The 2026-07-15 Author SDK/source-mode work replaced it
  with `CandidatePriority`: a native-verified Workshop candidate is weighted
  `0/1` before all enabled non-Workshop candidates at `10+`. Consequently a
  disabled Workshop copy (`1`) defeats an enabled official Local copy (`11`).
  The current Unit assertion explicitly preserves that incorrect result.
- `ModScanner.Discover()` also scans `<game>\Mods` and calls the official upload
  root `OfficialLocal`. `OfficialLocal` is a DTMAPI-internal distinction created
  only because of the extra development root; Doloc Town itself calls the
  upload identity `Local.*`.
- The native 1.00 `ModManager` enumerates `Application.persistentDataPath/MODS`
  plus `GetSubscribedMods()` install roots. It sorts enabled ModInfo rows by
  `priority` descending. `RefreshPriority()` assigns the front of the official
  order the greater number, and cache construction processes that winner last.
  Therefore the bounded duplicate rule is a unique highest numeric official
  priority, not a DTMAPI source label or version comparison.
- `OfficialModInfoState` already reads `priority`, and the native Workshop
  snapshot already carries `NativePriority`; the loader discarded both before
  duplicate selection. No new public API or authority file is required.
- Candidate11 already transactionally places candidate packages in the
  official `MODS` root. The later Local11 smoke setup writes
  `LocalDevelopment` selections back toward `<game>\Mods`; that indirection can
  make the acceptance load different bytes from the transaction candidate.
- Version, directory presence and subscription presence are identity,
  compatibility or authorization inputs. None is evidence that a candidate
  disabled in the official UI may load.

### Required correction

1. Ordinary managed-Mod discovery admits only the official Local upload root
   and native-verified current Workshop install roots. `<game>\Mods` cannot
   contribute a player candidate.
2. Filter authorized candidates by official `enabled=true` before selection.
   Zero enabled means no selected Mod; one means that candidate wins; multiple
   enabled candidates require one unique highest official priority. Equal or
   unavailable winner priority blocks the duplicate with a bounded prompt.
3. Remove Workshop/source weighted priority. Keep subscription evidence only
   as Workshop path authorization. Do not compare versions to select a source.
4. Current diagnostics use `Local` for the official upload source. Historical
   receipts and documents may continue to parse or describe `OfficialLocal` as
   legacy evidence.
5. The one-off correction matrix covers Local-enabled/Workshop-disabled, the
   inverse, both disabled, distinct enabled priorities, tied/unknown enabled
   priorities, stale unsubscribed numeric Workshop directories and a same-ID
   `<game>\Mods` tree. It is run for this correction and is not added to the
   recurring Release acceptance suite.
6. Final release acceptance must use the Candidate11 official-`MODS`
   transaction, enable matching `Local.*`, disable duplicate `Workshop.*`, cold
   start, and prove the exact selected root and package bytes before restoring
   the official state. The new cold lifecycle/hot-reload redesign itself is
   post-0.6 and is not part of this correction.
7. The Author SDK `install-local` transaction published to the historical
   `<game>\Mods` root. Runtime no longer loads that root, so 0.6.0 now disables
   public `deploy`, `update`, `install-local`, and `source local select` before
   any package read or state mutation. Existing journal status,
   `install-local-status`, `recover`, `withdraw`, and stale local-selection
   clear remain for old deployments. A future released SDK may design an
   official-`MODS` transaction, but this correction does not invent it.

### Rejected approaches

- Treating a current subscription, a higher package version or a DTMAPI source
  label as permission to override official disabled state.
- Retaining `<game>\Mods` in ordinary player discovery and attempting to hide it
  with more source weights.
- Deleting ISSUE-017's official successful-save transaction Hooks while the
  native page still reloads on open and before close persistence.
- Implementing the post-0.6 restart-plan comparison and hot-reconciliation
  removal inside this bounded 0.6 source correction.

### Acceptance boundary

The implementation may be source/unit/tooling verified without launching the
game. A later final candidate cold-start run owns exact local-candidate bytes,
process lifecycle and state restoration. No source-only result may be recorded
as that player acceptance.

## Implementation and validation — 2026-08-05

- Production discovery no longer scans `<game>\Mods`; the official upload root
  is reported as `Local`, and Workshop candidates require a current native
  subscribed-install root match.
- Duplicate selection now filters authorized candidates by official enabled
  state, then accepts one candidate or one unique greatest numeric native
  priority. Missing/tied priority blocks with bounded one-copy guidance.
- Legacy DTMAPI disable markers remain only for isolated historical Unit
  fixtures and cannot override Doloc Town's enabled state on official Local or
  Workshop roots. `LocalDevelopment` state is recovery-only and cannot change
  player selection.
- Candidate11/Local11 smoke setup no longer requests product
  `LocalDevelopment` selections; exact source logging expects official `Local`
  roots and priority diagnostics.
- The unreleased Author SDK now returns `SDK003` for every public mutation that
  would create, update, or select a `<game>\Mods` deployment. The Runtime
  installer removed its package-preflight/install-local/reconciliation branch
  and fails clearly if a managed product is requested through that retired
  path. Legacy transaction internals remain only to regression-test and execute
  read-only status, recovery, withdrawal, and stale-selection cleanup.
- `DTMAPI_UNIT_TEST_FOCUS=official-source-arbitration-once` passed the requested
  one-off eight-case matrix, including separate tied and omitted-priority
  negatives. The full Release Unit runner printed
  `DTMAPI.UnitTests: OK`; the project build had zero errors and only the ten
  pre-existing DebugConsole nullable warnings.
- PowerShell 7 and Windows PowerShell 5.1 parsed `run-game-smoke.ps1`;
  `test-game-smoke-save-modes.ps1`, `test-noqa-deadline.ps1`, and
  `test-candidate11-source-transaction.ps1` passed. The Batch 5 no-demand
  profile source/terminal validator also passed, preserving its bounded
  Core-only legacy isolation. No game process, Runtime install, Steam tree,
  official live `MODS`, or player save was touched.
