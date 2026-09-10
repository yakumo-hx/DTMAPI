# DTMAPI cross-platform Runtime installer architecture research

- Date: `2026-08-29`
- Status: `recorded`
- Area: `release/workshop/installer/linux/steamos/proton/macos/crossover`
- Source request: research how SMAPI installs on other platforms, then define the smallest maintainable DTMAPI installer route that keeps install, uninstall, status and log collection usable for Linux, Steam Deck and macOS/CrossOver players; keep the accepted Windows Workshop item small and unchanged by publishing a separate non-Windows item; avoid player-side archive extraction and reduce host-binary size where evidence permits; an unsigned executable is acceptable if its risk is handled honestly
- Current authority: [Runtime Workshop Installer Boundary](../../../../architecture/runtime-workshop-installer-boundary.md), [Workshop Package And Subscription Test Matrix](../../../../workflows/workshop-package-subscription-test-matrix.md)
- Related reviews: [0.6.1 top-level installer review](20260807-0001-runtime-installer-061-top-level-design-review.md), [fixed installer vs isolated V2](20260808-0001-runtime-installer-fixed-vs-v2-comparison.md), [receiptless-residue root cause](20260820-0001-runtime-installer-receiptless-residue-root-cause.md)

This is a pre-implementation architecture review. It does not change the
current Windows package, declare Linux/macOS support, authorize a Workshop
upload, or replace the current installer authority. Any implementation must
start a separate Update and explicitly revise the current Windows-only project
statement and package boundary.

Implementation decision: the user's follow-up broadened the new distribution
from a non-Windows-only item to a Steam Deck-first multi-platform item which
also retains native Windows installation. The existing Windows item remains
unchanged. That scoped resolution and all implementation evidence are owned by
[Update 20260829-0003](../../../../updates/2026/20260829-0003-dtmapi-multiplatform-runtime-installer.md);
the option analysis below remains the recorded pre-implementation evidence.

## 1. Conclusion

DTMAPI should keep two **Workshop distributions** of one Runtime identity:

- the current Windows item `3743016467`, with its accepted BAT/PowerShell
  implementation and zero-EXE package boundary unchanged;
- one new experimental non-Windows item for Linux/Steam Deck and
  macOS/CrossOver.

The new item is not a second Runtime, a managed Mod or an independent release
authority. It is another physical delivery of the same `DTMAPI.Runtime`
payload. It must be independently subscribable so ordinary Windows players do
not download host tools they cannot use, but it must not depend on the Windows
item or read payload bytes across another mutable Workshop directory.

