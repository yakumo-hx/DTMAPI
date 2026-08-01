# DTMAPI More Saves

Managed Advanced ProductNative implementation of the fixed MoreSaves behavior.

- Enabled state writes `GameManager.archiveFileCount = 12`; disabled or final owner deactivation restores native `6`.
- Doloc Town remains the sole owner of archive files, discovery, rendering, load, save, copy, delete, and selection.
- The product installs zero Harmony patches and does not consume the frozen `ISaveSlotsApi`.
- Missing native-manager state is retried at a bounded cadence. The exact product lease is retained until native six-slot restoration succeeds.
- Disabling the product never deletes additional `doloc-archive-{n}.data` files.
