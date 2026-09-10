# DTMAPI Runtime Smoke History - 2026-07-12

This file preserves the completed 2026-07-12 owner/platform and YConsole acceptance slice moved from the active smoke matrix when that router reached its governance size limit.

| ID | Date | Scope | Result | Runtime evidence | Related issue/update | Remaining gap |
| --- | --- | --- | --- | --- | --- | --- |
| `OWNER-PLATFORM-DEPENDENCY-20260712-011551` | 2026-07-12 | platform-provider title/save owner cleanup | passed | `GAME-SMOKE/20260712-011551`: six one-time Entries, registry `29/6`, roots `8 -> 8 -> 0`, Camera/Core/GameBridge cleanup and exit passed. | `20260712-0001`; `ISSUE-013` | None for this boundary. |
| `OWNER-PLATFORM-NOOP-REFRESH-20260712-011924` | 2026-07-12 | unchanged Workshop/source refresh | passed | `GAME-SMOKE/20260712-011924`: `29/6`, `loadedNow=0`, six one-time Entries, zero dependency/restart/cleanup failures, clean exit. | `20260712-0001`; `ISSUE-013` | None. |
| `OWNER-PLATFORM-COMBINED-20260712-080130` | 2026-07-12 | combined title/save, owner cleanup and refresh | passed | `GAME-SMOKE/20260712-080130`: registry stayed `29/6`, six one-time Entries, eight roots cleaned to zero with Camera released; all requested gates passed. | `20260712-0001`; `ISSUE-013` | None. |
| `OWNER-VERSION-AUTHORITY-NOOP-20260712-101106` | 2026-07-12 | resident-version/byte-authority no-op refresh | passed | `GAME-SMOKE/20260712-101106`: `29/6`, `loadedNow=0`, zero dependency warnings/errors, six one-time Entries, health and exit passed. | `20260712-0003`; `ISSUE-013` | Version mutation is unit-tested, not runtime-mutated. |
| `Y-CONSOLE-SOLE-EDGE-OWNER-20260712-170328` | 2026-07-12 | owner-bound Y modal and edge behavior | passed | `GAME-SMOKE/20260712-170328`: open `8`, Escape close `1`, Y close `6`, ten single-toggle taps, held-edge no flicker; lifecycle/fatal/exit passed. | `20260712-0006`; `ISSUE-014` | Manual QA separately confirms physical taps. |
