# SMAPI Mod、Runtime 与程序集版本历史审计

- Review ID: `20260803-0002`
- Date: `2026-08-03`
- Status: `recorded`
- Scope: SMAPI 历史版本中的 Mod `Version`、`MinimumApiVersion`、SMAPI 产品版本、文件版本、程序集版本及旧 Mod 绑定策略；DTMAPI 0.6 打包前版本身份决策
- Change type: audit-only Review；不修改 Runtime、API、版本权威、包、Workshop、存档或游戏环境
- Related DTMAPI baseline: [Workshop 0.5.5 binary compatibility review](../../api/2026/20260712-0002-workshop-055-binary-compatibility-review.md)
- Related SMAPI loader review: [SMAPI 失效 Mod 判定与 Hook 边界审计](20260802-0003-smapi-invalid-mod-classification-and-hook-boundary-audit.md)

## Source Request

用户要求研究 SMAPI 历史版本如何规定 Mod 版本、最低 SMAPI 版本，以及
SMAPI 自身发行版本与程序集版本的变迁，并据此判断 DTMAPI 0.6 应把：

```text
ReleaseVersion                0.6.0
BinaryFileVersion             0.6.0.0
AssemblyCompatibilityVersion  ?
```

设置成什么。重点问题是：打包流程能否因产品版本变成 `0.6.0`，就自动把
`AssemblyVersion` 也推成 `0.6.0.0`；如果不改公共 ABI，是否应继续保留
`0.5.3.0`。

## Audited Baselines

DTMAPI:

- workspace: `E:\Python_project\DTMAPI`
- inspected HEAD: `abe9596cbdde2d0d3b2a66d34f8b867b5af0beac`
- current machine authority: `tools/release/dtmapi-runtime-version.props`
- current public contract authority: `docs/api/public-api-matrix.md`
- retained ABI contract: `tools/release/contracts/retained-abi-synthetic-contract.json`

SMAPI:

- repository: `E:\Python_project\SMAPIlearning\SMAPI`
- branch: `develop`
- inspected commit: `5689c8d6aeecf54f670559ffaaed6684a5febc25`
- description: `4.5.2-54-g5689c8d6`
- current source product version: `4.5.2`
- released reference DLL:
  `E:\Python_project\SMAPIlearning\StardewValley_SMAPI_reference\game-root\StardewModdingAPI.dll`
- released DLL SHA-256:
  `5E4C51BB3CD6616F5ADCB804769DBB7C6872B2478FAB786FB4CA00C72C7456BF`

The SMAPI repository and installed Mods were inspected read-only as architectural
reference. No SMAPI or third-party implementation was copied into DTMAPI.

## Verdict

The proposed DTMAPI distinction is correct, and the SMAPI history does **not**
justify deriving `AssemblyVersion` from the release label.

1. SMAPI does not maintain a long-lived assembly compatibility line like
   DTMAPI's current `0.5.3.0`. From SMAPI 1.x onward, its release/API version and
   numeric assembly/file version normally advance together. Prerelease labels
   are the main exception: a release such as `2.8.0-beta.7` uses numeric
   `AssemblyVersion=2.8.0` because CLR assembly versions cannot carry SemVer
   prerelease text.
2. Old SMAPI Mods keep working **despite** those changing assembly versions,
   not because assembly version changes are intrinsically safe. SMAPI combines
   an author-declared `MinimumApiVersion`, version-ranged compatibility data,
   simple-name assembly fallback, Mono.Cecil compatibility rewriting, heuristic
   invalid-member detection, and load/Entry error handling.
3. Current DTMAPI has a manifest minimum-version gate and retained-ABI tests,
   but no equivalent resident `AssemblyResolve`/`ResolveAssembly` path and no
   SMAPI-equivalent CIL compatibility rewriter. Its real Unity Mono evidence
   proves selected old weak-named consumer DLLs can bind to `0.5.3.0`; that is
   not proof that an arbitrary future assembly identity change is harmless.
