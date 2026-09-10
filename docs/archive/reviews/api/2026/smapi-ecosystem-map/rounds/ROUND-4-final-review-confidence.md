# Round 4: Final Review Confidence

Status: complete
Date: 2026-06-13
Mode: parallel read-only final review

Round 4 confirmed that the SMAPI ecosystem map should be landed as a docs-only research library and not as an API stability record.

## Required Final Downgrades

| Item | Final label | Reason |
| --- | --- | --- |
| `GameLaunched` | `StableCandidate` | Needs load order, failure isolation, and real mod regression. |
| `UpdateTicked` / `OneSecondUpdateTicked` | `Experimental` | DTMAPI pump event, not Doloc native simulation tick. |
| Save/title lifecycle | `Experimental` | Sidecar cleanup/data write only first. |
| `IInputHelper.Suppress` | `Experimental` for DTMAPI hotkeys; native isolation unproven | Do not claim original game input suppression. |
| Config migration / live reload | `Experimental` / `Proposed` | Do not fold into stable config read/write. |
| `IDataApi` | `Proposed` | Split data scopes first. |
| Console command author API | `Proposed` plus Diagnostic split | DebugConsole does not prove ordinary author command stability. |
| Content pipeline | read-only `Experimental`; patching `Proposed` | Asset edit/token/condition remain later-stage. |
| HUD/Menu/Tooltip/Overlay | `Proposed` / `Experimental` | UI lifecycle and manual visual QA required. |
| Data Layers / debug overlays | `Diagnostic` / `Experimental` | Developer visualization first. |
| Container UI open | `Deep Experimental` / `Blocked` | Crosses UI, input, and inventory domains. |
| Inventory transaction | high-risk `Experimental` | Needs transaction proof. |
| Machine automation/network | P2/P3 `Proposed` | Query, transaction, and world events must mature first. |
| Runtime entity/custom vehicle/building expansion | `Blocked` | No stable native lifecycle owner. |
| Multiplayer | `Future-reserved` | Doloc networking model unknown. |

## Final Confidence Summary

| Dimension | Confidence | Conclusion |
| --- | ---: | --- |
| SMAPI semantic extraction | 92 | The ten-mod ecosystem signal is strong and useful. |
| DTMAPI Core fit | 85 | Config, registry, translation, manifest/helper, and some lifecycle concepts fit best. |
| UI host fit | 62 | Valuable, but needs screenshot/manual QA and menu/focus proof. |
| GameBridge fit | 55 | Read-only/query can progress; mutation/automation needs native owners. |
| Stable readiness | 35 | Only a small Core-owned surface is near stability. |
| Runtime creation readiness | 12 | New entities, custom vehicles, and runtime building expansion remain blocked. |

## Must Not Overclaim

1. Do not say DTMAPI is compatible with SMAPI or Content Patcher.
2. Do not say `UpdateTicked` is the Doloc native world tick.
3. Do not say `Suppress` can stop original game input.
4. Do not say read-only content index means the game loaded or can edit that content.
5. Do not say config menu protocol owns official settings menus.
6. Do not say HUD/menu overlay can inject into arbitrary native menus.
7. Do not return raw native objects from target inspection or info query.
8. Do not treat container enumeration as proof of a stable global container system.
9. Do not treat MineMod or Automate semantics as proof of a native machine scheduler.
10. Do not treat the native flying motor adapter as a generic vehicle registry.

## Final Structure

The final file set is:

```text
docs/reviews/api/smapi-ecosystem-map/
  INDEX.md
  SOURCE-INDEX.md
  semantic-api-map.md
  dtmapi-gap-map.md
  priority-roadmap.md
  rounds/
    ROUND-1-semantic-api-extraction.md
    ROUND-2-review-confidence.md
    ROUND-3-revised-api-map.md
    ROUND-4-final-review-confidence.md
```
