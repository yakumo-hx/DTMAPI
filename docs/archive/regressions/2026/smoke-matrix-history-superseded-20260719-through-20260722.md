# Superseded And Blocked Smoke History: 2026-07-19 Through 2026-07-22

These rows moved intact from the active runtime smoke matrix on 2026-07-26
after later evidence superseded them or they had already ended blocked. Their
evidence remains audit material and is not current acceptance authority.

| ID | Date | Scope | Result | Runtime evidence | Related issue/update | Remaining gap |
| --- | --- | --- | --- | --- | --- | --- |
| `BATCH5-PUBLISHED11-PREFLIGHT-20260719-010116` | 2026-07-19 | Published11 retained-tree preflight | superseded | `GAME-SMOKE/20260719-010116` stopped before launch on five frozen-hash drifts; all identities remained and no subscription/baseline changed. | `20260718-0003` | Superseded by final enabled/disabled acceptance. |
| `BATCH5-LOCAL11-NO-QA-20260719-034102` | 2026-07-19 | earlier ordinary Local11/no-QA route | superseded | `GAME-SMOKE/20260719-034102` passed YConsole, EquipmentSlots and two AnimalViewer renders with exact restore/exit; `031553` is interruption-only. | `20260718-0003`; `ISSUE-010`; `ISSUE-011` | Superseded by final Candidate11. |
| `BATCH5-LOCAL11-HANDSHAKE-TIMEOUT-20260719-130406` | 2026-07-19 | no-QA handshake | blocked | `GAME-SMOKE/20260719-130406/result.json` stopped before SaveLoaded/product: third-save handshake absent; cleanup/restore/exit retained. | `20260718-0003` | Infrastructure timeout, not acceptance. |
| `BATCH6-AUTOFISHING-D5-SHORT-20260722-004352` | 2026-07-21 | preliminary fifth-save D.5 lifecycle | superseded | `004352` loaded slot 5 twice, observed fishing around title re-entry, retained 22 patches and restored state/exit. Its aggregate transient counter omitted deep native snapshots, so `074938` supersedes it. | `20260721-0004`; `ISSUE-010`; `ISSUE-011` | Not deep-state, L5/GC or Release proof. |
| `BATCH6-ONEACTION-ADVANCED-THIRD-SAVE-20260722-080842` | 2026-07-22 | bounded partial OneAction migration | superseded | `080842` proved two patches, resource/wrong-tool/fuel behavior, ActionSpeed coexistence, physical F11 ConfigMenu and clean restoration/exit, but omitted partial energy, config reload and real owner deactivation. | `20260721-0005`; manual QA `20260722-0001`; `ActionCompletion` Hook map | Superseded by corrected combined run `141220`; not Release/L0-L5/long proof. |
| `BATCH6-ACTIONSPEED-ADVANCED-TYPE-FAILURE-20260722-125239` | 2026-07-22 | ActionSpeed Advanced pre-resolution | superseded | `GAME-SMOKE/20260722-125239`: wrong `AgentStateBase` namespace failed closed at zero patches; rollback/restoration/exit passed. | `20260722-0001`; review `0004`; `ActionSpeed` map | Superseded by `141220`; retained negative evidence. |