4. Therefore the default 0.6 decision, if the public binary contract remains
   backward compatible, is:

```text
ReleaseVersion                0.6.0
BinaryFileVersion             0.6.0.0
AssemblyCompatibilityVersion  0.5.3.0
```

`AssemblyCompatibilityVersion` should move only after a dedicated ABI/binding
review finds an intentional compatibility-line break. It must never be inferred
from `ReleaseVersion` by the packager.

## Four Independent Version Axes

SMAPI's design is easiest to understand by separating four values which are
often incorrectly collapsed:

| Axis | Owner | Meaning | It does not prove |
| --- | --- | --- | --- |
| Mod manifest `Version` | each Mod | That Mod's own SemVer; used for update checks, dependency comparisons and version-ranged compatibility data | which SMAPI DLL it compiled against, or the oldest SMAPI it can run on |
| Mod `MinimumApiVersion` | each Mod author | oldest SMAPI release the author promises to support; checked before assembly load | that every binary reference is valid, or that the value matches the compile reference |
| SMAPI release/API version | SMAPI product | player-facing host capability/version, including prerelease label | CLR binding identity by itself |
| SMAPI assembly/file/product metadata | build output | CLR reference identity, file diagnostics, and product/informational provenance | Mod semantic compatibility by itself |

DTMAPI already models the last two as three separate projections:

- `DtmApiReleaseVersion=0.5.5`;
- `DtmApiBinaryFileVersion=0.5.5.0`;
- `DtmApiAssemblyCompatibilityVersion=0.5.3.0`.

`Directory.Build.props` maps those independently to `Version` /
`InformationalVersion`, `FileVersion`, and `AssemblyVersion`. This is a deliberate
compatibility contract, not stale metadata.

## SMAPI Historical Evolution

| First relevant version | Product/API and assembly metadata | Mod version contract change | Compatibility consequence |
| --- | --- | --- | --- |
| `0.39.1`–`0.40.0` | product code reports the 0.x alpha line, while `AssemblyVersion` and `AssemblyFileVersion` remain template-like `1.0.0.0` | manifests carry a Mod version but no minimum SMAPI field | early metadata is not evidence of a deliberate stable ABI line |
| `1.0` | SMAPI adopts the 1.0 semantic release line; assembly/file are `1.0.0.0` | project-wide semantic version governance begins | release and assembly identities become aligned |
| `1.1` | assembly/file advance to `1.1.0.0` | commit `785af919` adds optional `MinimumApiVersion`; a newer requirement is parsed and rejected with a friendly error before load | minimum runtime becomes separate from both Mod `Version` and assembly reference version |
| `1.3` | release/assembly line continues advancing | commits `f7b88790`, `44dfb6fa`, and `ad773a94` add Mono and rewritten/dependent-Mod assembly-resolution fallbacks | changing dependency/API assembly versions is tolerated through active resolution policy, not metadata equality |
| `1.8` | normal advancing assembly line | commit `6ee14ecf` rewrites Mod assembly loading around the compatibility pipeline | non-executing inspection and rewriting become a first-class load stage |
| `1.15` | `1.15.x` release and assembly/file versions move together | commit `6073d24c` changes `MinimumApiVersion` from a string to `ISemanticVersion`; minimum dependency versions are supported | host and dependency gates use one semantic comparison model |
| `2.0` | assembly/file are `2.0.0.0` | `Name`, `Version`, and `UniqueID` become required; duplicate IDs and the old 1.x compatibility mode are rejected; string SemVer is preferred while the old structured version remains temporarily accepted | malformed or ambiguous identity becomes a pre-load failure |
| `2.6` beta line | API labels include `2.6-beta...`; assembly/file stay numeric `2.6.0` through that prerelease line | existing semantic manifest rules continue | prerelease identity is represented in product/API metadata, not CLR numeric assembly metadata |
| `2.8.0-beta.7` | API is `2.8.0-beta.7`; assembly/file are `2.8.0` | non-string manifest versions are deprecated with a stated 3.0 removal | authors get a warning cycle before format removal |
| `3.0` | SDK-style common build `Version=3.0.0` drives numeric assembly/file metadata | deprecated non-string format is removed; formatted semantic versions always include the patch component; `3.0.1` adds build-metadata support | the manifest comparison contract becomes consistently SemVer-shaped |
| `3.12.0` | assembly versions still track the release line | commit `7e5d77fb` adds a startup assertion that SMAPI's own Toolkit/CoreInterfaces DLL versions exactly match the current SMAPI numeric API version | flexible Mod binding is kept separate from strict internal-package coherence |
| `4.0.6` / `4.0.7` | release and assembly/file continue together | optional `MinimumGameVersion` is added, then added to the schema | runtime and game minimums are explicit independent gates |
| current `4.5.2` | source `Version=4.5.2`; released DLL has Assembly/File `4.5.2.0` and Product `4.5.2+821167e...` | `Version` is required and nonzero; `MinimumApiVersion` remains optional | current SMAPI still does not freeze a long-lived assembly compatibility version |

