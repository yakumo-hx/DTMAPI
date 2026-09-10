# Unit test routing

Use `tools/scripts/test-unit.ps1 -List` from the repository root to see suite IDs, existing focuses and actual projects. `-Focus moresaves-product` builds/runs only MoreSaves and its dependencies. With no focus, all ordinary suites run; MoreEquipment and the previously omitted DebugConsole/acceptance contracts are included. Process actors and the one-off source-arbitration matrix require their explicit focus.

`suites.json` is the one execution map. `SuiteEntry.cs` reads it from the repository, resolves every selected method before execution, and starts one managed test session. Add tests to the owning project and register their default/focus calls here. A suite may link a shared fixture; it must not reference another test executable. `Fixtures/` retains separately built fake/native-shape executables in their existing paths.

The projects intentionally keep assembly identity `DTMAPI.UnitTests` to preserve the existing production friend boundary. Each has a separate output directory and runs in its own process. An actor restarts its own suite executable. Do not load these test assemblies into one process or add production friend entries merely for this split.

`Program.cs` and `DTMAPI.UnitTests.csproj` now contain only the old compatibility launcher, with no project references. It forwards to `test-unit.ps1 -NoBuild`; missing suite binaries fail with the canonical build/run command. On a clean tree, run the new script first. `-NoBuild` is for already built, unchanged relevant inputs and never builds or prepares dependencies.

Game-loaded source remains `netstandard2.0`; these test executables run on the repository-selected .NET 8 host. Physical Harmony fixtures use Windows/.NET Framework and the tracked BepInEx bootstrap ZIP, staged only when a selected build requires them. The public-source CI scope is documented at [scripts](../../tools/scripts/README.md).
