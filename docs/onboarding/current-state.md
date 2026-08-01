# DTMAPI Current Truth Router

This file deliberately contains no copied version, branch, API status, issue summary, or latest-smoke prose. Those values change too often and previously made this page stale.

Use this page to locate the canonical owner of each current fact.

## Live Workspace Facts

| Question | Canonical source |
| --- | --- |
| Which branch, HEAD, and files are active? | `git status --short --branch`, `git log -1 --decorate --oneline`, and `git worktree list` |
| Which SDK, game path, runtime lock, logs, and process are active? | `tools/scripts/status.ps1` |
| What is the release/API, binary/file, and assembly compatibility version? | [Runtime version authority](../../tools/release/dtmapi-runtime-version.props); `Directory.Build.props` and `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs` project it |
| What changed recently? | [Update index](../updates/INDEX.md), then the matching year and month ledger |
| Which API contracts are current? | [Public API matrix](../api/public-api-matrix.md) |
| Which Hook boundaries are current? | [Hook Map](../hook-map/README.md) and focused domain maps |
| Which recurring runtime issues are open? | [Issue ledger](../debug/issues/README.md) |
| Which runtime checks are current? | [Active smoke matrix](../debug/regressions/smoke-matrix.md) |
| Where is pre-cutoff validation history? | [Historical smoke matrix](../debug/regressions/smoke-matrix-history-through-20260711.md) |
| Which native owner should an API start from? | [Native-owner domain library](../reviews/api/native-owner-domains/INDEX.md) |

## Stable Project Boundaries

- Read `AGENTS.md` and `PROJECT.md` before design or implementation.
- The canonical Strict/Advanced/ContentPack/External identities and Platform/SharedNative/ProductNative/ContentOwner ownership rules live in [`PROJECT.md`](../../PROJECT.md); this router does not duplicate them.
- `BepInEx/plugins/DTMAPI` contains one DTMAPI BepInEx plugin entry (Bootstrap) plus four co-located Runtime dependencies. DTMAPI-managed Mods do not belong there; third-party External BepInEx Plugins remain outside DTMAPI ownership.
- Native work starts from the native owner and is classified before implementation. Platform stays with its responsible platform component; GameBridge is for proven SharedNative adapters, not every single-product Hook.
- Build success alone does not prove runtime, Hook, lifecycle, or player-visible behavior.
- Historical Goal handoffs, long indexes, and old smoke successes are audit material, not current task authority.

## Task-Specific Reading

- Runtime or repeated bug: relevant issue, protocol, latest Update, and active smoke rows.
- API/GameBridge/Advanced CodeMod/Content Host: task-specific review, physical-owner classification, public API row when applicable, native-owner report, focused Hook map, and current reverse reference.
- Manual QA: preserve issue order, transcribe screenshots, and keep user facts separate from inference.
- Installer/Workshop: current release/workflow record and package test matrix.

## Maintenance Rule

Do not add volatile project summaries to this page. Add or change a route only when the canonical source itself moves. `tools/scripts/check-doc-governance.ps1` enforces this page's no-date/no-version boundary.
