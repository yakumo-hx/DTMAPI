# Frozen 0.5.5 compilation inputs

`source-build.json` binds 16 DTMAPI-authored inputs (180,322 bytes) from commit `a13f1597a5f3920e519049b273e16bc41114738b`, including their Git blob identities and SHA-256. The `source/` tree is a compilation snapshot for the published API target; active interface development remains in `src/DTMAPI.Abstractions` at the repository root.

The original Release build context uses exact .NET SDK `8.0.421`, `netstandard2.0`, deterministic output, `PathMap=<snapshot root>=/_/DTMAPI`, and no PDB. The rebuilt DLL must equal the unchanged `compatibility.contract.json` hash `d04d34cd756c18189314e68891e933ceb5c060a79ba1eb54af21832e5f3f2bf8`, with assembly version `0.5.3.0` and file version `0.5.5.0`. A newer SDK patch is not silently treated as that original toolchain.

Use `tools/scripts/prepare-author-sdk-compatibility.ps1`; it stages inputs and generated files in a separate output directory, so this snapshot has no `bin/obj` build outputs. The checked-in snapshot contains no DLL, official game source or third-party source. Restore uses public NETStandard.Library `2.0.3`; its exact reference inventory, license and notice are verified by the existing contract. The prepared payload, third-party references and build cache remain untracked.
