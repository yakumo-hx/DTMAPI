# DTMAPI More Equipment Slots

Experimental 0.2.4 runtime/UI mod for extra player equipment slots.

- The mod registers `IEquipmentSlotsApi` with three default extra attribute-only slots.
- Vanilla/default equipment slots remain responsible for visible decoration effects.
- Extra slots explicitly request no appearance effects to avoid conflicts.
- The DTMAPI config panel lists extra-slot state, lets the player enter a passive item id, equips the first empty DTMAPI extra slot, and recovers all stored extra-slot items.
- DTMAPI owns extra-slot storage, read-only player equipment strip rendering, and recovery; `GAME-SMOKE/20260603-210216` verifies `grandmas_button` equip `1->0`, native attribute application, AccessoriesBar-rendered extra slot state (`uiRendered=True`, `occupied=1`, `readOnly=true`), and recovery `0->1`.
- There is no official Doloc Town JSON table that expands player equipment UI slots, so this package is runtime API only.
- The in-save screenshot fallback for the DTMAPI strip returned unavailable, so the retained proof is log/summary evidence plus optional manual screenshot polish.
