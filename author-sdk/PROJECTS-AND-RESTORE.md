# Standard projects, resources and restore

The first public SDK uses .NET SDK 8.0.421 and standard MSBuild/Csc for CLI and IDE builds. `dtmapi.author.json` schema 4 selects one Mod `projectFile`, API target, Strict/Advanced identity, native intent and publish metadata. Compiler properties, project/package references, resources, conditions and targets belong to the csproj and its standard imports. Ordinary libraries need only their own csproj.

## Development environment

Extract the complete Windows x64 SDK and dot-source `Enter-DtmApiEnvironment.ps1` in PowerShell. It sets the toolchain and SDK paths for that process; launch your IDE or CI from that shell. It changes no system environment variables. CLI build/restore/pack can locate the bundled toolchain without this shell; direct dotnet and IDE use the environment. An explicit invalid `DTMAPI_AUTHOR_DOTNET`, incompatible ancestor `global.json`, or missing toolchain is an error. ContentPack and read-only Doctor do not require a compiler.

Keep the template import order: Microsoft.NET.Sdk props, DTMAPI.Author.props, author properties/items, Microsoft.NET.Sdk targets, DTMAPI.Author.targets. The thin integration prepares frozen references and observes evaluated outputs; it does not replace Build, Restore or Clean. Game-loaded entry and runtime libraries remain netstandard2.0. Tool/generator projects may target net8.0; multi-target libraries use the standard project-reference selection.

The Mod template enables checked arithmetic, nullable annotations, deterministic portable PDB and standard framework/configuration constants. `Version` initially matches the new manifest; subsequent assembly version properties belong to the csproj. Manifest Version remains the Mod/package version. AssemblyName/TargetFileName must produce the manifest EntryDll. For reproducibility across absolute roots, provide a stable standard `PathMap`, including generated input locations as needed. Arbitrary external targets may have their own nondeterministic inputs.

PathMap matches the compiler's actual path spelling, including drive-letter case. In the tested VS Code workspace the IDE supplied `e:\...` while CLI supplied `E:\...`; mapping both observed prefixes to the same stable source prefix made the final Release DLL and PDB identical across CLI, IDE and CI. Inspect compiler arguments and portable PDB documents when comparing outputs. Keep properties such as GenerateDocumentationFile before the Microsoft.NET.Sdk targets import so standard defaults observe them.

The tested IDE was VS Code 1.107.1 with C# Dev Kit 3.20.199 and C# 2.140.9 on Windows. Design-time frozen references, F12, generated source, error navigation and standard Debug/Release builds were exercised. PDB support is separate from a debugger connection: the installed coreclr attach flow did not expose managed frames, locals or stepping in the tested Unity shipping Mono process. No breakpoint support is claimed for that setup. Unity describes Player managed debugging with a compatible adapter and a Development Build with Script Debugging in its [managed debugging guide](https://docs.unity.cn/2020.3/Documentation/Manual/ManagedCodeDebugging.html); preparing a different game debugging host is a separate task.

## Author root, project directory and outputs

The CLI directory argument is the **author root**, containing `dtmapi.author.json` and `manifest.json`. `projectFile` is relative to that directory and may name `src/MyMod.csproj`. Do not copy the metadata into `src`. CLI restore/build/pack invoke the selected project from its own directory so ancestor toolchain selection follows the same project as the IDE.

In direct MSBuild and IDE builds, `DtmApiAuthorProjectRoot` defaults to the nearest ancestor directory containing `dtmapi.author.json`, starting at the csproj directory. An explicit value takes precedence; a relative value is relative to the csproj directory. A missing or wrong explicit owner is an error, with no fallback. The selected metadata's `projectFile` must name the project being built. This lookup does not search sibling workspaces or the game. Static `validate` checks metadata; it does not execute imports or prove a successful MSBuild evaluation.

Ordinary `Compile`, `Content`, `HintPath`, `ProjectReference`, output and intermediate properties retain standard MSBuild semantics. References, assets, analyzer lists, compiler arguments and build-facts filenames passed between MSBuild and the SDK are absolute. Facts include the actual project directory and absolute output, intermediate and documentation paths. SDK build reports/staging live under the selected project's `obj/dtmapi-author`, not a second parent `obj` for subdirectory projects. Ordinary libraries need no author metadata or DTMAPI imports.

SDK-specific `DtmApiLicenseFiles` paths remain relative to the author root. Native-contract `nativeReferences.gameRoot` remains an explicit absolute installed-game path; SDK reference preparation keeps its author-owned native cache. `DtmApiCompatibilityRoot` in MSBuild follows the csproj directory when relative; CLI `--compatibility-root` follows the invoking shell. CLI `build --output` / `pack --build-output` paths are relative to the author root; default package output is the author's `dist`. `DtmApiPackagePath` is a package-relative destination, independent of all filesystem roots. These rules do not change ordinary MSBuild item evaluation.

## Libraries and package metadata

Use standard ProjectReference, Reference/HintPath and PackageReference. For example:

