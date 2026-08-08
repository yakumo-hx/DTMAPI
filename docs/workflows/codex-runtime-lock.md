# Codex Runtime Lock Workflow

DTMAPI supports multiple Codex worktrees for parallel code work, but the local Doloc Town runtime is a shared resource. The game install, BepInEx files, local official `MODS`, Steam subscription cache, Player log, and DTMAPI runtime logs can be overwritten by whichever worktree installs or launches last.

Use the shared runtime lock before any operation that changes or consumes the live game runtime.

## Must Lock

- Installing DTMAPI into the game directory.
- Uninstalling DTMAPI or BepInEx-owned files.
- Launching Doloc Town manually or through smoke scripts.
- Running `run-game-smoke.ps1`, startup sampling, hook probes that launch the game, or any script that sends input to the game window.
- Writing local official `MODS` packages or Workshop upload folders that are intended for the live game/user runtime.
- Collecting logs as proof immediately after a game run, unless the collection is already inside a locked smoke script.

## Does Not Need Lock

- Editing code in a worktree.
- Reading source, docs, debug records, or reverse-reference notes.
- Running unit tests that do not install or launch the game.
- Building packages into repo-local `dist` paths without copying them to the live game/user runtime.

## Commands

```powershell
tools/scripts/runtime-lock-status.ps1
tools/scripts/wait-runtime-lock.ps1 -Reason "run ActionSpeed smoke"
tools/scripts/release-runtime-lock.ps1
```

For a one-command critical section, use:

```powershell
tools/scripts/invoke-runtime-locked.ps1 `
  -Reason "run ActionSpeed smoke" `
  -FilePath powershell `
  -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File','tools/scripts/run-game-smoke.ps1','-UseSteam')
```

`wait-runtime-lock.ps1` creates one lock file in the Git common directory, so all DTMAPI worktrees see the same owner. If the current worktree already owns the lock, pass `-AllowCurrentWorktreeReuse` when nesting commands.

Only use:

```powershell
tools/scripts/release-runtime-lock.ps1 -Force
```

after verifying the lock owner is stale or blocked, no `DolocTown.exe` process is running, and no other Codex is actively installing or launching the game.

## Recommended Parallel Flow

1. Create one worktree per feature branch.
2. Let each Codex do code edits, builds, unit tests, docs, and reviews in its own worktree.
3. Before any game install/smoke/manual launch, the Codex waits for the runtime lock.
4. The lock owner installs its candidate runtime, runs smoke/manual QA, collects evidence, records docs, then releases the lock.
5. Other Codex threads continue code work while waiting, then take their turn for game validation.

This does not make Doloc Town multi-instance safe. It makes the single shared runtime explicit, queued, and auditable.
