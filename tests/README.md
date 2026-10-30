# Source tests

Use the public test runner from the repository root:

```powershell
./tools/scripts/test-public-source.ps1 -Profile PublicWindows -Configuration Release
# Linux / WSL, without game inputs:
./tools/scripts/test-public-source.ps1 -Profile Generic -Configuration Release
```

`-List` prints the profile's projects and prerequisites. Use `-SuiteId moresaves` (or an array of suite IDs) for a smaller selection. CI uses `-BaseCommit <commit>` to select affected project graphs and declared runtime inputs. `-BuildOnly` builds the selection; `-NoBuild` requires its matching build receipt. Receipts bind evaluated inputs, selected scripts/data and selected outputs. Unrelated documents, other projects' outputs, commits and staging do not invalidate them. Reports retain the build commit and validation start/end Git provenance; relevant input/output changes during a run invalidate the result.

[suites.json](DTMAPI.UnitTests/suites.json) defines the public and internal classifications. Public profiles include Core, SDK, installation diagnostics, independent Mod behavior and optional content contracts. They do not require private game data, research records, Workshop publishing copy or internal acceptance scripts. Internal registrations can remain in the registry without their files being present in the public checkout; they are never selected by a public profile.

For a focused unit suite, use `tools/scripts/test-unit.ps1 -Focus <suite>`. Product suites include `moreequipment`, `moresaves`, `debugconsole`, `fishbreeding` and `oneaction`. Unit suite defaults use the projects present in the checkout. Shared temporary sessions use [DtmApiTestSession](Shared/DtmApiTestSession.cs).

Test fixture results establish the behavior they exercise. They do not prove game startup, native gameplay, physical input, saving, Workshop publication or a release package. Historical API targets before 0.6.0 are outside current compatibility maintenance; retained source does not establish a testing obligation.

For monster contracts, authoring and sequence executors, `tools/scripts/test-monster-content.ps1 -MonsterOnly` excludes the AudioEngine integration fixtures. Use it when the change does not affect that integration; the default command still includes those fixtures. The focused result does not establish AudioEngine compatibility or native game behavior.