```xml
<ItemGroup>
  <ProjectReference Include="../Logic/Logic.csproj" DtmApiDistribution="self-authored" />
  <ProjectReference Include="../Contract/Contract.csproj" DtmApiRole="shared-contract" DtmApiDistribution="self-authored" />
  <PackageReference Include="Example.Library" Version="1.2.3"
                    DtmApiDistribution="licensed-third-party" DtmApiLicenseFiles="licenses/Example-LICENSE.txt" />
</ItemGroup>
```

The package name/version above is illustrative. Use dependencies you have actually selected and licensed. `DtmApiRole` defaults to `private-managed`; `shared-contract` is explicit. `DtmApiDistribution` is required for every packaged library. Third-party distribution requires nonempty `DtmApiLicenseFiles` (semicolon-separated paths relative to the Mod project). When transitive libraries have no direct declaration, attach the same metadata to their evaluated ReferenceCopyLocalPaths items with a standard target before DtmApiCollect. Conflicting metadata is rejected. PrivateAssets controls standard reference propagation; it does not by itself remove a selected runtime dependency from the Mod package.

NuGet may select different ref and lib assemblies. Compilation uses the selected references; packaging uses the final runtime implementations and checks their actual type/member surface, identities and dependency closure. Actual field reads, writes and address instructions must match static/instance storage in the final implementation; unused API differences do not require whole-DLL equality. Build tasks, generators and analyzers are compiler inputs, not runtime payloads. Runtime assemblies must be pure managed netstandard2.0 with exact filenames. Native/RID and satellite runtime assets are currently rejected. Standard build support for a package does not imply that its runtime assets are supported by Unity Mono.

## Generated code, resources and content

Standard Directory.Build.props/targets, Directory.Packages.props, conditions, Compile Include/Remove/Link, source generators, analyzers, AdditionalFiles, EditorConfig and resx/embedded resources run through MSBuild. Required SDK203 analysis rejects direct async-void platform callbacks, including generated code. Pack refuses disabled required analysis or skipped compilation.

Select package content explicitly:

```xml
<ItemGroup>
  <Content Include="assets/**/*" DtmApiPackagePath="Content/DTMAPI/assets/%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>
```

Content paths cannot traverse, collide by case, replace generated metadata or disguise managed/native host binaries. `GenerateDocumentationFile=true` packages the final XML beside its assembly. Debug packages include matching portable PDB; Release includes them only with `--symbols true`. AfterBuild post-processing and `DtmApiPreparePackageDependsOn` finish before output collection. The SDK does not recursively collect stale DLLs from bin.

## Standard restore and offline work

```powershell
dtmapi-author restore MyMod --json
dtmapi-author restore MyMod --offline true --json
dtmapi-author build MyMod --configuration Debug --json
dtmapi-author pack MyMod --offline true --json
```

Restore and ordinary build use NuGet's normal sources, credentials, project graph and lock format. Templates request `packages.lock.json`; run restore explicitly and review the lock before publication. Pack requires an existing lock and uses locked restore. Missing locks or changed declarations fail with guidance to restore; pack does not silently choose new dependency versions. Keep credentials in normal NuGet configuration/provider storage, outside source and package content.

Offline mode supplies only the SDK's local feed plus already available package cache; DTMAPI does not download in that mode. The complete SDK includes the pinned toolchain, base netstandard package and analyzer development dependencies, not arbitrary author dependencies or the maintainer's entire cache. Prepare selected extension packages/tools first for offline use. Author-defined targets can perform I/O, so offline mode is not a network sandbox.

NuGet archive hashes/signatures and extracted payload bytes are rechecked. Final runtime libraries also pass PE/method, dependency, license and reserved-host checks. Valid CLR delegates are supported; P/Invoke, unmanaged/internal-call methods and non-IL-only runtime DLLs are rejected. A restored private Newtonsoft.Json library may conflict with the game's resident copy; use the explicitly supported Advanced host reference when appropriate.

## Reports, failure and CI

Pack builds once, then captures final DLL/PDB/XML and selected runtime/content into a private snapshot. `--build-output` selects the standard OutDir for that invocation. Reports bind the actual output, MSBuild facts, toolchain/compiler and frozen references by hash; they do not claim a universal source/target identity for arbitrary MSBuild. Same-path publication uses an exclusive lock. A failed or cancelled pack retains the prior successful ZIP; standard MSBuild bin/obj are not a transactional store. Ctrl+C stops the SDK's MSBuild process tree.

`ci/verify-project.ps1` uses the same packaged SDK, validates, optionally restores, packs once and runs read-only Doctor on the resulting ZIP. An explicit author logic-test script may inspect the extracted artifact. Strict CI needs no game files. Advanced builds need the author's licensed game references. Actual Mono loading, source lines, owner cleanup, restart and resident assembly behavior require separate game validation.

Doctor inspects the existing package/metadata contract without executing build targets or author IL. It is not a complete IL verifier, and a clean report does not prove runtime behavior. Pack's final-output checks and actual game validation have distinct responsibilities.