The important historical pattern is not “SMAPI always changes every version
field”. It is “SMAPI can advance assembly metadata because it also owns a broad
compatibility pipeline and ecosystem policy around that choice”.

## How A New SMAPI Version Handles Old And New Mods

Current SMAPI uses several independent layers. No single version number makes a
Mod compatible.

| Layer | Decision | Result |
| --- | --- | --- |
| Manifest validation | Mod `Version` must exist and not be `0.0.0`; identity and dependency fields must be valid | malformed Mod is skipped before code load |
| Minimum host/game gate | `MinimumApiVersion > current SMAPI`, or `MinimumGameVersion > current game` | friendly incompatibility result; assembly is not loaded |
| Dependency version gate | installed dependency Mod's manifest `Version` is below declared `MinimumVersion` | dependent Mod is skipped; optional failures may be ignored |
| Known compatibility data | `UniqueID` plus lower/upper Mod-version range selects `Obsolete`, `AssumeBroken`, `AssumeCompatible`, replacement/update information, or dependency overrides | known affected releases can be rejected or specially handled without changing their DLL metadata |
| CLR fallback resolution | after exact binding fails, current SMAPI can resolve by assembly simple name from known files or already loaded assemblies, ignoring the requested version in that fallback | an old `StardewModdingAPI, Version=x` reference can bind to the resident current assembly |
| Cecil compatibility pass | known references are rewritten; missing non-System assemblies and selected missing/changed API/game members are detected heuristically | detectable incompatibility is skipped as no longer compatible, unless a bounded `AssumeCompatible` override applies |
| Real load and Entry | remaining assembly/type/constructor/Entry failures are caught and attributed | load failures are skipped; an Entry crash is logged but is not an atomic unload/rollback contract |
| SMAPI package coherence | internal Toolkit/CoreInterfaces assembly versions must exactly equal the current numeric SMAPI API version | a mixed or partially updated SMAPI installation fails early with an installation error |

Two resolver stages are visible in current source: the early program resolver
maps simple names to DLL paths before core startup, while the later Mod loader
fallback returns an already loaded assembly with the same simple name after
exact resolution failed. This is intentionally lenient for Mod/dependency
binding. The exact internal-component assertion prevents that lenience from
silently accepting a mixed SMAPI installation.

The Cecil pass is risk reduction, not a complete ABI verifier. Its invalid
member finder is heuristic, operates on statically visible references, and
cannot prove reflection strings, dynamic code, every generic instantiation, or
late feature paths. `MinimumApiVersion` remains the only deterministic friendly
gate for a Mod which deliberately requires a newer public API.

## Real Installed Mod Evidence

