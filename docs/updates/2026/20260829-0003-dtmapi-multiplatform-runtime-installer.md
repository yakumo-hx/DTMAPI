# DTMAPI-多平台 Runtime installer and Workshop candidate

- Update ID: `20260829-0003`
- Date: `2026-08-29`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Area: `release/workshop/runtime/distribution/steamdeck/linux/windows/crossover/installer`
- Source Request: implement a separate Workshop package named `DTMAPI-多平台`, prioritize Steam Deck acceptance, retain Linux and CrossOver support, also keep native Windows installation capability, emphasize the required Proton launch option, and stage the completed package with description and branding in the local official upload directory
- Source Review: [20260829-0002](../../archive/reviews/code/2026/20260829-0002-cross-platform-runtime-installer-architecture.md)

## Scope

This Update owns the first local implementation and candidate packaging of a
second physical distribution of the existing `DTMAPI.Runtime` identity. The
new package is Steam Deck-first, supports ordinary Linux x64 through a native
host, supports Windows x64 through the same portable action contract, and
provides the Windows host as the CrossOver route on macOS. It keeps the four
player actions: install, uninstall, status, and log collection.

The candidate must import and verify the exact currently published `0.6.1`
shared Runtime/BepInEx payload rather than rebuilding current source under the
same version. The existing Windows Workshop item `3743016467`, its package
bytes, its PowerShell implementation, and its zero-EXE rule remain unchanged.
The new distribution may contain only its explicitly declared Linux and
Windows host artifacts, and no host executable may be installed into the game
tree.

The initial implementation prepared and staged a local official Workshop
upload directory without creating an item or uploading it. On 2026-08-30 the
user reported that they had created, uploaded and subscribed to the item. The
subsequent read-only observation binds item `3792681186`, Steam manifest
`5128092030483852458` and the first delivered content receipt. It records a
completed external user action; it does not retroactively claim that Codex
performed the upload, and it grants no authority for another upload.

## Player contract

- Steam Deck/Linux uses the shell entry points or their `bash` fallback and
  must set Steam launch options to
  `WINEDLLOVERRIDES="winhttp=n,b" %command%`.
- Native Windows uses the Windows entry points without that Linux launch-option
  instruction.
- CrossOver runs the packaged Windows x64 host inside the existing Steam/game
  Bottle and configures `winhttp` as `Native, then Builtin` for that Bottle.
- Status reports file health, launch-integration guidance, and observed Runtime
  startup separately; copied files alone never prove successful injection.
- The host tools are unsigned experimental utilities. Instructions may explain
  provenance and hashes, but must not tell players to disable system security.

## Changed Files

- `src/DTMAPI.MultiPlatformInstaller/*`: one trimmed, self-contained .NET 8
  host with install, Runtime-only uninstall, read-only status and bounded log
  collection actions for win-x64 and linux-x64. It owns exact package
  validation, game discovery, the shared per-game mutation boundary,
  BepInEx file-level repair, schema-1 Runtime transactions and conservative
  interrupted-state handling.
- `tools/scripts/build-dtmapi-multiplatform-package.ps1` and
  `tools/scripts/multiplatform-package-common.ps1`: build one atomic package
  leaf from the accepted published Runtime bytes plus the two host publishes.
  Repository candidates never invent `workshop.json`; rebuilding the bound
  official upload leaf preserves only the exact validated root control file
  for item `3792681186` and excludes it from the content receipt.
- `tools/scripts/test-dtmapi-multiplatform-package.ps1` and
  `tools/scripts/test-multiplatform-runtime-installer.ps1`: audit package,
  Catalog and host boundaries and run both real hosts through bounded fake-game
  lifecycles.
- `tools/release/dtmapi-multiplatform/*`: four LF/no-BOM shell action shims,
  package metadata, trilingual first-read instructions and Workshop publish
  text. `tools/release/dtmapi-mod-publish-zh.json` routes both physical
  distributions through the one existing `DTMAPI.Runtime` row.
- `assets/branding/dtmapi-multiplatform-icon.png` and
  `assets/branding/dtmapi-multiplatform-preview.png`: dedicated multi-platform
  icon and preview while the installed Runtime keeps its canonical internal
  DTMAPI branding bytes.
- `tools/release/dtmapi-product-catalog.json`,
  `tools/release/current-subscription-manifest.json` and
  `tools/scripts/check-product-catalog.ps1`: one Runtime identity now projects
  both published distributions, the observed multi-platform Workshop identity
  and delivered receipt, and the later local metadata successor separately.
  The generated managed-product registry changes only its Catalog status date;
  its 12 admission rows do not change.
