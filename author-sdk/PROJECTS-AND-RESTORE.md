# Projects, resources and locked restore

SDK 0.7.0 is an unpublished candidate with default API/Runtime target 0.7.0. These inputs require author schema 3. Existing project/target bytes are not migrated automatically. The SDK accepts a bounded compiler grammar and does not execute arbitrary MSBuild tasks.

The native reference surface generator retains version 0.6.4 and its generation identity. `referenceGeneration.toolVersion` identifies that component, while the CLI reports SDK 0.7.0. Native V2 describes generic uses separately; it does not change the frozen API payload or relabel the V1 generator.

## Local projects

Create auxiliary projects with `dtmapi-author new library ../Logic --id Author.Logic --api-target 0.6.4`. A library has `dtmapi.library.json`, an ordinary SDK-delegating `Author.Logic.csproj`, and sources. It has no Mod manifest, Entry or standalone Mod ZIP; `build` produces its DLL and portable PDB. The default role is `private-managed`; explicitly select `--role shared-contract` for an intentionally shared CLR contract. Private libraries are not isolated AppDomains.

The Mod and all libraries use the same API target. Declare exact `.csproj` references in the Mod's `dtmapi.author.json` or the library's descriptor:

```json
"build": {
  "workspaceRoot": "..",
  "projectReferences": ["../Logic/Author.Logic.csproj"],
  "embeddedResources": [{"path":"assets/help.txt","logicalName":"Author.Mod.Help"}],
  "contentFiles": [{"path":"assets/data.txt","targetPath":"Content/Author.Mod/data.txt"}],
  "generatedSourceFiles": ["generated/Constants.cs"]
}
```

The root project defines the whole graph's workspace boundary. References can be SDK libraries or supported ordinary libraries inside that boundary. Reparse points, cycles, conflicting assembly names, inherited Directory.Build/Packages files and graphs over 32 projects are rejected before compilation. SDK library sources/resources stay beneath their own directory. Generated source files must already exist; the SDK never executes a generator. Missing or duplicate inputs are errors.

Embedded names and content targets are explicit and case-insensitively unique. Resources are limited to 16 MiB each. Content targets stay beneath `Content/` and outside the SDK-owned `Content/DTMAPI/` directory. Content is copied into build output and the Mod package; embedded resources are included in the owning DLL and accessed by their declared logical names. All source/resource bytes, transitive project inputs and references participate in the build identity. Libraries enter the existing dependency inventory with their role and exact CLR references; official/platform DLLs never become distributable project libraries.

CLI build, pack and the generated IDE Build target use the same backend. For an SDK project, an IDE ProjectReference, EmbeddedResource/LogicalName, Content/TargetPath/CopyToOutputDirectory=PreserveNewest, generated Compile item or exact PackageReference must match the JSON declaration. Undeclared items, unsupported conditions, analyzers, modified build targets and arbitrary imports are errors. Build each Mod entry project; shared auxiliary projects are compiled by its DAG. Frozen target props are unchanged.

## Constants and XML documentation

Declare these directly in the csproj; no duplicate JSON declaration is needed:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);AUTHOR_FEATURE</DefineConstants>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DefineConstants>$(DefineConstants);AUTHOR_RELEASE</DefineConstants>
</PropertyGroup>
```

Literal symbols, self-append, and Debug/Release equality or inequality conditions on these properties are supported. Symbols are checked, deduplicated and sorted. Undeclared SDK projects retain DEBUG/TRACE in Debug, TRACE in Release, C# 12, checked arithmetic and nullable enabled. XML is emitted with the same DLL/PDB, reported by path/hash and packaged beside its assembly. A failed emit or publication preserves previous companion outputs; a successful build with documentation disabled removes its stale XML. `migrate-build` preserves these author options.

## Ordinary auxiliary libraries

A referenced `Microsoft.NET.Sdk` project with one `netstandard2.0` Library target needs no Mod manifest, UniqueID or `dtmapi.library.json`. Its csproj is read without rewriting. AssemblyName and Version/AssemblyVersion/FileVersion belong to that library. Default C# semantics follow the supported .NET 8 SDK subset: C# 7.3, unchecked, nullable disabled, and SDK configuration/framework constants appended after author DefineConstants. Explicit 7.3/12.0, nullable enable/disable and overflow checking are supported. These defaults differ from the Mod template deliberately.

Default Compile glob excludes bin/obj and tooling directories. Literal Compile Include/Remove and Link can reference source inside the declared workspace; external-to-library sources require a relative Link. Transitive ProjectReference and raw EmbeddedResource/LogicalName are supported. resx generation, multi-targeting, custom targets/imports, generators and other unsupported semantics name the actual child project in the error. Build such a library with standard MSBuild and declare its output through `managedReferences` instead.

Ordinary libraries default to self-authored/private-managed. The parent can declare `build.projectReferenceMetadata` keyed by the existing ProjectReference path, with `role` (`private-managed` or `shared-contract`), `distribution` and `licenseFiles`; paths and values follow [the build schema](schemas/dtmapi-build.schema.json). Existing string references remain valid. This is package metadata and does not turn a library into a Mod.

Ordinary PackageReference versions and `packages.lock.json` remain owned by the child. Restore sources and licenses for the full package closure come from the parent's `build.restore`; its `packages` may be empty when it only supplies referenced libraries. The lock replay target remains the dependency-only netstandard2.0 format described below; an implicit NETStandard.Library entry from a normal standalone restore is not a distributable dependency. Keep the ordinary project's standard build and the bounded author replay inputs explicit.

## Explicit NuGet restore

Add `build.restore` to a Mod or library. Direct versions are exact. Every package, including transitive ones, has explicit local license files:

```json
"restore": {
  "lockFile": "packages.lock.json",
  "cacheDirectory": "obj/dtmapi-author/packages",
  "sources": ["https://api.nuget.org/v3/index.json"],
  "packages": {"Nett.Coma":"0.15.0"},
  "licenseFiles": {
    "Nett.Coma":["licenses/Nett-LICENSE.md"],
    "Nett":["licenses/Nett-LICENSE.md"]
  }
}
```

This sample's archived MIT library pair is a compatibility fixture, not a recommendation for new application configuration. The official packages declare [Nett.Coma → Nett](https://www.nuget.org/packages/Nett.Coma/0.15.0), with an explicit netstandard2.0 asset group.

Use a normal NuGet-generated `packages.lock.json`, containing one `.NETStandard,Version=v2.0` target and the complete package closure. To prepare or deliberately update it, use the installed .NET SDK in a separate dependency-only project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <RestoreSources>https://api.nuget.org/v3/index.json</RestoreSources>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="Nett.Coma" Version="[0.15.0]" /></ItemGroup>
</Project>
```