A read-only Mono.Cecil scan of the installed SMAPI 4.5.2 reference environment
found the following representative combinations:

| Mod | Manifest `Version` | `MinimumApiVersion` | compiled `StardewModdingAPI` AssemblyRef |
| --- | --- | --- | --- |
| Ladder Locator | `1.5.2` | `3.9` | `4.0.8.0` |
| Skull Cavern Elevator | `1.6.2` | omitted | `4.1.4.0` |
| Generic Mod Config Menu | `1.16.0` | `4.1` | `4.1.7.0` |
| Pony Weight Loss | `1.0.0` | `4.0.0` | `4.1.7.0` |
| Passable Crops | `1.2.3` | `4.0.0` | `4.3.2.0` |
| Automate | `2.6.1` | `4.1.10` | `4.4.0.0` |
| Fast Animations | `1.16.2` | `4.2.0` | `4.4.0.0` |
| Lookup Anything | `1.55.0` | `4.3.1` | `4.4.0.0` |
| Automatic Gates | `2.5.5` | `4.0` | `4.5.1.0` |
| Console Commands | `4.5.2` | `4.5.2` | `4.5.2.0` |

The full local scan also found Content Patcher, Chests Anywhere, Data Layers,
Tractor Mod, Yet Another Fishing Mod, NPC Map Locations, Save Backup, and Part
of the Community following the same pattern.

This proves three things about a real SMAPI ecosystem snapshot:

1. A Mod's own `Version` is unrelated to the SMAPI assembly version it compiled
   against.
2. `MinimumApiVersion` is often lower than that compile reference and may be
   omitted. It expresses the author's supported API floor, not build provenance.
3. SMAPI 4.5.2 loads an ecosystem containing references to at least
   `4.0.8.0`–`4.5.2.0`; exact assembly-version equality is not its Mod admission
   rule.

This does not prove every combination runs correctly. A Mod built against a
newer SMAPI but declaring an older or absent minimum can run on an older host if
it only uses old members. If it uses a newer member, the friendly result depends
on an accurate `MinimumApiVersion`; otherwise the heuristic preflight or a later
runtime failure is the remaining defense.

## DTMAPI 0.5.5 Comparison

| Capability | SMAPI current behavior | DTMAPI 0.5.5 behavior | 0.6 implication |
| --- | --- | --- | --- |
| Product/API version | one semantic release authority | `ReleaseVersion=0.5.5` | advance independently to `0.6.0` |
| File diagnostics | current released DLL is `4.5.2.0` | `BinaryFileVersion=0.5.5.0` | advance to `0.6.0.0` for installer/Doctor/file provenance |
| Assembly identity | normally advances with each release | deliberately retained at `0.5.3.0` | retain unless an ABI/binding review opens a new line |
| Minimum runtime | optional `MinimumApiVersion` | Author SDK requires `MinimumDTMApiVersion`; Runtime blocks newer minimums before load | 0.6 SDK and first-party packages must declare the true floor; do not rewrite old compatible Mods automatically |
| Build target vs minimum | build package references installed SMAPI; lower minimum is author-owned | SDK targets exact `0.5.5`; lower minimum produces `SDK106` compatibility-test warning, higher minimum produces `SDK105` error | target should become exact `0.6.0`; a lower floor needs explicit compatibility evidence |
| Semantic comparison | SemVer prerelease ordering and build metadata rules | numeric comparison strips `-`/`+` suffix before `System.Version` comparison | stable `0.6.0` is unaffected, but any public 0.6 prerelease policy needs a separate version-system decision |
| Old AssemblyRef binding | explicit simple-name fallback plus rewrites | no `AssemblyResolve`/`ResolveAssembly` handler found; selected old DLLs have bound under real Unity Mono | do not assume SMAPI-style rebinding exists |
| ABI reduction | broad Cecil rewrite and heuristic invalid-member pass | retained physical-assembly/API contracts, consumer/member scans, and `PortableAssemblyReferenceInspector`; no equivalent general rewriter | continue exact retained ABI testing; do not use assembly-version change as a substitute |
| Mixed host install | exact numeric version assertion for internal SMAPI components | package transaction, file versions, hashes/receipts, Doctor and release-contract checks | if assembly identity stays `0.5.3.0`, internal coherence must continue to use file/product/hash evidence, not AssemblyVersion |

