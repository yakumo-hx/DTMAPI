# SecondMotor Retirement Retrospective - 2026-07-06

Status: docs-only synthesis

Sources:

- `archive/second-motor-20260615/README.md`
- `archive/second-motor-20260615/testmods/SecondMotorMod/README.md`
- `docs/api/public-api-matrix.md`
- `docs/reviews/api/native-owner-domains/06-flying-motor-vehicle-types.md`
- relevant 2026-06-14 and 2026-06-15 debug/update records

## What The Bug Was

The experimental SecondMotor route tried to provide a custom motor/vehicle path through `IMotorVehicleApi` and a DTMAPI-authored `SecondMotorMod`. User manual QA on 2026-06-15 reported the rebuilt second motor as effectively unusable:

- abnormal light texture behavior;
- abnormal farm-room textures;
- severe cross-map texture pollution.

The failure was not just a missing asset or a packaging mistake. It exposed that Doloc Town's native motor path is singleton-oriented and that the DTMAPI route was forcing a broader vehicle abstraction before the native owner boundary was proven.

## How It Was Resolved

- The active `IMotorVehicleApi` and MotorVehicle GameBridge path were retired/removed.
- The SecondMotor sample was archived under `archive/second-motor-20260615/`.
- Local installed packages were moved outside the repo to `E:\Python_project\DTMAPI-local-archives\second-motor-20260615\MODS`.
- The public API matrix marks motor vehicle API work as Retired/removed.
- Old mods that reference retired motor vehicle types are expected to fail rather than silently load against a broken API.

## New Or Old

This was a new experimental feature failure, not a regression in a stable API. Earlier smoke and implementation evidence became historical only after manual QA showed the feature boundary was wrong.

## Technical Debt Left Behind

- Future vehicle work must restart from `docs/reviews/api/native-owner-domains/06-flying-motor-vehicle-types.md`.
- The next vehicle goal should be smaller than "custom vehicle API": first identify the native owner slice, then prove one behavior through GameBridge, then decide whether a public abstraction exists.
- Asset scope remains risky. Do not install official example vehicle textures globally or revive local package assets as active content.
- Riding, collider, harvest, attack, key-item routing, store integration, and cleanup policies need separate proof if vehicle work resumes.
- Manual QA is mandatory before any future vehicle work is considered complete.

## Handoff Rule

Do not restore SecondMotor as an active package, smoke fixture, or public API sample. If vehicle work resumes, create a fresh API/native-owner review goal and treat this archive as a negative case study.
