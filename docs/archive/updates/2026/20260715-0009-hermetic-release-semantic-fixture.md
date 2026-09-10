# 20260715-0009 Hermetic Release Semantic Fixture

## Metadata

- Update ID: `20260715-0009`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: tests/release/reverse-boundary/oil/semantic-fixture/conformance
- Source: user requested that Release tests stop implicitly depending on ignored private reverse data after the Batch 2 progress review exposed a Git-only failure; related review: `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md`

## Scope

- replace the default Oil probability test's four ignored private reverse inputs with one tracked, DTMAPI-authored normalized semantic fixture;
- keep official table rows, field schema, native identifiers and decompiled source out of that fixture;
- add an explicit opt-in private conformance command that accepts a reverse build root, requires all four known inputs, and rejects drift between those inputs and the tracked projection;
- leave the opt-in command outside the default Release and Unit paths so a pure Git checkout has no hidden local-reference prerequisite.

## Known Facts And Rejected Paths

- The prior Unit test directly read `item_tbitemspawn.json`, `resource_tbresource.json`, `ISpawnLut.cs`, and `Tables.cs` from ignored `references/doloc-town/reverse`. A workspace with those private references passed, but a Git-only worktree failed before completing Release validation.
- `references/README.md` says local-only reverse folders must not be published or required to build DTMAPI. Adding a silent preflight would make the dependency clearer but would not make the default Release path hermetic.
- Copying official rows, schemas, native identifiers, or decompiled method bodies into a tracked fixture is outside the repository boundary. The tracked fixture therefore uses generic ordered-draw roles, weights, operation caps, draw range, normalized behavior flags, and derived expected probabilities only.
- Private conformance is a deliberate developer research check. It is not called from `build.ps1` or `test.ps1`, and absence of private material is not a default test failure.

## Changed Files

- `tests/DTMAPI.UnitTests/Fixtures/oil-coal-drop-semantics.v1.json`
  - adds the versioned normalized ordered-draw fixture with generic roles and no official schema, row, native identifier, or reverse-source name.
- `tests/DTMAPI.UnitTests/Program.cs`
  - reads the tracked fixture for the deterministic Oil probability regression;
  - rejects forbidden private tokens in the fixture;
  - computes the reviewed 3/4-draw probabilities and 5-8 monotonic ladder from normalized weights, capacities and fall-through semantics without reading local reverse files.
- `tools/scripts/test-private-reverse-conformance.ps1`
  - accepts `-ReverseBuildRoot` as an explicit opt-in input;
  - reports a missing root and lists the four expected files, or reports every missing file under an existing root;
  - compares the current private table/resource/source semantics to the tracked normalized projection and fails with a `Private reverse semantic drift` diagnostic.
- `docs/debug/evidence-retention-allowlist.json`
  - refreshes the deterministic tracked-reference allowlist required by the Release suite after the preceding review/update checkpoint added three referenced Markdown sources.
- `docs/updates/INDEX-2026-07.md`
  - routes this lifecycle record from the July ledger.

## Validation

- The exact staged Git tree `6b51c93481d582af61faa883885f970de313e262` was materialized as detached validation commit `74591ec2f026e53a58de190d440c84eb6ac6d026` in a clean sibling worktree with no ignored private reverse data. `tools/scripts/test.ps1 -Configuration Release` completed with exit `0` in 338.9 seconds.
- All runtime, product and test projects compiled in Release with zero warnings and zero errors; `DTMAPI.UnitTests: OK` and the full tracked Release script completed.
- The evidence-retention check passed with 378 tracked source files, 694 smoke runs and 62 runtime identities.
- The changed default Release/Unit sources contain no private reverse path and perform no file read for any of the four private inputs; their names appear only in the fixture's forbidden-token guard and the separate opt-in command.
- The opt-in conformance command passed against the current `23762374_public_C416D4` private root and confirmed all four inputs match the tracked normalized fixture.
- A fresh empty root was rejected with `Missing 4 of 4` and named `item_tbitemspawn.json`, `resource_tbresource.json`, `ISpawnLut.cs`, and `Tables.cs` explicitly.
- A temporary four-file root whose ordered-draw source was deliberately replaced by a nonmatching source was rejected with `Private reverse semantic drift`.
- PowerShell parsing passed under PowerShell 7 and the repository Windows PowerShell 5.1 compatibility validator.
- Focused `git diff --check` passed with only the existing line-ending normalization notice for `Program.cs`.
- No game directory write, runtime lock, game launch, or smoke-matrix update was required.

## Rollback

Revert this Update's fixture, Unit-test projection and optional conformance script together. Do not restore the prior implicit default dependency on ignored reverse data; if this implementation is reverted, the Release path must instead fail an explicit documented preflight until another hermetic projection is available.

## Follow-Up

- Run the opt-in conformance command when the private reverse build changes; review and deliberately update the normalized fixture only when new native evidence justifies semantic drift.
