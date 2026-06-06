# DTMAPI More Equipment Slots

Experimental 0.2.9 runtime/UI mod for extra player equipment slots.

- The mod registers `IEquipmentSlotsApi` with three default extra attribute-only slots.
- Vanilla/default equipment slots remain responsible for visible decoration effects.
- Extra slots explicitly request no appearance effects to avoid conflicts; hats equipped in extra slots are attribute-only and preserve the native hat visual slot.
- The DTMAPI config panel lists extra-slot state, lets the player enter an item id for smoke/manual diagnostics, equips the first empty DTMAPI extra slot, and recovers all stored extra-slot items.
- DTMAPI owns extra-slot storage, native-like player equipment strip rendering, click/hover binding, and recovery; `GAME-SMOKE/20260606-031316` verifies native `AccessorySlot` clones with `interactive=3`, `hoverable=3`, `readOnly=false`, passive `grandmas_button` equip/recover, hat `straw_hat` equip/recover, native hat preservation `nativeHat=miner_helmet->miner_helmet->miner_helmet`, and recovery `0->1`.
- There is no official Doloc Town JSON table that expands player equipment UI slots, so this package is runtime API only.
- The in-save screenshot fallback for the DTMAPI strip returned unavailable, so the retained proof is log/summary evidence plus optional manual screenshot polish.