The current DTMAPI retained contract explicitly expects
`DTMAPI.Abstractions, Version=0.5.3.0`. The public API matrix records real Unity
Mono exact-byte tests against retained consumers. That is stronger evidence than
source compatibility alone, but it only validates the tested identity and
consumer set.

## 0.6 AssemblyCompatibilityVersion Decision Matrix

| 0.6 public binary outcome | AssemblyCompatibilityVersion | Required policy |
| --- | --- | --- |
| no public/protected ABI change | keep `0.5.3.0` | preferred; release/file versions advance, binding identity remains on the proven line |
| backward-compatible additions only | normally keep `0.5.3.0` | old Mods keep binding; Mods using new 0.6 APIs must declare `MinimumDTMApiVersion=0.6.0` so 0.5.5 rejects them before load |
| compatible implementation/behavior fixes with unchanged signatures | keep `0.5.3.0` unless a documented behavioral contract is intentionally broken | AssemblyVersion is not a behavior-version channel |
| public type/member removal, incompatible signature/base/interface change, or intentional binding break | select a new line, normally `0.6.0.0`, only through a dedicated review | publish a breaking boundary, migration guidance, old/new consumer matrix, and explicit unsupported-old-Mod result |
| result is uncertain or only source-diffed | **do not package yet** | run exact physical ABI and Unity Mono binding review; uncertainty is not permission to bump or retain blindly |

Keeping `0.5.3.0` does not claim that 0.6 has no new capability. It claims that
the resident public assembly remains a valid binary target for consumers on the
existing compatibility line. Release capability is selected by
`MinimumDTMApiVersion`, not by the consumer's AssemblyRef alone.

The inverse case is important: a new 0.6 Mod compiled against an assembly which
still says `0.5.3.0` can physically bind on DTMAPI 0.5.5 and then call a missing
0.6 member. Therefore the 0.6 Author SDK/package contract must not treat the
retained assembly identity as the minimum runtime. The manifest floor remains a
separate mandatory pre-load guard.

## Required 0.6 Packaging Gate

Before changing the version authority for 0.6, one bounded ABI/binding decision
must record all of the following:

1. Compare the exact retained 0.5.5 `DTMAPI.Abstractions.dll` public/protected
   surface with the exact 0.6 candidate, including type kind, base type,
   interfaces, generic constraints, parameters, return types, property/event
   accessors, enum values and relevant attributes. A source-only diff is not
   sufficient.
2. Resolve every retained first-party and admitted external consumer's
   `TypeRef`/`MemberRef` against the candidate and keep zero unapproved failures.
3. Load representative exact old DLL bytes without recompilation under the real
   Unity Mono runtime and prove discovery, Entry, API lookup and the relevant
   behavior path. Preserve the current process-pinned boundary.
4. Build at least one 0.6-only consumer. On DTMAPI 0.5.5 it must be rejected by
   `MinimumDTMApiVersion=0.6.0` before assembly load; on 0.6 it must load and use
   the new member.
5. Exercise a mixed/partial 0.5.5–0.6 Runtime installation. Doctor, installer
   status and release-contract checks must identify it through file/product
   version and exact package evidence even if all public assemblies still carry
   `AssemblyVersion=0.5.3.0`.
6. Move `AuthorSdkContract.TargetRuntimeVersion` and generated compatibility
   assets to `0.6.0`. First-party 0.6 packages use that exact minimum unless a
   separately recorded lower-floor matrix proves otherwise; packaging must not
   silently rewrite already compatible third-party/legacy minimums upward.
