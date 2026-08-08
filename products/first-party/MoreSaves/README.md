# DTMAPI More Saves

Managed Advanced ProductNative implementation of the fixed MoreSaves behavior.

- Enabled state writes `GameManager.archiveFileCount = 12`; disabled or final owner deactivation restores native `6`.
- Before publishing twelve slots, one startup pass independently moves legacy indices 6–11 from the pre-1.00 current/prev/bak role names to `doloc-save-{n}.data`, `.prev0`, and `.bak`. A missing source is skipped; an existing destination is never overwritten and keeps its old source; no legacy files is a successful no-op.
- The migration obtains the exact SAVE root and official current names through native `LocalSave.GetDataFullPath`, but never reads, decrypts, hashes, validates, repairs, or rewrites archive contents. A move must leave its source absent and destination present; any failure keeps six slots for that activation, while later startup naturally resumes from the old names that remain. Doloc Town remains the sole owner of discovery, rendering, load, save, copy, delete, and selection afterward.
- The product installs zero Harmony patches and does not consume the frozen `ISaveSlotsApi`.
- Missing native-manager state is retried at a bounded cadence. The exact product lease is retained until native six-slot restoration succeeds.
- Disabling the product never deletes additional `doloc-save-{n}.data` files.
