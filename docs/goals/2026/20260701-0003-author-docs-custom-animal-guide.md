# 2026-07-01 Author Docs Custom Animal Guide Goal

## Objective

Create a standalone author documentation area for DTMAPI and write a complete, copyable guide for making a JSON + PNG + WAV custom animal content pack.

## Source Request

The user asked whether a complete, detailed explanation document could be written so they can follow it to make an independent new creature. The documentation must live in a standalone folder for future DTMAPI author docs, must not be loaded by the game or official upload packages, and should be easier to follow than the official docs where possible.

## Required Reading

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/goals/README.md`
- `docs/updates/INDEX.md`
- Official small-animal docs under `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md`
- Current Hatch/Shell Crab update and goal records from 2026-06-28 through 2026-07-01

## Tasks

1. Add a root-level standalone `author-docs/` directory for future DTMAPI author-facing documentation.
2. Add an index explaining that `author-docs/` is documentation-only, not a runtime/game/upload package source.
3. Add content-pack documentation index files.
4. Write a detailed `JSON + PNG + WAV` custom animal guide based on the verified Hatch route and Shell Crab audio follow-up.
5. Include copyable package tree, naming rules, template selection table, frame-count table, complete JSON skeletons, audio replacement JSON, hand-test checklist, troubleshooting, and publishing boundaries.
6. Add this update record and index link.

## Constraints

- Do not modify DTMAPI runtime code.
- Do not touch local game runtime, official local `MODS`, or Workshop upload staging.
- Do not copy official binaries, decompiled method bodies, or third-party mod code.
- Keep the guide focused on the currently verified content-pack route: JSON + loose PNG + WAV.
- Keep BGM/music replacement out of scope.

## Validation

- Static docs validation only.
- Confirm the release packaging script does not copy arbitrary root-level docs.
- Run `git diff --check`.
- No runtime lock, build, or game smoke is required for this docs-only change.

## Rollback

Remove `author-docs/`, this goal file, its `.goal.txt`, the update record, and the `docs/updates/INDEX.md` row.

