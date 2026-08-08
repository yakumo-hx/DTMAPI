# Runtime Workshop Installer V2 Candidate

This is an isolated, tracked overlay for a disposable player-package candidate.
It deliberately does not replace `tools/release/runtime-workshop` or the current
installer scripts under `tools/scripts`.

Build:

```powershell
tools/release/runtime-workshop-v2-candidate/build-candidate.ps1
```

Default input and output:

```text
base:   dist/workshop-packages-0.6.1/DTMAPI
output: dist/runtime-installer-v2-candidate/DTMAPI
```

The builder copies the base package, replaces only installer entry/tools in the
copy, and leaves payload binaries, Workshop metadata and the base tree intact.

Run the isolated regression/stress matrix only against a generated candidate:

```powershell
tools/release/runtime-workshop-v2-candidate/test-candidate.ps1
```

The candidate is not an upload authorization.
