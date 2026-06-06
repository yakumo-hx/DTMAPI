# DTMAPI More Saves

Experimental 0.2.9 official-local mod that expands Doloc Town's save slot count through `ISaveSlotsApi`.

- Uses `DolocAPI.gameManager.archiveFileCount`, so official `LocalSave`, `GameDataUiState`, and `GameDataPanel` keep owning archive file discovery, rendering, load, duplicate, and delete behavior.
- Defaults to 12 total slots; the first six remain the vanilla `doloc-archive-{0..5}.data` slots.
- Disabling the mod restores the visible official slot count to 6 but does not delete extra `doloc-archive-{n}.data` files.
