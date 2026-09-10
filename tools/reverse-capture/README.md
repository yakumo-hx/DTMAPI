# Existing-baseline comparison

Use the [complete capture entry](../portable-reverse-capture/README.zh-CN.md) only when a new frozen baseline is requested. Query symbols in the matching existing metadata/decompiled tree. This comparison tool reads two existing captures; it never discovers, installs, starts, exports or decompiles a game.

```powershell
tools/scripts/compare-doloctown-reverse-baselines.ps1 `
  -BeforeBuildRoot references/doloc-town/reverse/builds/24966367_public_958EAF `
  -AfterBuildRoot references/doloc-town/reverse/builds/25163613_public_604898
```

The default `-Scope code` verifies the two relevant DLLs, reuses an identical assembly directly, and compares existing ILSpy inventories only when the DLL changed and both decompiler identities match. Changed text is verified against its inventory and written as raw diffs. Unchanged rows consume frozen inventory evidence, without rehashing every text file. Missing tool identity is reported as unproven, not silently treated as code drift.

Use `-Scope resources -AssetPath <exact-export-relative-path>` for a bounded resource question, or `-Scope resources` for all changed export paths. `-Scope all` explicitly requests both. Output defaults to the after-baseline's `diffs/compare-<before-name>`; `-OutputDirectory` may select another ignored reverse or external directory, never an input tree.

Resource output preserves original hashes and raw textual diffs, including GUID/fileID values. The semantic view maps known external GUIDs to their exact asset paths and unambiguous internal objects to GameObject hierarchy/component identities. External subobject IDs remain significant. Duplicate identities, missing reference targets, unsupported/binary input and incomplete parsing remain unresolved; they cannot yield an equality claim. Only known GenDatas tables may compare unique natural keys independently of row order; ordinary JSON arrays preserve order. A changed font fallback remains a reference change, even if all other serialized fields match.

`comparison-summary.json` owns execution scope, input inventories, tool/script identity, complete/failed state and result routes. Capture completeness, static compatibility and player behavior are separate facts. Original data and raw diffs remain local-only under the [reference boundary](../../references/README.md).

Validation entry points:

```powershell
tools/scripts/test-reverse-capture-stages.ps1
python -m unittest discover -s tools/reverse-capture -p test_compare_baselines.py
```

These use tiny authored fixtures. There is no reason to recapture or rehash every installed baseline to test this tooling.
