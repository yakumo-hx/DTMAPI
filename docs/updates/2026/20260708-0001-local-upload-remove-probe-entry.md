# 20260708-0001 Local Upload Remove Probe Entry

## Summary

Removed the optional `0_probe_dtmapi_install.bat` support entry point from the local official upload folder while keeping it in the standalone root-file-probe zip package.

## Source Request

The user clarified that standard player diagnosis should remain `3_check_dtmapi_status.bat`, and asked to remove `0_probe_dtmapi_install.bat` from the upload directory while keeping the standalone zip as the probe-capable package.

## Changed Files And Artifacts

- Removed from local upload folder:
  - `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI\0_probe_dtmapi_install.bat`
- Kept unchanged:
  - `dist/DTMAPI-player-hotfix-20260703-root-file-probe.zip`
  - `dist/player-hotfix-20260703-root-file-probe/DTMAPI/0_probe_dtmapi_install.bat`

## Validation

- Acquired the shared runtime lock before editing the local official upload folder, then released it after the deletion.
- Verified the upload directory no longer contains `0_probe_dtmapi_install.bat`.
- Verified the standalone zip still exists.
- Verified the repo-local unpacked standalone package still contains `0_probe_dtmapi_install.bat`.
- Listed the local upload folder root and confirmed it now contains only `1_install_dtmapi.bat`, `2_uninstall_dtmapi.bat`, `3_check_dtmapi_status.bat`, `4_collect_dtmapi_logs.bat`, `Content`, `icon.png`, `info.json`, `preview.png`, and `workshop.json`.

## Evidence

- Runtime lock acquired and released from `E:\Python_project\DTMAPI`.
- Post-change checks:
  - Upload `0_probe_dtmapi_install.bat`: absent.
  - Standalone zip: present.
  - Repo-local standalone unpacked `0_probe_dtmapi_install.bat`: present.

## Not Run

- No Steam Workshop upload/resubscribe was run.
- No game smoke was run because this only removes an optional support entry point from the local upload package root.

## Rollback

Copy `dist/player-hotfix-20260703-root-file-probe/DTMAPI/0_probe_dtmapi_install.bat` back into `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI` if the upload package should expose the preflight probe entry point again.

## Follow-Up

- After the next Steam upload and redownload, compare the Steam subscription folder against the local upload folder. Missing `0_probe_dtmapi_install.bat` should be expected for the upload package.