7. Keep `DtmApiAssemblyCompatibilityVersion` an explicit reviewed input. Add a
   release-contract assertion that its change requires the ABI/binding decision;
   do not calculate it from `DtmApiReleaseVersion` or
   `DtmApiBinaryFileVersion`.

If these checks find an intentional binary break, the review may select
`0.6.0.0`. If they remain green on the retained line, changing the assembly
identity adds risk without adding compatibility value.

## Why DTMAPI Should Not Copy SMAPI's Resolver Blindly

Adding a global “same simple name means compatible” resolver solely to permit a
version bump would be the wrong order of decisions:

- it can hide a partial or mixed Runtime package;
- it can choose an unintended same-name DLL unless path, owner and package
  provenance are constrained;
- it cannot repair a genuinely missing or incompatible public member;
- DTMAPI's Unity Mono/BepInEx bootstrap and managed-Mod ownership boundaries are
  not SMAPI's standalone host layout;
- current DTMAPI evidence has not shown that a resident resolver is needed when
  the compatibility identity is retained.

If future real binding evidence requires a resolver, it needs its own bounded
design: DTMAPI-owned public assembly allowlist, exact resident path/hash
validation, duplicate/simple-name spoof rejection, requested-versus-resolved
identity logging, registration before any Mod load, and mixed-install tests.
That is a fallback mechanism, not permission to weaken ABI governance.

## Decision For The Current 0.6 Planning Boundary

This audit records the following recommendation but does not mutate the version
authority:

```text
DTMAPI 0.6 default candidate
  ReleaseVersion                = 0.6.0
  BinaryFileVersion             = 0.6.0.0
  AssemblyCompatibilityVersion  = 0.5.3.0

Exception
  Change AssemblyCompatibilityVersion only when a dedicated physical-ABI and
  Unity-Mono binding review proves an intentional new compatibility line.
```

The packaging script must fail closed if the assembly compatibility value is
missing. It must not infer `0.6.0.0` from the release version and must not infer
`0.5.3.0` merely from historical habit; the reviewed authority remains explicit.

## Validation And Limits

- Read the selected SMAPI tags and commits from `0.39.1` through current 4.5.2,
  including the introduction of `MinimumApiVersion`, Mono assembly-resolution
  fallbacks, semantic manifest versions, dependency minimums, prerelease
  assembly metadata, strict manifest requirements, internal package-coherence
  assertion and `MinimumGameVersion`.
- Inspected current SMAPI resolver, manifest validator, dependency resolver,
  compatibility metadata, assembly loader, invalid-member finder and startup
  component-version assertion.
- Read the actual released SMAPI 4.5.2 DLL metadata and scanned actual installed
  Mod manifests/AssemblyRefs with Mono.Cecil.
- Inspected DTMAPI's current three-version authority, build projection, retained
  ABI contract, public API matrix, Author SDK gates, runtime minimum-version
  comparison and assembly load path.
- Confirmed no DTMAPI `AssemblyResolve` or `ResolveAssembly` implementation in
  the inspected source.
- No build, unit test, game launch, runtime lock, save mutation, package change,
  external write or version-authority change was performed. This Review is a
  source/history decision input, not 0.6 runtime acceptance evidence.

## Resolution Owner

本审计只拥有版本历史、四条版本轴、binding 风险和“每个产品必须声明真实最低 Runtime”的决策输入。冻结 Author SDK 的 API 编译目标与产品可执行 Runtime floor 可以不同；若某个 exact policy 只存在于较新 Runtime，该产品必须独立提高 floor，并由旧 Runtime 的 pre-load 拒载证据证明，不能从公共成员兼容性反推旧 Runtime 能识别新 policy。

DTMAPI 0.6.0 实际采用的 marker/policy 合同、产品 floor、历史包兼容、changed files 与验证结果只由 [DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md) 维护；本 Review 不再承载实施或 PASS 记录。