- `PROJECT.md`,
  `docs/architecture/runtime-workshop-installer-boundary.md`, and
  `docs/workflows/workshop-package-subscription-test-matrix.md`: separate the
  unchanged published Windows zero-EXE boundary from the experimental sibling.
- `DTMAPI.sln`, `tools/scripts/build.ps1`, this Update and
  `docs/updates/INDEX-2026-08.md`: include the host in ordinary source builds
  and retain the lifecycle record.

## Validation

- `dotnet build` and self-contained single-file publish for `win-x64` and
  `linux-x64`: passed with zero warnings and zero errors. The resulting hosts
  are independent of a player-installed .NET runtime.
- Repository-local .NET 8 rerun of
  `DTMAPI.MultiPlatformInstaller.Tests` passed
  `AtomicReceiptValidationFailureRestoresPreviousBytes` after the Workshop-ID
  and uploader-control changes.
- `test-dtmapi-multiplatform-package.ps1`: passed for
  `dist/DTMAPI-MultiPlatform` and the staged official-local upload leaf under
  Windows PowerShell 5.1, including WSL Bash syntax. It proves exactly one PE,
  one ELF, exact metadata/branding projection and exact Catalog receipts. The
  repository candidate contains zero `workshop.json`; the official upload leaf
  contains exactly one validated root control file and has the same content
  receipt when that file is excluded. The existing accepted Windows package
  still contains zero EXE files.
- `test-multiplatform-runtime-installer.ps1`: passed the real win-x64 host and
  real linux-x64 host through WSL. Each used a separate fake game path with
  spaces, Chinese text, parentheses, `&` and `;`, then completed install,
  read-only status, two collision-safe log collections, Runtime-only uninstall
  and repeated no-op uninstall. External BepInEx plugin/config/log, DTMAPI
  config/history logs, Mods and ContentPacks sentinels remained intact; no host
  binary/manifest entered the game. Invalid explicit path `DTM-E1002` and
  corrupt payload `DTM-E1201` failed before mutation.
- The same focused matrix passed six install recovery-phase fixtures, three
  uninstall recovery/finalization fixtures, the accepted PowerShell
  classifier against a new canonical receipt, mixed own/orphan preflight,
  non-directory transaction prefixes, and eight Runtime provenance-conflict
  variants. Lower-version unknown distributions and legacy state without a
  release authority both fail as `DTM-E1202` with zero game-shaped-tree change.
- Link tests rejected linked uninstall/cleanup ownership and skipped linked log
  sources. A WSL Proton-shaped `compatdata/.../users` parent symlink pointed at
  a secret external `Player.log`; collection returned success with an explicit
  skip finding and copied none of the external marker bytes.
- All four final shell entries executed through WSL, retained LF/no-BOM bytes,
  and preserve a visible `noexec` failure. The four Windows BAT entries require
  `--pause` so double-click users can read/capture the result. All three
  localized Workshop descriptions retain Dolphin, install/check/log,
  CrossOver and unsigned-tool guidance.
- On 2026-08-30, the user-supplied Simplified Chinese Workshop description
  replaced the earlier copy after removing chat-only HTML/Markdown escaping;
  Traditional Chinese and English were translated section-for-section, and
  `README_FIRST.txt` carries the same three complete procedures. The metadata
  gate now requires the new product-overview lead while still requiring every
  locale to contain all four shell actions, the exact Proton launch option,
  the CrossOver host/override route, unsigned disclosure and Doloc Town
  Workshop source. This copy-only refresh did not change either host binary,
  shared Runtime bytes or action entry point, so the already-green lifecycle
  fault matrix was not repeated; both package-host audits were repeated.
- After the user's upload and subscription, the native Steam manifest at
  `appworkshop_2285550.acf` reported item `3792681186`, manifest
  `5128092030483852458`, 29,770,736 installed bytes and no pending update or
  download. The subscribed 36-file tree, the pre-edit repository candidate and
  the local upload content excluding its uploader-owned `workshop.json` were
  byte-identical at tree SHA-256 `e7b011d9...92a4`. No Steam-managed file was
  modified during this audit.
- All three localized descriptions and `README_FIRST.txt` now use the real
  subscription path `workshop/content/2285550/3792681186`. That metadata and
  the publication state added to the two package manifests form a 36-file
  local successor: 29,770,715 bytes, tree SHA-256
  `f7671fd07dae7664855be37bbd82f29ce23b003f56f4d0818717fa4f2aa2605a`.
  It is deliberately recorded separately from the still-current Steam
  manifest and was not uploaded by this task.