The current public Runtime page shows 30 Workshop items depending on item
`3743016467`, and current DTMAPI product pages display it under required items.
Valve documents ordinary item-to-item dependencies as **soft** dependencies,
not a platform-conditional alternative:
[ISteamUGC `AddDependency`](https://partner.steamgames.com/doc/api/isteamugc?language=english).
The new non-Windows distribution must therefore remain a manual additional
subscription and must not be added as a requirement to every Mod, which would
put it in front of Windows players too. Exact client behavior with an existing
soft dependency still belongs in the subscription matrix.

The smallest credible design is:

1. preserve the four public actions and their current safety semantics;
2. implement one host-tool codebase with a shared action core;
3. publish that codebase into the non-Windows item as a small `linux-x64`
   NativeAOT candidate and a small `win-x64` NativeAOT candidate;
4. let SteamOS/Linux call the Linux tool directly through four thin shell
   entries;
5. let CrossOver run the Windows tool inside the **existing bottle that owns
   Steam and Doloc Town**;
6. copy one canonical Runtime/BepInEx payload into the non-Windows item and
   prove its shared paths byte-for-byte against the Windows distribution;
7. keep the already published Windows bytes, BAT/PowerShell behavior, zero-EXE
   rule and existing assertions unchanged; sibling-distribution governance may
   grow around them only with an exact Windows-output regression;
8. use a trimmed/compressed managed single-file build only as the reliability
   baseline, and evaluate a pure-Go host only if NativeAOT size or CrossOver
   compatibility fails;
9. add native macOS x64/arm64 builds only if the CrossOver-hosted Windows tool
   proves inadequate.

The four actions are one contract, not four programs. A platform package may
show four friendly launchers or one menu with four choices, but all routes must
call the same engine.

The user-visible package must remain an expanded Workshop folder. It must not
be wrapped in another ZIP or embed the shared payload separately in each host
binary. Steam subscription is the delivery/extraction step; only the existing
fixed-hash BepInEx vendor ZIP may remain as an internal, automatically extracted
artifact while its size and AOT closure are measured.

## 2. Player evidence and its limit

The user reports more than ten Linux, Steam Deck and macOS/CrossOver players.
That is sufficient to reject the old planning assumption that non-Windows use
can be ignored, but it is not yet a support matrix.

The retained Fedora support bundle under
`D:\下载\DTMAPI-logsbob211418` proves that the current 0.6.1 payload,
install-state and log collector existed in a Wine-visible game tree. At the
time of collection it contained no fresh BepInEx or DTMAPI startup log, so the
bundle alone did not prove injection. The player later set
`WINEDLLOVERRIDES="winhttp=n,b" %command%`, reported that this fixed the
problem, and subsequently played normally. That later player report is useful
real-world acceptance evidence, but no retained fresh runtime log upgrades it
to a reproducible release test.

The user also reports a successful Ubuntu/Steam Deck player. The recent Steam
Deck screenshot shows the Workshop directory correctly, but opening
`1_install_dtmapi.bat` in Kate only displays Windows batch source; it does not
execute the installer. The CrossOver screenshots show the same presentation
failure in Finder. Those screenshots prove a missing host entry, not a Runtime
binary incompatibility.

The support status should therefore begin as **experimental compatibility-host
support** until the matrix in this review passes on exact subscription bytes
and produces fresh Runtime logs.

## 3. Runtime model: compatibility layer, not virtual Windows

[Valve describes Proton](https://github.com/ValveSoftware/Proton) as a Steam
Play compatibility tool based on Wine. The Proton FAQ places each game's Wine
prefix at `steamapps/compatdata/<appid>/pfx`, while the game directory remains
an ordinary host directory in the same Steam library. [Wine describes
itself](https://www.winehq.org/about/) as a compatibility layer that translates
Windows API calls rather than a virtual machine.

The resulting DTMAPI path is:

```text
native host installer (Linux), or Windows installer inside one CrossOver bottle
        |
        v
host-visible Doloc Town game directory
        |
        +-- winhttp.dll / doorstop_config.ini / BepInEx
        `-- BepInEx/plugins/DTMAPI + DTMAPI state
        |
        v
DolocTown.exe under Proton/CrossOver
        |
        v
Doorstop -> BepInEx -> DTMAPI Runtime
```

Only the game and a Windows-format helper need Wine/Proton/CrossOver. A native
Linux installer can copy, hash, rename and collect files directly in the host
filesystem. It does not need to enter the Proton prefix, install PowerShell,
install Mono, use `sudo`, or depend on Protontricks.

Protontricks can launch an external Windows program in a selected game prefix
with `protontricks-launch --appid <appid> <program>`, but its own documentation
shows that this adds an external dependency, and the Flatpak package needs
extra filesystem permissions for non-default libraries. It is a useful
fallback and diagnostic route, not the primary Steam Deck installer path:
[Protontricks usage](https://github.com/Matoking/protontricks/blob/master/README.md),
[Flatpak filesystem boundary](https://github.com/flathub/com.github.Matoking.protontricks).

## 4. What SMAPI actually does

The inspected local SMAPI reference is
`E:\Python_project\SMAPIlearning\SMAPI`, version `4.5.2` at commit
`5689c8d6aeecf54f670559ffaaed6684a5febc25`.

Current SMAPI uses three thin host entries:

- `src/SMAPI.Installer/assets/install on Windows.bat`;
- `src/SMAPI.Installer/assets/install on Linux.sh`;
- `src/SMAPI.Installer/assets/install on macOS.command`.

They start one C# installer implementation published self-contained for
`win-x64`, `linux-x64` and `osx-x64`. The build is owned by
`build/scripts/prepare-install-package.ps1`; the player does not install .NET.
The installer supports explicit `--game-path`, automatic discovery, manual
path entry, `--install`, `--uninstall` and non-interactive operation.

SMAPI's historical direction is also relevant:

- SMAPI 1.0 used one .NET Framework executable and required Unix players to
  run it through Mono;
- SMAPI 3.0 kept thin wrappers but still depended on Mono on Unix;
- the modern package switched to self-contained host executables because
  asking players to install a runtime generated support burden.

SMAPI does **not** contain a Proton, Wine, CrossOver, Steam Deck or
`WINEDLLOVERRIDES` path. Stardew Valley has native Linux/macOS builds, so SMAPI
replaces the native Unix game launcher on those platforms. Doloc Town does not
have that native launch boundary; DTMAPI must keep its Windows Doorstop payload
and configure the compatibility host instead.

The clean-room lessons are limited to:

- thin friendly entries;
- one shared installer domain model;
- self-contained, OS-specific publication;
- explicit plus bounded automatic path discovery;
- manual path fallback;
- no player-installed language/runtime prerequisite.

SMAPI source or launcher implementation must not be copied. Its simpler
delete-and-copy uninstall, lack of an independent status action, lack of a log
collector, and lack of DTMAPI-style transaction receipts are not reasons to
weaken DTMAPI.

## 5. Four action semantics that must survive the rewrite

| Action | Frozen result |
| --- | --- |
| `install` | Validate the package before mutation; install the exact Runtime version and bytes; safely install or repair fixed-hash BepInEx/Doorstop; stage a complete candidate; commit on one filesystem; write and read back the existing receipt authority; roll back only owned changes on failure. |
| `uninstall` | Remove only targets in the fixed Runtime-owned allowlist, supplemented by a valid state/receipt when available. Missing or corrupt install state must not prevent conservative partial-install cleanup. Preserve managed Mods, ContentPacks, External BepInEx plugins, unrelated BepInEx configuration, logs and player data. Never reinterpret “uninstall DTMAPI” as “remove BepInEx and every mod.” |
| `status` | Remain read-only. Reject missing, truncated, zero-byte or wrong-version Runtime files and invalid state/transaction classifications without repairing them. Do not call static files “loaded.” |
| `collect-logs` | Continue collecting useful evidence when optional sources are missing. Copy stable full files, verify length/hash, stage to a unique partial output and publish atomically. Keep crash dumps opt-in. |

The current checker proves the exact five-DLL set and file versions, but it
does not hash all five installed Runtime DLLs. Comparing those DLLs with the
authoritative package hashes is a recommended new cross-platform status gate,
not an existing frozen behavior.

Cross-cutting invariants also remain:

- exact five production Runtime DLLs plus the current dormant compatibility
  component and their version/hash authorities;
- fixed-hash package-local BepInEx first, fixed-hash online fallback second;
- `Clean`, `RecoverableReceipt`, `SterileNoReceipt`, `InvalidReceipt`,
  `UnsafeNoReceipt` and `OrphanState` transaction classifications;
- only a proven sterile receiptless shell may be removed;
- a per-game mutation lock;
- candidate/recovery paths constrained below the intended game or transaction
  root;
- no following of reparse points or Unix symlinks out of the owned tree;
- one existing receipt family and validator, evolved only where host-neutral
  paths require it; no Linux, CrossOver or action-specific receipt families.

The current normal package's zero-EXE rule is a package decision, not an
eternal product invariant. Adding a tool executable nevertheless requires an
explicit architecture-boundary and package-matrix change because the rule is
currently enforced and an earlier optional EXE correlated with endpoint
security friction.

## 6. Options considered

| Route | Benefit | Blocking cost | Disposition |
| --- | --- | --- | --- |
| Run the current BAT/PowerShell installer through Wine/Proton | Reuses current bytes | Requires a suitable PowerShell host inside the prefix/Bottle; file associations open BAT as text; Protontricks and Flatpak permissions add support burden | Keep only as an expert fallback |
| Reimplement all four actions independently in Bash and macOS shell | Small-looking entry files | Duplicates transaction, hash, receipt, path-safety and ownership logic; platform behavior will drift | Reject |
| Add both host tools to the existing Windows Workshop item | One item to publish | Every Windows subscriber downloads irrelevant non-Windows bytes and the current zero-EXE boundary must change | Reject after the user's two-item direction |
| New non-Windows item that requires or reads the existing Windows item | Avoids duplicating about 3.8 MiB | Steam dependencies are not an either/or platform selector; missing/stale second subscriptions, custom libraries and CrossOver path mapping become new failure modes | Reject; the new item is self-contained |
| One C# host-tool codebase, NativeAOT `win-x64` and `linux-x64`, thin entries | One action engine; true native single files; no player runtime or managed-bundle extraction; CrossOver can reuse the Windows build | Requires an AOT-safe design, separate OS builds and a full parity matrix | Recommended spike and intended MVP if measured gates pass |
| Trimmed, compressed .NET single-file `win-x64` and `linux-x64` | Closest managed fallback when AOT is blocked | Still carries a self-contained runtime; compression adds startup work and can extract native components | Keep as measured reliability baseline, not size target |
| Pure-Go `win-x64` and `linux-x64` | Small static hosts and convenient cross-compilation | Introduces another language/toolchain and rewrites the safety-critical action core | Plan B only if NativeAOT size or CrossOver behavior fails |
| Add native `osx-x64` and `osx-arm64` immediately | Native Finder experience; no CrossOver console path | Adds Bottle discovery, two architectures/universal packaging, Gatekeeper, signing/notarization and another host adapter before the CrossOver EXE route is disproved | Reserve, do not block MVP |
| Four separate self-contained executables per platform | Four obvious icons | Multiplies bundle size and update surface while adding no domain separation | Reject; use one menu or thin action launchers |
| Put the full Workshop item in an outer ZIP or self-extracting archive | Can reduce local staging bytes | Restores a manual/bootstrap extraction problem and defeats Workshop file-level update behavior | Reject |
| UPX-compress the host executables | May reduce executable bytes | Adds self-unpacking, endpoint-security uncertainty, harder byte provenance and no Valve/CodeWeavers compatibility guarantee | Reject for the first release |

## 7. Recommended source boundary

```text
current Windows distribution       new non-Windows distribution
BAT -> accepted PowerShell engine  Linux SH -----------+
                                   CrossOver Run EXE ---+-> C# host core
                                                          |-> filesystem adapter
                                                          `-> environment adapter
                 |                                      |
                 +---------- portable action contract --+
                 +---------- shared payload identity ----+
                                      |
                                      v
                       one Windows-format Runtime payload
```

This deliberately leaves two maintained installer implementations: the
already accepted Windows PowerShell engine and one new C# engine shared by the
Linux and CrossOver hosts. Trying to port the working Windows majority at the
same time would erase the isolation benefit the user requested. Drift is
controlled by a language-neutral action/fixture contract and exact installed-
tree parity, not by pretending both distributions execute the same source.

The source tree should generate or import one immutable shared candidate and
then overlay distribution-specific entries:

```text
canonical shared Runtime candidate
              |
       immutable path/hash set
          /               \
Windows overlay       non-Windows overlay
BAT/PowerShell        SH + ELF + CrossOver PE
          \               /
       shared-path parity gate
```

Catalog should continue to own one special `runtime` identity and grow a
bounded `distributions` projection beneath it. The existing item is the
`windows` distribution; the future item is a `nonwindows-installer`
distribution. `distributionId` is package metadata, not a Mod `UniqueID`, and
the new item must not enter `products[]` or the managed-product admission
registry. Its Workshop ID remains empty until Steam actually creates the item;
this review does not invent one.

Shared paths must include the Runtime payload, the original
`release-manifest.json`, fixed-hash BepInEx source and every installed support
tool retained for install-tree parity. The two packages compare the complete
shared path set by relative path, length and SHA-256. Host entries, root
`info.json`, README and the exact host-artifact manifest may differ.

Runtime payload identity and installer-host identity are separate axes. The
shared release manifest retains the Runtime `BuildCommit`; a host manifest
records `InstallerVersion`, `InstallerBuildCommit`, RID and binary hashes. A
host-only correction must not pretend that the Runtime DLLs were rebuilt.

That host manifest is not enough by itself. Each `runtime.distributions[]` row
must own an immutable distribution artifact revision, exact allowed host hashes,
full published tree hash, Workshop manifest ID, observed file/byte counts and
owning Release Update. `releaseStop` authorization must name the exact target
distribution and candidate digest; the current-subscription projection must be
able to record both Runtime distributions without promoting the new row to a
second Runtime identity. This prevents a host-only update from silently
replacing an ELF/PE while the shared Runtime version stays constant.

The first non-Windows candidate has an additional constraint: if the current
Windows Workshop item remains untouched, it must import and validate the exact
published `0.6.1` shared bytes and original release manifest. Rebuilding current
HEAD and labelling the result `0.6.1` would create same-version/different-hash
split brain. Later Runtime releases should construct the shared candidate once
and copy it into both distributions in one local build, but Steam still cannot
publish two Workshop items atomically.

That non-atomic publication window is made safe through installer semantics,
not a fictitious atomic receipt: the same payload is idempotent, a newer
payload may upgrade, an older package refuses downgrade by default, and the
same Runtime version with a different payload hash/commit fails closed. Both
distributions target the same Runtime ownership and one evolvable receipt
family, but **shared cross-engine locking is not an existing fact**. The current
PowerShell path uses a Windows named mutex and Windows absolute receipt paths;
a native Linux process cannot automatically participate in either semantic.
Uninstall never removes either Steam subscription. A package must never search
another Workshop item for missing bytes; missing local payload is an integrity
failure.

The first support contract therefore permits only the prescribed entry per
host: current BAT/PowerShell on native Windows, new ELF on Linux/SteamOS, and
new PE inside CrossOver. Two subscribed items are safe because neither executes
automatically, but concurrent or alternating old-BAT/new-host mutation on one
non-Windows game tree is unsupported and must be detected or documented as a
fail-closed boundary. Claiming true cross-engine interoperability would require
a backward-compatible receipt migration plus a filesystem-visible lock that
both engines adopt, which necessarily touches the Windows engine and is outside
the promise that its published artifact remains unchanged.

The installer is a host tool and may target the repository's .NET 8 host
toolchain. That does not change the `netstandard2.0` requirement for every
game-loaded DTMAPI assembly.

The core should own:

- the four action model, stable result codes and final summaries;
- package manifest, assembly identity, version, length and SHA-256 checks;
- safe ZIP extraction;
- owned relative-path inventory;
- candidate, commit, rollback and interrupted-recovery state machine;
- conservative uninstall;
- static health classification;
- stable log selection, copy and archive publication.

The platform adapter should own only:

- game, Workshop, Proton prefix or CrossOver Bottle discovery;
- path comparison, case behavior, symlink/reparse checks and atomic-operation
  capabilities;
- per-game locking and running-game detection;
- desktop/user-visible output directory;
- Unity, BepInEx, DTMAPI, Steam, Proton and CrossOver log locations;
- detection and guidance for the host-specific `winhttp` override;
- terminal, menu and wrapper presentation.

Absolute paths in the current installation and transaction receipts need a
focused schema-compatibility decision. A host-neutral successor should prefer
validated game-root-relative owned paths plus hash/version identity, while
retaining a host diagnostic path. This must be an evolution of the existing
generic receipt, with migration and old-receipt tests, not a second assurance
system.

## 8. Minimal package shape

```text
current DTMAPI Windows item 3743016467/       # unchanged
├── 1_install_dtmapi.bat
├── 2_uninstall_dtmapi.bat
├── 3_check_dtmapi_status.bat
├── 4_collect_dtmapi_logs.bat
└── Content/...                            # zero EXE remains mandatory

new DTMAPI non-Windows item <not created>/
├── 1_install_dtmapi.sh
├── 2_uninstall_dtmapi.sh
├── 3_check_dtmapi_status.sh
├── 4_collect_dtmapi_logs.sh
├── DTMAPI Runtime Tool.exe                # CrossOver menu + subcommands
├── README.txt
├── info.json
└── Content/
    ├── .tools/bepinex/BepInEx_win_x64_5.4.23.5.zip
    ├── DTMAPI/release-manifest.json       # byte-equal shared authority
    └── DTMAPIInstaller/
        ├── Payload/...                    # byte-equal shared Runtime bytes
        ├── tools/...                      # installed status/support bytes
        └── hosts/
            └── linux-x64/dtmapi-runtime-tool
```

The tree shows two sibling Workshop roots, not one combined package. The exact
final non-Windows layout stores the one CrossOver executable at the root so it
is easy to select through CrossOver **Run Command**; it must not duplicate that
PE below `Content`.

The host executable should remain in the Workshop package and not be copied
into `Doloc Town/DTMAPI/tools` in the first release. This reduces installed
surface and endpoint-security churn. Unsubscribed players retain the manual
uninstall fallback and can resubscribe to regain the tool.

The non-Windows package may contain exactly one allowlisted ELF and one
allowlisted PE host. The existing Windows package and every installed game tree
remain zero-EXE/ELF. This must be a separate checker boundary, not a switch that
disables the existing zero-EXE assertion. Player Doctor remains a separate
opt-in support tool and must not return through the new exception.

### 8.1 No player-side archive step

Valve documents a Workshop item as a content folder and explicitly advises
that files passed to `ISteamUGC::SetItemContent` should not be combined or
compressed into a single ZIP for upload/download efficiency:
[ISteamUGC](https://partner.steamgames.com/doc/api/isteamugc?language=english),
[Workshop implementation guide](https://partner.steamgames.com/doc/features/workshop/implementation?language=english).

The non-Windows item should therefore be uploaded as the expanded tree above.
Subscription materializes the files; the player never downloads and manually
extracts a second archive. Runtime DLLs must not be embedded separately in both
host executables.

The current fixed-hash BepInEx vendor ZIP is a bounded exception, not an outer
package. The tracked ZIP is `639,118` bytes; its 22 entries total `1,794,960`
bytes when expanded. Keeping it currently saves about `1.10 MiB` of Workshop
disk and preserves the accepted source/hash boundary, while the installer
extracts it automatically. The implementation spike should compare that saving
with the NativeAOT size added by ZIP support before deciding to expand it in the
new distribution. No player performs this extraction in either route.

Workshop documentation does not specify a dependable network compression ratio
or POSIX-mode preservation rule. Local ZIP size is therefore not a claimed
Workshop download size. Record both fresh-subscription network bytes and
`GetItemInstallInfo`/manifest disk bytes after a private candidate upload.

### 8.2 Measured host-size floor

A temporary representative .NET 8 console implementation was measured with
these capabilities: source-generated `System.Text.Json`, SHA-256,
`ZipArchive`, directory/file operations and the four action subcommands. It did
not contain HTTP, the full receipt/transaction engine or all diagnostic
sources, so these values are engineering floors, not release estimates.

The spike used SDK `8.0.421` / Runtime `8.0.27`. Its temporary source and
outputs were deleted after measurement and no repository file was changed, so
this Review is the retained result rather than a reproducible benchmark
artifact. Windows and Linux trimmed builds used self-contained, single-file,
`TrimMode=full` and `EnableCompressionInSingleFile=true`. Linux AOT used
`PublishAot=true`, stripped symbols and no debug file; the WSL build used a
temporary Linux SDK plus the already present GCC/objcopy/zlib toolchain, without
`sudo` or a system-package install. Treat the numbers accordingly.

| Publication | Windows x64 | Linux x64 | Result |
| --- | ---: | ---: | --- |
| Framework-dependent | `0.164 MiB` | `0.089 MiB` | Tiny only because the player must supply .NET 8; reject |
| Self-contained folder | `70.470 MiB` | `70.483 MiB` | Reject |
| Untrimmed true single file | `64.364 MiB` | `63.672 MiB` | Confirms the roughly `128 MiB` two-host failure mode; reject |
| Full-trim + compressed single file | `10.255 MiB` | `11.522 MiB` | Representative action-capability smoke passed on Chinese/space paths; retain as fallback |
| NativeAOT, size-optimized | Not measured: local Windows C++ linker prerequisite absent | `3.111 MiB` (`3,261,800` bytes) | Representative action-capability smoke passed in WSL without installed .NET and produced no self-extraction directory |

The measured Linux AOT ELF is self-contained with respect to .NET, but it is
not a universal static binary: `ldd` still reports the host loader, `libc`,
`libm` and `libz`. SteamOS/Ubuntu/Fedora acceptance must prove those native
dependencies and the chosen glibc floor; “no installed .NET” is not the same as
“no operating-system libraries.”

The compressed-single-file measurements produced no extraction directory for
this all-managed fixture, but Microsoft documents that bundled native libraries
can require extraction. The final tool must repeat the observation rather than
generalize from the spike:
[single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).

The current authoritative Steam-delivered player payload is `3,866,857` bytes,
or about `3.688 MiB`, excluding Steam-owned `workshop.json`. That is a
conservative base-package proxy; the eventual shared-path subset and new
distribution presentation bytes must be measured exactly.
Consequently:

- the fully measured managed fallback has an approximate floor of
  `3.688 + 10.255 + 11.522 = 25.465 MiB` on disk;
- a mixed Linux NativeAOT/Windows compressed-single-file candidate has an
  approximate floor of `17.054 MiB`;
- two NativeAOT hosts plausibly put the complete item in the low-teens MiB, but
  no number is accepted until the Windows host is built and both real tools are
  measured.

Microsoft's .NET 8 reference reports `1.84 MB` for a stripped Linux x64 and
`1.77 MB` for a Windows x64 NativeAOT Hello World. Those figures explain why
the route is credible; they do not substitute for DTMAPI's Windows measurement:
[.NET 8 NativeAOT size reference](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/runtime).

### 8.3 NativeAOT design constraints

The spike should optimize for size, keep symbols privately and publish without
the `.pdb`/`.dbg`, use source-generated JSON, and fail the build on unresolved
trim/AOT warnings. It must not dynamically load assemblies merely to inspect
their identity; use metadata readers instead. Microsoft documents that
NativeAOT forbids dynamic assembly loading and runtime code generation and
implies trimming and single-file constraints:
[NativeAOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

Do not disable stack traces or framework error text to save the last bytes;
supportability is one of the four required actions. `InvariantGlobalization`,
unused telemetry switches and other feature trims may be tested, but Chinese,
emoji and case-sensitive/case-insensitive path fixtures decide whether they
are safe. `System.Text.Json` models must use source generation rather than
reflection:
[JSON source generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation).

Windows and Linux NativeAOT artifacts require their own OS toolchains; .NET
does not support cross-OS NativeAOT compilation. Build the Linux candidate on
the oldest supported glibc baseline, then test SteamOS, Ubuntu and Fedora:
[NativeAOT cross-compilation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/cross-compile).

## 9. Platform-specific behavior

### 9.1 SteamOS and ordinary Linux

The Linux tool operates on the host-visible game directory. It should keep the
current bounded discovery order:

1. explicit `--game-path` or equivalent environment value;
2. an explicit local setting;
3. a complete package placed beside `DolocTown.exe`;
4. the current Workshop package's own Steam library and
   `appmanifest_2285550.acf`;
5. an interactive manual path.

It should not silently scan every Steam library and select a different game
copy. Steam Deck SD-card and custom-library cases belong in the test matrix;
when package-colocated evidence is insufficient, manual choice is safer than a
global guess.

The tool does not need `compatdata/2285550/pfx` for normal install, uninstall
or static status. It may use that prefix for optional Unity `LocalLow` logs and
diagnostics. The Proton FAQ documents the prefix location as
`steamapps/compatdata/<appid>/pfx`.

After install, the player still needs this native Steam launch option:

```text
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

The installer should display it prominently and make status report whether
launch integration is proven, unknown or missing. The first version should not
edit Steam `localconfig.vdf`; Steam owns that mutable file and may rewrite it.

Workshop delivery of POSIX executable bits is not yet proven. The accepted
flow must work after exact subscription download, not merely in a Git checkout.
The matrix must test Dolphin double-click, “Run in Konsole”, `bash <wrapper>`,
the internal ELF executable, `noexec` mounts and a Windows-origin upload. No
release may assume that `chmod +x` survived upload without this evidence.

The no-executable-bit fallback entry is `bash ./1_install_dtmapi.sh`, which does
not require the wrapper itself to be executable. The thin wrapper may validate
its package-relative target, apply `chmod u+x` to the exact allowlisted ELF and
execute one subcommand. A `noexec` mount can still block that ELF; the first
release treats it as an explicit unsupported/blocking state rather than
silently copying code elsewhere or executing unknown bytes. This is a
launch-permission fallback, not a manual decompression step.
The builder must emit wrappers as UTF-8 without BOM and LF-only, then verify the
same bytes in the Windows-origin upload folder and fresh Linux subscription.

### 9.2 macOS with CrossOver

CrossOver should initially reuse the self-contained Windows x64 tool. The
player selects the existing Bottle that already contains Windows Steam and
Doloc Town, then uses **Run Command** to browse to the executable. [CodeWeavers
documents this flow](https://support.codeweavers.com/en_US/crossover-mac-user-guide).
The player must not create a separate installer Bottle, because that Bottle
would have a different C: drive and registry and may not see the installed
Steam/game state.

Running inside the correct Bottle gives the Windows tool the simplest view of
the same `C:\...\steamapps` paths and Bottle registry. It also avoids a native
macOS helper having to discover every default or moved Bottle root; CodeWeavers
allows Bottle directories to be moved or multiplied.

The Linux Steam launch string must **not** be presented as the CrossOver
procedure. For CrossOver, select the game Bottle, open **Wine Configuration ->
Libraries**, add `winhttp`, and choose **Native, then Builtin**. CodeWeavers'
[DLL override guidance](https://support.codeweavers.com/troubleshooting-unlisted-applications-cxmac)
documents the Bottle-level Libraries setting.

The first CrossOver status implementation should read and report that Bottle
override. Automatic registry modification should remain opt-in and blocked
until a dedicated test proves exact old-value capture, rollback on failed
install and an explicit host-integration removal path. Ordinary Runtime-only
uninstall must preserve the override because retained BepInEx or External
plugins may still depend on Doorstop injection. Restoration is safe only for an
explicit complete host-integration removal after proving no retained consumer
needs it. The tool must never modify another Bottle.

If the Windows .NET self-contained tool cannot run reliably on both Intel and
Apple Silicon CrossOver, the fallback is a native macOS adapter published as
`osx-x64` and `osx-arm64`. It should begin with manual game-folder selection,
not an unbounded Bottle scan. Microsoft notes that .NET does not directly
produce a universal macOS binary: x64 and arm64 must be built separately and
merged, then signed again. Native macOS publication therefore remains a real
additional product surface, not a free checkbox.

### 9.3 Windows

The accepted Windows BAT/CMD/PowerShell item remains unchanged and keeps its
zero-EXE rule. Native Windows is not a support target for the new item even
though its CrossOver host is physically a Windows PE. The new Workshop title,
description and start screen must say that the PE is for the existing CrossOver
Bottle and is experimental.

Replacing the Windows BAT/PowerShell engine is outside this design. It would
need a later, independently justified Update and Windows acceptance rather than
following automatically from non-Windows success.

## 10. Status must become three-dimensional

Cross-platform support exposes a distinction the current `INSTALLED_HEALTHY`
wording does not fully represent:

1. **Files**: package, BepInEx, Runtime bytes, receipt and transaction state are
   healthy;
2. **Launch integration**: Proton launch option or CrossOver Bottle override is
   configured, missing or not inspectable;
3. **Runtime observation**: a fresh BepInEx/DTMAPI startup log proves that a
   post-install Runtime start was observed. It does not by itself bind that
   process to the current receipt/hash identity.

`status` must never promote (1) into (3). A useful final summary can say, for
example, “files healthy; winhttp override not verified; no fresh Runtime launch
observed.” This would have diagnosed the Fedora case before the player changed
the launch option. Claiming that the exact current bytes loaded requires a new
log identity such as build/package ID or a run nonce tied to the installed
receipt.

## 11. Unsigned executable boundary

An unsigned experimental tool is technically possible. It is not responsible
to reduce the policy to “the player solves security.” The zero-cost minimum is:

- exact Workshop item/source as the canonical download route;
- SHA-256 for each host binary and the full package tree;
- displayed tool version, payload version and source commit;
- automated reproducible build records and, where available, artifact
  attestation;
- an explicit “unsigned experimental host tool” notice before execution;
- no instruction to disable antivirus, SmartScreen, Gatekeeper, App Control or
  endpoint protection globally;
- a documented manual-copy fallback that does not execute the tool;
- the same conservative uninstall and log-collection support.

Microsoft's [current Windows signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options)
say that unsigned public executables can receive strong SmartScreen blocking
and that self-signed certificates are unsuitable for public distribution. The
same page points qualifying open-source projects to free SignPath Foundation
OV signing. Eligibility should be investigated; it is not a release
assumption.

A native macOS tool would add Gatekeeper and notarization pressure. Apple
documents the per-app **Open Anyway** path for a trusted but unidentified app,
but also warns that unsigned/unnotarized software carries risk. [.NET's macOS
publication guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/macos)
also warns that failure to sign/notarize can cause restricted operations to
fail. Avoiding a native macOS binary in the MVP is therefore both a complexity
and a trust decision, not a denial that native macOS is possible.

## 12. Migration route

### Phase A: freeze the portable contract

- Extract action-domain fixtures from the current authoritative tests.
- Define host-neutral path and receipt projections without creating another
  receipt family.
- Preserve stable result codes and one final summary per action.
- Define one Runtime identity with two distribution records; leave the future
  Workshop ID empty and keep the release stop active.
- Do not change the current Windows Workshop package.

### Phase B: prove the smallest host tools

- Create one AOT-safe .NET 8 host-tool source tree and shared core.
- Publish size-optimized `linux-x64` and `win-x64` NativeAOT spikes plus a
  full-trim/compressed-single-file baseline. A missing Windows native linker is
  a toolchain prerequisite to provision in the implementation Update, not
  evidence that Windows AOT is incompatible.
- First prove startup, source-generated JSON, SHA-256, ZIP, Unicode paths,
  locking and atomic replacement on Windows, SteamOS and CrossOver before
  porting every diagnostic source.
- Record raw host bytes, package disk bytes, startup, any extraction paths and
  endpoint/security prompts. Use the same fixture for a pure-Go comparison only
  if the AOT candidate fails its size or CrossOver gate.
- Keep Runtime payload bytes and game-loaded target frameworks unchanged.

### Phase C: reach four-action parity in temporary fixtures

- Port install/recovery/rollback first, then uninstall, read-only status and
  log collection.
- Run every current transaction, receipt, ownership and package fixture against
  the new core on both host builds.
- Keep the current Windows installer as the behavior oracle and accepted
  Windows implementation; use shared fixtures and installed-tree parity to
  prevent the two maintained engines from drifting.

### Phase D: opt-in real-player candidate

- Import the exact current published shared Runtime bytes when the Windows item
  is not being updated; overlay Linux/Steam Deck wrappers and the CrossOver
  executable in an isolated non-Windows candidate.
- Prove exact shared-path parity, exact allowlisted host binaries, Windows-item
  zero-EXE regression and zero host binary in the installed game tree.
- Validate exact uploaded and subscribed bytes.
- Obtain fresh startup logs and clean restart evidence from at least SteamOS,
  one ordinary Linux distribution and CrossOver.
- Keep support labeled experimental until the complete matrix is green.

### Phase E: promotion and cleanup

- Update `PROJECT.md`, the installer architecture boundary, package matrix,
  Catalog/checkers, subscription-manifest projection, player docs and support
  wording in one implementation Update.
- After private candidate acceptance, use a separate explicit release Update to
  authorize `NewWorkshopUpload`; record the actual created Workshop ID rather
  than guessing it in advance.
- Promote the non-Windows distribution without changing the accepted Windows
  route. Native macOS builds remain a later evidence-driven decision.

## 13. Minimum acceptance matrix

| Boundary | Required cases |
| --- | --- |
| Distribution identity | One `DTMAPI.Runtime` identity projects the Windows and non-Windows distributions; the new item is absent from managed products/admission; its real Workshop ID is recorded only after creation; each distribution independently binds artifact revision, host hashes, full published tree, Workshop manifest, owning Release Update and exact release-stop authorization. |
| Package | Exact source/import authority, generated candidate, official upload folder and fresh subscription tree have identical required bytes and declared SHA-256; the Windows item remains zero-EXE; the non-Windows item contains only the exact allowlisted ELF/PE; host binaries are not installed into the game tree. |
| Shared payload | Complete shared path sets, lengths and SHA-256 match between the two distribution candidates; `release-manifest.json` is byte-identical; first `0.6.1` candidate imports published bytes instead of rebuilding same-version/different-hash payload. |
| Size/delivery | Record each raw host, complete on-disk item, actual fresh-subscription download and any runtime extraction directory; reject an outer package ZIP, player runtime prerequisite and unmeasured download-size claims. |
| Actions | Never installed, partial install, healthy install, repair install, repeat install, uninstall, repeat uninstall, read-only status and two independent log collections. |
| Integrity | Wrong version, zero-byte/truncated DLL, wrong hash, wrong managed identity, missing compatibility component, damaged BepInEx ZIP and fixed-hash fallback. |
| Transactions | Every tracked commit/rollback fault; first-receipt write/publish failure; sterile, unsafe, invalid and orphan state; next-run convergence; disk full and read-only directory. |
| Ownership | Third-party BepInEx plugin/config sentinels, managed Mods, ContentPacks, logs and unknown files remain byte-identical after repair and uninstall. |
| Paths | Spaces, parentheses, `&`, semicolon, non-ASCII, symlink/reparse escape, very long path where supported, same-filesystem and cross-mount failure. |
| Concurrency | Within each supported host engine: two actions against one game, held per-game lock, game already running and stale lock recovery. Do not claim the native Linux lock coordinates with the unchanged Windows named mutex. |
| Variant skew | Same payload is idempotent; newer payload upgrades; older package refuses downgrade; same version with different payload hash/commit fails closed; simultaneous subscriptions never consume the other package implicitly. Old BAT/new-host alternating mutation on one non-Windows tree remains unsupported until the receipt/lock gate closes. |
| SteamOS | Internal drive and SD-card library; exact Workshop download; LF/no-BOM wrapper bytes; executable-bit/trust behavior; `bash <wrapper>` fallback; `chmod` and `noexec` classifications; Gaming Mode to Desktop Mode route; correct Proton launch option; fresh Runtime log and clean restart. |
| Linux | At least current Ubuntu and Fedora x64; default and custom Steam library; native Steam and relevant Flatpak boundary; AOT loader/libc/libm/libz resolution; no root/system package requirement. |
| CrossOver | Intel and Apple Silicon hosts where available; correct existing Bottle, wrong/new Bottle rejection, default and moved Bottle location, spaces/non-ASCII, `winhttp` missing/correct, fresh Runtime log and clean Bottle restart. |
| Status | Separately report file health, launch-integration health and fresh-runtime observation. Static health never claims successful injection. |
| Logs | DTMAPI/BepInEx game-local logs, optional Proton/CrossOver/Unity logs, newest-ten full-copy rule, active-write retry, atomic output, privacy review of host paths/usernames. |
| Security | Unsigned warning behavior, endpoint protection quarantine/lock, binary SHA/provenance display, no global-security-disable instruction, manual fallback. |

## 14. Open implementation decisions

The implementation Update must close these with evidence:

1. Does a standard .NET 8 `win-x64` self-contained console tool run cleanly in
   the currently encountered CrossOver versions on both Intel and Apple
   Silicon, and does the NativeAOT PE behave the same? The representative PE
   must be built after provisioning the required Windows native linker. If AOT
   fails, compare the already measured compressed-single-file fallback before
   adding a GUI stack or native macOS host.
2. Does Steam Workshop preserve the executable bit for a Linux binary and shell
   wrappers uploaded from the current Windows release machine? The exact
   subscription tree, not Git metadata, decides.
3. What `/proc`/Wine process evidence safely detects the matching running
   `DolocTown.exe` on Proton, and what is the fail-closed behavior when its path
   cannot be proven?
4. Can the existing receipt schema be read on both old Windows and new native
   Linux routes, or is a backward-compatible schema revision required to remove
   host-absolute owned paths?
5. Which optional Proton/CrossOver/Unity log locations are stable enough to
   collect without turning missing paths into action failure?
6. Should CrossOver install eventually offer a reversible, Bottle-local
   `winhttp` registry change? The MVP answer is “detect and guide”; automatic
   mutation requires its own capture/rollback tests.
7. How much larger than the measured `3.111 MiB` Linux AOT floor is the full
   transaction/log engine, and what is the corresponding measured Windows AOT
   size? Does retaining the `639,118`-byte fixed-hash BepInEx ZIP make the full
   package smaller than expanding its `1,794,960` bytes and trimming ZIP support
   from both hosts?
8. When a non-Windows player subscribes to a Mod that lists item `3743016467`
   as a soft dependency, does that Steam client also download or retain the
   Windows Runtime item? The public graph proves the dependency exists, not its
   exact client transfer behavior; actual subscription behavior and combined
   download size decide the player instructions.
9. Which backward-compatible receipt/lock representation makes Linux-native and
   CrossOver-hosted actions converge on one game tree? Until proven, Linux uses
   only the new shell/ELF route, CrossOver uses only the new PE route, and the
   old BAT route is not a supported non-Windows cross-entry.

These are bounded engineering questions. None requires four independent
installers, a full GUI framework, a player-installed runtime, an outer archive,
or a paid certificate before research can proceed. This review records a
preferred architecture and measurements; it still does not authorize a new
Workshop item or any upload.
