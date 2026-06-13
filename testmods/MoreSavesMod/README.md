# DTMAPI More Saves

Experimental official-local mod that fixes Doloc Town's expanded official save slot count through `ISaveSlotsApi`.

- Uses `DolocAPI.gameManager.archiveFileCount`, so official `LocalSave`, `GameDataUiState`, and `GameDataPanel` keep owning archive file discovery, rendering, load, duplicate, and delete behavior.
- Enabled state is fixed to 12 total official slots; the first six remain the vanilla `doloc-archive-{0..5}.data` slots.
- Disabling the mod restores the visible official slot count to 6 but does not delete extra `doloc-archive-{n}.data` files.