- The first lifecycle run exposed that official BepInEx uses `enabled = true`;
  `BepInExInstaller.IsComplete` now parses insignificant spaces and validates
  the complete 16-file BepInEx/Doorstop set.
- `check-product-catalog.ps1`: passed with two distribution projections but
  still one Runtime identity and the global `ActiveNoUploadAuthorization`
  release stop.
- `check-doc-governance.ps1`: passed 7,229 checks; `git diff --check` reported
  no whitespace errors.
- `tools/scripts/build.ps1 -Configuration Release` compiled every listed
  project, including the new installer and test project. Its broad test phase
  stopped in the unrelated existing `PreviewVersionMetadataIsConsistent`
  assertion: that test still hard-codes retired Runtime published-tree hashes
  `8280dc...` / `c73359...`, while both the current Catalog and accepted package
  authority use `846665...` / `b4ec6a...`. This Update did not rewrite that
  separate unit-test authority; the focused multi-platform unit and integration
  suites passed independently.
- No real Doloc Town launch, save access or game-directory mutation was made.
  Runtime Validation therefore remains `not-run`. Steam item creation, first
  upload and subscription-content parity are observed complete; Steam Deck,
  ordinary Linux and CrossOver runtime/player acceptance remain pending.

## Evidence

- Repository successor candidate:
  `E:/Python_project/DTMAPI/dist/DTMAPI-MultiPlatform`.
- Staged official-local upload leaf:
  `C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI_MultiPlatform`.
- Current local content trees: 36 files, 29,770,715 bytes, normalized tree
  SHA-256
  `f7671fd07dae7664855be37bbd82f29ce23b003f56f4d0818717fa4f2aa2605a`.
- Official-local uploader control file: 33 bytes, SHA-256
  `5e1ec4ac349bcd1031d5112b1a2608992d620d0098845d26e5c35a28558a9f26`,
  containing only Workshop ID `3792681186`; excluded from the content receipt.
- Observed subscription root:
  `D:/Steam/steamapps/workshop/content/2285550/3792681186`.
- First published/subscribed content: 36 files, 29,770,736 bytes, normalized
  tree SHA-256
  `e7b011d9e183e2da386f8e9c84c415f225e5ae41df8fd75c6b0481f9c56492a4`.
- Native Steam ACF stable two-read snapshot: 18,577 bytes, last write
  `2026-08-30T09:59:13.9443495Z`, SHA-256
  `5b81c0d110c143615325a7e3ff387dc1c13cb34e4dbe8263c4b0074b29879ba1`;
  53 installed items and 116,716,841 total installed bytes.
- Imported published player payload: 28 files, 3,866,857 bytes, tree SHA-256
  `b4ec6a441b4930b5174d4caed8799748fb4e4701ac0d8e72fc6bf6bd48eee4aa`.
- Shared byte-identical projection: 20 files, 3,826,337 bytes, tree SHA-256
  `ae80f6661390b82824af424b2006fb28267ec7181b2f98d75df9975c6a1d20c5`.
- win-x64 host: 11,818,211 bytes, SHA-256
  `da929a41ab46e2da3a405408fe5f4110b02246862189a8a4becd4cb35e5af09a`.
- linux-x64 host: 13,211,388 bytes, SHA-256
  `4210d9f2f22b2358b56a14aa5b0a99a5042f7cb6d956611bbe642f268a7e9dfb`.
- Branding receipts: icon
  `ce5281d9948920cb5bfbeddfaf77d7686927688d6a00f241ad50982a23068876`;
  preview
  `b6ae252f824d926dc5d914e8ce22f9ac8468af32e65717d2657695308f810cb2`.
- The shared Runtime lock was acquired before staging and released afterward.
  Only the new `DTMAPI_MultiPlatform` leaf was published; sibling `DTMAPI`,
  current subscription `3743016467`, the game tree and unrelated MODS entries
  were not changed.
- `host-artifacts.json` truthfully records the local candidate's installer
  source worktree as `dirty`; exact host and package hashes are bound above,
  but this is not a clean-commit release attestation and does not weaken the
  no-upload authorization boundary.

## Rollback notes

Remove only files introduced by this Update and the separately named local
upload directory. Do not alter or delete the current `DTMAPI` upload folder,
Steam subscription `3743016467`, game installation, player Mods, BepInEx tree,
or unrelated dirty worktree changes.

## Follow-up

Steam Deck player acceptance must prove the launch option plus a fresh
BepInEx/DTMAPI startup log before the distribution can be described as
supported rather than experimental. Publishing the local metadata successor
remains a separate future upload action; the present observation authorizes no
such update.
