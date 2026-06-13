# Round 2: Review And Confidence

Status: complete
Date: 2026-06-13
Mode: parallel read-only challenge review

Round 2 challenged Round 1 and lowered several status claims.

## Core And Ecosystem Review

| API semantic | Semantic confidence | DTMAPI fit | Native-owner readiness | Final pressure |
| --- | ---: | ---: | ---: | --- |
| Mod entry / manifest / helper | 96 | 96 | 100 | Core-owned and valid. |
| `GameLaunched` | 94 | 90 | 100 | `StableCandidate`, not full Stable yet. |
| `UpdateTicked` | 93 | 85 | 95 Core / 25 native simulation | `Experimental`; DTMAPI pump only. |
| Save/title events | 90 | 78 | 55 | `Experimental`; no native save transaction promise. |
| Config read/write | 95 | 96 | 100 | Stable DTMAPI-owned files. |
| `IDataApi` | 88 | 55 | 80 Core / 45 save-bound | `Proposed`; split data scopes. |
| Console command author API | 86 | 40 | 100 | Proposed plus Diagnostic split. |
| Mod registry/API exchange | 95 | 92 | 100 | `StableCandidate`, registry-only truth. |
| Content-pack discovery / asset edit | 90 / 88 | 65 / 45 | 60 / 35 | Split read-only discovery from proposed patching. |

## UI And Input Review

| API semantic | Semantic confidence | DTMAPI fit | Owner readiness | Required status |
| --- | ---: | ---: | ---: | --- |
| Input/keybind | 92 | 78 | 55 | `Experimental` |
| Input suppress | 90 | 62 | 35 | `Experimental`; native isolation unproven |
| Config menu registration | 88 | 86 | 70 | `StableCandidate` for registration only |
| HUD draw | 86 | 60 | 35 | `Proposed` / `Experimental` |
| Menu events/overlay | 84 / 82 | 58 / 52 | 30 / 25 | `Proposed` / `Experimental` |
| Tooltip/info panel | 88 | 66 | 45 | `Experimental` |
| Data Layers overlay | 90 | 68 | 40 | `Diagnostic` / `Experimental` |
| Target inspection | 92 | 64 | 28 | `Proposed` / `Experimental` |
| Info query | 90 | 70 | 45 | read-only `Experimental` |
| Debug/cheat menu | 86 | 58 | 35 | `Diagnostic` |

## GameBridge Native-Heavy Review

| API cluster | Semantic confidence | Doloc fit | Native-owner readiness | Round 2 conclusion |
| --- | ---: | ---: | ---: | --- |
| `IWorldObjectEvents` | 90 | 72 | 35 | Valuable, but native dirty signals unknown. |
| `IWorldScanApi` | 88 | 80 | 45 | Read-only snapshots can be explored first. |
| Map marker/overlay | 86 | 82 | 50 | P1 read-only/overlay candidate. |
| Target inspection/info query | 92 | 78 | 40 | Split owners per target type and return DTOs only. |
| Container query | 92 | 80 | 60 | Existing evidence helps, but not a global stable container system. |
| Container UI open | 86 | 65 | 25 | Deep Experimental or Blocked. |
| Inventory transaction | 94 | 78 | 45 | Needs separate native-owner review. |
| Machine query | 82 | 72 | 45 | Read-only first. |
| Machine automation/network | 90 | 68 | 28 | Later P2/P3, after primitives mature. |
| Runtime entities/custom vehicles | 85 / 82 | 45 / 35 | 15 / 10 | Blocked. |

## Round 2 Global Conclusion

- P0 research does not equal P0 stable release.
- Read-only/query/overlay layers should come before mutation and automation.
- Content Patcher-like features must be clean-room and staged.
- Debug/cheat mutation must remain Diagnostic.
- Full runtime creation and custom vehicles remain Blocked.