Run `dotnet restore --use-lock-file` on that project and review/copy the resulting lock into the author project. Implicit framework references are disabled only in this dependency-only lock recipe: the author compiler already uses its frozen BCL payload. Do not manually replace hash values or silently update dependency versions. NuGet owns dependency resolution and its [standard lock format](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#locking-dependencies); this SDK consumes that lock and does not introduce another resolver or lock format. Creating/updating a lock needs the .NET SDK; replaying it only needs the self-contained Author SDK.

```powershell
dtmapi-author restore MyMod --json
dtmapi-author restore MyMod --offline true --json
dtmapi-author build MyMod --json
dtmapi-author pack MyMod --json
```

Restore walks referenced libraries too. Only explicit online restore accesses the declared sources. HTTPS sources cannot embed credentials, query strings or fragments; explicit local feed directories also work. Authenticated source providers are not supported in this slice. No credentials or source configuration are put into the Mod package.

The SDK verifies NuGet identity, content hash, locked/direct versions, actual nuspec dependency groups and transitive closure. Cached nupkgs are rechecked, and changed extracted assets are rejected. Offline empty-cache and mismatched-lock errors tell the author to run or repair explicit restore. Build/pack can materialize a missing local asset from an already verified nupkg, but never download it. A lock is an integrity input, not evidence that a package is trustworthy.

Restore first selects applicable NuGet framework groups and reports selected, excluded and unsupported paths. The execution group must contain pure managed, exact netstandard2.0 `lib` DLLs. Unrelated incompatible framework assets are excluded; applicable build/buildTransitive tasks, analyzers/generators, RID/native selection, tools/content and satellite groups remain unsupported. Separate ref/lib surfaces are accepted only when the selected DLL sets and bytes are identical. Ref-only packages, differing reference/implementation bytes and lower-TFM execution libraries remain outside this model. These failures describe an unsupported asset model, not malicious content.

Normal runtime-implemented delegate methods, including generic and ref/out delegates, are accepted. P/Invoke, Unmanaged, InternalCall, Native code types, non-delegate Runtime methods and non-IL-only assemblies are rejected with package/version, selected path and member diagnostics. Entry/lib/BCL closure still passes the ordinary package policy; a NuGet dependency cannot replace a reserved or already-resident host assembly. Successful restore of Newtonsoft.Json does not establish that a private copy can coexist with the game's resident copy; use the exact host reference through Advanced when appropriate.

## CI and actual game tests

`ci/verify-project.ps1` validates, optionally restores, builds, packs and runs Doctor over the extracted artifact. Give it an extracted SDK already verified against your chosen ZIP hash. It accepts a separate explicit author logic-test script. A public runner needs no game files for Strict projects. It must not fetch private game binaries; an Advanced/native project needs its own licensed game runner and existing game-root settings.

Actual Mono loading, source selection, UI, owner cleanup and native compatibility are separate gates. No CI metadata or local CLR test substitutes for them. Keep old ZIPs before changing inputs and bump the Mod version when new bytes would replace an existing artifact. A failed graph does not create a new package or deploy over an existing installation.
